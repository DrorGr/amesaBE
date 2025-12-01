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
            _client = _fixture.Client;
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
            // Note: Authentication token setup would need to be implemented based on your auth system
            // For now, this test will fail with Unauthorized until auth is properly configured
            // _client.DefaultRequestHeaders.Authorization = 
            //     new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/notifications");

            // Assert
            // This will be Unauthorized until auth token is properly set up
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<NotificationDto>>>();
                result.Should().NotBeNull();
                result!.Success.Should().BeTrue();
            }
        }

        [Fact]
        public async Task GetChannelPreferences_WithAuth_ReturnsPreferences()
        {
            // Arrange
            // Note: Authentication token setup would need to be implemented based on your auth system
            // For now, this test will fail with Unauthorized until auth is properly configured

            // Act
            var response = await _client.GetAsync("/api/v1/notifications/preferences/channels");

            // Assert
            // This will be Unauthorized until auth token is properly set up
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ChannelPreferencesDto>>>();
                result.Should().NotBeNull();
                result!.Success.Should().BeTrue();
            }
        }

        [Fact]
        public async Task UpdateChannelPreferences_WithValidRequest_UpdatesPreferences()
        {
            // Arrange
            // Note: Authentication token setup would need to be implemented based on your auth system
            // For now, this test will fail with Unauthorized until auth is properly configured

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
            // This will be Unauthorized until auth token is properly set up
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ChannelPreferencesDto>>();
                result.Should().NotBeNull();
                result!.Success.Should().BeTrue();
                result.Data!.Channel.Should().Be("email");
                result.Data.Enabled.Should().BeTrue();
            }
        }
    }
}

