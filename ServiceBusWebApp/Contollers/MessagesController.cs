using Microsoft.AspNetCore.Mvc;

public class MessagesController : Controller
{
    private readonly ServiceBusService _serviceBusService;

    public MessagesController(ServiceBusService serviceBusService)
    {
        _serviceBusService = serviceBusService;
    }

    // public async Task<IActionResult> Messages()
    // {
    //     var messages = _serviceBusService.FetchUnconsumedMessagesAsync().Result;
    //     return View(messages);
    // }
}