using System.Threading.Tasks;

namespace AIPromptManager.Services;

/// <summary>
/// Interface for email service operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body content (can be HTML or plain text)</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(string to, string subject, string body);
    
    /// <summary>
    /// Sends a password reset email using a template
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="resetLink">Password reset link</param>
    /// <param name="userName">User's name for personalization</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendPasswordResetEmailAsync(string to, string resetLink, string userName);
    
    /// <summary>
    /// Sends an email confirmation email using a template
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="confirmationLink">Email confirmation link</param>
    /// <param name="userName">User's name for personalization</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendEmailConfirmationAsync(string to, string confirmationLink, string userName);
    
    /// <summary>
    /// Sends a welcome email to new users using a template
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="userName">User's name for personalization</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendWelcomeEmailAsync(string to, string userName);
}