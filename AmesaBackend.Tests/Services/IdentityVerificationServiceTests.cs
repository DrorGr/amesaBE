extern alias AuthApp;
using AuthApp::AmesaBackend.Auth.Services;
using AuthApp::AmesaBackend.Auth.Data;
using AuthApp::AmesaBackend.Auth.DTOs;
using AuthApp::AmesaBackend.Auth.Models;
using AuthUser = AuthApp::AmesaBackend.Auth.Models.User;
using AuthUserStatus = AuthApp::AmesaBackend.Auth.Models.UserStatus;
using AuthUserVerificationStatus = AuthApp::AmesaBackend.Auth.Models.UserVerificationStatus;
using Xunit;
using Moq;
using FluentAssertions;
using AmesaBackend.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace AmesaBackend.Tests.Services
{
    public class IdentityVerificationServiceTests
    {
        private readonly Mock<IAwsRekognitionService> _mockRekognitionService;
        private readonly Mock<ILogger<IdentityVerificationService>> _mockLogger;
        private readonly AuthDbContext _context;
        private readonly IdentityVerificationService _service;

        public IdentityVerificationServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _context = new AuthDbContext(options);

            _mockRekognitionService = new Mock<IAwsRekognitionService>();
            
            _mockLogger = new Mock<ILogger<IdentityVerificationService>>();

            _service = new IdentityVerificationService(
                _context,
                _mockRekognitionService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task VerifyIdentityAsync_WithValidImages_ReturnsVerified()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new AuthUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Status = AuthUserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new VerifyIdentityRequest
            {
                IdFrontImage = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                SelfieImage = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
                DocumentType = "passport",
                DocumentNumber = "P123456"
            };

            // Mock successful face detection (selfie)
            var selfieFaceDetail = new FaceDetail
            {
                Confidence = 99.5f,
                Quality = new ImageQuality { Brightness = 90f, Sharpness = 95f },
                EyesOpen = new EyeOpen { Confidence = 95f, Value = true },
                MouthOpen = new MouthOpen { Confidence = 20f, Value = false }
            };
            var selfieResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { selfieFaceDetail }
            };

            // Mock successful face detection (ID)
            var idFaceDetail = new FaceDetail
            {
                Confidence = 98.5f,
                Quality = new ImageQuality { Brightness = 88f, Sharpness = 92f }
            };
            var idResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { idFaceDetail }
            };

            // Mock successful face comparison
            var faceMatch = new CompareFacesMatch
            {
                Similarity = 95.5f,
                Face = new ComparedFace { Confidence = 99f }
            };
            var compareResponse = new CompareFacesResponse
            {
                FaceMatches = new List<CompareFacesMatch> { faceMatch }
            };

            // Mock text detection
            var textResponse = new DetectTextResponse
            {
                TextDetections = new List<TextDetection>
                {
                    new TextDetection { DetectedText = "P123456", Confidence = 99f }
                }
            };

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync((byte[] bytes) => bytes.Length == 3 ? selfieResponse : idResponse);

            _mockRekognitionService
                .Setup(x => x.CompareFacesAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .ReturnsAsync(compareResponse);

            _mockRekognitionService
                .Setup(x => x.DetectTextAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(textResponse);

            // Act
            var result = await _service.VerifyIdentityAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsVerified.Should().BeTrue();
            result.VerificationStatus.Should().Be("verified");
            result.ValidationKey.Should().NotBeEmpty();
            result.LivenessScore.Should().BeGreaterThan(80m);
            result.FaceMatchScore.Should().BeGreaterThan(90m);
            result.RejectionReason.Should().BeNull();

            // Verify database was updated
            var document = await _context.UserIdentityDocuments
                .FirstOrDefaultAsync(d => d.UserId == userId);
            document.Should().NotBeNull();
            document!.VerificationStatus.Should().Be("verified");
            document.ValidationKey.Should().NotBeEmpty();

            // Verify user status was updated
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser!.VerificationStatus.Should().Be(AuthUserVerificationStatus.IdentityVerified);
        }

        [Fact]
        public async Task VerifyIdentityAsync_WithNoFaceInSelfie_ReturnsRejected()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new AuthUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Status = AuthUserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new VerifyIdentityRequest
            {
                IdFrontImage = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                SelfieImage = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
                DocumentType = "passport"
            };

            // Mock no face detected in selfie
            var selfieResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail>()
            };

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.Is<byte[]>(b => b.Length == 3)))
                .ReturnsAsync(selfieResponse);

            // Act
            var result = await _service.VerifyIdentityAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsVerified.Should().BeFalse();
            result.VerificationStatus.Should().Be("rejected");
            result.RejectionReason.Should().Contain("No face detected in selfie");
            result.ValidationKey.Should().NotBeEmpty();
        }

        [Fact]
        public async Task VerifyIdentityAsync_WithNoFaceInId_ReturnsRejected()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new AuthUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Status = AuthUserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new VerifyIdentityRequest
            {
                IdFrontImage = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }), // Different length for ID
                SelfieImage = Convert.ToBase64String(new byte[] { 4, 5, 6 }), // Length 3 for selfie
                DocumentType = "passport"
            };

            // Mock face in selfie but not in ID
            var selfieFaceDetail = new FaceDetail { Confidence = 99f };
            var selfieResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { selfieFaceDetail }
            };

            var idResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail>()
            };

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.Is<byte[]>(b => b.Length == 3)))
                .ReturnsAsync(selfieResponse);

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.Is<byte[]>(b => b.Length != 3)))
                .ReturnsAsync(idResponse);

            // Act
            var result = await _service.VerifyIdentityAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsVerified.Should().BeFalse();
            result.VerificationStatus.Should().Be("rejected");
            result.RejectionReason.Should().Contain("No face detected in ID document");
        }

        [Fact]
        public async Task VerifyIdentityAsync_WithLowLivenessScore_ReturnsRejected()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new AuthUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Status = AuthUserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new VerifyIdentityRequest
            {
                IdFrontImage = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                SelfieImage = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
                DocumentType = "passport"
            };

            // Mock low quality face (low liveness score)
            var selfieFaceDetail = new FaceDetail
            {
                Confidence = 50f,
                Quality = new ImageQuality { Brightness = 30f, Sharpness = 25f },
                EyesOpen = new EyeOpen { Confidence = 50f, Value = true }
            };
            var selfieResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { selfieFaceDetail }
            };

            var idFaceDetail = new FaceDetail { Confidence = 98f };
            var idResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { idFaceDetail }
            };

            var compareResponse = new CompareFacesResponse
            {
                FaceMatches = new List<CompareFacesMatch>
                {
                    new CompareFacesMatch { Similarity = 95f }
                }
            };

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.Is<byte[]>(b => b.Length == 3)))
                .ReturnsAsync(selfieResponse);

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.Is<byte[]>(b => b.Length != 3)))
                .ReturnsAsync(idResponse);

            _mockRekognitionService
                .Setup(x => x.CompareFacesAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .ReturnsAsync(compareResponse);

            _mockRekognitionService
                .Setup(x => x.DetectTextAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(new DetectTextResponse { TextDetections = new List<TextDetection>() });

            // Act
            var result = await _service.VerifyIdentityAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsVerified.Should().BeFalse();
            result.VerificationStatus.Should().Be("rejected");
            result.LivenessScore.Should().BeLessThan(80m);
            result.RejectionReason.Should().NotBeNull();
        }

        [Fact]
        public async Task VerifyIdentityAsync_WithLowFaceMatchScore_ReturnsRejected()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new AuthUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Status = AuthUserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new VerifyIdentityRequest
            {
                IdFrontImage = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                SelfieImage = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
                DocumentType = "passport"
            };

            // Mock good quality faces but low match
            var selfieFaceDetail = new FaceDetail
            {
                Confidence = 99f,
                Quality = new ImageQuality { Brightness = 90f, Sharpness = 95f },
                EyesOpen = new EyeOpen { Confidence = 95f, Value = true }
            };
            var selfieResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { selfieFaceDetail }
            };

            var idFaceDetail = new FaceDetail { Confidence = 98f };
            var idResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { idFaceDetail }
            };

            // Low similarity (below 90% threshold)
            var compareResponse = new CompareFacesResponse
            {
                FaceMatches = new List<CompareFacesMatch>
                {
                    new CompareFacesMatch { Similarity = 75f }
                }
            };

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync((byte[] bytes) => bytes.Length == 3 ? selfieResponse : idResponse);

            _mockRekognitionService
                .Setup(x => x.CompareFacesAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .ReturnsAsync(compareResponse);

            _mockRekognitionService
                .Setup(x => x.DetectTextAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(new DetectTextResponse { TextDetections = new List<TextDetection>() });

            // Act
            var result = await _service.VerifyIdentityAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsVerified.Should().BeFalse();
            result.VerificationStatus.Should().Be("rejected");
            result.FaceMatchScore.Should().BeLessThan(90m);
            result.RejectionReason.Should().NotBeNull();
        }

        [Fact]
        public async Task GetVerificationStatusAsync_WithExistingDocument_ReturnsStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var validationKey = Guid.NewGuid();
            var document = new UserIdentityDocument
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DocumentType = "passport",
                DocumentNumber = "P123456",
                ValidationKey = validationKey,
                VerificationStatus = "verified",
                LivenessScore = 95m,
                FaceMatchScore = 98m,
                VerificationAttempts = 1,
                LastVerificationAttempt = DateTime.UtcNow,
                VerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserIdentityDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetVerificationStatusAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.VerificationStatus.Should().Be("verified");
            result.ValidationKey.Should().Be(validationKey);
            result.LivenessScore.Should().Be(95m);
            result.FaceMatchScore.Should().Be(98m);
            result.VerificationAttempts.Should().Be(1);
        }

        [Fact]
        public async Task GetVerificationStatusAsync_WithNoDocument_ReturnsNotStarted()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _service.GetVerificationStatusAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.VerificationStatus.Should().Be("not_started");
            result.ValidationKey.Should().BeNull();
        }

        [Fact]
        public async Task RetryVerificationAsync_CallsVerifyIdentityAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new AuthUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Status = AuthUserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new VerifyIdentityRequest
            {
                IdFrontImage = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                SelfieImage = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
                DocumentType = "passport"
            };

            // Mock successful verification
            var selfieFaceDetail = new FaceDetail
            {
                Confidence = 99f,
                Quality = new ImageQuality { Brightness = 90f, Sharpness = 95f },
                EyesOpen = new EyeOpen { Confidence = 95f, Value = true }
            };
            var selfieResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { selfieFaceDetail }
            };

            var idResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { new FaceDetail { Confidence = 98f } }
            };

            var compareResponse = new CompareFacesResponse
            {
                FaceMatches = new List<CompareFacesMatch>
                {
                    new CompareFacesMatch { Similarity = 95f }
                }
            };

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync((byte[] bytes) => bytes.Length == 3 ? selfieResponse : idResponse);

            _mockRekognitionService
                .Setup(x => x.CompareFacesAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .ReturnsAsync(compareResponse);

            _mockRekognitionService
                .Setup(x => x.DetectTextAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(new DetectTextResponse { TextDetections = new List<TextDetection>() });

            // Act
            var result = await _service.RetryVerificationAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            // Retry should work the same as initial verification
            _mockRekognitionService.Verify(x => x.DetectFacesAsync(It.IsAny<byte[]>()), Times.AtLeast(2));
            _mockRekognitionService.Verify(x => x.CompareFacesAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public async Task VerifyIdentityAsync_UpdatesExistingDocument_WhenDocumentExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new AuthUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Status = AuthUserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);

            var existingDocument = new UserIdentityDocument
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DocumentType = "id_card",
                DocumentNumber = "OLD123",
                ValidationKey = Guid.NewGuid(),
                VerificationStatus = "rejected",
                VerificationAttempts = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _context.UserIdentityDocuments.Add(existingDocument);
            await _context.SaveChangesAsync();

            var request = new VerifyIdentityRequest
            {
                IdFrontImage = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                SelfieImage = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
                DocumentType = "passport",
                DocumentNumber = "NEW456"
            };

            // Mock successful verification
            var selfieFaceDetail = new FaceDetail
            {
                Confidence = 99f,
                Quality = new ImageQuality { Brightness = 90f, Sharpness = 95f },
                EyesOpen = new EyeOpen { Confidence = 95f, Value = true }
            };
            var selfieResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { selfieFaceDetail }
            };

            var idResponse = new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail> { new FaceDetail { Confidence = 98f } }
            };

            var compareResponse = new CompareFacesResponse
            {
                FaceMatches = new List<CompareFacesMatch>
                {
                    new CompareFacesMatch { Similarity = 95f }
                }
            };

            _mockRekognitionService
                .Setup(x => x.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync((byte[] bytes) => bytes.Length == 3 ? selfieResponse : idResponse);

            _mockRekognitionService
                .Setup(x => x.CompareFacesAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .ReturnsAsync(compareResponse);

            _mockRekognitionService
                .Setup(x => x.DetectTextAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(new DetectTextResponse { TextDetections = new List<TextDetection>() });

            // Act
            var result = await _service.VerifyIdentityAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsVerified.Should().BeTrue();

            // Verify document was updated, not created
            var documents = await _context.UserIdentityDocuments
                .Where(d => d.UserId == userId)
                .ToListAsync();
            documents.Should().HaveCount(1);
            documents[0].DocumentType.Should().Be("passport");
            documents[0].VerificationAttempts.Should().Be(2); // Incremented from 1
            documents[0].VerificationStatus.Should().Be("verified");
        }
    }
}
