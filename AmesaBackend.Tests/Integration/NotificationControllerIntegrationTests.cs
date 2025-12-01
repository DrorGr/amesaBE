using Xunit;
using FluentAssertions;
using AmesaBackend.Tests.TestFixtures;
using AmesaBackend.Notification.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AmesaBackend.Tests.Integration
{
    public class NotificationControllerIntegrationTests : IClassFixture<WebApplicationFixture>
    {
        private readonly WebApplicationFixture _fixture;
        private readonly HttpClient _client;

        public NotificationControllerIntegrationTests(WebApplicationFixture fixture)
        {
            _fixture = fixture;
            _client = _fixture.CreateClient();
        }

        [Fact]
        public async Task GetNotifications_WithoutAuth_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/notifications");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetNotifications_WithAuth_ReturnsNotifications()
        {
            // Arrange
            var token = await _fixture.GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/notifications");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<NotificationDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetChannelPreferences_WithAuth_ReturnsPreferences()
        {
            // Arrange
            var token = await _fixture.GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/notifications/preferences/channels");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ChannelPreferencesDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateChannelPreferences_WithValidRequest_UpdatesPreferences()
        {
            // Arrange
            var token = await _fixture.GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new UpdateChannelPreferencesRequest
            {
                Channel = "email",
                Enabled = true,
                QuietHoursStart = TimeSpan.Parse("22:00:00"),
                QuietHoursEnd = TimeSpan.Parse("08:00:00")
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/notifications/preferences/channels", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ChannelPreferencesDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Channel.Should().Be("email");
            result.Data.Enabled.Should().BeTrue();
        }
    }
}

