using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CloseExpAISolution.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
