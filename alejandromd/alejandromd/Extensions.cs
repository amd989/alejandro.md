using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace alejandromd
{
    public static class Extensions
    {
        public static async Task<T> GetOrCacheAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> service)
        {
            string? item = await cache.GetStringAsync(key);
            if(!string.IsNullOrEmpty(item))
            {
                return JsonSerializer.Deserialize<T>(item);
            }
           
            var result = await service();
            await cache.SetStringAsync(key, JsonSerializer.Serialize(result));
            return result;
        }
    }
}
