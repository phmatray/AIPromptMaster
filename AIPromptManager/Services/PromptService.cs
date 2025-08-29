using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using AIPromptManager.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace AIPromptManager.Services;

public class PromptService(
    PromptManagerContext context,
    ILogger<PromptService> logger,
    IValidationService validationService,
    IStorageService storageService,
    IPerformanceMonitoringService performanceMonitoring,
    IHttpContextAccessor httpContextAccessor)
    : IPromptService
{
    private string GetCurrentUserId(bool allowAnonymous = false)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        // Handle cases where HttpContext might be null (background tasks, etc.)
        if (httpContext == null)
        {
            if (allowAnonymous)
            {
                logger.LogDebug("HttpContext is null, allowing anonymous access for background task");
                return string.Empty; // Return empty string for background tasks
            }
            logger.LogWarning("Unauthorized access attempt: HttpContext is null");
            throw new UnauthorizedAccessException("Access denied: User authentication required");
        }
        
        var user = httpContext.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            if (allowAnonymous)
            {
                logger.LogDebug("User not authenticated, allowing anonymous access");
                return string.Empty;
            }
            logger.LogWarning("Unauthorized access attempt: User not authenticated or missing claims");
            throw new UnauthorizedAccessException("Access denied: User authentication required");
        }
        
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Unauthorized access attempt: User ID claim missing for user {UserName}", 
                user.Identity?.Name ?? "Unknown");
            throw new UnauthorizedAccessException("Access denied: Invalid user credentials");
        }
        
        return userId;
    }

    public async Task<IEnumerable<Prompt>> GetAllPromptsAsync()
    {
        return await performanceMonitoring.MonitorPerformanceAsync("GetAllPrompts", async () =>
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                logger.LogDebug("Retrieving all prompts for user: {UserId}", currentUserId);
                return await context.Prompts
                    .Include(p => p.Tags)
                    .Where(p => p.UserId == currentUserId || p.UserId == null) // Include legacy data with null UserId
                    .OrderByDescending(p => p.UpdatedAt)
                    .AsNoTracking() // Performance optimization: don't track entities for read-only operations
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all prompts");
                throw;
            }
        });
    }

    public async Task<(IEnumerable<Prompt> Prompts, int TotalCount)> GetPromptsPagedAsync(int page = 1, int pageSize = 12)
    {
        return await performanceMonitoring.MonitorPerformanceAsync("GetPromptsPaged", async () =>
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                logger.LogDebug("Retrieving prompts page {Page} with size {PageSize} for user: {UserId}", page, pageSize, currentUserId);
                
                var query = context.Prompts
                    .Include(p => p.Tags)
                    .Where(p => p.UserId == currentUserId || p.UserId == null) // Include legacy data with null UserId
                    .OrderByDescending(p => p.UpdatedAt)
                    .AsNoTracking();

                var totalCount = await query.CountAsync();
                
                var prompts = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (prompts, totalCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving paged prompts");
                throw;
            }
        });
    }

    public async Task<Prompt?> GetPromptByIdAsync(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            logger.LogDebug("Retrieving prompt with ID: {PromptId} for user: {UserId}", id, currentUserId);
            
            var prompt = await context.Prompts
                .Include(p => p.Tags)
                .AsNoTracking() // Performance optimization for read-only operations
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (prompt != null && prompt.UserId != null && prompt.UserId != currentUserId)
            {
                logger.LogWarning("SECURITY: Unauthorized access attempt - User {UserId} attempted to access prompt {PromptId} owned by {OwnerId}", 
                    currentUserId, id, prompt.UserId);
                throw new UnauthorizedAccessException("Access denied: You do not have permission to access this resource");
            }
            
            return prompt;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving prompt with ID: {PromptId}", id);
            throw;
        }
    }

    public async Task<Prompt> CreatePromptAsync(Prompt prompt)
    {
        if (prompt == null)
            throw new ArgumentNullException(nameof(prompt));

        // Sanitize input data
        prompt.Title = validationService.SanitizeInput(prompt.Title);
        prompt.Description = string.IsNullOrEmpty(prompt.Description) ? null : validationService.SanitizeInput(prompt.Description);
        prompt.Content = validationService.SanitizeInput(prompt.Content);

        // Validate the prompt
        var validationResult = validationService.ValidatePrompt(prompt);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("; ", validationResult.Errors);
            logger.LogWarning("Prompt validation failed: {ValidationErrors}", errorMessage);
            throw new ArgumentException($"Prompt validation failed: {errorMessage}");
        }

        // Check storage availability and capacity before creating
        var canCreate = await storageService.CanCreatePromptAsync(
            prompt.Title, 
            prompt.Description ?? string.Empty, 
            prompt.Content, 
            prompt.Tags.Select(t => t.Name));
                
        if (!canCreate)
        {
            var storageHealth = await storageService.CheckStorageHealthAsync();
            var errorMessage = storageHealth switch
            {
                StorageHealthStatus.Unavailable => "Storage is currently unavailable. Please try again later.",
                StorageHealthStatus.Critical => "Storage is full. Please delete some prompts or contact support.",
                StorageHealthStatus.Warning => "Storage is nearly full. Consider cleaning up old prompts.",
                _ => "Unable to create prompt due to storage limitations."
            };
                
            logger.LogWarning("Cannot create prompt due to storage limitations: {StorageHealth}", storageHealth);
            throw new InvalidOperationException(errorMessage);
        }

        try
        {
            logger.LogDebug("Creating new prompt: {PromptTitle}", prompt.Title);
            
            // Automatically set the current user as the owner
            prompt.UserId = GetCurrentUserId();
            prompt.CreatedAt = DateTime.UtcNow;
            prompt.UpdatedAt = DateTime.UtcNow;

            // Handle tags - sanitize, validate, find existing ones or create new ones
            if (prompt.Tags?.Any() == true)
            {
                var sanitizedTagNames = validationService.SanitizeTags(prompt.Tags.Select(t => t.Name)).ToList();
                
                // Validate each tag name
                foreach (var tagName in sanitizedTagNames)
                {
                    var tagValidation = validationService.ValidateTagName(tagName);
                    if (!tagValidation.IsValid)
                    {
                        var errorMessage = string.Join("; ", tagValidation.Errors);
                        logger.LogWarning("Tag validation failed for '{TagName}': {ValidationErrors}", tagName, errorMessage);
                        throw new ArgumentException($"Tag validation failed for '{tagName}': {errorMessage}");
                    }
                }

                var existingTags = await context.Tags
                    .Where(t => sanitizedTagNames.Contains(t.Name))
                    .ToListAsync();

                var newTagNames = sanitizedTagNames.Except(existingTags.Select(t => t.Name), StringComparer.OrdinalIgnoreCase).ToList();
                var newTags = new List<Tag>();

                // Create new tags with validation and constraint handling
                foreach (var tagName in newTagNames)
                {
                    try
                    {
                        var newTag = new Tag { Name = tagName, CreatedAt = DateTime.UtcNow };
                        var tagValidation = validationService.ValidateTag(newTag);
                        
                        if (!tagValidation.IsValid)
                        {
                            var errorMessage = string.Join("; ", tagValidation.Errors);
                            throw new ArgumentException($"Tag validation failed: {errorMessage}");
                        }
                        
                        newTags.Add(newTag);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error creating tag: {TagName}", tagName);
                        throw;
                    }
                }

                if (newTags.Any())
                {
                    try
                    {
                        context.Tags.AddRange(newTags);
                        await context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        // Handle race condition where tag was created by another request
                        logger.LogWarning(ex, "Unique constraint violation when creating tags, retrying with existing tags");
                        
                        // Refresh existing tags and filter out duplicates
                        existingTags = await context.Tags
                            .Where(t => sanitizedTagNames.Contains(t.Name))
                            .ToListAsync();
                        
                        newTags = newTags.Where(nt => !existingTags.Any(et => 
                            string.Equals(et.Name, nt.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                        
                        if (newTags.Any())
                        {
                            context.Tags.AddRange(newTags);
                            await context.SaveChangesAsync();
                        }
                    }
                }

                prompt.Tags = existingTags.Concat(newTags).ToList();
            }

            context.Prompts.Add(prompt);
            await context.SaveChangesAsync();

            logger.LogInformation("AUDIT: User {UserId} successfully created prompt {PromptId} with title '{Title}'", 
                prompt.UserId, prompt.Id, prompt.Title);
            return await GetPromptByIdAsync(prompt.Id) ?? prompt;
        }
        catch (DbUpdateException ex) when (IsConstraintViolation(ex))
        {
            logger.LogError(ex, "Database constraint violation when creating prompt: {PromptTitle}", prompt.Title);
            throw new InvalidOperationException("Failed to create prompt due to database constraints. Please check your data and try again.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating prompt: {PromptTitle}", prompt.Title);
            throw;
        }
    }

    public async Task<Prompt> UpdatePromptAsync(Prompt prompt)
    {
        if (prompt == null)
            throw new ArgumentNullException(nameof(prompt));

        // Sanitize input data
        prompt.Title = validationService.SanitizeInput(prompt.Title);
        prompt.Description = string.IsNullOrEmpty(prompt.Description) ? null : validationService.SanitizeInput(prompt.Description);
        prompt.Content = validationService.SanitizeInput(prompt.Content);

        // Validate the prompt
        var validationResult = validationService.ValidatePrompt(prompt);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("; ", validationResult.Errors);
            logger.LogWarning("Prompt validation failed during update: {ValidationErrors}", errorMessage);
            throw new ArgumentException($"Prompt validation failed: {errorMessage}");
        }

        // Check storage health before updating (in case the update significantly increases size)
        var storageHealth = await storageService.CheckStorageHealthAsync();
        if (storageHealth == StorageHealthStatus.Unavailable)
        {
            logger.LogWarning("Cannot update prompt due to storage unavailability");
            throw new InvalidOperationException("Storage is currently unavailable. Please try again later.");
        }
            
        if (storageHealth == StorageHealthStatus.Critical)
        {
            logger.LogWarning("Storage is critical, but allowing update of existing prompt");
            // Allow updates even when storage is critical, as they might reduce size
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            logger.LogDebug("Updating prompt with ID: {PromptId} for user: {UserId}", prompt.Id, currentUserId);
                
            var existingPrompt = await context.Prompts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == prompt.Id);

            if (existingPrompt == null)
                throw new InvalidOperationException($"Prompt with ID {prompt.Id} not found");
            
            // Verify ownership before allowing updates
            if (existingPrompt.UserId != null && existingPrompt.UserId != currentUserId)
            {
                logger.LogWarning("SECURITY: Unauthorized update attempt - User {UserId} attempted to update prompt {PromptId} owned by {OwnerId}", 
                    currentUserId, prompt.Id, existingPrompt.UserId);
                throw new UnauthorizedAccessException("Access denied: You do not have permission to modify this resource");
            }

            // Set the original RowVersion for optimistic concurrency
            if (prompt.RowVersion != null)
            {
                context.Entry(existingPrompt).OriginalValues["RowVersion"] = prompt.RowVersion;
            }

            // Update basic properties
            existingPrompt.Title = prompt.Title;
            existingPrompt.Description = prompt.Description;
            existingPrompt.Content = prompt.Content;
            existingPrompt.UpdatedAt = DateTime.UtcNow;

            // Handle tags update with validation
            if (prompt.Tags != null)
            {
                var sanitizedTagNames = validationService.SanitizeTags(prompt.Tags.Select(t => t.Name)).ToList();
                
                // Validate each tag name
                foreach (var tagName in sanitizedTagNames)
                {
                    var tagValidation = validationService.ValidateTagName(tagName);
                    if (!tagValidation.IsValid)
                    {
                        var errorMessage = string.Join("; ", tagValidation.Errors);
                        logger.LogWarning("Tag validation failed for '{TagName}': {ValidationErrors}", tagName, errorMessage);
                        throw new ArgumentException($"Tag validation failed for '{tagName}': {errorMessage}");
                    }
                }

                var existingTags = await context.Tags
                    .Where(t => sanitizedTagNames.Contains(t.Name))
                    .ToListAsync();

                var newTagNames = sanitizedTagNames.Except(existingTags.Select(t => t.Name), StringComparer.OrdinalIgnoreCase).ToList();
                var newTags = new List<Tag>();

                // Create new tags with validation and constraint handling
                foreach (var tagName in newTagNames)
                {
                    try
                    {
                        var newTag = new Tag { Name = tagName, CreatedAt = DateTime.UtcNow };
                        var tagValidation = validationService.ValidateTag(newTag);
                        
                        if (!tagValidation.IsValid)
                        {
                            var errorMessage = string.Join("; ", tagValidation.Errors);
                            throw new ArgumentException($"Tag validation failed: {errorMessage}");
                        }
                        
                        newTags.Add(newTag);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error creating tag during update: {TagName}", tagName);
                        throw;
                    }
                }

                if (newTags.Any())
                {
                    try
                    {
                        context.Tags.AddRange(newTags);
                        await context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        // Handle race condition where tag was created by another request
                        logger.LogWarning(ex, "Unique constraint violation when creating tags during update, retrying with existing tags");
                        
                        // Refresh existing tags and filter out duplicates
                        existingTags = await context.Tags
                            .Where(t => sanitizedTagNames.Contains(t.Name))
                            .ToListAsync();
                        
                        newTags = newTags.Where(nt => !existingTags.Any(et => 
                            string.Equals(et.Name, nt.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                        
                        if (newTags.Any())
                        {
                            context.Tags.AddRange(newTags);
                            await context.SaveChangesAsync();
                        }
                    }
                }

                existingPrompt.Tags.Clear();
                existingPrompt.Tags = existingTags.Concat(newTags).ToList();
            }

            try
            {
                await context.SaveChangesAsync();
                logger.LogInformation("AUDIT: User {UserId} successfully updated prompt {PromptId} with title '{Title}'", 
                    currentUserId, prompt.Id, prompt.Title);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating prompt with ID: {PromptId}", prompt.Id);
                    
                // Handle optimistic concurrency conflict
                var entry = ex.Entries.Single();
                var clientValues = (Prompt)entry.Entity;
                var databaseEntry = await entry.GetDatabaseValuesAsync();
                    
                if (databaseEntry == null)
                {
                    throw new InvalidOperationException("The prompt was deleted by another user.");
                }

                var databaseValues = (Prompt)databaseEntry.ToObject();
                    
                throw new InvalidOperationException(
                    $"The prompt was modified by another user. " +
                    $"Database values: Title='{databaseValues.Title}', UpdatedAt='{databaseValues.UpdatedAt}'. " +
                    $"Your values: Title='{clientValues.Title}', UpdatedAt='{clientValues.UpdatedAt}'. " +
                    $"Please refresh and try again.");
            }

            return await GetPromptByIdAsync(existingPrompt.Id) ?? existingPrompt;
        }
        catch (DbUpdateException ex) when (IsConstraintViolation(ex))
        {
            logger.LogError(ex, "Database constraint violation when updating prompt with ID: {PromptId}", prompt.Id);
            throw new InvalidOperationException("Failed to update prompt due to database constraints. Please check your data and try again.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating prompt with ID: {PromptId}", prompt.Id);
            throw;
        }
    }

    public async Task DeletePromptAsync(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            logger.LogDebug("Deleting prompt with ID: {PromptId} for user: {UserId}", id, currentUserId);
                
            var prompt = await context.Prompts.FindAsync(id);
            if (prompt == null)
                throw new InvalidOperationException($"Prompt with ID {id} not found");
            
            // Verify ownership before allowing deletion
            if (prompt.UserId != null && prompt.UserId != currentUserId)
            {
                logger.LogWarning("SECURITY: Unauthorized deletion attempt - User {UserId} attempted to delete prompt {PromptId} owned by {OwnerId}", 
                    currentUserId, id, prompt.UserId);
                throw new UnauthorizedAccessException("Access denied: You do not have permission to delete this resource");
            }

            var promptTitle = prompt.Title; // Store title before deletion for logging
            context.Prompts.Remove(prompt);
            await context.SaveChangesAsync();
                
            logger.LogInformation("AUDIT: User {UserId} successfully deleted prompt {PromptId} with title '{Title}'", 
                currentUserId, id, promptTitle);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting prompt with ID: {PromptId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Prompt>> SearchPromptsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllPromptsAsync();

            var currentUserId = GetCurrentUserId();
            logger.LogDebug("Searching prompts with term: {SearchTerm} for user: {UserId}", searchTerm, currentUserId);
                
            var lowerSearchTerm = searchTerm.ToLower();

            return await context.Prompts
                .Include(p => p.Tags)
                .Where(p => (p.UserId == currentUserId || p.UserId == null) && // Include legacy data with null UserId
                            (EF.Functions.Like(p.Title.ToLower(), $"%{lowerSearchTerm}%") ||
                            (p.Description != null && EF.Functions.Like(p.Description.ToLower(), $"%{lowerSearchTerm}%")) ||
                            EF.Functions.Like(p.Content.ToLower(), $"%{lowerSearchTerm}%") ||
                            p.Tags.Any(t => EF.Functions.Like(t.Name.ToLower(), $"%{lowerSearchTerm}%"))))
                .OrderByDescending(p => p.UpdatedAt)
                .AsNoTracking() // Performance optimization
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching prompts with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<(IEnumerable<Prompt> Prompts, int TotalCount)> SearchPromptsPagedAsync(string searchTerm, int page = 1, int pageSize = 12)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetPromptsPagedAsync(page, pageSize);

            var currentUserId = GetCurrentUserId();
            logger.LogDebug("Searching prompts with term: {SearchTerm}, page {Page}, size {PageSize} for user: {UserId}", searchTerm, page, pageSize, currentUserId);
                
            var lowerSearchTerm = searchTerm.ToLower();

            var query = context.Prompts
                .Include(p => p.Tags)
                .Where(p => (p.UserId == currentUserId || p.UserId == null) && // Include legacy data with null UserId
                            (EF.Functions.Like(p.Title.ToLower(), $"%{lowerSearchTerm}%") ||
                            (p.Description != null && EF.Functions.Like(p.Description.ToLower(), $"%{lowerSearchTerm}%")) ||
                            EF.Functions.Like(p.Content.ToLower(), $"%{lowerSearchTerm}%") ||
                            p.Tags.Any(t => EF.Functions.Like(t.Name.ToLower(), $"%{lowerSearchTerm}%"))))
                .OrderByDescending(p => p.UpdatedAt)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            
            var prompts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (prompts, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching prompts with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<IEnumerable<Prompt>> GetPromptsByTagAsync(string tag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag))
                return await GetAllPromptsAsync();

            var currentUserId = GetCurrentUserId();
            logger.LogDebug("Retrieving prompts by tag: {Tag} for user: {UserId}", tag, currentUserId);

            return await context.Prompts
                .Include(p => p.Tags)
                .Where(p => (p.UserId == currentUserId || p.UserId == null) && // Include legacy data with null UserId
                            p.Tags.Any(t => t.Name.ToLower() == tag.ToLower()))
                .OrderByDescending(p => p.UpdatedAt)
                .AsNoTracking() // Performance optimization
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving prompts by tag: {Tag}", tag);
            throw;
        }
    }

    public async Task<(IEnumerable<Prompt> Prompts, int TotalCount)> GetPromptsByTagPagedAsync(string tag, int page = 1, int pageSize = 12)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag))
                return await GetPromptsPagedAsync(page, pageSize);

            var currentUserId = GetCurrentUserId();
            logger.LogDebug("Retrieving prompts by tag: {Tag}, page {Page}, size {PageSize} for user: {UserId}", tag, page, pageSize, currentUserId);

            var query = context.Prompts
                .Include(p => p.Tags)
                .Where(p => (p.UserId == currentUserId || p.UserId == null) && // Include legacy data with null UserId
                            p.Tags.Any(t => t.Name.ToLower() == tag.ToLower()))
                .OrderByDescending(p => p.UpdatedAt)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            
            var prompts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (prompts, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving prompts by tag: {Tag}", tag);
            throw;
        }
    }

    private bool IsConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase) == true ||
               ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
               ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
               ex.InnerException?.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
               ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;
    }
}