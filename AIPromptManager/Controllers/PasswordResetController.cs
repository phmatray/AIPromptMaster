using AIPromptManager.Models;
using AIPromptManager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace AIPromptManager.Controllers;

/// <summary>
/// API controller for password reset operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PasswordResetController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetController> _logger;

    /// <summary>
    /// Initializes a new instance of the PasswordResetController
    /// </summary>
    public PasswordResetController(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IRateLimitService rateLimitService,
        IConfiguration configuration,
        ILogger<PasswordResetController> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _rateLimitService = rateLimitService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a password reset process by sending a reset email
    /// </summary>
    /// <param name="request">Password reset request containing user email</param>
    /// <returns>Success response regardless of whether user exists (security measure)</returns>
    [HttpPost("request")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var identifier = $"{request.Email}_{clientIp}";

            // Check rate limiting first
            if (!await _rateLimitService.IsAllowedAsync(identifier, "PasswordReset"))
            {
                _logger.LogWarning("Password reset rate limit exceeded for email: {Email} from IP: {IP}", 
                    request.Email, clientIp);
                
                var timeUntilReset = await _rateLimitService.GetTimeUntilResetAsync(identifier, "PasswordReset");
                return new ObjectResult(new { 
                    message = "Too many password reset attempts. Please try again later.",
                    retryAfter = timeUntilReset.TotalMinutes
                })
                {
                    StatusCode = 429
                };
            }

            // Record the attempt
            await _rateLimitService.RecordAttemptAsync(identifier, "PasswordReset");

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            
            // Always log the attempt, regardless of whether user exists
            _logger.LogInformation("Password reset requested for email: {Email} from IP: {IP}", 
                request.Email, clientIp);

            if (user != null)
            {
                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // URL encode the token for safe transmission
                var encodedToken = HttpUtility.UrlEncode(token);
                
                // Get base URL from configuration
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
                
                // Create reset link
                var resetLink = $"{baseUrl}/Account/ResetPassword?email={HttpUtility.UrlEncode(request.Email)}&token={encodedToken}";
                
                // Send password reset email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    user.Email!, 
                    resetLink, 
                    user.DisplayName);

                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent successfully to: {Email}", request.Email);
                }
                else
                {
                    _logger.LogError("Failed to send password reset email to: {Email}", request.Email);
                }
            }
            else
            {
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
            }

            // Always return success response for security (don't reveal if user exists)
            return Ok(new { 
                message = "If an account with that email address exists, you will receive a password reset link shortly.",
                success = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset request for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while processing your request." });
        }
    }

    /// <summary>
    /// Resets the user's password using the provided token
    /// </summary>
    /// <param name="model">Password reset model containing email, token, and new password</param>
    /// <returns>Result of the password reset operation</returns>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            _logger.LogInformation("Password reset attempt for email: {Email} from IP: {IP}", 
                model.Email, clientIp);

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("Password reset attempted for non-existent email: {Email}", model.Email);
                return BadRequest(new { message = "Invalid reset token or email address." });
            }

            // URL decode the token
            var decodedToken = HttpUtility.UrlDecode(model.Token);

            // Reset password using the token
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for user: {UserId}", user.Id);
                
                // Update the user's security stamp to invalidate existing sessions
                await _userManager.UpdateSecurityStampAsync(user);
                
                return Ok(new { 
                    message = "Password has been reset successfully.",
                    success = true 
                });
            }
            else
            {
                _logger.LogWarning("Password reset failed for user: {UserId}. Errors: {Errors}", 
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                
                return BadRequest(new { 
                    message = "Invalid or expired reset token.",
                    errors = result.Errors.Select(e => e.Description) 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset for email: {Email}", model.Email);
            return StatusCode(500, new { message = "An error occurred while processing your request." });
        }
    }

    /// <summary>
    /// Validates if a password reset token is still valid
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="token">Password reset token to validate</param>
    /// <returns>Token validation result</returns>
    [HttpGet("validate")]
    public async Task<IActionResult> ValidateResetToken([FromQuery] string email, [FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Email and token are required." });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            _logger.LogInformation("Token validation requested for email: {Email} from IP: {IP}", 
                email, clientIp);

            // Find user by email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Token validation attempted for non-existent email: {Email}", email);
                return BadRequest(new { 
                    message = "Invalid token or email address.",
                    valid = false 
                });
            }

            // URL decode the token
            var decodedToken = HttpUtility.UrlDecode(token);

            // Validate the token by attempting to verify it
            // We'll use a dummy password to test token validity without actually changing anything
            var tokenProvider = _userManager.Options.Tokens.PasswordResetTokenProvider;
            var isValid = await _userManager.VerifyUserTokenAsync(
                user, 
                tokenProvider, 
                "ResetPassword", 
                decodedToken);

            if (isValid)
            {
                _logger.LogInformation("Token validation successful for user: {UserId}", user.Id);
                return Ok(new { 
                    message = "Token is valid.",
                    valid = true 
                });
            }
            else
            {
                _logger.LogWarning("Token validation failed for user: {UserId}", user.Id);
                return BadRequest(new { 
                    message = "Invalid or expired token.",
                    valid = false 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset token for email: {Email}", email);
            return StatusCode(500, new { 
                message = "An error occurred while validating the token.",
                valid = false 
            });
        }
    }
}