using AmesaBackend.Auth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AmesaBackend.Admin.Hubs
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminHub : Hub
    {
        private readonly ILogger<AdminHub> _logger;
        private readonly IAdminAuthService _authService;

        public AdminHub(ILogger<AdminHub> logger, IAdminAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        public override async Task OnConnectedAsync()
        {
            if (!IsAdminAuthenticated())
            {
                _logger.LogWarning("Unauthenticated admin hub connection rejected: {ConnectionId}", Context.ConnectionId);
                Context.Abort();
                return;
            }

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
            EnsureAdminAuthenticated();

            // SECURITY: Validate group name against whitelist
            if (string.IsNullOrWhiteSpace(groupName) || !AllowedGroups.Contains(groupName))
            {
                _logger.LogWarning("Invalid group name attempted: {GroupName} by connection {ConnectionId}", groupName, Context.ConnectionId);
                throw new HubException("Invalid group name.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogDebug("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
        }

        // Leave a group
        public async Task LeaveGroup(string groupName)
        {
            EnsureAdminAuthenticated();

            if (string.IsNullOrWhiteSpace(groupName) || !AllowedGroups.Contains(groupName))
            {
                _logger.LogWarning("Invalid group leave attempted: {GroupName} by connection {ConnectionId}", groupName, Context.ConnectionId);
                throw new HubException("Invalid group name.");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogDebug("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
        }

        private bool IsAdminAuthenticated()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                return true;
            }

            try
            {
                return _authService.IsAuthenticated();
            }
            catch
            {
                return false;
            }
        }

        private void EnsureAdminAuthenticated()
        {
            if (!IsAdminAuthenticated())
            {
                _logger.LogWarning("Unauthenticated admin hub method call rejected: {ConnectionId}", Context.ConnectionId);
                throw new HubException("Authentication required.");
            }
        }
    }
}

