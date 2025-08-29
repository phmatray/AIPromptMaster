using System.Collections.Concurrent;
using AIPromptManager.Models;

namespace AIPromptManager.Services;

public class RateLimitService : IRateLimitService, IDisposable
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimitEntries;
    private readonly ILogger<RateLimitService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Timer _cleanupTimer;
    private readonly object _lockObject = new();

    // Configuration keys
    private const string ConfigSectionKey = "RateLimit";
    private const string MaxAttemptsPerHourKey = "MaxAttemptsPerHour";
    private const string WindowDurationHoursKey = "WindowDurationHours";
    private const string CleanupIntervalMinutesKey = "CleanupIntervalMinutes";

    // Default configuration values
    private readonly Dictionary<string, int> _defaultMaxAttempts = new()
    {
        { "Login", 5 },
        { "PasswordReset", 3 },
        { "Registration", 5 },
        { "ChangePassword", 10 },
        { "TwoFactorAuth", 5 },
        { "AccountRecovery", 3 }
    };

    private readonly int _defaultWindowDurationHours = 1;
    private readonly int _defaultCleanupIntervalMinutes = 15;

    public RateLimitService(ILogger<RateLimitService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _rateLimitEntries = new ConcurrentDictionary<string, RateLimitEntry>();

        // Start cleanup timer
        var cleanupInterval = TimeSpan.FromMinutes(GetCleanupIntervalMinutes());
        _cleanupTimer = new Timer(
            callback: _ => CleanupExpiredEntries(),
            state: null,
            dueTime: cleanupInterval,
            period: cleanupInterval
        );

        _logger.LogInformation("RateLimitService initialized with cleanup interval of {Interval} minutes", 
            GetCleanupIntervalMinutes());
    }

    public async Task<bool> IsAllowedAsync(string identifier, string operation)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(operation))
        {
            _logger.LogWarning("Invalid identifier or operation provided for rate limiting check");
            return false;
        }

        return await Task.Run(() =>
        {
            var key = GetKey(identifier, operation);
            var maxAttempts = GetMaxAttemptsForOperation(operation);
            var windowDuration = GetWindowDuration();

            lock (_lockObject)
            {
                if (!_rateLimitEntries.TryGetValue(key, out var entry))
                {
                    // No previous attempts, allow the operation
                    return true;
                }

                // Check if the entry has expired
                if (entry.IsExpired(windowDuration))
                {
                    // Entry has expired, remove it and allow the operation
                    _rateLimitEntries.TryRemove(key, out _);
                    return true;
                }

                // Check if under the limit
                var isAllowed = entry.AttemptCount < maxAttempts;
                
                if (!isAllowed)
                {
                    _logger.LogWarning("Rate limit exceeded for identifier {Identifier}, operation {Operation}. " +
                                     "Attempts: {Attempts}/{MaxAttempts} in window starting {WindowStart}", 
                                     identifier, operation, entry.AttemptCount, maxAttempts, entry.FirstAttemptTime);
                }

                return isAllowed;
            }
        });
    }

    public async Task RecordAttemptAsync(string identifier, string operation)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(operation))
        {
            _logger.LogWarning("Invalid identifier or operation provided for recording attempt");
            return;
        }

        await Task.Run(() =>
        {
            var key = GetKey(identifier, operation);
            var windowDuration = GetWindowDuration();

            lock (_lockObject)
            {
                if (_rateLimitEntries.TryGetValue(key, out var existingEntry))
                {
                    // Check if the entry has expired
                    if (existingEntry.IsExpired(windowDuration))
                    {
                        // Entry has expired, reset it
                        existingEntry.Reset();
                    }
                    else
                    {
                        // Entry is still valid, record the attempt
                        existingEntry.RecordAttempt();
                    }
                }
                else
                {
                    // Create new entry
                    var newEntry = RateLimitEntry.Create(identifier, operation);
                    _rateLimitEntries.TryAdd(key, newEntry);
                }

                _logger.LogDebug("Recorded attempt for identifier {Identifier}, operation {Operation}", 
                                identifier, operation);
            }
        });
    }

    public async Task<int> GetRemainingAttemptsAsync(string identifier, string operation)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(operation))
        {
            _logger.LogWarning("Invalid identifier or operation provided for getting remaining attempts");
            return 0;
        }

        return await Task.Run(() =>
        {
            var key = GetKey(identifier, operation);
            var maxAttempts = GetMaxAttemptsForOperation(operation);
            var windowDuration = GetWindowDuration();

            lock (_lockObject)
            {
                if (!_rateLimitEntries.TryGetValue(key, out var entry))
                {
                    return maxAttempts; // No attempts yet
                }

                // Check if the entry has expired
                if (entry.IsExpired(windowDuration))
                {
                    // Entry has expired, remove it and return max attempts
                    _rateLimitEntries.TryRemove(key, out _);
                    return maxAttempts;
                }

                return Math.Max(0, maxAttempts - entry.AttemptCount);
            }
        });
    }

    public async Task ResetAsync(string identifier, string operation)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(operation))
        {
            _logger.LogWarning("Invalid identifier or operation provided for reset");
            return;
        }

        await Task.Run(() =>
        {
            var key = GetKey(identifier, operation);

            lock (_lockObject)
            {
                if (_rateLimitEntries.TryRemove(key, out var entry))
                {
                    _logger.LogInformation("Reset rate limit for identifier {Identifier}, operation {Operation}. " +
                                         "Previous attempts: {Attempts}", identifier, operation, entry.AttemptCount);
                }
            }
        });
    }

    public async Task<TimeSpan> GetTimeUntilResetAsync(string identifier, string operation)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(operation))
        {
            return TimeSpan.Zero;
        }

        return await Task.Run(() =>
        {
            var key = GetKey(identifier, operation);
            var windowDuration = GetWindowDuration();

            lock (_lockObject)
            {
                if (!_rateLimitEntries.TryGetValue(key, out var entry))
                {
                    return TimeSpan.Zero; // No active rate limit
                }

                // Check if the entry has expired
                if (entry.IsExpired(windowDuration))
                {
                    // Entry has expired, remove it
                    _rateLimitEntries.TryRemove(key, out _);
                    return TimeSpan.Zero;
                }

                // Calculate time until reset
                var resetTime = entry.FirstAttemptTime.Add(windowDuration);
                var timeUntilReset = resetTime - DateTime.UtcNow;

                return timeUntilReset > TimeSpan.Zero ? timeUntilReset : TimeSpan.Zero;
            }
        });
    }

    private void CleanupExpiredEntries()
    {
        try
        {
            var windowDuration = GetWindowDuration();
            var keysToRemove = new List<string>();

            lock (_lockObject)
            {
                foreach (var kvp in _rateLimitEntries)
                {
                    if (kvp.Value.IsExpired(windowDuration))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                // Remove expired entries
                foreach (var key in keysToRemove)
                {
                    _rateLimitEntries.TryRemove(key, out _);
                }
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired rate limit entries", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during rate limit entry cleanup");
        }
    }

    private string GetKey(string identifier, string operation)
    {
        return $"{identifier.ToLowerInvariant()}:{operation.ToLowerInvariant()}";
    }

    private int GetMaxAttemptsForOperation(string operation)
    {
        // First try to get operation-specific config
        var operationKey = $"{ConfigSectionKey}:{MaxAttemptsPerHourKey}:{operation}";
        var operationSpecific = _configuration.GetValue<int?>(operationKey);
        if (operationSpecific.HasValue && operationSpecific.Value > 0)
        {
            return operationSpecific.Value;
        }

        // Try to get default max attempts from config
        var defaultConfigKey = $"{ConfigSectionKey}:{MaxAttemptsPerHourKey}:Default";
        var defaultFromConfig = _configuration.GetValue<int?>(defaultConfigKey);
        if (defaultFromConfig.HasValue && defaultFromConfig.Value > 0)
        {
            return defaultFromConfig.Value;
        }

        // Fall back to hardcoded defaults
        return _defaultMaxAttempts.TryGetValue(operation, out var defaultValue) ? defaultValue : 5;
    }

    private TimeSpan GetWindowDuration()
    {
        var hours = _configuration.GetValue($"{ConfigSectionKey}:{WindowDurationHoursKey}", _defaultWindowDurationHours);
        return TimeSpan.FromHours(Math.Max(1, hours)); // Minimum 1 hour window
    }

    private int GetCleanupIntervalMinutes()
    {
        var minutes = _configuration.GetValue($"{ConfigSectionKey}:{CleanupIntervalMinutesKey}", _defaultCleanupIntervalMinutes);
        return Math.Max(1, minutes); // Minimum 1 minute cleanup interval
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _logger.LogInformation("RateLimitService disposed");
    }
}