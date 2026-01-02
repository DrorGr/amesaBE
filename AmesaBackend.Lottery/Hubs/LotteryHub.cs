using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Hubs
{
    [Authorize]
    public class LotteryHub : Hub
    {
        public async Task JoinLotteryGroup(string houseId)
        {
            // Validate GUID format
            if (string.IsNullOrWhiteSpace(houseId) || !Guid.TryParse(houseId, out var houseIdGuid))
            {
                throw new ArgumentException("Invalid house ID format");
            }
            
            await Groups.AddToGroupAsync(Context.ConnectionId, $"lottery_{houseId}");
        }

        public async Task LeaveLotteryGroup(string houseId)
        {
            // Validate GUID format
            if (string.IsNullOrWhiteSpace(houseId) || !Guid.TryParse(houseId, out var houseIdGuid))
            {
                throw new ArgumentException("Invalid house ID format");
            }
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lottery_{houseId}");
        }

        public override async Task OnConnectedAsync()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }

    // Extension methods for broadcasting from services
    public static class LotteryHubExtensions
    {
        public static async Task BroadcastInventoryUpdate(
            this IHubContext<LotteryHub> hubContext,
            Guid houseId,
            DTOs.InventoryUpdate update)
        {
            await hubContext.Clients.Group($"lottery_{houseId}")
                .SendAsync("InventoryUpdated", update);
        }

        public static async Task BroadcastCountdownUpdate(
            this IHubContext<LotteryHub> hubContext,
            Guid houseId,
            DTOs.CountdownUpdate update)
        {
            await hubContext.Clients.Group($"lottery_{houseId}")
                .SendAsync("CountdownUpdated", update);
        }

        public static async Task BroadcastReservationStatus(
            this IHubContext<LotteryHub> hubContext,
            Guid userId,
            DTOs.ReservationStatusUpdate update)
        {
            await hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReservationStatusChanged", update);
        }

        public static async Task BroadcastFavoriteUpdate(
            this IHubContext<LotteryHub> hubContext,
            Guid userId,
            DTOs.FavoriteUpdateDto update,
            CancellationToken cancellationToken = default)
        {
            await hubContext.Clients.Group($"user_{userId}")
                .SendAsync("FavoriteUpdate", update, cancellationToken);
        }

        public static async Task BroadcastDrawStarted(
            this IHubContext<LotteryHub> hubContext,
            Guid houseId,
            AmesaBackend.Models.LotteryDraw draw)
        {
            await hubContext.Clients.Group($"lottery_{houseId}")
                .SendAsync("LotteryDrawStarted", new { 
                    HouseId = houseId, 
                    DrawId = draw.Id, 
                    DrawDate = draw.DrawDate 
                });
        }

        public static async Task BroadcastDrawCompleted(
            this IHubContext<LotteryHub> hubContext,
            Guid houseId,
            AmesaBackend.Models.LotteryDraw draw,
            AmesaBackend.Models.LotteryTicket? winningTicket)
        {
            await hubContext.Clients.Group($"lottery_{houseId}")
                .SendAsync("LotteryDrawCompleted", new { 
                    HouseId = houseId, 
                    DrawId = draw.Id, 
                    WinningTicketNumber = draw.WinningTicketNumber,
                    WinnerUserId = draw.WinnerUserId,
                    DrawDate = draw.DrawDate
                });
        }
    }
}











