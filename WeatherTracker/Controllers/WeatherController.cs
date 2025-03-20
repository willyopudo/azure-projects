using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WeatherTracker.Services;
using System.Text;
using Newtonsoft.Json;

namespace WeatherTracker.Controllers
{
    public class WeatherController : Controller
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetWeather(string city)
        {
            if (string.IsNullOrEmpty(city))
            {
                ViewBag.Error = "Please enter a city name.";
                return View("Index");
            }

            try
            {
                var weather = await _weatherService.GetWeatherAsync(city);
        
                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(new { weather = weather.Current.Condition.Text, city = city }), Encoding.UTF8, "application/json");
                    await client.PostAsync("func conn", content);
                }
                
                Console.WriteLine(weather.Current.Condition.Text);
                return View("Index", weather);
            }
            catch
            {
                ViewBag.Error = "Could not fetch weather data.";
                return View("Index");
            }
        }
    }
}
