namespace AIPromptManager.Models;

/// <summary>
/// Represents a rate limit entry for tracking attempts by identifier and operation
/// </summary>
public class RateLimitEntry
{
    /// <summary>
    /// Unique identifier for the entity being rate limited (email, IP address, user ID, etc.)
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Type of operation being rate limited (PasswordReset, Login, Registration, etc.)
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Number of attempts made within the current time window
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Timestamp of the first attempt in the current window
    /// </summary>
    public DateTime FirstAttemptTime { get; set; }

    /// <summary>
    /// Timestamp of the most recent attempt
    /// </summary>
    public DateTime LastAttemptTime { get; set; }

    /// <summary>
    /// Indicates whether this entry has expired based on the time window
    /// </summary>
    /// <param name="windowDuration">Duration of the rate limiting window</param>
    /// <returns>True if the entry has expired and should be cleaned up</returns>
    public bool IsExpired(TimeSpan windowDuration)
    {
        return DateTime.UtcNow - FirstAttemptTime > windowDuration;
    }

    /// <summary>
    /// Gets a composite key for identifying this rate limit entry
    /// </summary>
    public string GetKey() => $"{Identifier}:{Operation}";

    /// <summary>
    /// Creates a new rate limit entry with the current timestamp
    /// </summary>
    /// <param name="identifier">Unique identifier</param>
    /// <param name="operation">Operation type</param>
    /// <returns>New RateLimitEntry instance</returns>
    public static RateLimitEntry Create(string identifier, string operation)
    {
        var now = DateTime.UtcNow;
        return new RateLimitEntry
        {
            Identifier = identifier,
            Operation = operation,
            AttemptCount = 1,
            FirstAttemptTime = now,
            LastAttemptTime = now
        };
    }

    /// <summary>
    /// Records a new attempt, incrementing the count and updating the last attempt time
    /// </summary>
    public void RecordAttempt()
    {
        AttemptCount++;
        LastAttemptTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets the entry to start a new time window
    /// </summary>
    public void Reset()
    {
        var now = DateTime.UtcNow;
        AttemptCount = 1;
        FirstAttemptTime = now;
        LastAttemptTime = now;
    }
}