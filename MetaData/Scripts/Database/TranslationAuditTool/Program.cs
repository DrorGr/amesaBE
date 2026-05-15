// Translation audit tool — policy:
// - Default: scrape FE only; compare to local audit files under translation-audit/ (no DB).
// - --use-api: compare to production Content API (no DB).
// - --use-db: query DB (requires AMESA_DB_CONNECTION_STRING). Never implied by default.
// - --seed-only: generate seed SQL (requires --use-db). --apply-seed: execute it (explicit only).
// - --exec-sql <path>, --flush-redis: explicit side effects only.

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

var applySeed = args.Contains("--apply-seed", StringComparer.OrdinalIgnoreCase);
var seedOnly = args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase);
var useDb = args.Contains("--use-db", StringComparer.OrdinalIgnoreCase);
var useApi = args.Contains("--use-api", StringComparer.OrdinalIgnoreCase);
var repoArg = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));

var connectionString = Environment.GetEnvironmentVariable("AMESA_DB_CONNECTION_STRING");
var redisConnectionString = Environment.GetEnvironmentVariable("AMESA_REDIS_CONNECTION_STRING");

if (args.Contains("--flush-redis", StringComparer.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(redisConnectionString))
        throw new InvalidOperationException("Set AMESA_REDIS_CONNECTION_STRING (e.g. from SSM /amesa/prod/ConnectionStrings/Redis).");
    await FlushRedisTranslations.FlushAsync(redisConnectionString.Trim());
    return;
}

var execSqlIndex = Array.FindIndex(args, a => a.Equals("--exec-sql", StringComparison.OrdinalIgnoreCase));
if (execSqlIndex >= 0)
{
    if (!useDb)
        throw new InvalidOperationException("--exec-sql requires --use-db and AMESA_DB_CONNECTION_STRING.");
    if (execSqlIndex + 1 >= args.Length)
        throw new InvalidOperationException("--exec-sql requires a path to a .sql file.");
    RequireDbConnectionString(connectionString);
    var sqlPath = Path.GetFullPath(args[execSqlIndex + 1]);
    var sql = await File.ReadAllTextAsync(sqlPath);
    await using var sqlConn = new NpgsqlConnection(connectionString!);
    await sqlConn.OpenAsync();
    await using var cmd = new NpgsqlCommand(sql, sqlConn);
    var n = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Executed {sqlPath} ({n} rows affected).");
    return;
}

if (seedOnly || applySeed)
{
    if (!useDb)
        throw new InvalidOperationException("--seed-only / --apply-seed require --use-db (and AMESA_DB_CONNECTION_STRING). Seeding is never automatic.");
    RequireDbConnectionString(connectionString);
    var repoForSeed = repoArg is not null
        ? Path.GetFullPath(repoArg)
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    await SeedGenerator.GenerateAndApplyAsync(connectionString!, repoForSeed, applySeed);
    return;
}

var repoRoot = repoArg is not null
    ? Path.GetFullPath(repoArg)
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

var outputDir = Path.Combine(repoRoot, "MetaData", "Scripts", "Database", "translation-audit");
Directory.CreateDirectory(outputDir);

var languages = new[] { "en", "es", "fr", "pl", "de", "ru" };
var codeKeys = ScrapeFrontendKeys(Path.Combine(repoRoot, "FE", "src"));
await File.WriteAllLinesAsync(Path.Combine(outputDir, "codebase-keys-fe.txt"), codeKeys.OrderBy(k => k));
Console.WriteLine($"FE keys scraped: {codeKeys.Count}");

if (useApi)
{
    var apiBase = Environment.GetEnvironmentVariable("AMESA_TRANSLATIONS_API_BASE")
        ?? "https://amesa-group.net/api/v1/translations";
    await RunApiAuditAsync(codeKeys, languages, apiBase, outputDir);
    return;
}

if (useDb)
{
    RequireDbConnectionString(connectionString);
    await RunDbAuditAsync(codeKeys, languages, connectionString!, outputDir);
    return;
}

await RunLocalFileAuditAsync(codeKeys, languages, repoRoot, outputDir);

static void RequireDbConnectionString(string? cs)
{
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Set AMESA_DB_CONNECTION_STRING when using --use-db, --apply-seed, or --exec-sql.");
}

