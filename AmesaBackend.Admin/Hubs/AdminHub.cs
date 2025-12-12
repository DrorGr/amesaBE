using Microsoft.AspNetCore.SignalR;

namespace AmesaBackend.Admin.Hubs
{
    public class AdminHub : Hub
    {
        private readonly ILogger<AdminHub> _logger;

        public AdminHub(ILogger<AdminHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Admin client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Admin client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // SECURITY: Whitelist of allowed SignalR groups
        private static readonly HashSet<string> AllowedGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            "houses",
            "users",
            "draws"
        };

        // Join a group for real-time updates (e.g., house updates, user updates)
        public async Task JoinGroup(string groupName)
        {
            // SECURITY: Validate group name against whitelist
            if (string.IsNullOrWhiteSpace(groupName) || !AllowedGroups.Contains(groupName))
            {
                _logger.LogWarning("Invalid group name attempted: {GroupName} by connection {ConnectionId}", groupName, Context.ConnectionId);
                throw new ArgumentException($"Invalid group name: {groupName}");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogDebug("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
        }

        // Leave a group
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogDebug("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
        }
    }
}

