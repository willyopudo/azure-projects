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

        // Step 2: Start continuous listening for new messages
        while (true)
        {
            var newMessages = await receiver.ReceiveMessagesAsync(maxMessages: 10, TimeSpan.FromSeconds(1));

            foreach (var message in newMessages)
            {
                // Broadcast the message to all connected SignalR clients
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message.Body.ToString());

                // Complete the message
                await receiver.CompleteMessageAsync(message);
            }
        }
    }

    // New method to fetch all unconsumed messages without broadcasting to SignalR
    public async Task<List<string>> FetchUnconsumedMessagesAsync()
    {
        var messages = new List<string>();

        await using var client = new ServiceBusClient(_connectionString);
        var receiver = client.CreateReceiver(_topicName, _subscriptionName);

        // Fetch messages without completing them
        var receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: 50, TimeSpan.FromSeconds(5));
        Console.WriteLine($"Received message: {receivedMessages.Count}");
        // Process each message
        foreach (var message in receivedMessages)
        {
            messages.Add(message.Body.ToString());

            Console.WriteLine($"Received message: {message.Body}");
            // Optionally defer the message instead of completing it
            // await receiver.DeferMessageAsync(message);
            //await receiver.CompleteMessageAsync(message);
        }

        return messages;
    }
}