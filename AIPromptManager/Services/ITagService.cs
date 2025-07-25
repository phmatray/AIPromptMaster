using AIPromptManager.Models;

namespace AIPromptManager.Services
{
    public interface ITagService
    {
        Task<IEnumerable<Tag>> GetAllTagsAsync();
        Task<IEnumerable<string>> GetTagSuggestionsAsync(string input);
        Task<Tag> CreateTagAsync(string name);
    }
}