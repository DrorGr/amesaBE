// Seed SQL generation/apply — only invoked with --seed-only [--use-db] or --apply-seed (never from default audit).

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

public static class SeedGenerator
{
    private static readonly string[] Languages = ["en", "es", "fr", "pl", "de", "ru"];

    public static async Task<int> GenerateAndApplyAsync(
        string connectionString,
        string repoRoot,
        bool apply,
        CancellationToken ct = default)
    {
        var auditDir = Path.Combine(repoRoot, "MetaData", "Scripts", "Database", "translation-audit");
        var missingFePath = Path.Combine(auditDir, "missing-in-db.txt");
        if (!File.Exists(missingFePath))
            throw new FileNotFoundException("Run TranslationAuditTool first.", missingFePath);

        var feMissingKeys = (await File.ReadAllLinesAsync(missingFePath, ct))
            .Select(l => l.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Where(IsValidKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var enByKey = await LoadTranslationsAsync(conn, "en", ct);
        var sql = new StringBuilder();
        sql.AppendLine("-- Auto-generated: seed missing translation keys + backfill all languages from English");
        sql.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sql.AppendLine("SET search_path TO amesa_content;");
        sql.AppendLine();

        AppendActivateLanguages(sql);
        sql.AppendLine();

        var newEnRows = BuildNewEnglishRows(feMissingKeys, enByKey);
        AppendEnglishInserts(sql, newEnRows);
        sql.AppendLine();

        foreach (var lang in Languages.Where(l => l != "en"))
            AppendBackfillFromEnglish(sql, lang);

        var outPath = Path.Combine(repoRoot, "MetaData", "Scripts", "Database", "seed-missing-translation-keys.sql");
        await File.WriteAllTextAsync(outPath, sql.ToString(), ct);
        Console.WriteLine($"Wrote {outPath} ({newEnRows.Count} new EN keys, then backfill es/fr/pl/de/ru from en)");

        if (!apply)
            return 0;

        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await using var cmd = new NpgsqlCommand(sql.ToString(), conn, tx);
            var inserted = await cmd.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);
            Console.WriteLine($"Applied seed SQL (command reports {inserted} rows affected).");
            return inserted;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static bool IsValidKey(string key) =>
        key.Length >= 3 &&
        key.Contains('.') &&
        !key.EndsWith('.') &&
        !key.EndsWith("..", StringComparison.Ordinal) &&
        Regex.IsMatch(key, @"^[a-zA-Z][a-zA-Z0-9]*(?:\.[a-zA-Z0-9][a-zA-Z0-9_]*)+$");

    private static async Task<Dictionary<string, (string Value, string? Category)>> LoadTranslationsAsync(
        NpgsqlConnection conn,
        string lang,
        CancellationToken ct)
    {
        var dict = new Dictionary<string, (string, string?)>(StringComparer.Ordinal);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT "Key", "Value", "Category"
            FROM amesa_content.translations
            WHERE "LanguageCode" = @lang AND "IsActive" = true
            """, conn);
        cmd.Parameters.AddWithValue("lang", lang);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            dict[reader.GetString(0)] = (reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
        return dict;
    }

    private static List<(string Key, string Value, string Category, string Description)> BuildNewEnglishRows(
        IReadOnlyList<string> feKeys,
        IReadOnlyDictionary<string, (string Value, string? Category)> enByKey)
    {
        var rows = new List<(string, string, string, string)>();
        foreach (var key in feKeys)
        {
            if (enByKey.ContainsKey(key))
                continue;

            var value = ResolveEnglishValue(key, enByKey);
            var category = InferCategory(key);
            rows.Add((key, value, category, $"FE key sync: {key}"));
        }
        return rows;
    }

    private static string ResolveEnglishValue(
        string key,
        IReadOnlyDictionary<string, (string Value, string? Category)> enByKey)
    {
        foreach (var alt in GetAliasKeys(key))
        {
            if (enByKey.TryGetValue(alt, out var existing))
                return existing.Value;
        }
        return HumanizeKey(key);
    }

    private static IEnumerable<string> GetAliasKeys(string key)
    {
        yield return key;

        if (key.StartsWith("auth.2fa.", StringComparison.Ordinal))
            yield return "auth.twoFactor." + key["auth.2fa.".Length..];

        if (key == "auth.login")
        {
            yield return "auth.signIn";
            yield return "nav.login";
        }

        if (key.StartsWith("entries.", StringComparison.Ordinal))
        {
            yield return "lottery." + key;
            var tail = key["entries.".Length..];
            yield return "lottery.entries." + tail;
        }

        if (key.StartsWith("favorites.", StringComparison.Ordinal) && !key.StartsWith("lottery.", StringComparison.Ordinal))
            yield return "lottery." + key;

        if (key.StartsWith("filters.", StringComparison.Ordinal) && !key.StartsWith("lottery.", StringComparison.Ordinal))
            yield return "lottery." + key;

        if (key == "house.viewDetails")
            yield return "house.viewDetailsForTitle";
    }

    private static string HumanizeKey(string key)
    {
        var last = key.Split('.')[^1];
        var words = Regex.Split(last, "(?<!^)(?=[A-Z])|_|-")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(w.ToLowerInvariant()));
        var text = string.Join(' ', words);
        if (key.Contains("error", StringComparison.OrdinalIgnoreCase))
            return text.StartsWith("Failed", StringComparison.Ordinal) ? text : $"Failed: {text}";
        return text;
    }

    private static string InferCategory(string key)
    {
        var prefix = key.Split('.')[0];
        return prefix switch
        {
            "nav" => "Navigation",
            "auth" => "Auth",
            "payment" => "Payment",
            "lottery" => "Lottery",
            "content" => "Content",
            "notifications" => "Notifications",
            "preferences" => "Preferences",
            "house" => "House",
            "status" => "Status",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(prefix)
        };
    }

    private static void AppendActivateLanguages(StringBuilder sql)
    {
        sql.AppendLine("-- Activate German and Russian; deactivate Hebrew and Arabic");
        sql.AppendLine(
            """
            INSERT INTO amesa_content.languages ("Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
            VALUES
                ('de', 'German', 'Deutsch', '🇩🇪', true, false, 5, NOW(), NOW()),
                ('ru', 'Russian', 'Русский', '🇷🇺', true, false, 6, NOW(), NOW())
            ON CONFLICT ("Code") DO UPDATE SET
                "Name" = EXCLUDED."Name",
                "NativeName" = EXCLUDED."NativeName",
                "IsActive" = true,
                "UpdatedAt" = NOW();

            UPDATE amesa_content.languages
            SET "IsActive" = false, "UpdatedAt" = NOW()
            WHERE "Code" IN ('he', 'ar');
            """);
    }

    private static void AppendEnglishInserts(StringBuilder sql, List<(string Key, string Value, string Category, string Description)> rows)
    {
        if (rows.Count == 0)
        {
            sql.AppendLine("-- No new English keys to insert");
            return;
        }

        sql.AppendLine($"-- Insert {rows.Count} new English keys");
        sql.AppendLine(
            """
            INSERT INTO amesa_content.translations (
                "Id", "LanguageCode", "Key", "Value", "Description", "Category",
                "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
            )
            VALUES
            """);

        for (var i = 0; i < rows.Count; i++)
        {
            var (key, value, category, description) = rows[i];
            var comma = i < rows.Count - 1 ? "," : "";
            sql.AppendLine(
                $"    (gen_random_uuid(), 'en', {SqlLiteral(key)}, {SqlLiteral(value)}, {SqlLiteral(description)}, {SqlLiteral(category)}, true, NOW(), NOW(), 'seed-missing-keys', 'seed-missing-keys'){comma}");
        }

        sql.AppendLine("ON CONFLICT (\"LanguageCode\", \"Key\") DO NOTHING;");
    }

    private static void AppendBackfillFromEnglish(StringBuilder sql, string lang)
    {
        sql.AppendLine();
        sql.AppendLine($"-- Backfill {lang} from English");
        sql.AppendLine(
            $"""
            INSERT INTO amesa_content.translations (
                "Id", "LanguageCode", "Key", "Value", "Description", "Category",
                "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
            )
            SELECT
                gen_random_uuid(),
                '{lang}',
                e."Key",
                e."Value",
                e."Description",
                e."Category",
                true,
                NOW(),
                NOW(),
                'seed-missing-keys',
                'seed-missing-keys'
            FROM amesa_content.translations e
            WHERE e."LanguageCode" = 'en'
              AND e."IsActive" = true
              AND NOT EXISTS (
                  SELECT 1
                  FROM amesa_content.translations t
                  WHERE t."LanguageCode" = '{lang}'
                    AND t."Key" = e."Key"
              )
            ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
            """);
    }

    private static string SqlLiteral(string? s)
    {
        if (s is null)
            return "NULL";
        return "'" + s.Replace("'", "''") + "'";
    }
}
