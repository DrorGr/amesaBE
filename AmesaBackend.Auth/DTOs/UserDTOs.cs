using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Auth.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string? Phone { get; set; }
        public bool PhoneVerified { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? IdNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string VerificationStatus { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string PreferredLanguage { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserLotteryDataDto? LotteryData { get; set; }
    }

    /// <summary>
    /// User lottery data for inclusion in user profile
    /// Fixed: Changed from counts to arrays as per frontend contract
    /// </summary>
    public class UserLotteryDataDto
    {
        // Arrays for frontend consumption (as per API contract)
        public List<Guid> FavoriteHouseIds { get; set; } = new();
        public List<object> ActiveEntries { get; set; } = new(); // List<LotteryTicketDto> - using object to avoid circular dependency
        
        // Statistics
        public int TotalEntriesCount { get; set; }
        public int TotalWins { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal TotalWinnings { get; set; }
        public decimal WinRatePercentage { get; set; }
        public decimal AverageSpendingPerEntry { get; set; }
        public Guid? FavoriteHouseId { get; set; }
        public string? MostActiveMonth { get; set; }
        public DateTime? LastEntryDate { get; set; }
        
        // Additional data
        public object? Stats { get; set; } // UserLotteryStatsDto
        public object? Preferences { get; set; } // LotteryPreferencesDto
    }

    public class UpdateUserProfileRequest
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [StringLength(10)]
        public string? PreferredLanguage { get; set; }

        [StringLength(50)]
        public string? Timezone { get; set; }
    }

    public class UserAddressDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? HouseNumber { get; set; }
        public string? ZipCode { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAddressRequest
    {
        [Required]
        [StringLength(20)]
        public string Type { get; set; } = "home";

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(255)]
        public string? Street { get; set; }

        [StringLength(20)]
        public string? HouseNumber { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        public bool IsPrimary { get; set; } = false;
    }

    public class UserPhoneDto
    {
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddPhoneRequest
    {
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
    }

    public class IdentityDocumentDto
    {
        public Guid Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string? FrontImageUrl { get; set; }
        public string? BackImageUrl { get; set; }
        public string? SelfieImageUrl { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UploadIdentityDocumentRequest
    {
        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required]
        public string FrontImage { get; set; } = string.Empty;

        [Required]
        public string BackImage { get; set; } = string.Empty;

        [Required]
        public string SelfieImage { get; set; } = string.Empty;
    }
}

