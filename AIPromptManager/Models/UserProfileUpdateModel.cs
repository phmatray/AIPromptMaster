using System.ComponentModel.DataAnnotations;

namespace AIPromptManager.Models;

/// <summary>
/// Data Transfer Object (DTO) for updating user profile information.
/// Used to transfer profile data between the UI and service layers with validation.
/// </summary>
public class UserProfileUpdateModel
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's biography or personal description.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
    [Display(Name = "Biography")]
    [DataType(DataType.MultilineText)]
    public string? Bio { get; set; }

    /// <summary>
    /// Gets or sets the user's job title.
    /// </summary>
    [StringLength(200, ErrorMessage = "Job title cannot exceed 200 characters")]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Gets or sets the user's company name.
    /// </summary>
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
    [Display(Name = "Company")]
    public string? Company { get; set; }

    /// <summary>
    /// Creates a new instance of UserProfileUpdateModel.
    /// </summary>
    public UserProfileUpdateModel()
    {
    }

    /// <summary>
    /// Creates a new instance of UserProfileUpdateModel from an ApplicationUser.
    /// </summary>
    /// <param name="user">The ApplicationUser to copy data from.</param>
    public UserProfileUpdateModel(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        
        FirstName = user.FirstName;
        LastName = user.LastName;
        Bio = user.Bio;
        JobTitle = user.JobTitle;
        Company = user.Company;
    }

    /// <summary>
    /// Updates an ApplicationUser with the values from this model.
    /// </summary>
    /// <param name="user">The ApplicationUser to update.</param>
    public void UpdateUser(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        
        user.FirstName = FirstName?.Trim();
        user.LastName = LastName?.Trim();
        user.Bio = Bio?.Trim();
        user.JobTitle = JobTitle?.Trim();
        user.Company = Company?.Trim();
        user.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the trimmed full name by combining first and last names.
    /// </summary>
    public string FullName => $"{FirstName?.Trim()} {LastName?.Trim()}".Trim();
}