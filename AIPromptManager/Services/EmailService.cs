using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIPromptManager.Services;

/// <summary>
/// Email service implementation using SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _environment;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpConfig = _configuration.GetSection("Smtp");
            var host = smtpConfig["Host"];
            var port = smtpConfig.GetValue<int>("Port");
            var enableSsl = smtpConfig.GetValue<bool>("EnableSsl");
            var username = smtpConfig["Username"];
            var password = smtpConfig["Password"];
            var fromEmail = smtpConfig["FromEmail"];
            var fromName = smtpConfig["FromName"];

            // In development, if SMTP is not configured, just log the email
            if (_environment.IsDevelopment() && string.IsNullOrEmpty(host))
            {
                _logger.LogInformation("Development Mode - Email would be sent:");
                _logger.LogInformation("To: {To}", to);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Body: {Body}", body);
                return true;
            }

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("SMTP configuration is missing required values");
                return false;
            }

            using var client = new SmtpClient(host, port);
            client.EnableSsl = enableSsl;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName ?? fromEmail);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = body.Contains("<html>") || body.Contains("<div>") || body.Contains("<p>");

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'", to, subject);
            return false;
        }
    }

    /// <summary>
    /// Sends a password reset email using a template
    /// </summary>
    public async Task<bool> SendPasswordResetEmailAsync(string to, string resetLink, string userName)
    {
        try
        {
            var subject = "Password Reset Request - AI Prompt Manager";
            var body = await LoadEmailTemplateAsync("PasswordResetEmail.html", new Dictionary<string, string>
            {
                { "{{UserName}}", userName },
                { "{{ResetLink}}", resetLink },
                { "{{AppName}}", "AI Prompt Manager" }
            });

            return await SendEmailAsync(to, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {To}", to);
            return false;
        }
    }

    /// <summary>
    /// Sends an email confirmation email using a template
    /// </summary>
    public async Task<bool> SendEmailConfirmationAsync(string to, string confirmationLink, string userName)
    {
        try
        {
            var subject = "Confirm Your Email - AI Prompt Manager";
            var body = await LoadEmailTemplateAsync("EmailConfirmation.html", new Dictionary<string, string>
            {
                { "{{UserName}}", userName },
                { "{{ConfirmationLink}}", confirmationLink },
                { "{{AppName}}", "AI Prompt Manager" }
            });

            return await SendEmailAsync(to, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email confirmation to {To}", to);
            return false;
        }
    }

    /// <summary>
    /// Sends a welcome email to new users using a template
    /// </summary>
    public async Task<bool> SendWelcomeEmailAsync(string to, string userName)
    {
        try
        {
            var subject = "Welcome to AI Prompt Manager!";
            var body = await LoadEmailTemplateAsync("Welcome.html", new Dictionary<string, string>
            {
                { "{{UserName}}", userName },
                { "{{AppName}}", "AI Prompt Manager" },
                { "{{LoginUrl}}", _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7134" }
            });

            return await SendEmailAsync(to, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {To}", to);
            return false;
        }
    }

    /// <summary>
    /// Loads an email template and replaces placeholders
    /// </summary>
    private async Task<string> LoadEmailTemplateAsync(string templateName, Dictionary<string, string> placeholders)
    {
        try
        {
            var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", templateName);
            
            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("Email template not found at {TemplatePath}, using fallback", templatePath);
                return GetFallbackTemplate(templateName, placeholders);
            }

            var template = await File.ReadAllTextAsync(templatePath);
            
            // Replace all placeholders
            foreach (var placeholder in placeholders)
            {
                template = template.Replace(placeholder.Key, placeholder.Value);
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load email template {TemplateName}", templateName);
            return GetFallbackTemplate(templateName, placeholders);
        }
    }

    /// <summary>
    /// Provides fallback templates when template files are not available
    /// </summary>
    private string GetFallbackTemplate(string templateName, Dictionary<string, string> placeholders)
    {
        var userName = placeholders.GetValueOrDefault("{{UserName}}", "User");
        var appName = placeholders.GetValueOrDefault("{{AppName}}", "AI Prompt Manager");

        return templateName.ToLower() switch
        {
            "passwordresetemail.html" => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Password Reset</title>
</head>
<body>
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2>Password Reset Request</h2>
        <p>Hello {userName},</p>
        <p>We received a request to reset your password for your {appName} account.</p>
        <p>Click the link below to reset your password:</p>
        <p><a href='{placeholders.GetValueOrDefault("{{ResetLink}}", "#")}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Reset Password</a></p>
        <p>If you didn't request this password reset, please ignore this email.</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
</body>
</html>",

            "emailconfirmation.html" => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Email Confirmation</title>
</head>
<body>
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2>Confirm Your Email Address</h2>
        <p>Hello {userName},</p>
        <p>Thank you for registering with {appName}!</p>
        <p>Please confirm your email address by clicking the link below:</p>
        <p><a href='{placeholders.GetValueOrDefault("{{ConfirmationLink}}", "#")}' style='background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Confirm Email</a></p>
        <p>If you didn't create this account, please ignore this email.</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
</body>
</html>",

            "welcome.html" => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome</title>
</head>
<body>
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2>Welcome to {appName}!</h2>
        <p>Hello {userName},</p>
        <p>Welcome to {appName}! We're excited to have you on board.</p>
        <p>You can now start organizing and managing your AI prompts with our powerful features:</p>
        <ul>
            <li>Create and organize prompts with tags</li>
            <li>Search through your prompt collection</li>
            <li>Performance monitoring and analytics</li>
            <li>Secure storage and management</li>
        </ul>
        <p><a href='{placeholders.GetValueOrDefault("{{LoginUrl}}", "#")}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Get Started</a></p>
        <p>If you have any questions, don't hesitate to reach out to our support team.</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
</body>
</html>",

            _ => $@"
<!DOCTYPE html>
<html>
<body>
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
        <p>Hello {userName},</p>
        <p>This is a message from {appName}.</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
</body>
</html>"
        };
    }
}