using System.Threading.Tasks;

namespace Qorrect.Integration.Services
{
    public interface ICacheService
    {
        Task<string> GetCachevalueAsync(string key);
        Task SetCachevalueAsync(string key , string value);
    }
}
