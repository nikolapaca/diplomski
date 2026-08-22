using FakeTrello.Config;
using FakeTrello.Service.Contract;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FakeTrello.Service
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendConfirmationEmail(string email, string token)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _emailSettings.DisplayName,
                _emailSettings.Email));

            message.To.Add(MailboxAddress.Parse(email));

            message.Subject = "Confirm your FakeTrello account";

            var confirmationLink =
                $"http://localhost:4200/confirm-email?token={token}";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Welcome to FakeTrello!</h2>

                    <p>Thank you for registering.</p>

                    <p>Please click the button below to confirm your email address:</p>

                    <a href='{confirmationLink}'
                       style='
                            background:#2563eb;
                            color:white;
                            padding:12px 20px;
                            text-decoration:none;
                            border-radius:6px;'>
                        Confirm Email
                    </a>

                    <br/><br/>

                    <p>If you didn't create this account, you can safely ignore this email.</p>
                "
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _emailSettings.Host,
                _emailSettings.Port,
                 SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _emailSettings.Email,
                _emailSettings.Password);

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }

        public async Task SendPasswordResetEmail(string email, string token)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _emailSettings.DisplayName,
                _emailSettings.Email));

            message.To.Add(MailboxAddress.Parse(email));

            message.Subject = "Reset your FakeTrello password";

            var resetLink =
                $"http://localhost:4200/reset-password?token={token}";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Password reset requested</h2>

                    <p>We received a request to reset your FakeTrello password.</p>

                    <p>Click the button below to choose a new password. This link expires in 1 hour.</p>

                    <a href='{resetLink}'
                       style='
                            background:#2563eb;
                            color:white;
                            padding:12px 20px;
                            text-decoration:none;
                            border-radius:6px;'>
                        Reset Password
                    </a>

                    <br/><br/>

                    <p>If you didn't request this, you can safely ignore this email - your password won't change.</p>
                "
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _emailSettings.Host,
                _emailSettings.Port,
                 SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _emailSettings.Email,
                _emailSettings.Password);

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }
    }
}
