using Microsoft.AspNetCore.Identity;

namespace AIPromptManager.Services;

public interface IEmailSender<TUser> where TUser : class
{
    Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink);
    Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink);
    Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode);
}

public class EmailSender : IEmailSender<IdentityUser>
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink)
    {
        _logger.LogInformation("Email confirmation link would be sent to {Email}: {Link}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink)
    {
        _logger.LogInformation("Password reset link would be sent to {Email}: {Link}", email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode)
    {
        _logger.LogInformation("Password reset code would be sent to {Email}: {Code}", email, resetCode);
        return Task.CompletedTask;
    }
}