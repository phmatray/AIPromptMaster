using AIPromptManager.Models;

namespace AIPromptManager.Services;

public interface IPromptService
{
    Task<IEnumerable<Prompt>> GetAllPromptsAsync();
    Task<(IEnumerable<Prompt> Prompts, int TotalCount)> GetPromptsPagedAsync(int page = 1, int pageSize = 12);
    Task<Prompt?> GetPromptByIdAsync(int id);
    Task<Prompt> CreatePromptAsync(Prompt prompt);
    Task<Prompt> UpdatePromptAsync(Prompt prompt);
    Task DeletePromptAsync(int id);
    Task<IEnumerable<Prompt>> SearchPromptsAsync(string searchTerm);
    Task<(IEnumerable<Prompt> Prompts, int TotalCount)> SearchPromptsPagedAsync(string searchTerm, int page = 1, int pageSize = 12);
    Task<IEnumerable<Prompt>> GetPromptsByTagAsync(string tag);
    Task<(IEnumerable<Prompt> Prompts, int TotalCount)> GetPromptsByTagPagedAsync(string tag, int page = 1, int pageSize = 12);
}