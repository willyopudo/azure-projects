using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    public async Task OnGetAsync()
    {
        // Fetch initial messages synchronously for rendering
        Messages = await _serviceBusService.FetchUnconsumedMessagesAsync();

        // Start listening for new messages in the background
        _ = Task.Run(() => _serviceBusService.StartListeningAsync());
    }
}
