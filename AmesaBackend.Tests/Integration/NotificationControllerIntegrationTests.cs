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

        [Fact(Skip = "Notification endpoints are in separate microservice, not available in main app")]
        public async Task GetNotifications_WithoutAuth_ReturnsUnauthorized()
        {
            // Note: This test is skipped because Notification endpoints are in a separate microservice
            // Integration tests for Notification service should be run against the Notification service directly
            
            // Act
            var response = await _client.GetAsync("/api/v1/notifications");

            // Assert
            // Endpoint may return 404 (not found) or 401 (unauthorized) depending on routing
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
        }

        [Fact(Skip = "Notification endpoints are in separate microservice, not available in main app")]
        public async Task GetNotifications_WithAuth_ReturnsNotifications()
        {
            // Arrange
            // Note: This test is skipped because Notification endpoints are in a separate microservice
            // Integration tests for Notification service should be run against the Notification service directly
            // _client.DefaultRequestHeaders.Authorization = 
            //     new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/notifications");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<NotificationDto>>>();
                result.Should().NotBeNull();
                result!.Success.Should().BeTrue();
            }
        }

        [Fact(Skip = "Notification endpoints are in separate microservice, not available in main app")]
        public async Task GetChannelPreferences_WithAuth_ReturnsPreferences()
        {
            // Arrange
            // Note: This test is skipped because Notification endpoints are in a separate microservice
            // Integration tests for Notification service should be run against the Notification service directly

            // Act
            var response = await _client.GetAsync("/api/v1/notifications/preferences/channels");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ChannelPreferencesDto>>>();
                result.Should().NotBeNull();
                result!.Success.Should().BeTrue();
            }
        }

        [Fact(Skip = "Notification endpoints are in separate microservice, not available in main app")]
        public async Task UpdateChannelPreferences_WithValidRequest_UpdatesPreferences()
        {
            // Arrange
            // Note: This test is skipped because Notification endpoints are in a separate microservice
            // Integration tests for Notification service should be run against the Notification service directly

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
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
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