static async Task RunLocalFileAuditAsync(
    HashSet<string> codeKeys,
    string[] languages,
    string repoRoot,
    string outputDir)
{
    var dbKeysByLang = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    var loadedFrom = new List<string>();

    foreach (var lang in languages)
    {
        var path = Path.Combine(outputDir, $"db-keys-{lang}.txt");
        if (!File.Exists(path))
            continue;
        var keys = (await File.ReadAllLinesAsync(path))
            .Select(l => l.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.Ordinal);
        dbKeysByLang[lang] = keys;
        loadedFrom.Add($"db-keys-{lang}.txt");
    }

    if (dbKeysByLang.Count == 0)
    {
        var unionPath = Path.Combine(outputDir, "db-keys-union.txt");
        if (File.Exists(unionPath))
        {
            var union = (await File.ReadAllLinesAsync(unionPath))
                .Select(l => l.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToHashSet(StringComparer.Ordinal);
            dbKeysByLang["en"] = union;
            loadedFrom.Add("db-keys-union.txt");
        }
    }

    if (dbKeysByLang.Count == 0)
    {
        var allFromSql = LoadKeysFromSeedSqlFiles(repoRoot);
        dbKeysByLang["en"] = allFromSql;
        loadedFrom.Add("seed SQL files in MetaData/Scripts/Database");
        Console.WriteLine($"No translation-audit/*.txt found; using {allFromSql.Count} keys from local seed SQL files.");
    }
    else
    {
        Console.WriteLine($"Compared against local files: {string.Join(", ", loadedFrom)}");
    }

    if (!dbKeysByLang.TryGetValue("en", out var enKeys))
        enKeys = dbKeysByLang.Values.FirstOrDefault() ?? new HashSet<string>(StringComparer.Ordinal);

    await WriteAuditOutputsAsync(codeKeys, enKeys, dbKeysByLang, languages, outputDir,
        "# Translation audit (local files / SQL reference)",
        "No database connection. Re-run with --use-api or --use-db for live data.");
}

static async Task RunApiAuditAsync(
    HashSet<string> codeKeys,
    string[] languages,
    string apiBase,
    string outputDir)
{
    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    var dbKeysByLang = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

    foreach (var lang in languages)
    {
        var url = $"{apiBase.TrimEnd('/')}/{lang}";
        Console.WriteLine($"API GET {url}");
        var json = await http.GetFromJsonAsync<Dictionary<string, string>>(url);
        var keys = json?.Keys.ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
        dbKeysByLang[lang] = keys;
        await File.WriteAllLinesAsync(Path.Combine(outputDir, $"api-keys-{lang}.txt"), keys.OrderBy(k => k));
        Console.WriteLine($"API {lang}: {keys.Count} keys");
    }

    if (!dbKeysByLang.TryGetValue("en", out var enKeys))
        enKeys = new HashSet<string>(StringComparer.Ordinal);

    await WriteAuditOutputsAsync(codeKeys, enKeys, dbKeysByLang, languages, outputDir,
        "# Translation audit (production API)",
        $"Source: {apiBase}/{{lang}}");
}

static async Task RunDbAuditAsync(
    HashSet<string> codeKeys,
    string[] languages,
    string connectionString,
    string outputDir)
{
    var dbKeysByLang = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    await using (var langCmd = new NpgsqlCommand(
        "SELECT \"Code\" FROM amesa_content.languages WHERE \"IsActive\" = true ORDER BY \"Code\"", conn))
    await using (var langReader = await langCmd.ExecuteReaderAsync())
    {
        var active = new List<string>();
        while (await langReader.ReadAsync())
            active.Add(langReader.GetString(0));
        await File.WriteAllLinesAsync(Path.Combine(outputDir, "db-active-languages.txt"), active);
    }

    foreach (var lang in languages)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT DISTINCT "Key"
            FROM amesa_content.translations
            WHERE "LanguageCode" = @lang AND "IsActive" = true
            ORDER BY "Key"
            """, conn);
        cmd.Parameters.AddWithValue("lang", lang);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            keys.Add(reader.GetString(0));
        dbKeysByLang[lang] = keys;
        await File.WriteAllLinesAsync(Path.Combine(outputDir, $"db-keys-{lang}.txt"), keys.OrderBy(k => k));
        Console.WriteLine($"DB {lang}: {keys.Count} keys");
    }

    await using (var countCmd = new NpgsqlCommand(
        """
        SELECT "LanguageCode", COUNT(DISTINCT "Key") AS key_count
        FROM amesa_content.translations
        WHERE "IsActive" = true
        GROUP BY "LanguageCode"
        ORDER BY "LanguageCode"
        """, conn))
    await using (var countReader = await countCmd.ExecuteReaderAsync())
    {
        var lines = new List<string> { "language_code,key_count" };
        while (await countReader.ReadAsync())
            lines.Add($"{countReader.GetString(0)},{countReader.GetInt64(1)}");
        await File.WriteAllLinesAsync(Path.Combine(outputDir, "db-key-counts-by-language.csv"), lines);
    }

    if (!dbKeysByLang.TryGetValue("en", out var enKeys))
        enKeys = new HashSet<string>(StringComparer.Ordinal);

    await WriteAuditOutputsAsync(codeKeys, enKeys, dbKeysByLang, languages, outputDir,
        "# Translation audit (database)",
        "Host: amesa_content.translations (via AMESA_DB_CONNECTION_STRING)");
}

static async Task WriteAuditOutputsAsync(
    HashSet<string> codeKeys,
    HashSet<string> enKeys,
    Dictionary<string, HashSet<string>> dbKeysByLang,
    string[] languages,
    string outputDir,
    string reportTitle,
    string sourceNote)
{
    var missingInDb = codeKeys.Where(k => !enKeys.Contains(k)).OrderBy(k => k).ToList();
    await File.WriteAllLinesAsync(Path.Combine(outputDir, "missing-in-db.txt"), missingInDb);

    foreach (var lang in languages.Where(l => l != "en"))
    {
        if (!dbKeysByLang.TryGetValue(lang, out var langKeys))
            langKeys = new HashSet<string>(StringComparer.Ordinal);
        var missing = enKeys.Where(k => !langKeys.Contains(k)).OrderBy(k => k).ToList();
        await File.WriteAllLinesAsync(Path.Combine(outputDir, $"missing-{lang}-vs-en.txt"), missing);
        Console.WriteLine($"Missing {lang} vs en: {missing.Count}");
    }

    var report = $"""
        {reportTitle}

        Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
        {sourceNote}

        ## Summary
        | Metric | Count |
        |--------|------:|
        | Frontend keys scraped | {codeKeys.Count} |
        | English keys in reference | {enKeys.Count} |
        | **FE keys missing from EN reference** | {missingInDb.Count} |

        ## Keys per language
        {string.Join("\n", languages.Select(l => $"- **{l}**: {dbKeysByLang.GetValueOrDefault(l)?.Count ?? 0}"))}

        ## Cross-language gaps vs English
        {string.Join("\n", languages.Where(l => l != "en").Select(l =>
        {
            var langSet = dbKeysByLang.GetValueOrDefault(l) ?? new HashSet<string>(StringComparer.Ordinal);
            var gap = enKeys.Count(k => !langSet.Contains(k));
            return $"- **{l}**: {gap} keys missing vs en";
        }))}

        See missing-in-db.txt and missing-*-vs-en.txt for full lists.
        """;

    var reportName = reportTitle.Contains("database", StringComparison.OrdinalIgnoreCase)
        ? "REPORT-DB.md"
        : reportTitle.Contains("API", StringComparison.OrdinalIgnoreCase)
            ? "REPORT.md"
            : "REPORT-LOCAL.md";

    await File.WriteAllTextAsync(Path.Combine(outputDir, reportName), report);
    Console.WriteLine($"FE missing from EN: {missingInDb.Count}");
    Console.WriteLine($"Report: {Path.Combine(outputDir, reportName)}");
}

static HashSet<string> LoadKeysFromSeedSqlFiles(string repoRoot)
{
    var keys = new HashSet<string>(StringComparer.Ordinal);
    var seedFiles = new[]
    {
        "complete-507-translations.sql",
        "seed-payment-sandbox-translation-keys.sql",
        "seed-status-translation-keys.sql",
        "lottery-favorites-translations.sql",
        "add-missing-translation-keys.sql"
    };
    var dbDir = Path.Combine(repoRoot, "MetaData", "Scripts", "Database");
    foreach (var sf in seedFiles)
    {
        var path = Path.Combine(dbDir, sf);
        if (!File.Exists(path))
            continue;
        var text = File.ReadAllText(path);
        foreach (Match m in Regex.Matches(text, @"',\s*'([a-zA-Z][a-zA-Z0-9_.]+)',\s*'"))
            keys.Add(m.Groups[1].Value);
    }
    return keys;
}

static HashSet<string> ScrapeFrontendKeys(string feRoot)
{
    var keys = new HashSet<string>(StringComparer.Ordinal);
    var pattern = new Regex(
        @"(?:translate|translationService\.translate|this\.translate)\s*\(\s*['""]([a-zA-Z][a-zA-Z0-9_.]+)['""]",
        RegexOptions.Compiled);

    if (Directory.Exists(feRoot))
    {
        foreach (var file in Directory.EnumerateFiles(feRoot, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains("node_modules", StringComparison.OrdinalIgnoreCase)) continue;
            if (file.EndsWith(".spec.ts", StringComparison.OrdinalIgnoreCase)) continue;
            if (!file.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                continue;

            var text = File.ReadAllText(file);
            foreach (Match m in pattern.Matches(text))
            {
                var key = m.Groups[1].Value;
                if (key.Contains('.') && !key.EndsWith('.'))
                    keys.Add(key);
            }
        }
    }

    var lotteryFile = Path.Combine(feRoot, "shared", "constants", "lottery-translation-keys.ts");
    if (File.Exists(lotteryFile))
    {
        var text = File.ReadAllText(lotteryFile);
        foreach (Match m in Regex.Matches(text, @"['""]([a-zA-Z][a-zA-Z0-9_.]+)['""]"))
        {
            if (m.Groups[1].Value.Contains('.') && m.Groups[1].Value.Count(c => c == '.') >= 1)
                keys.Add(m.Groups[1].Value);
        }
    }

    return keys;
}
