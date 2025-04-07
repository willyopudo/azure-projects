using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.SignalR;
using ServiceBusWebApp.Hubs;

public class ServiceBusService
{
    private readonly string _connectionString;
    private readonly string _topicName;
    private readonly string _subscriptionName;
    private readonly IHubContext<MessageHub> _hubContext;

    public ServiceBusService(IConfiguration configuration, IHubContext<MessageHub> hubContext)
    {
        var serviceBusConfig = configuration.GetSection("AzureServiceBus");
        _connectionString = serviceBusConfig["ConnectionString"];
        _topicName = serviceBusConfig["TopicName"];
        _subscriptionName = serviceBusConfig["SubscriptionName"];
        _hubContext = hubContext;
    }

    public async Task StartListeningAsync()
    {
        await using var client = new ServiceBusClient(_connectionString);
        var receiver = client.CreateReceiver(_topicName, _subscriptionName);

        while (true)
        {
            var receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: 10, TimeSpan.FromSeconds(5));

            foreach (var message in receivedMessages)
            {
                // Broadcast the message to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message.Body.ToString());

                // Complete the message
                await receiver.CompleteMessageAsync(message);
            }
        }
    }
}