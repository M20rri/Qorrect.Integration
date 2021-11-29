using StackExchange.Redis;
using System.Threading.Tasks;

namespace Qorrect.Integration.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public async Task<string> GetCachevalueAsync(string key)
        {
            var _db = _connectionMultiplexer.GetDatabase();
            return await _db.StringGetAsync(key);
        }

        public async Task SetCachevalueAsync(string key, string value)
        {
            var _db = _connectionMultiplexer.GetDatabase();
            await _db.StringSetAsync(key, value);
        }
    }
}
