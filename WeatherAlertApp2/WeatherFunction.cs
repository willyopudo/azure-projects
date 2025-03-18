using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace WeatherAlertApp2
{
    public  class WeatherFunction
    {
        private readonly ILogger<WeatherFunction> _logger;
        private const string eventHubConnectionString = "conn";
        private const string eventHubName = "weatherEventHub01001";

        public WeatherFunction(ILogger<WeatherFunction> logger)
        {
            _logger = logger;
        }

        [FunctionName("WeatherFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("Processing weather data...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var weatherData = JsonSerializer.Deserialize<WeatherPayload>(requestBody);

            if (weatherData?.Weather == "Sunny" || weatherData?.Weather == "Partly cloudy")
            {
                await SendToEventHub(requestBody);
            }

            return weatherData != null
            ? (ActionResult)new OkObjectResult($"Weather data processed success for , {weatherData.City}!")
            : new BadRequestObjectResult("Please pass correct payload in the request body.");
        }

        private async Task SendToEventHub(string payload)
        {
            await using var producerClient = new EventHubProducerClient(eventHubConnectionString, eventHubName);
            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
            eventBatch.TryAdd(new EventData(payload));
            await producerClient.SendAsync(eventBatch);
            _logger.LogInformation("Sent to Event Hub: " + payload);
        }
    }

    public class WeatherPayload
    {
        public string City { get; set; }
        public string Weather { get; set; }
    }
}
