using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using AIPromptManager.Models;

namespace AIPromptManager.Services;

/// <summary>
/// Service for administrative operations and system management.
/// Provides functionality for user management, role assignment, system statistics, and administrative data access.
/// </summary>
public class AdminService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    PromptManagerContext context,
    ILogger<AdminService> logger)
    : IAdminService
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
    /// Validates that a role name is not null or empty.
    /// </summary>
    /// <param name="roleName">The role name to validate.</param>
    /// <param name="parameterName">The name of the parameter for exception reporting.</param>
    /// <exception cref="ArgumentException">Thrown when roleName is null or empty.</exception>
    private static void ValidateRoleName(string roleName, string parameterName = "roleName")
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be null or empty.", parameterName);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApplicationUser>> GetAllUsersWithRolesAsync()
    {
        try
        {
            logger.LogDebug("Retrieving all users with their roles");

            var users = await context.Users
                .AsNoTracking()
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            // Note: In a more complex implementation, we might want to include roles directly in the query
            // For now, roles can be retrieved separately using GetUserRolesAsync for each user
            
            logger.LogDebug("Successfully retrieved {UserCount} users", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all users with roles");
            throw new InvalidOperationException("Failed to retrieve users with roles.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IdentityResult> AssignRoleAsync(string userId, string roleName)
    {
        ValidateUserId(userId);
        ValidateRoleName(roleName);

        try
        {
            logger.LogDebug("Assigning role '{RoleName}' to user '{UserId}'", roleName, userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found for role assignment: {UserId}", userId);
                throw new InvalidOperationException($"User with ID '{userId}' not found.");
            }

            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                logger.LogWarning("Role '{RoleName}' does not exist", roleName);
                throw new InvalidOperationException($"Role '{roleName}' does not exist.");
            }

            // Check if user already has the role
            var hasRole = await userManager.IsInRoleAsync(user, roleName);
            if (hasRole)
            {
                logger.LogInformation("User '{UserId}' already has role '{RoleName}'", userId, roleName);
                return IdentityResult.Success;
            }

            var result = await userManager.AddToRoleAsync(user, roleName);
            
            if (result.Succeeded)
            {
                logger.LogInformation("Successfully assigned role '{RoleName}' to user '{UserId}'", roleName, userId);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to assign role '{RoleName}' to user '{UserId}'. Errors: {Errors}", roleName, userId, errors);
            }

            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning role '{RoleName}' to user '{UserId}'", roleName, userId);
            throw new InvalidOperationException($"Failed to assign role '{roleName}' to user '{userId}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IdentityResult> RemoveRoleAsync(string userId, string roleName)
    {
        ValidateUserId(userId);
        ValidateRoleName(roleName);

        try
        {
            logger.LogDebug("Removing role '{RoleName}' from user '{UserId}'", roleName, userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found for role removal: {UserId}", userId);
                throw new InvalidOperationException($"User with ID '{userId}' not found.");
            }

            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                logger.LogWarning("Role '{RoleName}' does not exist", roleName);
                throw new InvalidOperationException($"Role '{roleName}' does not exist.");
            }

            // Check if user has the role
            var hasRole = await userManager.IsInRoleAsync(user, roleName);
            if (!hasRole)
            {
                logger.LogInformation("User '{UserId}' does not have role '{RoleName}' to remove", userId, roleName);
                return IdentityResult.Success;
            }

            // Prevent removing the last admin
            if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminCount = await GetUsersInRoleCountAsync("Admin");
                if (adminCount <= 1)
                {
                    logger.LogWarning("Attempt to remove the last admin user: {UserId}", userId);
                    throw new UnauthorizedAccessException("Cannot remove the last admin user from the system.");
                }
            }

            var result = await userManager.RemoveFromRoleAsync(user, roleName);
            
            if (result.Succeeded)
            {
                logger.LogInformation("Successfully removed role '{RoleName}' from user '{UserId}'", roleName, userId);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to remove role '{RoleName}' from user '{UserId}'. Errors: {Errors}", roleName, userId, errors);
            }

            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing role '{RoleName}' from user '{UserId}'", roleName, userId);
            throw new InvalidOperationException($"Failed to remove role '{roleName}' from user '{userId}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SystemStatsModel> GetSystemStatsAsync()
    {
        try
        {
            logger.LogDebug("Retrieving system statistics");

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // Get basic counts
            var totalUsers = await context.Users.CountAsync();
            var totalPrompts = await context.Prompts.CountAsync();
            var totalTags = await context.Tags.CountAsync();

            // Get activity metrics
            var activeUsersLast30Days = await context.Users
                .Where(u => u.UpdatedAt >= thirtyDaysAgo || 
                           u.Prompts.Any(p => p.CreatedAt >= thirtyDaysAgo))
                .CountAsync();

            var promptsCreatedLast30Days = await context.Prompts
                .Where(p => p.CreatedAt >= thirtyDaysAgo)
                .CountAsync();

            // Get database size (this is an approximation, actual implementation may vary by database provider)
            long databaseSizeBytes = 0;
            try
            {
                // For PostgreSQL, we could use pg_database_size function
                // For now, we'll provide a basic estimation
                var promptContentSize = await context.Prompts
                    .AsNoTracking()
                    .Select(p => (long)(p.Content.Length + p.Title.Length + (p.Description == null ? 0 : p.Description.Length)))
                    .SumAsync();
                
                var userDataSize = await context.Users
                    .AsNoTracking()
                    .Select(u => (long)((u.FirstName == null ? 0 : u.FirstName.Length) + 
                                       (u.LastName == null ? 0 : u.LastName.Length) + 
                                       (u.Bio == null ? 0 : u.Bio.Length) + 
                                       (u.Email == null ? 0 : u.Email.Length) + 
                                       (u.UserName == null ? 0 : u.UserName.Length)))
                    .SumAsync();

                // Basic estimation (actual size would be larger due to indexes, metadata, etc.)
                databaseSizeBytes = promptContentSize + userDataSize;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not calculate database size accurately");
                databaseSizeBytes = 0;
            }

            var stats = new SystemStatsModel
            {
                TotalUsers = totalUsers,
                TotalPrompts = totalPrompts,
                TotalTags = totalTags,
                DatabaseSizeBytes = databaseSizeBytes,
                LastBackup = null, // This would need to be implemented based on backup strategy
                ActiveUsersLast30Days = activeUsersLast30Days,
                PromptsCreatedLast30Days = promptsCreatedLast30Days
            };

            logger.LogDebug("Successfully retrieved system statistics");
            return stats;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving system statistics");
            throw new InvalidOperationException("Failed to retrieve system statistics.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Prompt>> GetAllPromptsAsync()
    {
        try
        {
            logger.LogDebug("Retrieving all prompts for administrative view");

            var prompts = await context.Prompts
                .Include(p => p.User)
                .Include(p => p.Tags)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            logger.LogDebug("Successfully retrieved {PromptCount} prompts", prompts.Count);
            return prompts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all prompts");
            throw new InvalidOperationException("Failed to retrieve all prompts.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IdentityResult> DeleteUserAndDataAsync(string userId)
    {
        ValidateUserId(userId);

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            logger.LogDebug("Deleting user and all associated data for user: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found for deletion: {UserId}", userId);
                throw new InvalidOperationException($"User with ID '{userId}' not found.");
            }

            // Check if this is the last admin user
            var userRoles = await userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                var adminCount = await GetUsersInRoleCountAsync("Admin");
                if (adminCount <= 1)
                {
                    logger.LogWarning("Attempt to delete the last admin user: {UserId}", userId);
                    throw new UnauthorizedAccessException("Cannot delete the last admin user from the system.");
                }
            }

            // Delete user's prompts (this will also cascade to PromptTags due to database configuration)
            var userPrompts = await context.Prompts
                .Where(p => p.UserId == userId)
                .ToListAsync();

            if (userPrompts.Any())
            {
                context.Prompts.RemoveRange(userPrompts);
                logger.LogDebug("Marked {PromptCount} prompts for deletion", userPrompts.Count);
            }

            // Delete the user (Identity framework handles role relationships)
            var result = await userManager.DeleteAsync(user);
            
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to delete user '{UserId}'. Errors: {Errors}", userId, errors);
                await transaction.RollbackAsync();
                return result;
            }

            // Save changes to delete prompts
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Successfully deleted user '{UserId}' and all associated data", userId);
            return IdentityResult.Success;
        }
        catch (UnauthorizedAccessException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error deleting user '{UserId}' and associated data", userId);
            throw new InvalidOperationException($"Failed to delete user '{userId}' and associated data.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IdentityRole>> GetAllRolesAsync()
    {
        try
        {
            logger.LogDebug("Retrieving all roles");

            var roles = await roleManager.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync();

            logger.LogDebug("Successfully retrieved {RoleCount} roles", roles.Count);
            return roles;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all roles");
            throw new InvalidOperationException("Failed to retrieve all roles.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            logger.LogDebug("Retrieving roles for user: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found for role retrieval: {UserId}", userId);
                throw new InvalidOperationException($"User with ID '{userId}' not found.");
            }

            var roles = await userManager.GetRolesAsync(user);
            
            logger.LogDebug("Successfully retrieved {RoleCount} roles for user: {UserId}", roles.Count, userId);
            return roles;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving roles for user: {UserId}", userId);
            throw new InvalidOperationException($"Failed to retrieve roles for user '{userId}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UserHasRoleAsync(string userId, string roleName)
    {
        ValidateUserId(userId);
        ValidateRoleName(roleName);

        try
        {
            logger.LogDebug("Checking if user '{UserId}' has role '{RoleName}'", userId, roleName);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogDebug("User not found: {UserId}", userId);
                return false;
            }

            var hasRole = await userManager.IsInRoleAsync(user, roleName);
            
            logger.LogDebug("User '{UserId}' has role '{RoleName}': {HasRole}", userId, roleName, hasRole);
            return hasRole;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user '{UserId}' has role '{RoleName}'", userId, roleName);
            throw new InvalidOperationException($"Failed to check if user '{userId}' has role '{roleName}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> GetUsersInRoleCountAsync(string roleName)
    {
        ValidateRoleName(roleName);

        try
        {
            logger.LogDebug("Getting count of users in role: {RoleName}", roleName);

            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                logger.LogWarning("Role '{RoleName}' does not exist", roleName);
                throw new InvalidOperationException($"Role '{roleName}' does not exist.");
            }

            var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
            var count = usersInRole.Count;

            logger.LogDebug("Successfully retrieved count of users in role '{RoleName}': {Count}", roleName, count);
            return count;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting count of users in role: {RoleName}", roleName);
            throw new InvalidOperationException($"Failed to get count of users in role '{roleName}'.", ex);
        }
    }
}