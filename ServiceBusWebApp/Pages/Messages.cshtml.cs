using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace ServiceBusWebApp.Pages;

public class MessagesModel : PageModel
{
    private readonly ILogger<MessagesModel> _logger;
    public List<string> Messages { get; set; } = new List<string>();
    private readonly ServiceBusService _serviceBusService;

    public MessagesModel(ILogger<MessagesModel> logger, ServiceBusService serviceBusService)
    {
        _serviceBusService = serviceBusService;
        _logger = logger;
    }

    public void OnGet()
    {
        //Messages = _serviceBusService.ReceiveMessagesAsync().Result;
    }
}
