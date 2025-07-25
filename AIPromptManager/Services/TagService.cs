using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using AIPromptManager.Models;

namespace AIPromptManager.Services
{
    public class TagService : ITagService
    {
        private readonly PromptManagerContext _context;

        public TagService(PromptManagerContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Tag>> GetAllTagsAsync()
        {
            return await _context.Tags
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetTagSuggestionsAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return await _context.Tags
                    .OrderBy(t => t.Name)
                    .Select(t => t.Name)
                    .ToListAsync();

            var lowerInput = input.ToLower();

            return await _context.Tags
                .Where(t => t.Name.ToLower().Contains(lowerInput))
                .OrderBy(t => t.Name)
                .Select(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tag> CreateTagAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(name));

            name = name.Trim();

            // Check if tag already exists (case-insensitive)
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

            if (existingTag != null)
                return existingTag;

            var newTag = new Tag
            {
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Tags.Add(newTag);
                await _context.SaveChangesAsync();
                return newTag;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint failed") == true)
            {
                // Handle race condition where tag was created between our check and insert
                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
                
                if (tag != null)
                    return tag;
                
                throw;
            }
        }
    }
}