using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Controllers;

public class GamificationControllerNullServiceTests
{
    private readonly Mock<ILogger<GamificationController>> _loggerMock;

    public GamificationControllerNullServiceTests()
    {
        _loggerMock = new Mock<ILogger<GamificationController>>();
    }

    [Fact]
    public async Task GetGamificationData_ShouldReturn503_WhenServiceIsNull()
    {
        // Arrange
        var controller = new GamificationController(null!, _loggerMock.Object);
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(controller, Guid.NewGuid());

        // Act
        var result = await controller.GetGamificationData();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }
}
