using System.ComponentModel.DataAnnotations;

namespace AIPromptManager.Models;

/// <summary>
/// Model for password reset request containing user email
/// </summary>
public class PasswordResetRequest
{
    /// <summary>
    /// Gets or sets the email address for the password reset request
    /// </summary>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;
}