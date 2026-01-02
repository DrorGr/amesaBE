using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Hubs
{
    /// <summary>
    /// SignalR hub for real-time lottery updates and notifications.
    /// Provides real-time communication for lottery events including inventory updates, countdowns, draws, and user-specific updates.
    /// Requires JWT Bearer authentication.
    /// </summary>
    [Authorize]
    public class LotteryHub : Hub
    {
        /// <summary>
        /// Joins a lottery group for a specific house to receive real-time updates for that house.
        /// </summary>
        /// <param name="houseId">The unique identifier of the house (must be a valid GUID).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the house ID is not a valid GUID format.</exception>
        public async Task JoinLotteryGroup(string houseId)
        {
            // Validate GUID format
            if (string.IsNullOrWhiteSpace(houseId) || !Guid.TryParse(houseId, out var houseIdGuid))
            {
                throw new ArgumentException("Invalid house ID format");
            }
            
            await Groups.AddToGroupAsync(Context.ConnectionId, $"lottery_{houseId}");
        }

        /// <summary>
        /// Leaves a lottery group for a specific house, stopping real-time updates for that house.
        /// </summary>
        /// <param name="houseId">The unique identifier of the house (must be a valid GUID).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the house ID is not a valid GUID format.</exception>
        public async Task LeaveLotteryGroup(string houseId)
        {
            // Validate GUID format
            if (string.IsNullOrWhiteSpace(houseId) || !Guid.TryParse(houseId, out var houseIdGuid))
            {
                throw new ArgumentException("Invalid house ID format");
            }
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lottery_{houseId}");
        }

        /// <summary>
        /// Called when a client connects to the hub.
        /// Automatically adds the user to their personal group based on their user ID from the authentication token.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnConnectedAsync()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// Automatically removes the user from their personal group.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnection, if any.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Extension methods for broadcasting lottery-related events from services to SignalR clients.
    /// These methods provide a convenient way to send real-time updates to connected clients.
    /// </summary>
    public static class LotteryHubExtensions
    {
        /// <summary>
        /// Broadcasts an inventory update to all clients subscribed to a specific house's lottery group.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context for the LotteryHub.</param>
        /// <param name="houseId">The unique identifier of the house whose inventory was updated.</param>
        /// <param name="update">The inventory update information containing ticket availability and counts.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task BroadcastInventoryUpdate(
            this IHubContext<LotteryHub> hubContext,
            Guid houseId,
            DTOs.InventoryUpdate update)
        {
            await hubContext.Clients.Group($"lottery_{houseId}")
                .SendAsync("InventoryUpdated", update);
        }

        /// <summary>
        /// Broadcasts a countdown update to all clients subscribed to a specific house's lottery group.
        /// Used to notify clients about remaining time until the next lottery draw.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context for the LotteryHub.</param>
        /// <param name="houseId">The unique identifier of the house whose countdown was updated.</param>
        /// <param name="update">The countdown update information containing time remaining until the draw.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task BroadcastCountdownUpdate(
            this IHubContext<LotteryHub> hubContext,
            Guid houseId,
            DTOs.CountdownUpdate update)
        {
            await hubContext.Clients.Group($"lottery_{houseId}")
                .SendAsync("CountdownUpdated", update);
        }

        /// <summary>
        /// Broadcasts a reservation status update to a specific user.
        /// Used to notify users about changes to their ticket reservations (confirmed, expired, etc.).
        /// </summary>
        /// <param name="hubContext">The SignalR hub context for the LotteryHub.</param>
        /// <param name="userId">The unique identifier of the user whose reservation status changed.</param>
        /// <param name="update">The reservation status update information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task BroadcastReservationStatus(
            this IHubContext<LotteryHub> hubContext,
            Guid userId,
            DTOs.ReservationStatusUpdate update)
        {
            await hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReservationStatusChanged", update);
        }

        /// <summary>
        /// Broadcasts a favorite update to a specific user.
        /// Used to notify users when houses are added to or removed from their favorites list.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context for the LotteryHub.</param>
        /// <param name="userId">The unique identifier of the user whose favorites changed.</param>
        /// <param name="update">The favorite update information containing the house ID and action (added/removed).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task BroadcastFavoriteUpdate(
            this IHubContext<LotteryHub> hubContext,
            Guid userId,
            DTOs.FavoriteUpdateDto update,
            CancellationToken cancellationToken = default)
        {
            await hubContext.Clients.Group($"user_{userId}")
                .SendAsync("FavoriteUpdate", update, cancellationToken);
        }

        /// <summary>
        /// Broadcasts a lottery draw started event to all clients subscribed to a specific house's lottery group.
        /// Notifies clients that a lottery draw has begun.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context for the LotteryHub.</param>
        /// <param name="houseId">The unique identifier of the house where the draw started.</param>
        /// <param name="draw">The lottery draw information containing draw ID and date.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Broadcasts a lottery draw completed event to all clients subscribed to a specific house's lottery group.
        /// Notifies clients that a lottery draw has completed and includes winner information.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context for the LotteryHub.</param>
        /// <param name="houseId">The unique identifier of the house where the draw completed.</param>
        /// <param name="draw">The lottery draw information containing draw ID, winning ticket number, winner user ID, and draw date.</param>
        /// <param name="winningTicket">The winning lottery ticket, if available (optional).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
