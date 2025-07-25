using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using AIPromptManager.Models;
using System.Data;

namespace AIPromptManager.Services
{
    public class PromptService : IPromptService
    {
        private readonly PromptManagerContext _context;
        private readonly ILogger<PromptService> _logger;
        private readonly IValidationService _validationService;
        private readonly IStorageService _storageService;

        public PromptService(PromptManagerContext context, ILogger<PromptService> logger, IValidationService validationService, IStorageService storageService)
        {
            _context = context;
            _logger = logger;
            _validationService = validationService;
            _storageService = storageService;
        }

        public async Task<IEnumerable<Prompt>> GetAllPromptsAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all prompts");
                return await _context.Prompts
                    .Include(p => p.Tags)
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all prompts");
                throw;
            }
        }

        public async Task<Prompt?> GetPromptByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving prompt with ID: {PromptId}", id);
                return await _context.Prompts
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prompt with ID: {PromptId}", id);
                throw;
            }
        }

        public async Task<Prompt> CreatePromptAsync(Prompt prompt)
        {
            if (prompt == null)
                throw new ArgumentNullException(nameof(prompt));

            // Sanitize input data
            prompt.Title = _validationService.SanitizeInput(prompt.Title);
            prompt.Description = string.IsNullOrEmpty(prompt.Description) ? null : _validationService.SanitizeInput(prompt.Description);
            prompt.Content = _validationService.SanitizeInput(prompt.Content);

            // Validate the prompt
            var validationResult = _validationService.ValidatePrompt(prompt);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                _logger.LogWarning("Prompt validation failed: {ValidationErrors}", errorMessage);
                throw new ArgumentException($"Prompt validation failed: {errorMessage}");
            }

            // Check storage availability and capacity before creating
            var canCreate = await _storageService.CanCreatePromptAsync(
                prompt.Title, 
                prompt.Description ?? string.Empty, 
                prompt.Content, 
                prompt.Tags?.Select(t => t.Name) ?? Enumerable.Empty<string>());
                
            if (!canCreate)
            {
                var storageHealth = await _storageService.CheckStorageHealthAsync();
                var errorMessage = storageHealth switch
                {
                    StorageHealthStatus.Unavailable => "Storage is currently unavailable. Please try again later.",
                    StorageHealthStatus.Critical => "Storage is full. Please delete some prompts or contact support.",
                    StorageHealthStatus.Warning => "Storage is nearly full. Consider cleaning up old prompts.",
                    _ => "Unable to create prompt due to storage limitations."
                };
                
                _logger.LogWarning("Cannot create prompt due to storage limitations: {StorageHealth}", storageHealth);
                throw new InvalidOperationException(errorMessage);
            }

            try
            {
                _logger.LogDebug("Creating new prompt: {PromptTitle}", prompt.Title);
                
                prompt.CreatedAt = DateTime.UtcNow;
                prompt.UpdatedAt = DateTime.UtcNow;

            // Handle tags - sanitize, validate, find existing ones or create new ones
            if (prompt.Tags?.Any() == true)
            {
                var sanitizedTagNames = _validationService.SanitizeTags(prompt.Tags.Select(t => t.Name)).ToList();
                
                // Validate each tag name
                foreach (var tagName in sanitizedTagNames)
                {
                    var tagValidation = _validationService.ValidateTagName(tagName);
                    if (!tagValidation.IsValid)
                    {
                        var errorMessage = string.Join("; ", tagValidation.Errors);
                        _logger.LogWarning("Tag validation failed for '{TagName}': {ValidationErrors}", tagName, errorMessage);
                        throw new ArgumentException($"Tag validation failed for '{tagName}': {errorMessage}");
                    }
                }

                var existingTags = await _context.Tags
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
                        var tagValidation = _validationService.ValidateTag(newTag);
                        
                        if (!tagValidation.IsValid)
                        {
                            var errorMessage = string.Join("; ", tagValidation.Errors);
                            throw new ArgumentException($"Tag validation failed: {errorMessage}");
                        }
                        
                        newTags.Add(newTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating tag: {TagName}", tagName);
                        throw;
                    }
                }

                if (newTags.Any())
                {
                    try
                    {
                        _context.Tags.AddRange(newTags);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        // Handle race condition where tag was created by another request
                        _logger.LogWarning(ex, "Unique constraint violation when creating tags, retrying with existing tags");
                        
                        // Refresh existing tags and filter out duplicates
                        existingTags = await _context.Tags
                            .Where(t => sanitizedTagNames.Contains(t.Name))
                            .ToListAsync();
                        
                        newTags = newTags.Where(nt => !existingTags.Any(et => 
                            string.Equals(et.Name, nt.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                        
                        if (newTags.Any())
                        {
                            _context.Tags.AddRange(newTags);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                prompt.Tags = existingTags.Concat(newTags).ToList();
            }

                _context.Prompts.Add(prompt);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created prompt with ID: {PromptId}", prompt.Id);
                return await GetPromptByIdAsync(prompt.Id) ?? prompt;
            }
            catch (DbUpdateException ex) when (IsConstraintViolation(ex))
            {
                _logger.LogError(ex, "Database constraint violation when creating prompt: {PromptTitle}", prompt.Title);
                throw new InvalidOperationException("Failed to create prompt due to database constraints. Please check your data and try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prompt: {PromptTitle}", prompt.Title);
                throw;
            }
        }

        public async Task<Prompt> UpdatePromptAsync(Prompt prompt)
        {
            if (prompt == null)
                throw new ArgumentNullException(nameof(prompt));

            // Sanitize input data
            prompt.Title = _validationService.SanitizeInput(prompt.Title);
            prompt.Description = string.IsNullOrEmpty(prompt.Description) ? null : _validationService.SanitizeInput(prompt.Description);
            prompt.Content = _validationService.SanitizeInput(prompt.Content);

            // Validate the prompt
            var validationResult = _validationService.ValidatePrompt(prompt);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                _logger.LogWarning("Prompt validation failed during update: {ValidationErrors}", errorMessage);
                throw new ArgumentException($"Prompt validation failed: {errorMessage}");
            }

            // Check storage health before updating (in case the update significantly increases size)
            var storageHealth = await _storageService.CheckStorageHealthAsync();
            if (storageHealth == StorageHealthStatus.Unavailable)
            {
                _logger.LogWarning("Cannot update prompt due to storage unavailability");
                throw new InvalidOperationException("Storage is currently unavailable. Please try again later.");
            }
            
            if (storageHealth == StorageHealthStatus.Critical)
            {
                _logger.LogWarning("Storage is critical, but allowing update of existing prompt");
                // Allow updates even when storage is critical, as they might reduce size
            }

            try
            {
                _logger.LogDebug("Updating prompt with ID: {PromptId}", prompt.Id);
                
                var existingPrompt = await _context.Prompts
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.Id == prompt.Id);

                if (existingPrompt == null)
                    throw new InvalidOperationException($"Prompt with ID {prompt.Id} not found");

            // Set the original RowVersion for optimistic concurrency
            if (prompt.RowVersion != null)
            {
                _context.Entry(existingPrompt).OriginalValues["RowVersion"] = prompt.RowVersion;
            }

            // Update basic properties
            existingPrompt.Title = prompt.Title;
            existingPrompt.Description = prompt.Description;
            existingPrompt.Content = prompt.Content;
            existingPrompt.UpdatedAt = DateTime.UtcNow;

            // Handle tags update with validation
            if (prompt.Tags != null)
            {
                var sanitizedTagNames = _validationService.SanitizeTags(prompt.Tags.Select(t => t.Name)).ToList();
                
                // Validate each tag name
                foreach (var tagName in sanitizedTagNames)
                {
                    var tagValidation = _validationService.ValidateTagName(tagName);
                    if (!tagValidation.IsValid)
                    {
                        var errorMessage = string.Join("; ", tagValidation.Errors);
                        _logger.LogWarning("Tag validation failed for '{TagName}': {ValidationErrors}", tagName, errorMessage);
                        throw new ArgumentException($"Tag validation failed for '{tagName}': {errorMessage}");
                    }
                }

                var existingTags = await _context.Tags
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
                        var tagValidation = _validationService.ValidateTag(newTag);
                        
                        if (!tagValidation.IsValid)
                        {
                            var errorMessage = string.Join("; ", tagValidation.Errors);
                            throw new ArgumentException($"Tag validation failed: {errorMessage}");
                        }
                        
                        newTags.Add(newTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating tag during update: {TagName}", tagName);
                        throw;
                    }
                }

                if (newTags.Any())
                {
                    try
                    {
                        _context.Tags.AddRange(newTags);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        // Handle race condition where tag was created by another request
                        _logger.LogWarning(ex, "Unique constraint violation when creating tags during update, retrying with existing tags");
                        
                        // Refresh existing tags and filter out duplicates
                        existingTags = await _context.Tags
                            .Where(t => sanitizedTagNames.Contains(t.Name))
                            .ToListAsync();
                        
                        newTags = newTags.Where(nt => !existingTags.Any(et => 
                            string.Equals(et.Name, nt.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                        
                        if (newTags.Any())
                        {
                            _context.Tags.AddRange(newTags);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                existingPrompt.Tags.Clear();
                existingPrompt.Tags = existingTags.Concat(newTags).ToList();
            }

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated prompt with ID: {PromptId}", prompt.Id);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict updating prompt with ID: {PromptId}", prompt.Id);
                    
                    // Handle optimistic concurrency conflict
                    var entry = ex.Entries.Single();
                    var clientValues = (Prompt)entry.Entity;
                    var databaseEntry = entry.GetDatabaseValues();
                    
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
                _logger.LogError(ex, "Database constraint violation when updating prompt with ID: {PromptId}", prompt.Id);
                throw new InvalidOperationException("Failed to update prompt due to database constraints. Please check your data and try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prompt with ID: {PromptId}", prompt.Id);
                throw;
            }
        }

        public async Task DeletePromptAsync(int id)
        {
            try
            {
                _logger.LogDebug("Deleting prompt with ID: {PromptId}", id);
                
                var prompt = await _context.Prompts.FindAsync(id);
                if (prompt == null)
                    throw new InvalidOperationException($"Prompt with ID {id} not found");

                _context.Prompts.Remove(prompt);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully deleted prompt with ID: {PromptId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting prompt with ID: {PromptId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Prompt>> SearchPromptsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllPromptsAsync();

                _logger.LogDebug("Searching prompts with term: {SearchTerm}", searchTerm);
                
                var lowerSearchTerm = searchTerm.ToLower();

                return await _context.Prompts
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
                _logger.LogError(ex, "Error searching prompts with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<Prompt>> GetPromptsByTagAsync(string tag)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tag))
                    return await GetAllPromptsAsync();

                _logger.LogDebug("Retrieving prompts by tag: {Tag}", tag);

                return await _context.Prompts
                    .Include(p => p.Tags)
                    .Where(p => p.Tags.Any(t => t.Name.ToLower() == tag.ToLower()))
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prompts by tag: {Tag}", tag);
                throw;
            }
        }

        private bool IsConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message?.Contains("constraint", StringComparison.OrdinalIgnoreCase) == true ||
                   ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                   ex.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
                   ex.InnerException?.Message?.Contains("foreign key", StringComparison.OrdinalIgnoreCase) == true;
        }

        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
                   ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}