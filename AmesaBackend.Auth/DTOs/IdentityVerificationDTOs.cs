using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Auth.DTOs
{
    public class VerifyIdentityRequest
    {
        [Required]
        public string IdFrontImage { get; set; } = string.Empty; // base64 encoded

        public string? IdBackImage { get; set; } // base64 encoded, optional

        [Required]
        public string SelfieImage { get; set; } = string.Empty; // base64 encoded

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty; // passport, id_card, drivers_license

        [StringLength(100)]
        public string? DocumentNumber { get; set; } // Optional, can be extracted via OCR
    }

    public class IdentityVerificationResult
    {
        public bool IsVerified { get; set; }
        public Guid ValidationKey { get; set; }
        public decimal? LivenessScore { get; set; }
        public decimal? FaceMatchScore { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public Dictionary<string, object>? VerificationMetadata { get; set; }
    }

    public class IdentityVerificationStatusDto
    {
        public bool IsVerified { get; set; }
        public Guid? ValidationKey { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        public decimal? LivenessScore { get; set; }
        public decimal? FaceMatchScore { get; set; }
        public int VerificationAttempts { get; set; }
        public DateTime? LastVerificationAttempt { get; set; }
        public string? RejectionReason { get; set; }
    }
}

