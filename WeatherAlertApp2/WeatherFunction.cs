using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.EventHubs;

namespace WeatherAlertApp2
{
    public class WeatherFunction
    {
        private readonly ILogger<WeatherFunction> _logger;
        public WeatherFunction(ILogger<WeatherFunction> logger)
        {
            _logger = logger;
        }

        [FunctionName("WeatherFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [EventHub("weathereventhub01001", Connection = "wilfLearnEventHubNamespace_RootManageSharedAccessKey_EVENTHUB")] IAsyncCollector<string> outputEvents)
        {
            _logger.LogInformation("Processing weather data...");
            // Force assembly reference
            var _ = typeof(Azure.Core.Serialization.JsonObjectSerializer);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var weatherData = JsonConvert.DeserializeObject<WeatherPayload>(requestBody);

            if (weatherData?.Weather == "Sunny" || weatherData?.Weather == "Partly cloudy")
            {
               await outputEvents.AddAsync(requestBody); // Sending to Event Hub
            }

            return weatherData != null
            ? (ActionResult)new OkObjectResult($"Weather data processed success for , {weatherData.City}!")
            : new BadRequestObjectResult("Please pass correct payload in the request body.");
        }

        
    }

    public class WeatherPayload
    {
        public string City { get; set; }
        public string Weather { get; set; }
    }
}
