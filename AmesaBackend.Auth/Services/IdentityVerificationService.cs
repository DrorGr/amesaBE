using System.Text;
using System.Text.Json;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Auth.Services
{
    public class IdentityVerificationService : IIdentityVerificationService
    {
        private readonly AuthDbContext _context;
        private readonly IAwsRekognitionService _rekognitionService;
        private readonly ILogger<IdentityVerificationService> _logger;
        private const decimal MIN_LIVENESS_SCORE = 80.0m;
        private const decimal MIN_FACE_MATCH_SCORE = 90.0m;

        public IdentityVerificationService(
            AuthDbContext context,
            IAwsRekognitionService rekognitionService,
            ILogger<IdentityVerificationService> logger)
        {
            _context = context;
            _rekognitionService = rekognitionService;
            _logger = logger;
        }

        public async Task<IdentityVerificationResult> VerifyIdentityAsync(Guid userId, VerifyIdentityRequest request)
        {
            try
            {
                // Convert base64 images to byte arrays
                var idFrontBytes = Convert.FromBase64String(request.IdFrontImage);
                var selfieBytes = Convert.FromBase64String(request.SelfieImage);
                byte[]? idBackBytes = null;
                if (!string.IsNullOrEmpty(request.IdBackImage))
                {
                    idBackBytes = Convert.FromBase64String(request.IdBackImage);
                }

                // Step 1: Detect faces in selfie (liveness detection)
                var selfieFacesResponse = await _rekognitionService.DetectFacesAsync(selfieBytes);
                if (selfieFacesResponse.FaceDetails == null || selfieFacesResponse.FaceDetails.Count == 0)
                {
                    return new IdentityVerificationResult
                    {
                        IsVerified = false,
                        ValidationKey = Guid.NewGuid(),
                        VerificationStatus = "rejected",
                        RejectionReason = "No face detected in selfie. Please ensure your face is clearly visible."
                    };
                }

                var selfieFace = selfieFacesResponse.FaceDetails[0];
                var livenessScore = CalculateLivenessScore(selfieFace);

                // Step 2: Detect faces in ID document
                var idFacesResponse = await _rekognitionService.DetectFacesAsync(idFrontBytes);
                if (idFacesResponse?.FaceDetails == null || idFacesResponse.FaceDetails.Count == 0)
                {
                    return new IdentityVerificationResult
                    {
                        IsVerified = false,
                        ValidationKey = Guid.NewGuid(),
                        VerificationStatus = "rejected",
                        RejectionReason = "No face detected in ID document. Please ensure the ID photo is clearly visible."
                    };
                }

                var idFace = idFacesResponse.FaceDetails[0];

                // Step 3: Compare faces (ID photo vs selfie)
                var compareResponse = await _rekognitionService.CompareFacesAsync(idFrontBytes, selfieBytes);
                var faceMatchScore = (decimal)(compareResponse?.FaceMatches?.FirstOrDefault()?.Similarity ?? 0);

                // Step 4: Extract text from ID (optional, for document number validation)
                var textResponse = await _rekognitionService.DetectTextAsync(idFrontBytes);
                var extractedText = string.Join(" ", textResponse.TextDetections?.Select(t => t.DetectedText) ?? Array.Empty<string>());

                // Step 5: Determine verification result
                var isVerified = livenessScore >= MIN_LIVENESS_SCORE && faceMatchScore >= MIN_FACE_MATCH_SCORE;

                // Generate validation key
                var validationKey = Guid.NewGuid();

                // Build verification metadata
                var metadata = new Dictionary<string, object>
                {
                    ["livenessScore"] = livenessScore,
                    ["faceMatchScore"] = faceMatchScore,
                    ["selfieFaceCount"] = selfieFacesResponse.FaceDetails.Count,
                    ["idFaceCount"] = idFacesResponse.FaceDetails.Count,
                    ["extractedText"] = extractedText,
                    ["documentType"] = request.DocumentType,
                    ["verificationProvider"] = "aws_rekognition",
                    ["verifiedAt"] = DateTime.UtcNow
                };

                // Get or create identity document record
                var document = await _context.UserIdentityDocuments
                    .FirstOrDefaultAsync(d => d.UserId == userId);

                if (document == null)
                {
                    document = new UserIdentityDocument
                    {
                        UserId = userId,
                        DocumentType = request.DocumentType,
                        DocumentNumber = request.DocumentNumber ?? extractedText,
                        ValidationKey = validationKey,
                        LivenessScore = livenessScore,
                        FaceMatchScore = (decimal)faceMatchScore,
                        VerificationProvider = "aws_rekognition",
                        VerificationMetadata = JsonSerializer.Serialize(metadata),
                        VerificationStatus = isVerified ? "verified" : "rejected",
                        VerificationAttempts = 1,
                        LastVerificationAttempt = DateTime.UtcNow,
                        VerifiedAt = isVerified ? DateTime.UtcNow : null,
                        RejectionReason = isVerified ? null : $"Liveness: {livenessScore:F2}% (min: {MIN_LIVENESS_SCORE}%), Face Match: {faceMatchScore:F2}% (min: {MIN_FACE_MATCH_SCORE}%)",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserIdentityDocuments.Add(document);
                }
                else
                {
                    document.ValidationKey = validationKey;
                    document.DocumentType = request.DocumentType; // Update document type
                    document.DocumentNumber = request.DocumentNumber ?? document.DocumentNumber ?? extractedText;
                    document.LivenessScore = livenessScore;
                    document.FaceMatchScore = (decimal)faceMatchScore;
                    document.VerificationMetadata = JsonSerializer.Serialize(metadata);
                    document.VerificationStatus = isVerified ? "verified" : "rejected";
                    document.VerificationAttempts++;
                    document.LastVerificationAttempt = DateTime.UtcNow;
                    document.VerifiedAt = isVerified ? DateTime.UtcNow : null;
                    document.RejectionReason = isVerified ? null : $"Liveness: {livenessScore:F2}% (min: {MIN_LIVENESS_SCORE}%), Face Match: {faceMatchScore:F2}% (min: {MIN_FACE_MATCH_SCORE}%)";
                    document.UpdatedAt = DateTime.UtcNow;
                }

                // Update user verification status if verified
                if (isVerified)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.VerificationStatus = UserVerificationStatus.IdentityVerified;
                        user.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Identity verification completed for user {UserId}. Verified: {IsVerified}, ValidationKey: {ValidationKey}",
                    userId, isVerified, validationKey);

                return new IdentityVerificationResult
                {
                    IsVerified = isVerified,
                    ValidationKey = validationKey,
                    LivenessScore = livenessScore,
                    FaceMatchScore = (decimal)faceMatchScore,
                    VerificationStatus = isVerified ? "verified" : "rejected",
                    RejectionReason = isVerified ? null : $"Liveness: {livenessScore:F2}% (min: {MIN_LIVENESS_SCORE}%), Face Match: {faceMatchScore:F2}% (min: {MIN_FACE_MATCH_SCORE}%)",
                    VerificationMetadata = metadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during identity verification for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IdentityVerificationStatusDto> GetVerificationStatusAsync(Guid userId)
        {
            try
            {
                var document = await _context.UserIdentityDocuments
                    .FirstOrDefaultAsync(d => d.UserId == userId);

                if (document == null)
                {
                    return new IdentityVerificationStatusDto
                    {
                        VerificationStatus = "not_started"
                    };
                }

                return new IdentityVerificationStatusDto
                {
                    ValidationKey = document.ValidationKey,
                    VerificationStatus = document.VerificationStatus,
                    VerifiedAt = document.VerifiedAt,
                    LivenessScore = document.LivenessScore,
                    FaceMatchScore = document.FaceMatchScore,
                    VerificationAttempts = document.VerificationAttempts,
                    LastVerificationAttempt = document.LastVerificationAttempt,
                    RejectionReason = document.RejectionReason
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving verification status for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IdentityVerificationResult> RetryVerificationAsync(Guid userId, VerifyIdentityRequest request)
        {
            // Retry is the same as initial verification
            return await VerifyIdentityAsync(userId, request);
        }

        /// <summary>
        /// Calculate liveness score based on face detection quality
        /// Higher quality = higher liveness confidence
        /// </summary>
        private decimal CalculateLivenessScore(Amazon.Rekognition.Model.FaceDetail faceDetail)
        {
            var score = 0.0m;

            // Face quality contributes to liveness
            if (faceDetail.Quality != null)
            {
                score += (decimal)faceDetail.Quality.Brightness * 0.3m;
                score += (decimal)faceDetail.Quality.Sharpness * 0.3m;
            }

            // Face confidence
            score += 40.0m; // Base score for face detection

            // Additional factors
            if (faceDetail.EyesOpen != null && faceDetail.EyesOpen.Confidence > 80)
            {
                score += 10.0m;
            }

            if (faceDetail.MouthOpen != null && faceDetail.MouthOpen.Confidence > 80)
            {
                score += 10.0m;
            }

            // Cap at 100
            return Math.Min(100.0m, score);
        }
    }
}

