using AmesaBackend.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmesaBackend.Admin.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Allow anonymous access for testing
    public class TestAuthController : ControllerBase
    {
        private readonly IAdminAuthService _authService;
        private readonly ILogger<TestAuthController> _logger;

        public TestAuthController(IAdminAuthService authService, ILogger<TestAuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> TestLogin([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogWarning("TestAuthController: Login attempt for email: {Email}", request.Email);
                
                var success = await _authService.AuthenticateAsync(request.Email, request.Password);
                
                if (success)
                {
                    var isAuthenticated = _authService.IsAuthenticated();
                    var email = _authService.GetCurrentAdminEmail();
                    
                    _logger.LogWarning("TestAuthController: Authentication result - Success: {Success}, IsAuthenticated: {IsAuth}, Email: {Email}", 
                        success, isAuthenticated, email);
                    
                    return Ok(new
                    {
                        success = true,
                        isAuthenticated = isAuthenticated,
                        email = email,
                        message = "Authentication successful"
                    });
                }
                else
                {
                    _logger.LogWarning("TestAuthController: Authentication failed for email: {Email}", request.Email);
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid email or password"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestAuthController: Exception during login for email: {Email}", request.Email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        [HttpGet("check")]
        public IActionResult CheckAuth()
        {
            try
            {
                var isAuthenticated = _authService.IsAuthenticated();
                var email = _authService.GetCurrentAdminEmail();
                
                return Ok(new
                {
                    isAuthenticated = isAuthenticated,
                    email = email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestAuthController: Exception during auth check");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
