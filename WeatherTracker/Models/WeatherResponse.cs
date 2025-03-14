using Newtonsoft.Json;

namespace WeatherTracker.Models
{
    public class WeatherResponse
    {
        [JsonProperty("location")]
        public Location Location { get; set; }

        [JsonProperty("current")]
        public CurrentWeather Current { get; set; }
    }

    public class Location
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }

    public class CurrentWeather
    {
        [JsonProperty("temp_c")]
        public double TemperatureC { get; set; }

        [JsonProperty("condition")]
        public WeatherCondition Condition { get; set; }
    }

    public class WeatherCondition
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
