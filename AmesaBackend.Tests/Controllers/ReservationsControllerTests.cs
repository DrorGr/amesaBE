using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Tests.Controllers
{
    public class ReservationsControllerTests
    {
        private readonly Mock<ITicketReservationService> _mockReservationService;
        private readonly Mock<IRedisInventoryManager> _mockInventoryManager;
        private readonly Mock<ILogger<ReservationsController>> _mockLogger;
        private readonly ReservationsController _controller;

        public ReservationsControllerTests()
        {
            _mockReservationService = new Mock<ITicketReservationService>();
            _mockInventoryManager = new Mock<IRedisInventoryManager>();
            _mockLogger = new Mock<ILogger<ReservationsController>>();

            _controller = new ReservationsController(
                _mockReservationService.Object,
                _mockInventoryManager.Object,
                _mockLogger.Object
            );

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "Test");
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.Controllers.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        [Fact]
        public async Task CreateReservation_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var request = new CreateReservationRequest
            {
                Quantity = 5,
                PaymentMethodId = Guid.NewGuid()
            };

            var reservation = new ReservationDto
            {
                Id = Guid.NewGuid(),
                HouseId = houseId,
                UserId = Guid.NewGuid(),
                Quantity = 5,
                TotalPrice = 250,
                Status = "pending",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockReservationService.Setup(s => s.CreateReservationAsync(
                request, 
                houseId, 
                It.IsAny<Guid>()))
                .ReturnsAsync(reservation);

            // Act
            var result = await _controller.CreateReservation(request, houseId);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateReservation_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var request = new CreateReservationRequest
            {
                Quantity = 5
            };

            _mockReservationService.Setup(s => s.CreateReservationAsync(
                request, 
                houseId, 
                It.IsAny<Guid>()))
                .ThrowsAsync(new InvalidOperationException("Insufficient tickets"));

            // Act
            var result = await _controller.CreateReservation(request, houseId);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetReservation_WithValidId_ReturnsOk()
        {
            // Arrange
            var reservationId = Guid.NewGuid();
            var reservation = new ReservationDto
            {
                Id = reservationId,
                HouseId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Quantity = 5,
                Status = "pending"
            };

            _mockReservationService.Setup(s => s.GetReservationAsync(
                reservationId, 
                It.IsAny<Guid>()))
                .ReturnsAsync(reservation);

            // Act
            var result = await _controller.GetReservation(reservationId);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetReservation_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var reservationId = Guid.NewGuid();

            _mockReservationService.Setup(s => s.GetReservationAsync(
                reservationId, 
                It.IsAny<Guid>()))
                .ReturnsAsync((ReservationDto?)null);

            // Act
            var result = await _controller.GetReservation(reservationId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CancelReservation_WithValidId_ReturnsOk()
        {
            // Arrange
            var reservationId = Guid.NewGuid();

            _mockReservationService.Setup(s => s.CancelReservationAsync(
                reservationId, 
                It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CancelReservation(reservationId);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetUserReservations_ReturnsList()
        {
            // Arrange
            var reservations = new List<ReservationDto>
            {
                new ReservationDto
                {
                    Id = Guid.NewGuid(),
                    HouseId = Guid.NewGuid(),
                    Status = "pending"
                }
            };

            _mockReservationService.Setup(s => s.GetUserReservationsAsync(
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ReturnsAsync(reservations);

            // Act
            var result = await _controller.GetUserReservations();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
        }
    }
}





