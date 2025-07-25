using AIPromptManager.Models;

namespace AIPromptManager.Services;

public interface IPromptService
{
    Task<IEnumerable<Prompt>> GetAllPromptsAsync();
    Task<Prompt?> GetPromptByIdAsync(int id);
    Task<Prompt> CreatePromptAsync(Prompt prompt);
    Task<Prompt> UpdatePromptAsync(Prompt prompt);
    Task DeletePromptAsync(int id);
    Task<IEnumerable<Prompt>> SearchPromptsAsync(string searchTerm);
    Task<IEnumerable<Prompt>> GetPromptsByTagAsync(string tag);
}