using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Data;
using System.Diagnostics;

namespace AmesaBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(AmesaDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var healthCheck = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Checks = new
                {
                    Database = await CheckDatabaseAsync(),
                    Memory = CheckMemory(),
                    Disk = CheckDisk()
                }
            };

            return Ok(healthCheck);
        }

        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var healthCheck = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    MachineName = Environment.MachineName,
                    ProcessId = process.Id,
                    WorkingSet = process.WorkingSet64,
                    PrivateMemorySize = process.PrivateMemorySize64,
                    VirtualMemorySize = process.VirtualMemorySize64,
                    Threads = process.Threads.Count,
                    Handles = process.HandleCount,
                    Checks = new
                    {
                        Database = await CheckDatabaseDetailedAsync(),
                        Memory = CheckMemoryDetailed(),
                        Disk = CheckDiskDetailed()
                    }
                };

                return Ok(healthCheck);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in detailed health check");
                return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
            }
        }

        private async Task<object> CheckDatabaseAsync()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return new { Status = "Healthy", Message = "Database connection successful" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return new { Status = "Unhealthy", Message = ex.Message };
            }
        }

        private async Task<object> CheckDatabaseDetailedAsync()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return new { Status = "Unhealthy", Message = "Cannot connect to database" };
                }

                // Test a simple query
                var userCount = await _context.Users.CountAsync();
                var houseCount = await _context.Houses.CountAsync();

                return new
                {
                    Status = "Healthy",
                    Message = "Database connection and queries successful",
                    UserCount = userCount,
                    HouseCount = houseCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed database health check failed");
                return new { Status = "Unhealthy", Message = ex.Message };
            }
        }

        private object CheckMemory()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var workingSet = process.WorkingSet64;
                var maxMemory = 1024 * 1024 * 1024; // 1GB limit for container

                var status = workingSet < maxMemory ? "Healthy" : "Warning";
                var message = workingSet < maxMemory 
                    ? "Memory usage is within limits" 
                    : "Memory usage is approaching limits";

                return new
                {
                    Status = status,
                    Message = message,
                    WorkingSetMB = workingSet / (1024 * 1024),
                    MaxMemoryMB = maxMemory / (1024 * 1024)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory health check failed");
                return new { Status = "Unhealthy", Message = ex.Message };
            }
        }

        private object CheckMemoryDetailed()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var workingSet = process.WorkingSet64;
                var privateMemory = process.PrivateMemorySize64;
                var virtualMemory = process.VirtualMemorySize64;
                var maxMemory = 1024 * 1024 * 1024; // 1GB limit for container

                var status = workingSet < maxMemory ? "Healthy" : "Warning";
                var message = workingSet < maxMemory 
                    ? "Memory usage is within limits" 
                    : "Memory usage is approaching limits";

                return new
                {
                    Status = status,
                    Message = message,
                    WorkingSetMB = workingSet / (1024 * 1024),
                    PrivateMemoryMB = privateMemory / (1024 * 1024),
                    VirtualMemoryMB = virtualMemory / (1024 * 1024),
                    MaxMemoryMB = maxMemory / (1024 * 1024),
                    Threads = process.Threads.Count,
                    Handles = process.HandleCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed memory health check failed");
                return new { Status = "Unhealthy", Message = ex.Message };
            }
        }

        private object CheckDisk()
        {
            try
            {
                var drive = new DriveInfo("/");
                var freeSpace = drive.AvailableFreeSpace;
                var totalSpace = drive.TotalSize;
                var usedSpace = totalSpace - freeSpace;
                var usagePercentage = (double)usedSpace / totalSpace * 100;

                var status = usagePercentage < 90 ? "Healthy" : "Warning";
                var message = usagePercentage < 90 
                    ? "Disk usage is within limits" 
                    : "Disk usage is approaching limits";

                return new
                {
                    Status = status,
                    Message = message,
                    FreeSpaceGB = freeSpace / (1024 * 1024 * 1024),
                    TotalSpaceGB = totalSpace / (1024 * 1024 * 1024),
                    UsedSpaceGB = usedSpace / (1024 * 1024 * 1024),
                    UsagePercentage = Math.Round(usagePercentage, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disk health check failed");
                return new { Status = "Unhealthy", Message = ex.Message };
            }
        }

        private object CheckDiskDetailed()
        {
            try
            {
                var drive = new DriveInfo("/");
                var freeSpace = drive.AvailableFreeSpace;
                var totalSpace = drive.TotalSize;
                var usedSpace = totalSpace - freeSpace;
                var usagePercentage = (double)usedSpace / totalSpace * 100;

                var status = usagePercentage < 90 ? "Healthy" : "Warning";
                var message = usagePercentage < 90 
                    ? "Disk usage is within limits" 
                    : "Disk usage is approaching limits";

                return new
                {
                    Status = status,
                    Message = message,
                    DriveName = drive.Name,
                    DriveType = drive.DriveType.ToString(),
                    DriveFormat = drive.DriveFormat,
                    FreeSpaceGB = freeSpace / (1024 * 1024 * 1024),
                    TotalSpaceGB = totalSpace / (1024 * 1024 * 1024),
                    UsedSpaceGB = usedSpace / (1024 * 1024 * 1024),
                    UsagePercentage = Math.Round(usagePercentage, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed disk health check failed");
                return new { Status = "Unhealthy", Message = ex.Message };
            }
        }
    }
}

