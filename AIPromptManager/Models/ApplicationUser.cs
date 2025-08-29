using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AIPromptManager.Models;

/// <summary>
/// Represents an application user with extended profile information.
/// Extends IdentityUser to add custom properties for user profiles and preferences.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's biography or personal description.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
    public string? Bio { get; set; }

    /// <summary>
    /// Gets or sets the user's job title.
    /// </summary>
    [StringLength(200, ErrorMessage = "Job title cannot exceed 200 characters")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Gets or sets the user's company name.
    /// </summary>
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
    public string? Company { get; set; }

    /// <summary>
    /// Gets or sets the user's preferences stored as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public UserPreferences? Preferences { get; set; }

    /// <summary>
    /// Gets or sets the date when the user profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date when the user profile was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the user's prompts.
    /// </summary>
    public virtual ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();

    /// <summary>
    /// Gets the user's full name by combining first and last names.
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets the user's display name, preferring full name or falling back to email.
    /// </summary>
    [NotMapped]
    public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : Email ?? UserName ?? "Unknown User";
}