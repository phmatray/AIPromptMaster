using System.ComponentModel.DataAnnotations;

namespace AIPromptManager.Models;

/// <summary>
/// Represents system-wide statistics and metrics for administrative monitoring.
/// Contains counts, performance metrics, and system health information.
/// </summary>
public class SystemStatsModel
{
    /// <summary>
    /// Gets or sets the total number of registered users in the system.
    /// </summary>
    [Display(Name = "Total Users")]
    public int TotalUsers { get; set; }

    /// <summary>
    /// Gets or sets the total number of prompts created across all users.
    /// </summary>
    [Display(Name = "Total Prompts")]
    public int TotalPrompts { get; set; }

    /// <summary>
    /// Gets or sets the total number of unique tags in the system.
    /// </summary>
    [Display(Name = "Total Tags")]
    public int TotalTags { get; set; }

    /// <summary>
    /// Gets or sets the database size in bytes.
    /// Used for storage monitoring and capacity planning.
    /// </summary>
    [Display(Name = "Database Size (Bytes)")]
    public long DatabaseSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last database backup.
    /// Null if no backup has been performed yet.
    /// </summary>
    [Display(Name = "Last Backup")]
    public DateTime? LastBackup { get; set; }

    /// <summary>
    /// Gets or sets the number of users who have been active in the last 30 days.
    /// Based on login activity or prompt creation/modification.
    /// </summary>
    [Display(Name = "Active Users (Last 30 Days)")]
    public int ActiveUsersLast30Days { get; set; }

    /// <summary>
    /// Gets or sets the number of prompts created in the last 30 days.
    /// Used for measuring system usage and growth trends.
    /// </summary>
    [Display(Name = "Prompts Created (Last 30 Days)")]
    public int PromptsCreatedLast30Days { get; set; }

    /// <summary>
    /// Gets the database size formatted as a human-readable string.
    /// Converts bytes to appropriate units (KB, MB, GB, etc.).
    /// </summary>
    [Display(Name = "Database Size")]
    public string FormattedDatabaseSize
    {
        get
        {
            if (DatabaseSizeBytes < 1024)
                return $"{DatabaseSizeBytes} B";
            if (DatabaseSizeBytes < 1024 * 1024)
                return $"{DatabaseSizeBytes / 1024.0:F1} KB";
            if (DatabaseSizeBytes < 1024 * 1024 * 1024)
                return $"{DatabaseSizeBytes / (1024.0 * 1024):F1} MB";
            
            return $"{DatabaseSizeBytes / (1024.0 * 1024 * 1024):F1} GB";
        }
    }

    /// <summary>
    /// Gets a formatted string representing time since the last backup.
    /// Returns "Never" if no backup has been performed.
    /// </summary>
    [Display(Name = "Time Since Last Backup")]
    public string TimeSinceLastBackup
    {
        get
        {
            if (!LastBackup.HasValue)
                return "Never";

            var timeSince = DateTime.UtcNow - LastBackup.Value;
            
            if (timeSince.TotalDays >= 1)
                return $"{(int)timeSince.TotalDays} day(s) ago";
            if (timeSince.TotalHours >= 1)
                return $"{(int)timeSince.TotalHours} hour(s) ago";
            if (timeSince.TotalMinutes >= 1)
                return $"{(int)timeSince.TotalMinutes} minute(s) ago";
            
            return "Just now";
        }
    }

    /// <summary>
    /// Gets the user activity rate as a percentage of total users.
    /// Represents what percentage of users have been active in the last 30 days.
    /// </summary>
    [Display(Name = "User Activity Rate")]
    public double UserActivityRate
    {
        get
        {
            if (TotalUsers == 0)
                return 0;
            
            return Math.Round((double)ActiveUsersLast30Days / TotalUsers * 100, 2);
        }
    }

    /// <summary>
    /// Gets the average number of prompts per user.
    /// Calculates the overall prompt-to-user ratio in the system.
    /// </summary>
    [Display(Name = "Average Prompts per User")]
    public double AveragePromptsPerUser
    {
        get
        {
            if (TotalUsers == 0)
                return 0;
            
            return Math.Round((double)TotalPrompts / TotalUsers, 2);
        }
    }
}