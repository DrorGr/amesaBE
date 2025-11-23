namespace AmesaBackend.Tests.TestData;

/// <summary>
/// Constants for test data
/// </summary>
public static class TestConstants
{
    public const string TestUserEmail = "test@example.com";
    public const string TestUserPassword = "Test123!@#";
    public const string TestUserUsername = "testuser";
    public const string TestAdminEmail = "admin@amesa.com";
    public const string TestAdminPassword = "Admin123!";

    public static class ApiEndpoints
    {
        public const string AuthLogin = "/api/v1/auth/login";
        public const string AuthRegister = "/api/v1/auth/register";
        public const string AuthLogout = "/api/v1/auth/logout";
        public const string AuthRefresh = "/api/v1/auth/refresh";
        public const string Houses = "/api/v1/houses";
        public const string HousesFavorites = "/api/v1/houses/favorites";
        public const string Tickets = "/api/v1/tickets";
        public const string LotteryResults = "/api/v1/lotteryresults";
        public const string Payments = "/api/v1/payments";
        public const string UserPreferences = "/api/v1/preferences";
    }

    public static class ErrorMessages
    {
        public const string Unauthorized = "Unauthorized";
        public const string NotFound = "Not Found";
        public const string BadRequest = "Bad Request";
        public const string ValidationError = "Validation failed";
    }
}

