using Xunit;
using FluentAssertions;
using AmesaBackend.Lottery.Hubs;

namespace AmesaBackend.Tests.Hubs;

/// <summary>
/// Tests for LotteryHub
/// Note: Full SignalR hub testing requires a SignalR test host which is complex to set up.
/// These tests verify the hub class structure and method signatures.
/// </summary>
public class LotteryHubTests
{
    [Fact]
    public void LotteryHub_ShouldExist()
    {
        // Arrange & Act
        var hubType = typeof(LotteryHub);

        // Assert
        hubType.Should().NotBeNull();
        hubType.Name.Should().Be("LotteryHub");
    }

    [Fact]
    public void LotteryHub_ShouldHaveJoinLotteryGroupMethod()
    {
        // Arrange
        var hubType = typeof(LotteryHub);

        // Act
        var method = hubType.GetMethod("JoinLotteryGroup");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void LotteryHub_ShouldHaveLeaveLotteryGroupMethod()
    {
        // Arrange
        var hubType = typeof(LotteryHub);

        // Act
        var method = hubType.GetMethod("LeaveLotteryGroup");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void LotteryHubExtensions_ShouldExist()
    {
        // Arrange & Act
        var extensionsType = typeof(LotteryHubExtensions);

        // Assert
        extensionsType.Should().NotBeNull();
        extensionsType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void LotteryHubExtensions_ShouldHaveBroadcastMethods()
    {
        // Arrange
        var extensionsType = typeof(LotteryHubExtensions);

        // Act
        var methods = extensionsType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        methods.Should().Contain(m => m.Name == "BroadcastInventoryUpdate");
        methods.Should().Contain(m => m.Name == "BroadcastCountdownUpdate");
        methods.Should().Contain(m => m.Name == "BroadcastReservationStatus");
        methods.Should().Contain(m => m.Name == "BroadcastFavoriteUpdate");
        methods.Should().Contain(m => m.Name == "BroadcastDrawStarted");
        methods.Should().Contain(m => m.Name == "BroadcastDrawCompleted");
    }
}
