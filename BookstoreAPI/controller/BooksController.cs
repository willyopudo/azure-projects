using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Azure.Messaging.ServiceBus;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly CosmosClient _cosmosClient;
    private readonly string     _databaseName = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE_NAME");
    private readonly string     _collectionName = Environment.GetEnvironmentVariable("COSMOS_DB_COLLECTION_NAME");
    private readonly string _topicName = Environment.GetEnvironmentVariable("SERVICE_BUS_TOPIC_NAME");

    //private readonly EventGridPublisherClient _eventGridClient;
    private readonly ServiceBusClient _serviceBusClient;

    public BooksController(CosmosClient cosmosClient, ServiceBusClient serviceBusClient)
    {
        _cosmosClient = cosmosClient;
        //_eventGridClient = eventGridClient;
        _serviceBusClient = serviceBusClient;
    }

    [HttpPost("add")] 
    public async Task<IActionResult> AddBook([FromBody] Book book)
    {
        var container = _cosmosClient.GetContainer(_databaseName, _collectionName);
        book.id = Guid.NewGuid().ToString(); // Generate a unique ID for the book
        await container.CreateItemAsync(book, new PartitionKey(book.id));

        var eventData = new EventGridEvent(
            "NewBookAdded",
            "Bookstore.Book",
            "1.0",
            book);
        //await _eventGridClient.SendEventAsync(eventData);

        await NotifySubscribers(book);

        return Ok(new { message = "Book added successfully!" });
    }

    private async Task NotifySubscribers(Book book)
    {
        var container = _cosmosClient.GetContainer("BookStoreDB", "Subscribers");
        var query = new QueryDefinition("SELECT * FROM c");
        var iterator = container.GetItemQueryIterator<Subscriber>(query);
        
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var subscriber in response)
            {
                if (!subscriber.notifiedBooks.Contains(book.id))
                {
                    await SendNotification(subscriber, book);
                    subscriber.notifiedBooks.Add(book.id);
                    await container.UpsertItemAsync(subscriber, new PartitionKey(subscriber.id));
                }
                else
                {
                    Console.WriteLine($"Subscriber {subscriber.email} already notified for book {book.title}");
                }
            }
        }
    }

    private async Task SendNotification(Subscriber subscriber, Book book)
    {
        var sender = _serviceBusClient.CreateSender(_topicName);
        var message = new ServiceBusMessage($"New Book: {book.title} by {book.author}");
        await sender.SendMessageAsync(message);
    }

    [HttpPost("subscribe")] 
    public async Task<IActionResult> RegisterSubscriber([FromBody] Subscriber subscriber)
    {
        var container = _cosmosClient.GetContainer("BookStoreDB", "Subscribers");
        subscriber.notifiedBooks = new List<string>(); // Initialize empty list
        try{
            subscriber.id = Guid.NewGuid().ToString(); // Generate a unique ID for the subscriber
            await container.CreateItemAsync(subscriber, new PartitionKey(subscriber.id));

            return Ok(new { message = "Subscriber registered successfully!" });
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return Conflict(new { message = "Subscriber already exists!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while registering the subscriber.", error = ex.Message });
        }

    }
}

public class Book
{
    public string id { get; set; }
    public string title { get; set; }
    public string author { get; set; }
    public decimal price { get; set; }
}

public class Subscriber
{
    public string id { get; set; }
    public string email { get; set; }
    public string name { get; set; }
    public List<string> notifiedBooks { get; set; } // Track notified books
}
