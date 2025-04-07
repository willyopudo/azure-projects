using Microsoft.AspNetCore.SignalR;
using ServiceBusWebApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ServiceBusService>();

var app = builder.Build();

// Start the Service Bus listener
var serviceBusService = app.Services.GetRequiredService<ServiceBusService>();
_ = Task.Run(() => serviceBusService.StartListeningAsync());

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<MessageHub>("/messageHub");

app.Run();
