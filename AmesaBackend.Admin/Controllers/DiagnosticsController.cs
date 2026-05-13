using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Admin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AmesaBackend.Admin.Controllers
{
    [ApiController]
    [Route("api/v1/admin/diagnostics")]
    [Authorize(Policy = "AdminOnly")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly IHostEnvironment _environment;

        public DiagnosticsController(
            IServiceProvider serviceProvider,
            ILogger<DiagnosticsController> logger,
            IHostEnvironment environment)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet("admin-users")]
        public async Task<IActionResult> GetAdminUsers()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

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

                // Return limited diagnostics only; do not expose password hash metadata.
                var users = await adminDbContext.AdminUsers
                    .Select(u => new
                    {
                        email = u.Email,
                        username = u.Username,
                        isActive = u.IsActive
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
                    error = "Diagnostics query failed"
                });
            }
        }
    }
}
