using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmesaBackend.Shared.Models;

namespace AmesaBackend.Auth.Models
{
    /// <summary>
    /// User preferences entity for storing user-specific settings
    /// </summary>
    [Table("user_preferences", Schema = "amesa_auth")]
    public class UserPreferences : BaseEntity
    {
        /// <summary>
        /// Reference to the user who owns these preferences
        /// </summary>
        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        /// <summary>
        /// JSON string containing all user preferences
        /// </summary>
        [Required]
        [Column("preferences_json", TypeName = "jsonb")]
        public string PreferencesJson { get; set; } = string.Empty;

        /// <summary>
        /// Version of the preferences schema for migration purposes
        /// </summary>
        [Required]
        [Column("version")]
        [MaxLength(20)]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Indicates if preferences should sync with server
        /// </summary>
        [Column("sync_enabled")]
        public bool SyncEnabled { get; set; } = true;

        /// <summary>
        /// Last time preferences were synced with client
        /// </summary>
        [Column("last_sync_at")]
        public DateTime? LastSyncAt { get; set; }

        /// <summary>
        /// Hash of preferences for conflict detection
        /// </summary>
        [Column("preferences_hash")]
        [MaxLength(64)]
        public string? PreferencesHash { get; set; }

        /// <summary>
        /// Navigation property to User
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// Navigation property to preference history
        /// </summary>
        public virtual ICollection<UserPreferenceHistory> History { get; set; } = new List<UserPreferenceHistory>();
    }

    /// <summary>
    /// User preference history for audit trail
    /// </summary>
    [Table("user_preference_history", Schema = "amesa_auth")]
    public class UserPreferenceHistory : BaseEntity
    {
        /// <summary>
        /// Reference to user preferences
        /// </summary>
        [Required]
        [Column("user_preferences_id")]
        public Guid UserPreferencesId { get; set; }

        /// <summary>
        /// Reference to the user
        /// </summary>
        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Category of preference that changed
        /// </summary>
        [Required]
        [Column("category")]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Specific property name that changed
        /// </summary>
        [Required]
        [Column("property_name")]
        [MaxLength(100)]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Previous value (JSON)
        /// </summary>
        [Column("old_value")]
        public string? OldValue { get; set; }

        /// <summary>
        /// New value (JSON)
        /// </summary>
        [Column("new_value")]
        public string? NewValue { get; set; }

        /// <summary>
        /// Reason for the change
        /// </summary>
        [Column("change_reason")]
        [MaxLength(255)]
        public string? ChangeReason { get; set; }

        /// <summary>
        /// IP address of the change request
        /// </summary>
        [Column("ip_address")]
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent of the change request
        /// </summary>
        [Column("user_agent")]
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Navigation property to UserPreferences
        /// </summary>
        [ForeignKey("UserPreferencesId")]
        public virtual UserPreferences? UserPreferences { get; set; }

        /// <summary>
        /// Navigation property to User
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// User preference sync log for tracking synchronization
    /// </summary>
    [Table("user_preference_sync_log", Schema = "amesa_auth")]
    public class UserPreferenceSyncLog : BaseEntity
    {
        /// <summary>
        /// Reference to user preferences
        /// </summary>
        [Required]
        [Column("user_preferences_id")]
        public Guid UserPreferencesId { get; set; }

        /// <summary>
        /// Reference to the user
        /// </summary>
        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Sync operation type
        /// </summary>
        [Required]
        [Column("sync_type")]
        [MaxLength(20)]
        public string SyncType { get; set; } = string.Empty; // 'upload', 'download', 'conflict'

        /// <summary>
        /// Sync status
        /// </summary>
        [Required]
        [Column("sync_status")]
        [MaxLength(20)]
        public string SyncStatus { get; set; } = string.Empty; // 'success', 'failed', 'partial'

        /// <summary>
        /// Client version that initiated sync
        /// </summary>
        [Column("client_version")]
        [MaxLength(20)]
        public string? ClientVersion { get; set; }

        /// <summary>
        /// Server version during sync
        /// </summary>
        [Column("server_version")]
        [MaxLength(20)]
        public string? ServerVersion { get; set; }

        /// <summary>
        /// Sync duration in milliseconds
        /// </summary>
        [Column("sync_duration_ms")]
        public int? SyncDurationMs { get; set; }

        /// <summary>
        /// Error message if sync failed
        /// </summary>
        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Conflict resolution strategy used
        /// </summary>
        [Column("conflict_resolution")]
        [MaxLength(20)]
        public string? ConflictResolution { get; set; } // 'local', 'remote', 'merge'

        /// <summary>
        /// Number of preferences synced
        /// </summary>
        [Column("preferences_count")]
        public int PreferencesCount { get; set; }

        /// <summary>
        /// Size of synced data in bytes
        /// </summary>
        [Column("data_size_bytes")]
        public long DataSizeBytes { get; set; }

        /// <summary>
        /// Navigation property to UserPreferences
        /// </summary>
        [ForeignKey("UserPreferencesId")]
        public virtual UserPreferences? UserPreferences { get; set; }

        /// <summary>
        /// Navigation property to User
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}





















