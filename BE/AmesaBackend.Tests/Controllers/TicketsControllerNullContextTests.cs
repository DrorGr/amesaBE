using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Controllers;

public class TicketsControllerNullContextTests
{
    private readonly Mock<ILogger<TicketsController>> _loggerMock;

    public TicketsControllerNullContextTests()
    {
        _loggerMock = new Mock<ILogger<TicketsController>>();
    }

    [Fact]
    public async Task GetActiveTickets_ShouldReturn503_WhenContextIsNull()
    {
        // Arrange
        var controller = new TicketsController(null!, _loggerMock.Object);
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(controller, Guid.NewGuid());

        // Act
        var result = await controller.GetActiveTickets();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetTicketAnalytics_ShouldReturn503_WhenContextIsNull()
    {
        // Arrange
        var controller = new TicketsController(null!, _loggerMock.Object);
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(controller, Guid.NewGuid());

        // Act
        var result = await controller.GetTicketAnalytics();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }
}
