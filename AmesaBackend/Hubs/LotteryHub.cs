using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AmesaBackend.Hubs
{
    [Authorize]
    public class LotteryHub : Hub
    {
        public async Task JoinLotteryGroup(string houseId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"lottery_{houseId}");
        }

        public async Task LeaveLotteryGroup(string houseId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lottery_{houseId}");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
