using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;

namespace AmesaBackend.Tests.Services;

public class LotteryServiceTests
{
    private readonly Mock<ILogger<LotteryService>> _loggerMock;
    private readonly LotteryService _service;

    public LotteryServiceTests()
    {
        _loggerMock = new Mock<ILogger<LotteryService>>();
        _service = new LotteryService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act & Assert
        _service.Should().NotBeNull();
    }
}
