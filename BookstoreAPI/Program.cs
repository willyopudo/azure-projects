using Microsoft.Azure.Cosmos;
using Azure.Messaging.ServiceBus;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
DotNetEnv.Env.Load();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

// Configure  CosmosClient using environment variables
builder.Services.AddSingleton(new CosmosClient(
    Environment.GetEnvironmentVariable("COSMOS_DB_ACCOUNT_ENDPOINT"),
    Environment.GetEnvironmentVariable("COSMOS_DB_ACCOUNT_KEY")
));

// Configure ServiceBusClient using environment variables
builder.Services.AddSingleton(new ServiceBusClient(
    Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING")
));


// Configure CORS to allow all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin() // Allow requests from any origin
              .AllowAnyHeader() // Allow any HTTP headers
              .AllowAnyMethod(); // Allow any HTTP methods (GET, POST, etc.)
    });
    Console.WriteLine("CORS policy applied: AllowAllOrigins");
});


var app = builder.Build();

// Handle preflight requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        return;
    }
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
});

// Enable CORS
app.UseCors("AllowAll");
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
