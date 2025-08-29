using System.ComponentModel.DataAnnotations;

namespace AIPromptManager.Models;

/// <summary>
/// Represents user preferences and settings for the application.
/// This class is stored as JSON in the ApplicationUser.Preferences property.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Gets or sets the user's preferred theme.
    /// </summary>
    [StringLength(50, ErrorMessage = "Theme name cannot exceed 50 characters")]
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Gets or sets the user's default view for the prompts page.
    /// </summary>
    [StringLength(50, ErrorMessage = "Default view cannot exceed 50 characters")]
    public string DefaultView { get; set; } = "grid";

    /// <summary>
    /// Gets or sets whether the user wants to receive email notifications.
    /// </summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of items to display per page in listings.
    /// </summary>
    [Range(5, 100, ErrorMessage = "Items per page must be between 5 and 100")]
    public int ItemsPerPage { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to show tooltips and help text throughout the application.
    /// </summary>
    public bool ShowHelpText { get; set; } = true;

    /// <summary>
    /// Gets or sets the user's preferred language code (e.g., "en-US", "fr-FR").
    /// </summary>
    [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets whether to automatically save prompts as drafts while typing.
    /// </summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>
    /// Gets or sets the default privacy level for new prompts.
    /// </summary>
    [StringLength(20, ErrorMessage = "Privacy level cannot exceed 20 characters")]
    public string DefaultPromptPrivacy { get; set; } = "private";

    /// <summary>
    /// Creates a new instance of UserPreferences with default values.
    /// </summary>
    public UserPreferences()
    {
        // Default values are set via property initializers
    }

    /// <summary>
    /// Creates a copy of the current preferences.
    /// </summary>
    /// <returns>A new UserPreferences instance with the same values.</returns>
    public UserPreferences Clone()
    {
        return new UserPreferences
        {
            Theme = Theme,
            DefaultView = DefaultView,
            EmailNotifications = EmailNotifications,
            ItemsPerPage = ItemsPerPage,
            ShowHelpText = ShowHelpText,
            Language = Language,
            AutoSave = AutoSave,
            DefaultPromptPrivacy = DefaultPromptPrivacy
        };
    }
}