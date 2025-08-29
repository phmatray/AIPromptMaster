using AIPromptManager.Models;

namespace AIPromptManager.Services;

public interface IRateLimitService
{
    /// <summary>
    /// Checks if an operation is allowed for the given identifier within the rate limit window
    /// </summary>
    /// <param name="identifier">Unique identifier (email, IP address, user ID, etc.)</param>
    /// <param name="operation">Type of operation being performed</param>
    /// <returns>True if the operation is allowed, false if rate limit exceeded</returns>
    Task<bool> IsAllowedAsync(string identifier, string operation);

    /// <summary>
    /// Records an attempt for the given identifier and operation
    /// </summary>
    /// <param name="identifier">Unique identifier (email, IP address, user ID, etc.)</param>
    /// <param name="operation">Type of operation being performed</param>
    /// <returns>Task representing the async operation</returns>
    Task RecordAttemptAsync(string identifier, string operation);

    /// <summary>
    /// Gets the number of remaining attempts for the given identifier and operation
    /// </summary>
    /// <param name="identifier">Unique identifier (email, IP address, user ID, etc.)</param>
    /// <param name="operation">Type of operation being performed</param>
    /// <returns>Number of remaining attempts in the current time window</returns>
    Task<int> GetRemainingAttemptsAsync(string identifier, string operation);

    /// <summary>
    /// Resets the rate limit for the given identifier and operation
    /// </summary>
    /// <param name="identifier">Unique identifier (email, IP address, user ID, etc.)</param>
    /// <param name="operation">Type of operation being performed</param>
    /// <returns>Task representing the async operation</returns>
    Task ResetAsync(string identifier, string operation);

    /// <summary>
    /// Gets the time until the rate limit window resets for the given identifier and operation
    /// </summary>
    /// <param name="identifier">Unique identifier (email, IP address, user ID, etc.)</param>
    /// <param name="operation">Type of operation being performed</param>
    /// <returns>TimeSpan until reset, or TimeSpan.Zero if no limit is active</returns>
    Task<TimeSpan> GetTimeUntilResetAsync(string identifier, string operation);
}