using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace alejandromd
{
    public static class Extensions
    {
        public static async Task<T> GetOrCacheAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> service, TimeSpan? expiration = null)
        {
            string? item = await cache.GetStringAsync(key);
            if(!string.IsNullOrEmpty(item))
            {
                return JsonSerializer.Deserialize<T>(item);
            }

            var result = await service();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
            };
            await cache.SetStringAsync(key, JsonSerializer.Serialize(result), options);
            return result;
        }

        public static string CssSanitize(this string language)
        {
            return language.ToLower().Replace(" ", "-").Replace("+", "plus").Replace("#", "sharp");
        }
    }
}
