using AIPromptManager.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AIPromptManager.Services
{
    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        
        // Validation constants
        private const int MAX_TITLE_LENGTH = 200;
        private const int MAX_DESCRIPTION_LENGTH = 500;
        private const int MAX_TAG_NAME_LENGTH = 50;
        private const int MAX_CONTENT_LENGTH = 50000; // Reasonable limit for prompt content
        private const int MAX_TAGS_PER_PROMPT = 20;
        
        // Regex patterns for validation
        private static readonly Regex TagNamePattern = new(@"^[a-zA-Z0-9\-_\s]+$", RegexOptions.Compiled);
        private static readonly Regex SqlInjectionPattern = new(@"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT|SELECT|UNION|UPDATE)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex XssPattern = new(@"<script[^>]*>.*?</script>|javascript:|on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger;
        }

        public ValidationResult ValidatePrompt(Prompt prompt)
        {
            var result = new ValidationResult();

            if (prompt == null)
            {
                result.AddError("Prompt cannot be null");
                return result;
            }

            // Validate title
            if (string.IsNullOrWhiteSpace(prompt.Title))
            {
                result.AddError("Title is required");
            }
            else
            {
                if (prompt.Title.Length > MAX_TITLE_LENGTH)
                {
                    result.AddError($"Title cannot exceed {MAX_TITLE_LENGTH} characters");
                }
                
                if (ContainsSqlInjection(prompt.Title))
                {
                    result.AddError("Title contains potentially harmful content");
                }
                
                if (ContainsXss(prompt.Title))
                {
                    result.AddError("Title contains potentially harmful script content");
                }
            }

            // Validate description
            if (!string.IsNullOrEmpty(prompt.Description))
            {
                if (prompt.Description.Length > MAX_DESCRIPTION_LENGTH)
                {
                    result.AddError($"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters");
                }
                
                if (ContainsSqlInjection(prompt.Description))
                {
                    result.AddError("Description contains potentially harmful content");
                }
                
                if (ContainsXss(prompt.Description))
                {
                    result.AddError("Description contains potentially harmful script content");
                }
            }

            // Validate content
            if (string.IsNullOrWhiteSpace(prompt.Content))
            {
                result.AddError("Content is required");
            }
            else
            {
                if (prompt.Content.Length > MAX_CONTENT_LENGTH)
                {
                    result.AddError($"Content cannot exceed {MAX_CONTENT_LENGTH} characters");
                }
                
                if (ContainsSqlInjection(prompt.Content))
                {
                    result.AddError("Content contains potentially harmful content");
                }
                
                // Note: We don't check for XSS in content as prompts might legitimately contain HTML/JS examples
            }

            // Validate tags
            if (prompt.Tags != null)
            {
                if (prompt.Tags.Count > MAX_TAGS_PER_PROMPT)
                {
                    result.AddError($"Cannot have more than {MAX_TAGS_PER_PROMPT} tags per prompt");
                }

                foreach (var tag in prompt.Tags)
                {
                    var tagValidation = ValidateTag(tag);
                    if (!tagValidation.IsValid)
                    {
                        result.Errors.AddRange(tagValidation.Errors);
                        result.IsValid = false;
                    }
                }

                // Check for duplicate tags
                var duplicateTags = prompt.Tags
                    .GroupBy(t => t.Name.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicate in duplicateTags)
                {
                    result.AddWarning($"Duplicate tag found: {duplicate}");
                }
            }

            // Validate dates
            if (prompt.CreatedAt == default)
            {
                result.AddError("CreatedAt date is required");
            }
            
            if (prompt.UpdatedAt == default)
            {
                result.AddError("UpdatedAt date is required");
            }
            
            if (prompt.CreatedAt > DateTime.UtcNow.AddMinutes(5)) // Allow small clock skew
            {
                result.AddError("CreatedAt cannot be in the future");
            }
            
            if (prompt.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
            {
                result.AddError("UpdatedAt cannot be in the future");
            }

            if (prompt.Id > 0 && prompt.UpdatedAt < prompt.CreatedAt)
            {
                result.AddError("UpdatedAt cannot be earlier than CreatedAt");
            }

            return result;
        }

        public ValidationResult ValidateTag(Tag tag)
        {
            var result = new ValidationResult();

            if (tag == null)
            {
                result.AddError("Tag cannot be null");
                return result;
            }

            var nameValidation = ValidateTagName(tag.Name);
            if (!nameValidation.IsValid)
            {
                result.Errors.AddRange(nameValidation.Errors);
                result.IsValid = false;
            }

            // Validate CreatedAt
            if (tag.CreatedAt == default)
            {
                result.AddError("CreatedAt date is required");
            }
            else if (tag.CreatedAt > DateTime.UtcNow.AddMinutes(5))
            {
                result.AddError("CreatedAt cannot be in the future");
            }

            return result;
        }

        public ValidationResult ValidateTagName(string tagName)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(tagName))
            {
                result.AddError("Tag name is required");
                return result;
            }

            if (tagName.Length > MAX_TAG_NAME_LENGTH)
            {
                result.AddError($"Tag name cannot exceed {MAX_TAG_NAME_LENGTH} characters");
            }

            if (!TagNamePattern.IsMatch(tagName))
            {
                result.AddError("Tag name can only contain letters, numbers, hyphens, underscores, and spaces");
            }

            if (ContainsSqlInjection(tagName))
            {
                result.AddError("Tag name contains potentially harmful content");
            }

            if (ContainsXss(tagName))
            {
                result.AddError("Tag name contains potentially harmful script content");
            }

            // Check for reserved words or patterns
            var reservedWords = new[] { "null", "undefined", "admin", "system", "root" };
            if (reservedWords.Contains(tagName.ToLowerInvariant()))
            {
                result.AddWarning($"Tag name '{tagName}' is a reserved word and may cause issues");
            }

            return result;
        }

        public bool IsValidPromptTitle(string title)
        {
            return !string.IsNullOrWhiteSpace(title) && 
                   title.Length <= MAX_TITLE_LENGTH &&
                   !ContainsSqlInjection(title) &&
                   !ContainsXss(title);
        }

        public bool IsValidPromptContent(string content)
        {
            return !string.IsNullOrWhiteSpace(content) && 
                   content.Length <= MAX_CONTENT_LENGTH &&
                   !ContainsSqlInjection(content);
        }

        public bool IsValidTagName(string tagName)
        {
            return !string.IsNullOrWhiteSpace(tagName) &&
                   tagName.Length <= MAX_TAG_NAME_LENGTH &&
                   TagNamePattern.IsMatch(tagName) &&
                   !ContainsSqlInjection(tagName) &&
                   !ContainsXss(tagName);
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove potential XSS content
            var sanitized = XssPattern.Replace(input, "");
            
            // Trim whitespace
            sanitized = sanitized.Trim();
            
            // Remove null characters and other control characters except newlines and tabs
            sanitized = new string(sanitized.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());
            
            return sanitized;
        }

        public IEnumerable<string> SanitizeTags(IEnumerable<string> tags)
        {
            if (tags == null)
                return Enumerable.Empty<string>();

            return tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(SanitizeInput)
                .Where(tag => !string.IsNullOrWhiteSpace(tag) && IsValidTagName(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MAX_TAGS_PER_PROMPT);
        }

        private bool ContainsSqlInjection(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return SqlInjectionPattern.IsMatch(input);
        }

        private bool ContainsXss(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return XssPattern.IsMatch(input);
        }
    }
}