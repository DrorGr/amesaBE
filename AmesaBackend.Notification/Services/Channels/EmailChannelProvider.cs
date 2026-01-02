using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Notification.Services.Interfaces;
using AmesaBackend.Shared.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AmesaBackend.Notification.Services.Channels
{
    public class EmailChannelProvider : IChannelProvider
    {
        public string ChannelName => NotificationChannelConstants.Email;

        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailChannelProvider> _logger;
        private readonly NotificationDbContext _context;
        private readonly IHttpRequest _httpRequest;
        private readonly ITemplateEngine _templateEngine;

        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _region;
        private readonly string? _configurationSet;

        public EmailChannelProvider(
            IAmazonSimpleEmailService sesClient,
            IConfiguration configuration,
            ILogger<EmailChannelProvider> logger,
            NotificationDbContext context,
            IHttpRequest httpRequest,
            ITemplateEngine templateEngine)
        {
            _sesClient = sesClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _httpRequest = httpRequest;
            _templateEngine = templateEngine;

            var emailConfig = _configuration.GetSection("NotificationChannels:Email");
            _fromEmail = emailConfig["FromEmail"] ?? "noreply@amesa.com";
            _fromName = emailConfig["FromName"] ?? "Amesa Lottery";
            _region = emailConfig["Region"] ?? "eu-north-1";
            _configurationSet = emailConfig["ConfigurationSet"];
        }

        public async Task<DeliveryResult> SendAsync(NotificationRequest request)
        {
            try
            {
                // Fetch user email from Auth service
                var userData = await _httpRequest.GetRequest<Dictionary<string, object>>(
                    $"{_configuration["Services:AuthService:Url"]}/api/v1/users/{request.UserId}",
                    _configuration["JwtSettings:SecretKey"] ?? "");

                if (userData == null || !userData.ContainsKey("email"))
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "User email not found"
                    };
                }

                var userEmail = userData["email"]?.ToString();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "User email is empty"
                    };
                }

                // Render template if provided
                string subject = request.Title;
                string body = request.Message;

                if (!string.IsNullOrEmpty(request.TemplateName))
                {
                    body = await _templateEngine.RenderTemplateAsync(
                        request.TemplateName,
                        request.Language,
                        NotificationChannelConstants.Email,
                        request.TemplateVariables ?? new Dictionary<string, object>());
                }

                // Create SES send request
                var sendRequest = new SendEmailRequest
                {
                    Source = $"{_fromName} <{_fromEmail}>",
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { userEmail }
                    },
                    Message = new Message
                    {
                        Subject = new Content(subject),
                        Body = new Body
                        {
                            Html = new Content(body),
                            Text = new Content(StripHtml(body))
                        }
                    }
                };

                if (!string.IsNullOrEmpty(_configurationSet))
                {
                    sendRequest.ConfigurationSetName = _configurationSet;
                }

                // Send email via AWS SES
                var response = await _sesClient.SendEmailAsync(sendRequest);

                return new DeliveryResult
                {
                    Success = true,
                    ExternalId = response.MessageId,
                    Cost = 0.0001m // Approximate cost per email (SES pricing)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification for user {UserId}", request.UserId);
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<bool> ValidatePreferencesAsync(Guid userId, NotificationPreferences preferences)
        {
            // Email channel always valid
            return Task.FromResult(true);
        }

        public bool IsChannelEnabled(Guid userId)
        {
            // Check user channel preferences
            var preference = _context.UserChannelPreferences
                .FirstOrDefault(p => p.UserId == userId && p.Channel == NotificationChannelConstants.Email);
            
            return preference == null || preference.Enabled;
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Simple HTML stripping - remove tags
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
            // Decode HTML entities
            text = text.Replace("&nbsp;", " ")
                      .Replace("&amp;", "&")
                      .Replace("&lt;", "<")
                      .Replace("&gt;", ">")
                      .Replace("&quot;", "\"")
                      .Replace("&#39;", "'");
            return text;
        }
    }
}

