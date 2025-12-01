namespace AmesaBackend.Notification.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? TemplateId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SendNotificationRequest
    {
        public Guid UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Channels { get; set; } = new();
        public Dictionary<string, object>? Data { get; set; }
        public string? TemplateName { get; set; }
        public Dictionary<string, object>? TemplateVariables { get; set; }
    }

    public class DeliveryStatusDto
    {
        public Guid Id { get; set; }
        public Guid NotificationId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ExternalId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? OpenedAt { get; set; }
        public DateTime? ClickedAt { get; set; }
        public int RetryCount { get; set; }
        public decimal? Cost { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime CreatedAt { get; set; }
    }

    public class ChannelPreferencesDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public List<string>? NotificationTypes { get; set; }
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
    }

    public class UpdateChannelPreferencesRequest
    {
        public string Channel { get; set; } = string.Empty;
        public bool? Enabled { get; set; }
        public List<string>? NotificationTypes { get; set; }
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
    }

    public class PushSubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public Dictionary<string, object>? DeviceInfo { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubscribePushRequest
    {
        public string Endpoint { get; set; } = string.Empty;
        public string P256dhKey { get; set; } = string.Empty;
        public string AuthKey { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public Dictionary<string, object>? DeviceInfo { get; set; }
    }

    public class TelegramLinkDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public long TelegramUserId { get; set; }
        public string? TelegramUsername { get; set; }
        public bool Verified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TelegramLinkRequest
    {
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class SocialMediaLinkDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string PlatformUserId { get; set; } = string.Empty;
        public bool Verified { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public ErrorResponse? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Details { get; set; }
    }

    public class DeliveryResult
    {
        public bool Success { get; set; }
        public string? ExternalId { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal? Cost { get; set; }
    }

    public class OrchestrationResult
    {
        public Guid NotificationId { get; set; }
        public List<DeliveryResult> DeliveryResults { get; set; } = new();
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }

    public class NotificationRequest
    {
        public Guid UserId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
        public string? TemplateName { get; set; }
        public Dictionary<string, object>? TemplateVariables { get; set; }
        public string Language { get; set; } = "en";
    }

    public class NotificationPreferences
    {
        public Dictionary<string, ChannelPreferencesDto> Channels { get; set; } = new();
    }

    public class UnsubscribePushRequest
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}

