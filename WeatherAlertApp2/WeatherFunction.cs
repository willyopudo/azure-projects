using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

public class WeatherFunction
{
    private readonly ILogger<WeatherFunction> _logger;
    private const string eventHubConnectionString = "conn";
    private const string eventHubName = "weatherEventHub01001";

    public WeatherFunction(ILogger<WeatherFunction> logger)
    {
        _logger = logger;
    }

    [Function("WeatherFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Processing weather data...");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var weatherData = JsonSerializer.Deserialize<WeatherPayload>(requestBody);

        if (weatherData?.Weather == "Sunny" || weatherData?.Weather == "Partly cloudy")
        {
            await SendToEventHub(requestBody);
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Weather data processed.");
        return response;
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