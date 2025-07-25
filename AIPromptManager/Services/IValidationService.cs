using AIPromptManager.Models;
using System.ComponentModel.DataAnnotations;

namespace AIPromptManager.Services
{
    public interface IValidationService
    {
        ValidationResult ValidatePrompt(Prompt prompt);
        ValidationResult ValidateTag(Tag tag);
        ValidationResult ValidateTagName(string tagName);
        bool IsValidPromptTitle(string title);
        bool IsValidPromptContent(string content);
        bool IsValidTagName(string tagName);
        string SanitizeInput(string input);
        IEnumerable<string> SanitizeTags(IEnumerable<string> tags);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; } = true; // Default to true
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static ValidationResult Success() => new() { IsValid = true };
        
        public static ValidationResult Failure(params string[] errors) => new() 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}