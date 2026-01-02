using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services.Interfaces;
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

                // Update user verification status and profile from ID document if verified
                if (isVerified)
                {
                    // Use AsNoTracking and FirstOrDefaultAsync to avoid DbContext concurrency issues
                    var user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        user.VerificationStatus = UserVerificationStatus.IdentityVerified;
                        
                        // Extract and update user profile from ID document OCR text
                        var updatedFields = new List<string>();
                        
                        // Extract data from OCR text (extractedText contains all detected text)
                        if (!string.IsNullOrEmpty(extractedText))
                        {
                            // Try to extract ID number (usually a long numeric string)
                            var idNumberMatch = Regex.Match(extractedText, @"\b\d{6,20}\b");
                            if (idNumberMatch.Success && string.IsNullOrEmpty(user.IdNumber))
                            {
                                user.IdNumber = idNumberMatch.Value;
                                updatedFields.Add("IdNumber");
                            }
                            
                            // Try to extract date of birth (various formats: DD/MM/YYYY, DD-MM-YYYY, YYYY-MM-DD)
                            var datePatterns = new[]
                            {
                                @"\b(\d{1,2})[/-](\d{1,2})[/-](\d{4})\b",  // DD/MM/YYYY or DD-MM-YYYY
                                @"\b(\d{4})[/-](\d{1,2})[/-](\d{1,2})\b"   // YYYY-MM-DD or YYYY/MM/DD
                            };
                            
                            foreach (var pattern in datePatterns)
                            {
                                var dateMatch = Regex.Match(extractedText, pattern);
                                if (dateMatch.Success)
                                {
                                    try
                                    {
                                        int day, month, year;
                                        if (dateMatch.Groups.Count == 4)
                                        {
                                            if (pattern.Contains(@"(\d{4})")) // YYYY-MM-DD format
                                            {
                                                year = int.Parse(dateMatch.Groups[1].Value);
                                                month = int.Parse(dateMatch.Groups[2].Value);
                                                day = int.Parse(dateMatch.Groups[3].Value);
                                            }
                                            else // DD/MM/YYYY format
                                            {
                                                day = int.Parse(dateMatch.Groups[1].Value);
                                                month = int.Parse(dateMatch.Groups[2].Value);
                                                year = int.Parse(dateMatch.Groups[3].Value);
                                            }
                                            
                                            var dob = new DateTime(year, month, day);
                                            // Only update if date is reasonable (between 1900 and today, and user is at least 18)
                                            if (dob.Year >= 1900 && dob <= DateTime.UtcNow.AddYears(-18))
                                            {
                                                if (!user.DateOfBirth.HasValue)
                                                {
                                                    user.DateOfBirth = dob;
                                                    updatedFields.Add("DateOfBirth");
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Invalid date, continue
                                    }
                                }
                            }
                        }
                        
                        // Note: firstName, lastName, and gender are harder to extract reliably from OCR
                        // These would typically require more sophisticated parsing or manual review
                        // For now, we only update fields that can be reliably extracted (IdNumber, DateOfBirth)
                        
                        user.UpdatedAt = DateTime.UtcNow;
                        
                        if (updatedFields.Any())
                        {
                            _logger.LogInformation("Updated user profile from ID document OCR for user {UserId}. Fields updated: {Fields}",
                                userId, string.Join(", ", updatedFields));
                        }
                        
                        // Use Update() to mark the entity as modified since we used AsNoTracking()
                        _context.Users.Update(user);
                    }
                }

                // Save changes for both UserIdentityDocument and User (if updated)
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
                // Removed CanConnectAsync() check - it causes DbContext thread safety issues
                // The query will fail gracefully if the database is unavailable
                // Use AsNoTracking to avoid DbContext concurrency issues
                
                var document = await _context.UserIdentityDocuments
                    .AsNoTracking()
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
                    VerificationStatus = document.VerificationStatus ?? "not_started",
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
                _logger.LogError(ex, "Error retrieving verification status for user {UserId}. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                    userId, ex.GetType().Name, ex.Message, ex.StackTrace);
                
                // Return a safe default instead of throwing to prevent 500 errors
                return new IdentityVerificationStatusDto
                {
                    VerificationStatus = "not_started"
                };
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

