using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Services;

public class EmailSettings
{
    public const string SectionName = "Email";
    public string Host        { get; init; } = string.Empty;
    public int    Port        { get; init; } = 587;
    public bool   EnableSsl   { get; init; } = true;
    public string Username    { get; init; } = string.Empty;
    public string Password    { get; init; } = string.Empty;
    public string FromName    { get; init; } = "TaskFlow";
    public string FromAddress { get; init; } = string.Empty;
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _log;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> log)
    { _settings = settings.Value; _log = log; }

    public Task SendEmailVerificationAsync(string toEmail, string fullName, string token)
    {
        var link = $"https://taskflow.com/verify-email?token={token}";
        return SendAsync(toEmail, "Verify Your Email – TaskFlow", $@"
            <h2>Hi {fullName},</h2>
            <p>Click the button below to verify your email address:</p>
            <a href='{link}' style='background:#6366F1;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;'>
                Verify Email
            </a>
            <p>This link expires in 24 hours.</p>
            <p>If you didn't create a TaskFlow account, please ignore this email.</p>");
    }

    public Task SendPasswordResetAsync(string toEmail, string fullName, string token)
    {
        var link = $"https://taskflow.com/reset-password?token={token}";
        return SendAsync(toEmail, "Reset Your Password – TaskFlow", $@"
            <h2>Hi {fullName},</h2>
            <p>You requested a password reset. Click the button below:</p>
            <a href='{link}' style='background:#6366F1;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;'>
                Reset Password
            </a>
            <p>This link expires in 1 hour.</p>
            <p>If you didn't request this, please ignore and your password will remain unchanged.</p>");
    }

    public Task SendWelcomeEmailAsync(string toEmail, string fullName)
        => SendAsync(toEmail, "Welcome to TaskFlow! 🚀", $@"
            <h2>Welcome, {fullName}!</h2>
            <p>Your TaskFlow account is ready. Start managing your projects today.</p>
            <a href='https://taskflow.com/dashboard' style='background:#6366F1;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;'>
                Go to Dashboard
            </a>");

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;
            msg.Body = new TextPart("html")
            {
                Text = $@"<!DOCTYPE html>
<html><body style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;padding:40px 20px;color:#1F2937;'>
{htmlBody}
<hr style='margin:40px 0;border:none;border-top:1px solid #E5E7EB;'/>
<p style='color:#9CA3AF;font-size:12px;'>© 2025 TaskFlow. All rights reserved.</p>
</body></html>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host, _settings.Port,
                _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);

            _log.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
        }
    }
}
