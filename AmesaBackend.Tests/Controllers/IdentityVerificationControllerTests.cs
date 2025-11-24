using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;
using AmesaBackend.Auth.Controllers;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.DTOs;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System;
using System.Threading.Tasks;

namespace AmesaBackend.Tests.Controllers
{
    public class IdentityVerificationControllerTests
    {
        private readonly Mock<IIdentityVerificationService> _mockVerificationService;
        private readonly Mock<ILogger<IdentityVerificationController>> _mockLogger;
        private readonly IdentityVerificationController _controller;

        public IdentityVerificationControllerTests()
        {
            _mockVerificationService = new Mock<IIdentityVerificationService>();
            _mockLogger = new Mock<ILogger<IdentityVerificationController>>();
            _controller = new IdentityVerificationController(
                _mockVerificationService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task VerifyIdentity_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new VerifyIdentityRequest
            {
                IdFrontImage = "base64image1",
                SelfieImage = "base64image2",
                DocumentType = "passport",
                DocumentNumber = "P123456"
            };

            var verificationResult = new IdentityVerificationResult
            {
                IsVerified = true,
                ValidationKey = Guid.NewGuid(),
                LivenessScore = 95m,
                FaceMatchScore = 98m,
                VerificationStatus = "verified"
            };

            _mockVerificationService
                .Setup(x => x.VerifyIdentityAsync(It.IsAny<Guid>(), It.IsAny<VerifyIdentityRequest>()))
                .ReturnsAsync(verificationResult);

            // Setup user claims
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims))
                }
            };

            // Act
            var result = await _controller.VerifyIdentity(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IdentityVerificationResult>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.IsVerified.Should().BeTrue();
            apiResponse.Message.Should().Be("Identity verified successfully");
            _mockVerificationService.Verify(x => x.VerifyIdentityAsync(userId, request), Times.Once);
        }

        [Fact]
        public async Task VerifyIdentity_WithRejectedVerification_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new VerifyIdentityRequest
            {
                IdFrontImage = "base64image1",
                SelfieImage = "base64image2",
                DocumentType = "passport"
            };

            var verificationResult = new IdentityVerificationResult
            {
                IsVerified = false,
                ValidationKey = Guid.NewGuid(),
                LivenessScore = 75m,
                FaceMatchScore = 85m,
                VerificationStatus = "rejected",
                RejectionReason = "Liveness score too low"
            };

            _mockVerificationService
                .Setup(x => x.VerifyIdentityAsync(It.IsAny<Guid>(), It.IsAny<VerifyIdentityRequest>()))
                .ReturnsAsync(verificationResult);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims))
                }
            };

            // Act
            var result = await _controller.VerifyIdentity(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<IdentityVerificationResult>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.IsVerified.Should().BeFalse();
            apiResponse.Message.Should().Be("Liveness score too low");
        }

        [Fact]
        public async Task VerifyIdentity_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new VerifyIdentityRequest
            {
                IdFrontImage = "base64image1",
                SelfieImage = "base64image2",
                DocumentType = "passport"
            };

            // No user claims set up

            // Act
            var result = await _controller.VerifyIdentity(request);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var apiResponse = unauthorizedResult.Value.Should().BeOfType<ApiResponse<IdentityVerificationResult>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("User not authenticated");
            _mockVerificationService.Verify(x => x.VerifyIdentityAsync(It.IsAny<Guid>(), It.IsAny<VerifyIdentityRequest>()), Times.Never);
        }

        [Fact]
        public async Task VerifyIdentity_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new VerifyIdentityRequest
            {
                IdFrontImage = "base64image1",
                SelfieImage = "base64image2",
                DocumentType = "passport"
            };

            _mockVerificationService
                .Setup(x => x.VerifyIdentityAsync(It.IsAny<Guid>(), It.IsAny<VerifyIdentityRequest>()))
                .ThrowsAsync(new Exception("Database error"));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims))
                }
            };

            // Act
            var result = await _controller.VerifyIdentity(request);

            // Assert
            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
            var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<IdentityVerificationResult>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error.Should().NotBeNull();
            apiResponse.Error!.Code.Should().Be("INTERNAL_ERROR");
        }

        [Fact]
        public async Task GetVerificationStatus_WithValidUser_ReturnsStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var statusDto = new IdentityVerificationStatusDto
            {
                ValidationKey = Guid.NewGuid(),
                VerificationStatus = "verified",
                LivenessScore = 95m,
                FaceMatchScore = 98m,
                VerificationAttempts = 1,
                VerifiedAt = DateTime.UtcNow
            };

            _mockVerificationService
                .Setup(x => x.GetVerificationStatusAsync(It.IsAny<Guid>()))
                .ReturnsAsync(statusDto);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims))
                }
            };

            // Act
            var result = await _controller.GetVerificationStatus();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IdentityVerificationStatusDto>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.VerificationStatus.Should().Be("verified");
            _mockVerificationService.Verify(x => x.GetVerificationStatusAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetVerificationStatus_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange - No user claims

            // Act
            var result = await _controller.GetVerificationStatus();

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var apiResponse = unauthorizedResult.Value.Should().BeOfType<ApiResponse<IdentityVerificationStatusDto>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("User not authenticated");
        }

        [Fact]
        public async Task RetryVerification_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new VerifyIdentityRequest
            {
                IdFrontImage = "base64image1",
                SelfieImage = "base64image2",
                DocumentType = "passport"
            };

            var verificationResult = new IdentityVerificationResult
            {
                IsVerified = true,
                ValidationKey = Guid.NewGuid(),
                VerificationStatus = "verified"
            };

            _mockVerificationService
                .Setup(x => x.RetryVerificationAsync(It.IsAny<Guid>(), It.IsAny<VerifyIdentityRequest>()))
                .ReturnsAsync(verificationResult);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims))
                }
            };

            // Act
            var result = await _controller.RetryVerification(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IdentityVerificationResult>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.IsVerified.Should().BeTrue();
            _mockVerificationService.Verify(x => x.RetryVerificationAsync(userId, request), Times.Once);
        }

        [Fact]
        public async Task RetryVerification_WithRejectedVerification_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new VerifyIdentityRequest
            {
                IdFrontImage = "base64image1",
                SelfieImage = "base64image2",
                DocumentType = "passport"
            };

            var verificationResult = new IdentityVerificationResult
            {
                IsVerified = false,
                ValidationKey = Guid.NewGuid(),
                VerificationStatus = "rejected",
                RejectionReason = "Face match failed"
            };

            _mockVerificationService
                .Setup(x => x.RetryVerificationAsync(It.IsAny<Guid>(), It.IsAny<VerifyIdentityRequest>()))
                .ReturnsAsync(verificationResult);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims))
                }
            };

            // Act
            var result = await _controller.RetryVerification(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<IdentityVerificationResult>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Data!.IsVerified.Should().BeFalse();
        }
    }
}

