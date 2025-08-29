using AIPromptManager.Models;

namespace AIPromptManager.Services;

/// <summary>
/// Service interface for managing user profiles and preferences.
/// Provides methods for retrieving and updating user information, preferences, and related data.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves the complete user profile by user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The ApplicationUser with profile information, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    Task<ApplicationUser?> GetUserProfileAsync(string userId);

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="model">The profile update model containing new information.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when model is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user is not found.</exception>
    Task<bool> UpdateUserProfileAsync(string userId, UserProfileUpdateModel model);

    /// <summary>
    /// Retrieves the user's preferences.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user's preferences, or default preferences if none exist.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    Task<UserPreferences> GetUserPreferencesAsync(string userId);

    /// <summary>
    /// Updates the user's preferences.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="preferences">The new preferences to save.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when preferences is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user is not found.</exception>
    Task<bool> UpdateUserPreferencesAsync(string userId, UserPreferences preferences);

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The ApplicationUser if found, null otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    Task<ApplicationUser?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>The ApplicationUser if found, null otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when email is null or empty.</exception>
    Task<ApplicationUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Checks if a user exists with the specified ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>True if the user exists, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    Task<bool> UserExistsAsync(string userId);

    /// <summary>
    /// Gets the total number of prompts created by the user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The number of prompts created by the user.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    Task<int> GetUserPromptCountAsync(string userId);

    /// <summary>
    /// Gets basic user statistics including prompt count, creation date, etc.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A dictionary containing user statistics, or null if user not found.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    Task<Dictionary<string, object>?> GetUserStatsAsync(string userId);
}