using AeroHub_Mock.Entities;
using Microsoft.AspNetCore.Identity;

namespace AeroHub_Mock.Services;

/// <summary>
/// DEV ONLY. The links/codes that Identity would normally email are written to the
/// application log instead, so the confirm-email and password-reset flows are
/// testable without a real mail server. Replace with a real SMTP/SendGrid sender before production.
/// </summary>
public class DevEmailSender(ILogger<DevEmailSender> logger) : IEmailSender<User>
{
    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        logger.LogWarning("[DEV EMAIL] Confirm email for {Email}:\n{Link}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        logger.LogWarning("[DEV EMAIL] Password reset link for {Email}:\n{Link}", email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        logger.LogWarning("[DEV EMAIL] Password reset code for {Email}: {Code}", email, resetCode);
        return Task.CompletedTask;
    }
}
