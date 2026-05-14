namespace AmesaBackend.Admin.DTOs;

public sealed class AdminPromotionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public string? ValueType { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid[]? ApplicableHouses { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal TotalDiscountGiven { get; set; }
}

public sealed class SaveAdminPromotionRequest
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "discount";
    public decimal? Value { get; set; }
    public string? ValueType { get; set; } = "percentage";
    public decimal? MinAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int? UsageLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ApplicableHouseIds { get; set; }
}

public sealed class AdminNotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PendingDeliveries { get; set; }
    public int SentDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
}

public sealed class SendAdminNotificationRequest
{
    public string TargetMode { get; set; } = "user";
    public string UserLookup { get; set; } = string.Empty;
    public string GroupBasis { get; set; } = "house";
    public Guid? HouseId { get; set; }
    public Guid? LotteryHouseId { get; set; }
    public int? BirthdayMonth { get; set; }
    public int? BirthdayDay { get; set; }
    public string? LocationQuery { get; set; }
    public string? Language { get; set; }
    public string Type { get; set; } = "system_announcement";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool InApp { get; set; } = true;
    public bool Email { get; set; }
    public bool Sms { get; set; }
    public bool Push { get; set; }
    public bool WebPush { get; set; }
    public bool Telegram { get; set; }
    public DateTime? ScheduledFor { get; set; }
}

public sealed class SendAdminNotificationResult
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public IReadOnlyCollection<string> QueuedChannels { get; set; } = Array.Empty<string>();
}
