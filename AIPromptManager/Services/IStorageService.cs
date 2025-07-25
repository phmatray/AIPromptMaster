namespace AIPromptManager.Services
{
    public interface IStorageService
    {
        Task<StorageInfo> GetStorageInfoAsync();
        Task<bool> IsStorageAvailableAsync();
        Task<bool> HasSufficientSpaceAsync(long requiredBytes);
        Task<CleanupResult> CleanupOldDataAsync(int daysToKeep = 365);
        Task<CleanupResult> CleanupUnusedTagsAsync();
        Task<bool> CanCreatePromptAsync(string title, string description, string content, IEnumerable<string> tags);
        Task<StorageHealthStatus> CheckStorageHealthAsync();
    }

    public class StorageInfo
    {
        public long TotalSize { get; set; }
        public long UsedSize { get; set; }
        public long AvailableSize { get; set; }
        public int PromptCount { get; set; }
        public int TagCount { get; set; }
        public DateTime LastChecked { get; set; }
        public bool IsHealthy { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class CleanupResult
    {
        public bool Success { get; set; }
        public int ItemsRemoved { get; set; }
        public long SpaceFreed { get; set; }
        public List<string> Messages { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public enum StorageHealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Unavailable
    }
}