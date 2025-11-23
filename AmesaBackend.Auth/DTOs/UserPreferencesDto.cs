using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AmesaBackend.Auth.DTOs
{
    /// <summary>
    /// User preferences data transfer object
    /// </summary>
    public class UserPreferencesDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string PreferencesJson { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Request to update user preferences
    /// </summary>
    public class UpdateUserPreferencesRequest
    {
        [Required]
        public JsonElement Preferences { get; set; }
        
        public string? Version { get; set; }
    }

    /// <summary>
    /// Preferences sync status DTO
    /// </summary>
    public class PreferencesSyncStatusDto
    {
        public DateTime LastSync { get; set; }
        public bool SyncInProgress { get; set; }
        public string ConflictResolution { get; set; } = "local";
        public bool HasLocalChanges { get; set; }
        public bool HasRemoteChanges { get; set; }
        public string? SyncError { get; set; }
    }

    /// <summary>
    /// Preference validation result DTO
    /// </summary>
    public class PreferenceValidationDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Preference category update requests
    /// </summary>
    public class UpdateAppearancePreferencesRequest
    {
        public string? Theme { get; set; }
        public string? PrimaryColor { get; set; }
        public string? AccentColor { get; set; }
        public string? FontSize { get; set; }
        public string? FontFamily { get; set; }
        public string? UiDensity { get; set; }
        public int? BorderRadius { get; set; }
        public bool? ShowAnimations { get; set; }
        public string? AnimationLevel { get; set; }
        public bool? ReducedMotion { get; set; }
    }

    public class UpdateLocalizationPreferencesRequest
    {
        public string? Language { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? NumberFormat { get; set; }
        public string? Currency { get; set; }
        public string? Timezone { get; set; }
        public bool? RtlSupport { get; set; }
    }

    public class UpdateAccessibilityPreferencesRequest
    {
        public bool? HighContrast { get; set; }
        public bool? ColorBlindAssist { get; set; }
        public string? ColorBlindType { get; set; }
        public bool? ScreenReaderOptimized { get; set; }
        public bool? KeyboardNavigation { get; set; }
        public bool? FocusIndicators { get; set; }
        public bool? SkipLinks { get; set; }
        public string? AltTextVerbosity { get; set; }
        public bool? CaptionsEnabled { get; set; }
        public bool? AudioDescriptions { get; set; }
        public bool? LargeClickTargets { get; set; }
        public bool? ReducedFlashing { get; set; }
    }

    public class UpdateNotificationPreferencesRequest
    {
        public bool? EmailNotifications { get; set; }
        public bool? PushNotifications { get; set; }
        public bool? BrowserNotifications { get; set; }
        public bool? SmsNotifications { get; set; }
        public bool? LotteryResults { get; set; }
        public bool? NewLotteries { get; set; }
        public bool? Promotions { get; set; }
        public bool? AccountUpdates { get; set; }
        public bool? SecurityAlerts { get; set; }
        public QuietHoursDto? QuietHours { get; set; }
        public bool? SoundEnabled { get; set; }
        public int? SoundVolume { get; set; }
        public bool? CustomSounds { get; set; }
    }

    public class QuietHoursDto
    {
        public bool Enabled { get; set; }
        public string StartTime { get; set; } = "22:00";
        public string EndTime { get; set; } = "08:00";
    }

    public class UpdateInteractionPreferencesRequest
    {
        public bool? AutoSave { get; set; }
        public int? AutoSaveInterval { get; set; }
        public bool? ConfirmationDialogs { get; set; }
        public bool? DoubleClickToOpen { get; set; }
        public bool? HoverEffects { get; set; }
        public int? TooltipDelay { get; set; }
        public int? ScrollSpeed { get; set; }
        public bool? KeyboardShortcuts { get; set; }
        public Dictionary<string, string>? CustomShortcuts { get; set; }
        public int? ClickSensitivity { get; set; }
        public bool? TouchGestures { get; set; }
        public bool? RightClickContext { get; set; }
    }

    public class UpdateLotteryPreferencesRequest
    {
        public List<string>? FavoriteCategories { get; set; }
        public decimal? PriceRangeMin { get; set; }
        public decimal? PriceRangeMax { get; set; }
        public List<string>? PreferredLocations { get; set; }
        public List<string>? HouseTypes { get; set; }
        public string? DefaultView { get; set; }
        public int? ItemsPerPage { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public bool? PriceDropAlerts { get; set; }
        public bool? NewMatchingLotteries { get; set; }
        public bool? EndingSoonAlerts { get; set; }
        public bool? WinnerAnnouncements { get; set; }
    }

    public class UpdatePrivacyPreferencesRequest
    {
        public bool? AnalyticsTracking { get; set; }
        public bool? PerformanceTracking { get; set; }
        public bool? MarketingTracking { get; set; }
        public bool? PersonalizedAds { get; set; }
        public bool? DataSharing { get; set; }
        public bool? CookieConsent { get; set; }
        public bool? LocationTracking { get; set; }
        public int? HistoryRetention { get; set; }
        public bool? AutoDeleteOldData { get; set; }
        public string? ProfileVisibility { get; set; }
        public bool? ShowActivity { get; set; }
        public bool? ShowWinnings { get; set; }
    }

    public class UpdatePerformancePreferencesRequest
    {
        public string? ImageQuality { get; set; }
        public bool? PreloadImages { get; set; }
        public bool? LazyLoading { get; set; }
        public int? CacheSize { get; set; }
        public bool? OfflineMode { get; set; }
        public bool? DataSaver { get; set; }
        public bool? PrefetchContent { get; set; }
        public bool? BackgroundSync { get; set; }
        public bool? DebugMode { get; set; }
        public bool? ShowPerformanceMetrics { get; set; }
        public bool? VerboseLogging { get; set; }
    }

    /// <summary>
    /// Preference export/import DTOs
    /// </summary>
    public class ExportPreferencesRequest
    {
        public bool IncludePersonalData { get; set; } = false;
        public List<string>? Categories { get; set; }
        public string Format { get; set; } = "json";
    }

    public class ImportPreferencesRequest
    {
        [Required]
        public string PreferencesData { get; set; } = string.Empty;
        
        public string Format { get; set; } = "json";
        public bool MergeWithExisting { get; set; } = false;
        public List<string>? CategoriesToImport { get; set; }
    }

    /// <summary>
    /// Preference history DTO for audit trail
    /// </summary>
    public class PreferenceHistoryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Bulk preference update request
    /// </summary>
    public class BulkUpdatePreferencesRequest
    {
        public UpdateAppearancePreferencesRequest? Appearance { get; set; }
        public UpdateLocalizationPreferencesRequest? Localization { get; set; }
        public UpdateAccessibilityPreferencesRequest? Accessibility { get; set; }
        public UpdateNotificationPreferencesRequest? Notifications { get; set; }
        public UpdateInteractionPreferencesRequest? Interaction { get; set; }
        public UpdateLotteryPreferencesRequest? Lottery { get; set; }
        public UpdatePrivacyPreferencesRequest? Privacy { get; set; }
        public UpdatePerformancePreferencesRequest? Performance { get; set; }
        public string? Version { get; set; }
        public string? Reason { get; set; }
    }
}


