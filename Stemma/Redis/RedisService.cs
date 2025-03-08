using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Stemma.Redis
{
    public class RedisService : IRedisService
    {
        private readonly IDistributedCache _cache;

        public RedisService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<int> SetLangForUserAsync(string UserName, Dictionary<string, int> langDic)
        {
            try
            {
                string key = $"{UserName}-Languages";
                var sortTotalLanguage = langDic
                    .OrderByDescending(kvp => kvp.Value)
                    .ToList();
                string serializedLanguages = JsonConvert.SerializeObject(sortTotalLanguage);
                Console.WriteLine(serializedLanguages);
                await _cache.SetStringAsync(key, serializedLanguages, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }
        }

        public async Task<List<KeyValuePair<string, int>>> GetLangFromUserAsync(string UserName)
        {
            try
            {
                string key = $"{UserName}-Languages";
                var serializedLanguages = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(serializedLanguages))
                {
                    return new List<KeyValuePair<string, int>>();
                }
                var result = JsonConvert.DeserializeObject<List<KeyValuePair<string, int>>>(serializedLanguages);
                return result ?? new List<KeyValuePair<string, int>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<KeyValuePair<string, int>>();
            }
        }

    }
}
