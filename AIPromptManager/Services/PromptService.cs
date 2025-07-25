using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using AIPromptManager.Models;

namespace AIPromptManager.Services;

public class PromptService(
    PromptManagerContext context,
    ILogger<PromptService> logger,
    IValidationService validationService,
    IStorageService storageService)
    : IPromptService
{
    public async Task<IEnumerable<Prompt>> GetAllPromptsAsync()
    {
        try
        {
            logger.LogDebug("Retrieving all prompts");
            return await context.Prompts
                .Include(p => p.Tags)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all prompts");
            throw;
        }
    }

    public async Task<Prompt?> GetPromptByIdAsync(int id)
    {
        try
        {
            logger.LogDebug("Retrieving prompt with ID: {PromptId}", id);
            return await context.Prompts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);
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

            logger.LogInformation("Successfully created prompt with ID: {PromptId}", prompt.Id);
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
            logger.LogDebug("Updating prompt with ID: {PromptId}", prompt.Id);
                
            var existingPrompt = await context.Prompts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == prompt.Id);

            if (existingPrompt == null)
                throw new InvalidOperationException($"Prompt with ID {prompt.Id} not found");

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
                logger.LogInformation("Successfully updated prompt with ID: {PromptId}", prompt.Id);
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
            logger.LogDebug("Deleting prompt with ID: {PromptId}", id);
                
            var prompt = await context.Prompts.FindAsync(id);
            if (prompt == null)
                throw new InvalidOperationException($"Prompt with ID {id} not found");

            context.Prompts.Remove(prompt);
            await context.SaveChangesAsync();
                
            logger.LogInformation("Successfully deleted prompt with ID: {PromptId}", id);
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

            logger.LogDebug("Searching prompts with term: {SearchTerm}", searchTerm);
                
            var lowerSearchTerm = searchTerm.ToLower();

            return await context.Prompts
                .Include(p => p.Tags)
                .Where(p => p.Title.ToLower().Contains(lowerSearchTerm) ||
                            (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                            p.Content.ToLower().Contains(lowerSearchTerm) ||
                            p.Tags.Any(t => t.Name.ToLower().Contains(lowerSearchTerm)))
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
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

            logger.LogDebug("Retrieving prompts by tag: {Tag}", tag);

            return await context.Prompts
                .Include(p => p.Tags)
                .Where(p => p.Tags.Any(t => t.Name.ToLower() == tag.ToLower()))
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
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