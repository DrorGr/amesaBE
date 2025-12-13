using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Admin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AmesaBackend.Admin.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Allow anonymous access for diagnostics
    public class DiagnosticsController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            IServiceProvider serviceProvider,
            ILogger<DiagnosticsController> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [HttpGet("admin-users")]
        public async Task<IActionResult> GetAdminUsers()
        {
            try
            {
                // Try to get AdminDbContext from service provider (may be null if not configured)
                var adminDbContext = _serviceProvider.GetService<AdminDbContext>();
                
                if (adminDbContext == null)
                {
                    return Ok(new
                    {
                        success = false,
                        error = "AdminDbContext is not registered - DB_CONNECTION_STRING may not be configured",
                        users = Array.Empty<object>()
                    });
                }

                // Get all admin users (safe - no sensitive data)
                var users = await adminDbContext.AdminUsers
                    .Select(u => new
                    {
                        email = u.Email,
                        username = u.Username,
                        isActive = u.IsActive,
                        hasPasswordHash = !string.IsNullOrEmpty(u.PasswordHash),
                        hashLength = u.PasswordHash != null ? u.PasswordHash.Length : 0,
                        hashPrefix = u.PasswordHash != null && u.PasswordHash.Length > 10 
                            ? u.PasswordHash.Substring(0, 10) 
                            : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    totalUsers = users.Count,
                    users = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying admin users for diagnostics");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    errorType = ex.GetType().Name
                });
            }
        }
    }
}
