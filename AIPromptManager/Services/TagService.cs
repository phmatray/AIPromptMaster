using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using AIPromptManager.Models;

namespace AIPromptManager.Services
{
    public class TagService : ITagService
    {
        private readonly PromptManagerContext _context;
        private readonly ILogger<TagService> _logger;
        private readonly IValidationService _validationService;

        public TagService(PromptManagerContext context, ILogger<TagService> logger, IValidationService validationService)
        {
            _context = context;
            _logger = logger;
            _validationService = validationService;
        }

        public async Task<IEnumerable<Tag>> GetAllTagsAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all tags");
                return await _context.Tags
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tags");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetTagSuggestionsAsync(string input)
        {
            try
            {
                _logger.LogDebug("Getting tag suggestions for input: {Input}", input);
                
                if (string.IsNullOrWhiteSpace(input))
                    return await _context.Tags
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .ToListAsync();

                // Sanitize input for safety
                var sanitizedInput = _validationService.SanitizeInput(input);
                if (string.IsNullOrWhiteSpace(sanitizedInput))
                    return Enumerable.Empty<string>();

                var lowerInput = sanitizedInput.ToLower();

                return await _context.Tags
                    .Where(t => t.Name.ToLower().Contains(lowerInput))
                    .OrderBy(t => t.Name)
                    .Select(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag suggestions for input: {Input}", input);
                throw;
            }
        }

        public async Task<Tag> CreateTagAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(name));

            try
            {
                // Sanitize and validate the tag name
                name = _validationService.SanitizeInput(name);
                
                var validationResult = _validationService.ValidateTagName(name);
                if (!validationResult.IsValid)
                {
                    var errorMessage = string.Join("; ", validationResult.Errors);
                    _logger.LogWarning("Tag validation failed for '{TagName}': {ValidationErrors}", name, errorMessage);
                    throw new ArgumentException($"Tag validation failed: {errorMessage}");
                }

                _logger.LogDebug("Creating tag: {TagName}", name);

                // Check if tag already exists (case-insensitive)
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

                if (existingTag != null)
                {
                    _logger.LogDebug("Tag already exists: {TagName}", name);
                    return existingTag;
                }

                var newTag = new Tag
                {
                    Name = name,
                    CreatedAt = DateTime.UtcNow
                };

                // Validate the complete tag object
                var tagValidation = _validationService.ValidateTag(newTag);
                if (!tagValidation.IsValid)
                {
                    var errorMessage = string.Join("; ", tagValidation.Errors);
                    throw new ArgumentException($"Tag validation failed: {errorMessage}");
                }

                try
                {
                    _context.Tags.Add(newTag);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully created tag: {TagName}", name);
                    return newTag;
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    _logger.LogWarning(ex, "Unique constraint violation creating tag: {TagName}", name);
                    
                    // Handle race condition where tag was created between our check and insert
                    var tag = await _context.Tags
                        .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
                    
                    if (tag != null)
                        return tag;
                    
                    throw new InvalidOperationException($"Failed to create tag '{name}' due to a unique constraint violation.", ex);
                }
                catch (DbUpdateException ex) when (IsConstraintViolation(ex))
                {
                    _logger.LogError(ex, "Database constraint violation when creating tag: {TagName}", name);
                    throw new InvalidOperationException($"Failed to create tag '{name}' due to database constraints.", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag: {TagName}", name);
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
                   ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                   ex.InnerException?.Message?.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}