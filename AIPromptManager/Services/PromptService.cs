using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using AIPromptManager.Models;

namespace AIPromptManager.Services
{
    public class PromptService : IPromptService
    {
        private readonly PromptManagerContext _context;

        public PromptService(PromptManagerContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Prompt>> GetAllPromptsAsync()
        {
            return await _context.Prompts
                .Include(p => p.Tags)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        public async Task<Prompt?> GetPromptByIdAsync(int id)
        {
            return await _context.Prompts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Prompt> CreatePromptAsync(Prompt prompt)
        {
            if (prompt == null)
                throw new ArgumentNullException(nameof(prompt));

            prompt.CreatedAt = DateTime.UtcNow;
            prompt.UpdatedAt = DateTime.UtcNow;

            // Handle tags - find existing ones or create new ones
            if (prompt.Tags?.Any() == true)
            {
                var tagNames = prompt.Tags.Select(t => t.Name).ToList();
                var existingTags = await _context.Tags
                    .Where(t => tagNames.Contains(t.Name))
                    .ToListAsync();

                var newTagNames = tagNames.Except(existingTags.Select(t => t.Name)).ToList();
                var newTags = newTagNames.Select(name => new Tag { Name = name, CreatedAt = DateTime.UtcNow }).ToList();

                if (newTags.Any())
                {
                    _context.Tags.AddRange(newTags);
                    await _context.SaveChangesAsync();
                }

                prompt.Tags = existingTags.Concat(newTags).ToList();
            }

            _context.Prompts.Add(prompt);
            await _context.SaveChangesAsync();

            return await GetPromptByIdAsync(prompt.Id) ?? prompt;
        }

        public async Task<Prompt> UpdatePromptAsync(Prompt prompt)
        {
            if (prompt == null)
                throw new ArgumentNullException(nameof(prompt));

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

            // Handle tags update
            if (prompt.Tags != null)
            {
                var tagNames = prompt.Tags.Select(t => t.Name).ToList();
                var existingTags = await _context.Tags
                    .Where(t => tagNames.Contains(t.Name))
                    .ToListAsync();

                var newTagNames = tagNames.Except(existingTags.Select(t => t.Name)).ToList();
                var newTags = newTagNames.Select(name => new Tag { Name = name, CreatedAt = DateTime.UtcNow }).ToList();

                if (newTags.Any())
                {
                    _context.Tags.AddRange(newTags);
                    await _context.SaveChangesAsync();
                }

                existingPrompt.Tags.Clear();
                existingPrompt.Tags = existingTags.Concat(newTags).ToList();
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
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

        public async Task DeletePromptAsync(int id)
        {
            var prompt = await _context.Prompts.FindAsync(id);
            if (prompt == null)
                throw new InvalidOperationException($"Prompt with ID {id} not found");

            _context.Prompts.Remove(prompt);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Prompt>> SearchPromptsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllPromptsAsync();

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

        public async Task<IEnumerable<Prompt>> GetPromptsByTagAsync(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return await GetAllPromptsAsync();

            return await _context.Prompts
                .Include(p => p.Tags)
                .Where(p => p.Tags.Any(t => t.Name.ToLower() == tag.ToLower()))
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }
    }
}