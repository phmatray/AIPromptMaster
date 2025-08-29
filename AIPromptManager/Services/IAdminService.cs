using AIPromptManager.Models;
using Microsoft.AspNetCore.Identity;

namespace AIPromptManager.Services;

/// <summary>
/// Service interface for administrative operations and system management.
/// Provides methods for user management, role assignment, system statistics, and administrative data access.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Retrieves all users in the system along with their assigned roles.
    /// </summary>
    /// <returns>A collection of ApplicationUser objects with their roles populated.</returns>
    /// <exception cref="InvalidOperationException">Thrown when database operation fails.</exception>
    Task<IEnumerable<ApplicationUser>> GetAllUsersWithRolesAsync();

    /// <summary>
    /// Assigns a role to the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to assign the role to.</param>
    /// <param name="roleName">The name of the role to assign (e.g., "Admin", "User").</param>
    /// <returns>An IdentityResult indicating the success or failure of the operation.</returns>
    /// <exception cref="ArgumentException">Thrown when userId or roleName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user is not found or role doesn't exist.</exception>
    Task<IdentityResult> AssignRoleAsync(string userId, string roleName);

    /// <summary>
    /// Removes a role from the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to remove the role from.</param>
    /// <param name="roleName">The name of the role to remove.</param>
    /// <returns>An IdentityResult indicating the success or failure of the operation.</returns>
    /// <exception cref="ArgumentException">Thrown when userId or roleName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user is not found or role doesn't exist.</exception>
    Task<IdentityResult> RemoveRoleAsync(string userId, string roleName);

    /// <summary>
    /// Retrieves comprehensive system statistics and metrics.
    /// </summary>
    /// <returns>A SystemStatsModel containing current system metrics and statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when database operation fails.</exception>
    Task<SystemStatsModel> GetSystemStatsAsync();

    /// <summary>
    /// Retrieves all prompts in the system across all users.
    /// Includes user information and tags for administrative oversight.
    /// </summary>
    /// <returns>A collection of all Prompt objects in the system with related data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when database operation fails.</exception>
    Task<IEnumerable<Prompt>> GetAllPromptsAsync();

    /// <summary>
    /// Completely removes a user and all their associated data from the system.
    /// This includes their prompts, preferences, and other related data.
    /// This is a destructive operation that cannot be undone.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <returns>An IdentityResult indicating the success or failure of the operation.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when attempting to delete the last admin user.</exception>
    Task<IdentityResult> DeleteUserAndDataAsync(string userId);

    /// <summary>
    /// Gets all available roles in the system.
    /// </summary>
    /// <returns>A collection of all IdentityRole objects in the system.</returns>
    /// <exception cref="InvalidOperationException">Thrown when database operation fails.</exception>
    Task<IEnumerable<IdentityRole>> GetAllRolesAsync();

    /// <summary>
    /// Gets the roles assigned to a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A collection of role names assigned to the user.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user is not found.</exception>
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Checks if a user has a specific role.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="roleName">The name of the role to check.</param>
    /// <returns>True if the user has the specified role, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when userId or roleName is null or empty.</exception>
    Task<bool> UserHasRoleAsync(string userId, string roleName);

    /// <summary>
    /// Gets the count of users in a specific role.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <returns>The number of users assigned to the specified role.</returns>
    /// <exception cref="ArgumentException">Thrown when roleName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when role doesn't exist.</exception>
    Task<int> GetUsersInRoleCountAsync(string roleName);
}