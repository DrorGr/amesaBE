namespace AmesaBackend.Shared.Caching
{
    public interface ICache : IDisposable
    {
        /// <summary>
        /// Gets a raw record as string without deserialization.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="isGlobal"></param>
        /// <returns>Returns raw record as string without deserialization.</returns>
        public Task<string?> GetRecordAsync(string cacheKey, bool isGlobal = false);

        /// <summary>
        /// Gets a deserialized record.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="isGlobal"></param>
        /// <returns>Returns deserialized record.</returns>
        public Task<T?> GetRecordAsync<T>(string cacheKey, bool isGlobal = false);

        public Task<T?> GetValueTypeRecordAsync<T>(string cacheKey, bool isGlobal = false) where T : struct;

        public Task SetRecordAsync<T>(string cacheKey, T data,
            TimeSpan? absoluteExpiteTime = null, TimeSpan? unusedExpiteTime = null, bool isGlobal = false);

        public Task RemoveRecordAsync(string cacheKey, bool isGlobal = false);
        public Task<bool> ClearAllCache();
        T? GetRecord<T>(string cacheKey, bool isGlobal = false);
        Task BatchSet<T>(Dictionary<string, T> data, bool isGlobal = false);

        Task<long> RemoveByControllerName(string controllerName);

        public Task<bool> DeleteByRegex(string regex);
    }
}

