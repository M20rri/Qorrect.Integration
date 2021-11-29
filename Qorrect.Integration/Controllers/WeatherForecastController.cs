using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Qorrect.Integration.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qorrect.Integration.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ICacheService _iCahe;

        public WeatherForecastController(ICacheService iCahe)
        {
            _iCahe = iCahe;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {

            var cacheKey = "weatherList";
            string serializedCustomerList;
            List<string> weatherList = new List<string>();
            var redisCustomerList = await _iCahe.GetCachevalueAsync(cacheKey);
            if (redisCustomerList != null)
            {
                weatherList = JsonConvert.DeserializeObject<List<string>>(redisCustomerList);
            }
            else
            {
                weatherList = GetFromDb();
                serializedCustomerList = JsonConvert.SerializeObject(weatherList);
                await _iCahe.SetCachevalueAsync(cacheKey, serializedCustomerList);
            }
            return Ok(weatherList);

        }

        private List<string> GetFromDb()
        {
            // Sample code getting from db
            return new List<string> {
                   "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            }.ToList();
        }
    }
}

