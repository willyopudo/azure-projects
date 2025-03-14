using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WeatherTracker.Models;

namespace WeatherTracker.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "0a0de3c2af65410092a80751251103"; // Replace with your key
        private const string BaseUrl = "http://api.weatherapi.com/v1/current.json";

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherResponse> GetWeatherAsync(string city)
        {
            var url = $"{BaseUrl}?key={ApiKey}&q={city}&aqi=no";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch weather data.");
            }

            var json = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<WeatherResponse>(json);
        }
    }
}
