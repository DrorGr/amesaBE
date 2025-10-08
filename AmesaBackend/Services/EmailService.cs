using MailKit.Net.Smtp;
using MimeKit;
using AmesaBackend.Data;
using AmesaBackend.Models;

namespace AmesaBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly AmesaDbContext _context;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            AmesaDbContext context,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        public async Task SendEmailVerificationAsync(string email, string token)
        {
            // Skip email verification in development mode
            if (_configuration.GetValue<bool>("EmailSettings:SkipEmailVerification"))
            {
                _logger.LogInformation("Skipping email verification for development. Email: {Email}, Token: {Token}", email, token);
                return;
            }

            var subject = "Verify Your Email Address - Amesa Lottery";
            var verificationUrl = $"{_configuration["FrontendUrl"]}/verify-email?token={token}";
            var body = $@"
                <h1>Welcome to Amesa Lottery!</h1>
                <p>Thank you for registering. Please click the link below to verify your email address:</p>
                <p><a href='{verificationUrl}' style='background-color: #3b82f6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p>{verificationUrl}</p>
                <p>This link will expire in 24 hours.</p>
                <p>Best regards,<br>The Amesa Team</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetAsync(string email, string token)
        {
            var subject = "Reset Your Password - Amesa Lottery";
            var resetUrl = $"{_configuration["FrontendUrl"]}/reset-password?token={token}";
            var body = $@"
                <h1>Password Reset Request</h1>
                <p>You requested to reset your password. Click the link below to create a new password:</p>
                <p><a href='{resetUrl}' style='background-color: #3b82f6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p>{resetUrl}</p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request this password reset, please ignore this email.</p>
                <p>Best regards,<br>The Amesa Team</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string email, string name)
        {
            var subject = "Welcome to Amesa Lottery!";
            var body = $@"
                <h1>Welcome to Amesa Lottery, {name}!</h1>
                <p>Your account has been successfully created and verified. You can now:</p>
                <ul>
                    <li>Browse available lottery properties</li>
                    <li>Purchase lottery tickets</li>
                    <li>Track your lottery history</li>
                    <li>Manage your profile and payment methods</li>
                </ul>
                <p>Start exploring our amazing properties and good luck!</p>
                <p><a href='{_configuration["FrontendUrl"]}' style='background-color: #3b82f6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Visit Amesa Lottery</a></p>
                <p>Best regards,<br>The Amesa Team</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendLotteryWinnerNotificationAsync(string email, string name, string houseTitle, string ticketNumber)
        {
            var subject = "🎉 Congratulations! You Won the Lottery!";
            var body = $@"
                <h1>🎉 Congratulations, {name}!</h1>
                <p>We have amazing news! You have won the lottery for:</p>
                <h2>{houseTitle}</h2>
                <p><strong>Winning Ticket Number:</strong> {ticketNumber}</p>
                <p>Our legal team will contact you within 24 hours to discuss the next steps for claiming your prize.</p>
                <p>Please ensure your contact information is up to date in your profile.</p>
                <p><a href='{_configuration["FrontendUrl"]}/member-settings' style='background-color: #3b82f6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Update Profile</a></p>
                <p>Congratulations again on your amazing win!</p>
                <p>Best regards,<br>The Amesa Team</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendLotteryEndedNotificationAsync(string email, string name, string houseTitle, string? winnerName)
        {
            var subject = "Lottery Ended - " + houseTitle;
            var body = $@"
                <h1>Lottery Results</h1>
                <p>Hello {name},</p>
                <p>The lottery for <strong>{houseTitle}</strong> has ended.</p>
                {(winnerName != null ? $"<p><strong>Winner:</strong> {winnerName}</p>" : "<p>Unfortunately, this lottery did not meet the minimum participation requirements and has been cancelled. All ticket purchases will be refunded.</p>")}
                <p>Thank you for participating!</p>
                <p><a href='{_configuration["FrontendUrl"]}' style='background-color: #3b82f6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>View Other Lotteries</a></p>
                <p>Best regards,<br>The Amesa Team</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendGeneralNotificationAsync(string email, string subject, string body)
        {
            await SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailAsync(string email, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:FromName"],
                    _configuration["EmailSettings:FromEmail"]));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _configuration["EmailSettings:SmtpServer"],
                    int.Parse(_configuration["EmailSettings:SmtpPort"]!),
                    MailKit.Security.SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(
                    _configuration["EmailSettings:SmtpUsername"],
                    _configuration["EmailSettings:SmtpPassword"]);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                throw;
            }
        }
    }
}
