namespace AmesaBackend.Admin.Services
{
    public class AdminDatabaseService : IAdminDatabaseService
    {
        private readonly IConfiguration _configuration;

        public AdminDatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetCurrentEnvironment()
        {
            var envFromVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrEmpty(envFromVar))
            {
                return envFromVar switch
                {
                    "Production" => "Production",
                    _ => "Development"
                };
            }
            
            return "Development";
        }
    }
}

