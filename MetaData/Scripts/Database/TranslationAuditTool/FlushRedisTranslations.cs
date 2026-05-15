using StackExchange.Redis;

public static class FlushRedisTranslations
{
    private const string ContentInstance = "amesa-content";

    public static async Task FlushAsync(string? redisConnectionString, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(redisConnectionString))
            throw new InvalidOperationException("Set AMESA_REDIS_CONNECTION_STRING or pass Redis host via SSM.");

        var options = ConfigurationOptions.Parse(redisConnectionString);
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 15000;
        options.AsyncTimeout = 15000;

        await using var mux = await ConnectionMultiplexer.ConnectAsync(options);
        var db = mux.GetDatabase();
        var endpoint = mux.GetEndPoints().First();
        var server = mux.GetServer(endpoint);

        var patterns = new[]
        {
            $"{ContentInstance}:translations_*",
            $"{ContentInstance}:languages_list"
        };

        var deleted = 0L;
        foreach (var pattern in patterns)
        {
            await foreach (var key in server.KeysAsync(pattern: pattern).WithCancellation(ct))
            {
                if (await db.KeyDeleteAsync(key))
                    deleted++;
                Console.WriteLine($"Deleted {key}");
            }
        }

        Console.WriteLine($"Flush complete. Deleted {deleted} key(s) matching translations_* and languages_list.");
    }
}
