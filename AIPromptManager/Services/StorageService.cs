using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using System.Text;

namespace AIPromptManager.Services;

public class StorageService(
    PromptManagerContext context,
    ILogger<StorageService> logger,
    IConfiguration configuration)
    : IStorageService
{
    // Storage limits and thresholds
    private const long MaxDatabaseSize = 100 * 1024 * 1024; // 100MB default limit
    private const long WarningThreshold = 80 * 1024 * 1024; // 80MB warning threshold
    private const int MaxPromptsPerUser = 10000; // Maximum prompts per user
    private const int MaxTagsTotal = 5000; // Maximum total tags
    private const long MaxPromptSize = 100 * 1024; // 100KB per prompt

    public async Task<StorageInfo> GetStorageInfoAsync()
    {
        try
        {
            logger.LogDebug("Getting storage information");
                
            var promptCount = await context.Prompts.CountAsync();
            var tagCount = await context.Tags.CountAsync();
                
            // Estimate database size (this is approximate for SQLite)
            var estimatedSize = await EstimateDatabaseSizeAsync();
            var maxSize = GetMaxDatabaseSize();
                
            var storageInfo = new StorageInfo
            {
                TotalSize = maxSize,
                UsedSize = estimatedSize,
                AvailableSize = maxSize - estimatedSize,
                PromptCount = promptCount,
                TagCount = tagCount,
                LastChecked = DateTime.UtcNow,
                IsHealthy = estimatedSize < WarningThreshold
            };

            // Add warnings based on usage
            if (estimatedSize > WarningThreshold)
            {
                storageInfo.Warnings.Add("Database size is approaching the limit");
            }
                
            if (promptCount > MaxPromptsPerUser * 0.8)
            {
                storageInfo.Warnings.Add("Prompt count is approaching the maximum limit");
            }
                
            if (tagCount > MaxTagsTotal * 0.8)
            {
                storageInfo.Warnings.Add("Tag count is approaching the maximum limit");
            }

            return storageInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting storage information");
            return new StorageInfo
            {
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                Warnings = { "Unable to retrieve storage information" }
            };
        }
    }

    public async Task<bool> IsStorageAvailableAsync()
    {
        try
        {
            // Test database connectivity
            await context.Database.CanConnectAsync();
                
            // Check if we can perform basic operations
            // If we can count prompts, storage is available
            return await context.Prompts.Take(1).CountAsync() >= 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Storage is not available");
            return false;
        }
    }

    public async Task<bool> HasSufficientSpaceAsync(long requiredBytes)
    {
        try
        {
            var storageInfo = await GetStorageInfoAsync();
            return storageInfo.AvailableSize >= requiredBytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking available space");
            return false;
        }
    }

    public async Task<CleanupResult> CleanupOldDataAsync(int daysToKeep = 365)
    {
        var result = new CleanupResult();
            
        try
        {
            logger.LogInformation("Starting cleanup of old data, keeping {DaysToKeep} days", daysToKeep);
                
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                
            // Find old prompts that haven't been updated recently
            var oldPrompts = await context.Prompts
                .Where(p => p.UpdatedAt < cutoffDate)
                .ToListAsync();

            if (oldPrompts.Any())
            {
                var sizeBefore = await EstimateDatabaseSizeAsync();
                    
                context.Prompts.RemoveRange(oldPrompts);
                await context.SaveChangesAsync();
                    
                var sizeAfter = await EstimateDatabaseSizeAsync();
                    
                result.Success = true;
                result.ItemsRemoved = oldPrompts.Count;
                result.SpaceFreed = sizeBefore - sizeAfter;
                result.Messages.Add($"Removed {oldPrompts.Count} old prompts");
                    
                logger.LogInformation("Cleanup completed: removed {Count} old prompts, freed {Size} bytes", 
                    oldPrompts.Count, result.SpaceFreed);
            }
            else
            {
                result.Success = true;
                result.Messages.Add("No old data found to clean up");
            }
                
            // Also cleanup unused tags
            var tagCleanup = await CleanupUnusedTagsAsync();
            result.ItemsRemoved += tagCleanup.ItemsRemoved;
            result.SpaceFreed += tagCleanup.SpaceFreed;
            result.Messages.AddRange(tagCleanup.Messages);
                
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during data cleanup");
            result.Success = false;
            result.Errors.Add($"Cleanup failed: {ex.Message}");
            return result;
        }
    }

    public async Task<CleanupResult> CleanupUnusedTagsAsync()
    {
        var result = new CleanupResult();
            
        try
        {
            logger.LogDebug("Starting cleanup of unused tags");
                
            // Find tags that are not associated with any prompts
            var unusedTags = await context.Tags
                .Where(t => !t.Prompts.Any())
                .ToListAsync();

            if (unusedTags.Any())
            {
                var sizeBefore = await EstimateDatabaseSizeAsync();
                    
                context.Tags.RemoveRange(unusedTags);
                await context.SaveChangesAsync();
                    
                var sizeAfter = await EstimateDatabaseSizeAsync();
                    
                result.Success = true;
                result.ItemsRemoved = unusedTags.Count;
                result.SpaceFreed = sizeBefore - sizeAfter;
                result.Messages.Add($"Removed {unusedTags.Count} unused tags");
                    
                logger.LogInformation("Tag cleanup completed: removed {Count} unused tags", unusedTags.Count);
            }
            else
            {
                result.Success = true;
                result.Messages.Add("No unused tags found");
            }
                
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during tag cleanup");
            result.Success = false;
            result.Errors.Add($"Tag cleanup failed: {ex.Message}");
            return result;
        }
    }

    public async Task<bool> CanCreatePromptAsync(string title, string description, string content, IEnumerable<string> tags)
    {
        try
        {
            // Check if storage is available
            if (!await IsStorageAvailableAsync())
            {
                logger.LogWarning("Cannot create prompt: storage is not available");
                return false;
            }

            // Estimate the size of the new prompt
            var estimatedSize = EstimatePromptSize(title, description, content, tags);
                
            if (estimatedSize > MaxPromptSize)
            {
                logger.LogWarning("Cannot create prompt: size {Size} exceeds maximum {MaxSize}", 
                    estimatedSize, MaxPromptSize);
                return false;
            }

            // Check if we have sufficient space
            if (!await HasSufficientSpaceAsync(estimatedSize))
            {
                logger.LogWarning("Cannot create prompt: insufficient space available");
                return false;
            }

            // Check prompt count limits
            var currentPromptCount = await context.Prompts.CountAsync();
            if (currentPromptCount >= MaxPromptsPerUser)
            {
                logger.LogWarning("Cannot create prompt: maximum prompt count {MaxCount} reached", MaxPromptsPerUser);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if prompt can be created");
            return false;
        }
    }

    public async Task<StorageHealthStatus> CheckStorageHealthAsync()
    {
        try
        {
            if (!await IsStorageAvailableAsync())
            {
                return StorageHealthStatus.Unavailable;
            }

            var storageInfo = await GetStorageInfoAsync();
                
            // Critical: Over 95% usage or any critical errors
            if (storageInfo.UsedSize > storageInfo.TotalSize * 0.95)
            {
                return StorageHealthStatus.Critical;
            }
                
            // Warning: Over 80% usage
            if (storageInfo.UsedSize > storageInfo.TotalSize * 0.80)
            {
                return StorageHealthStatus.Warning;
            }
                
            return StorageHealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking storage health");
            return StorageHealthStatus.Unavailable;
        }
    }

    private async Task<long> EstimateDatabaseSizeAsync()
    {
        try
        {
            // For SQLite, we can get the actual file size
            var connectionString = context.Database.GetConnectionString();
            if (connectionString?.Contains("Data Source=") == true)
            {
                var dbPath = ExtractDbPathFromConnectionString(connectionString);
                if (File.Exists(dbPath))
                {
                    return new FileInfo(dbPath).Length;
                }
            }
                
            // Fallback: estimate based on record counts and average sizes
            var promptCount = await context.Prompts.CountAsync();
            var tagCount = await context.Tags.CountAsync();
                
            // Rough estimates based on typical data sizes
            var estimatedPromptSize = promptCount * 2000; // ~2KB per prompt on average
            var estimatedTagSize = tagCount * 100; // ~100 bytes per tag on average
            var estimatedOverhead = (promptCount + tagCount) * 200; // Index and metadata overhead
                
            return estimatedPromptSize + estimatedTagSize + estimatedOverhead;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error estimating database size");
            return 0;
        }
    }

    private long EstimatePromptSize(string title, string description, string content, IEnumerable<string> tags)
    {
        var size = 0L;
            
        // Base record overhead
        size += 200; // Estimated overhead for ID, dates, etc.
            
        // Content sizes (UTF-8 encoding)
        size += Encoding.UTF8.GetByteCount(title);
        size += Encoding.UTF8.GetByteCount(description);
        size += Encoding.UTF8.GetByteCount(content);
            
        // Tag associations (estimated)
        size += (tags?.Count() ?? 0) * 50; // Estimated overhead per tag association
            
        return size;
    }

    private string ExtractDbPathFromConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring(part.IndexOf('=') + 1).Trim();
            }
        }
        return string.Empty;
    }

    private long GetMaxDatabaseSize()
    {
        return configuration.GetValue("Storage:MaxDatabaseSize", MaxDatabaseSize);
    }
}