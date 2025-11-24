using System.Security.Claims;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmesaBackend.Auth.Controllers
{
    [ApiController]
    [Route("api/v1/admin/config")]
    [Authorize]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IConfigurationService configurationService,
            ILogger<ConfigurationController> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Get configuration value by key
        /// GET /api/v1/admin/config/{key}
        /// </summary>
        [HttpGet("{key}")]
        public async Task<ActionResult<ApiResponse<object>>> GetConfiguration(string key)
        {
            try
            {
                var config = await _configurationService.GetConfigurationAsync(key);
                if (config == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Configuration with key '{key}' not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { key = config.Key, value = System.Text.Json.JsonDocument.Parse(config.Value), description = config.Description }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration for key: {Key}", key);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Update configuration value
        /// PUT /api/v1/admin/config/{key}
        /// </summary>
        [HttpPut("{key}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateConfiguration(string key, [FromBody] UpdateConfigurationRequest request)
        {
            try
            {
                var config = await _configurationService.SetConfigurationAsync(key, request.Value, request.Description);
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { key = config.Key, value = System.Text.Json.JsonDocument.Parse(config.Value), description = config.Description },
                    Message = "Configuration updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration for key: {Key}", key);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Check if a feature is enabled
        /// GET /api/v1/admin/config/{key}/enabled
        /// </summary>
        [HttpGet("{key}/enabled")]
        public async Task<ActionResult<ApiResponse<bool>>> IsFeatureEnabled(string key)
        {
            try
            {
                var isEnabled = await _configurationService.IsFeatureEnabledAsync(key);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = isEnabled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if feature is enabled for key: {Key}", key);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }
    }

    public class UpdateConfigurationRequest
    {
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

