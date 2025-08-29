using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using AIPromptManager.Models;
using System.Text.Json;

namespace AIPromptManager.Services;

/// <summary>
/// Service for managing user profiles and preferences.
/// Provides methods for retrieving and updating user information, preferences, and related data.
/// </summary>
public class UserService(
    UserManager<ApplicationUser> userManager,
    PromptManagerContext context,
    ILogger<UserService> logger)
    : IUserService
{
    /// <summary>
    /// Validates that a user ID is not null or empty.
    /// </summary>
    /// <param name="userId">The user ID to validate.</param>
    /// <param name="parameterName">The name of the parameter for exception reporting.</param>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    private static void ValidateUserId(string userId, string parameterName = "userId")
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", parameterName);
        }
    }

    /// <summary>
    /// Validates that an email is not null or empty.
    /// </summary>
    /// <param name="email">The email to validate.</param>
    /// <param name="parameterName">The name of the parameter for exception reporting.</param>
    /// <exception cref="ArgumentException">Thrown when email is null or empty.</exception>
    private static void ValidateEmail(string email, string parameterName = "email")
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty.", parameterName);
        }
    }

    /// <summary>
    /// Creates default user preferences if none exist.
    /// </summary>
    /// <returns>A new UserPreferences instance with default values.</returns>
    private static UserPreferences CreateDefaultPreferences()
    {
        return new UserPreferences();
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetUserProfileAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            logger.LogDebug("Retrieving user profile for user ID: {UserId}", userId);
            
            var user = await context.Users
                .Include(u => u.Prompts)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                logger.LogWarning("User profile not found for user ID: {UserId}", userId);
                return null;
            }

            logger.LogDebug("Successfully retrieved user profile for user ID: {UserId}", userId);
            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user profile for user ID: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUserProfileAsync(string userId, UserProfileUpdateModel model)
    {
        ValidateUserId(userId);
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            logger.LogDebug("Updating user profile for user ID: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found for profile update: {UserId}", userId);
                throw new InvalidOperationException($"User with ID '{userId}' not found.");
            }

            // Update user properties using the model's helper method
            model.UpdateUser(user);

            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                logger.LogInformation("Successfully updated user profile for user ID: {UserId}", userId);
                return true;
            }

            // Log validation errors
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to update user profile for user ID: {UserId}. Errors: {Errors}", userId, errors);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user profile for user ID: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<UserPreferences> GetUserPreferencesAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            logger.LogDebug("Retrieving user preferences for user ID: {UserId}", userId);

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                logger.LogWarning("User not found for preferences retrieval: {UserId}", userId);
                return CreateDefaultPreferences();
            }

            var preferences = user.Preferences ?? CreateDefaultPreferences();
            logger.LogDebug("Successfully retrieved user preferences for user ID: {UserId}", userId);
            return preferences;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user preferences for user ID: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUserPreferencesAsync(string userId, UserPreferences preferences)
    {
        ValidateUserId(userId);
        ArgumentNullException.ThrowIfNull(preferences);

        try
        {
            logger.LogDebug("Updating user preferences for user ID: {UserId}", userId);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                logger.LogWarning("User not found for preferences update: {UserId}", userId);
                throw new InvalidOperationException($"User with ID '{userId}' not found.");
            }

            // Update preferences and timestamp
            user.Preferences = preferences;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            logger.LogInformation("Successfully updated user preferences for user ID: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user preferences for user ID: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            logger.LogDebug("Retrieving user by ID: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogDebug("User not found for ID: {UserId}", userId);
            }
            else
            {
                logger.LogDebug("Successfully retrieved user by ID: {UserId}", userId);
            }

            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        ValidateEmail(email);

        try
        {
            logger.LogDebug("Retrieving user by email: {Email}", email);

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                logger.LogDebug("User not found for email: {Email}", email);
            }
            else
            {
                logger.LogDebug("Successfully retrieved user by email: {Email}", email);
            }

            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UserExistsAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            logger.LogDebug("Checking if user exists for ID: {UserId}", userId);

            var exists = await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == userId);

            logger.LogDebug("User existence check for ID {UserId}: {Exists}", userId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking user existence for ID: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetUserPromptCountAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            logger.LogDebug("Retrieving prompt count for user ID: {UserId}", userId);

            var count = await context.Prompts
                .AsNoTracking()
                .CountAsync(p => p.UserId == userId);

            logger.LogDebug("Successfully retrieved prompt count for user ID {UserId}: {Count}", userId, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving prompt count for user ID: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>?> GetUserStatsAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            logger.LogDebug("Retrieving user statistics for user ID: {UserId}", userId);

            var user = await context.Users
                .Include(u => u.Prompts)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                logger.LogWarning("User not found for statistics retrieval: {UserId}", userId);
                return null;
            }

            var stats = new Dictionary<string, object>
            {
                ["UserId"] = user.Id,
                ["UserName"] = user.UserName ?? "Unknown",
                ["Email"] = user.Email ?? "No Email",
                ["DisplayName"] = user.DisplayName,
                ["FullName"] = user.FullName,
                ["CreatedAt"] = user.CreatedAt,
                ["UpdatedAt"] = user.UpdatedAt,
                ["PromptCount"] = user.Prompts?.Count ?? 0,
                ["HasBio"] = !string.IsNullOrWhiteSpace(user.Bio),
                ["HasJobTitle"] = !string.IsNullOrWhiteSpace(user.JobTitle),
                ["HasCompany"] = !string.IsNullOrWhiteSpace(user.Company),
                ["EmailConfirmed"] = user.EmailConfirmed,
                ["PhoneNumberConfirmed"] = user.PhoneNumberConfirmed,
                ["TwoFactorEnabled"] = user.TwoFactorEnabled,
                ["LockoutEnabled"] = user.LockoutEnabled,
                ["AccessFailedCount"] = user.AccessFailedCount
            };

            // Add preferences information if available
            if (user.Preferences != null)
            {
                stats["PreferencesConfigured"] = true;
                stats["Theme"] = user.Preferences.Theme;
                stats["DefaultView"] = user.Preferences.DefaultView;
                stats["Language"] = user.Preferences.Language;
                stats["ItemsPerPage"] = user.Preferences.ItemsPerPage;
            }
            else
            {
                stats["PreferencesConfigured"] = false;
            }

            logger.LogDebug("Successfully retrieved user statistics for user ID: {UserId}", userId);
            return stats;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user statistics for user ID: {UserId}", userId);
            throw;
        }
    }
}