using Microsoft.EntityFrameworkCore;
using AIPromptManager.Data;
using System.Diagnostics;

namespace AIPromptManager.Services;

public class PerformanceMonitoringService(
    PromptManagerContext context,
    ILogger<PerformanceMonitoringService> logger)
    : IPerformanceMonitoringService
{
    private static readonly List<QueryPerformanceLog> _queryLogs = new();
    private static readonly object _lockObject = new();
    private const int MaxLogEntries = 1000;
    private const double SlowQueryThresholdMs = 1000; // 1 second

    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Get database statistics
            var totalPrompts = await context.Prompts.CountAsync();
            var totalTags = await context.Tags.CountAsync();
            
            stopwatch.Stop();
            
            lock (_lockObject)
            {
                var recentLogs = _queryLogs.Where(q => q.ExecutedAt > DateTime.UtcNow.AddHours(-1)).ToList();
                var slowQueries = recentLogs.Where(q => q.Duration.TotalMilliseconds > SlowQueryThresholdMs).Count();
                var averageQueryTime = recentLogs.Any() ? recentLogs.Average(q => q.Duration.TotalMilliseconds) : 0;

                var metrics = new PerformanceMetrics
                {
                    AverageQueryTime = averageQueryTime,
                    TotalQueries = recentLogs.Count,
                    SlowQueries = slowQueries,
                    TotalPrompts = totalPrompts,
                    TotalTags = totalTags,
                    LastOptimization = DateTime.UtcNow, // This would be stored in a real implementation
                    Recommendations = GenerateRecommendations(recentLogs, totalPrompts, totalTags)
                };

                return metrics;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting performance metrics");
            throw;
        }
    }

    public Task LogQueryPerformanceAsync(string queryName, TimeSpan duration, int resultCount = 0)
    {
        try
        {
            var log = new QueryPerformanceLog
            {
                QueryName = queryName,
                Duration = duration,
                ResultCount = resultCount,
                ExecutedAt = DateTime.UtcNow
            };

            lock (_lockObject)
            {
                _queryLogs.Add(log);
                
                // Keep only the most recent entries
                if (_queryLogs.Count > MaxLogEntries)
                {
                    var toRemove = _queryLogs.Count - MaxLogEntries;
                    _queryLogs.RemoveRange(0, toRemove);
                }
            }

            // Log slow queries
            if (duration.TotalMilliseconds > SlowQueryThresholdMs)
            {
                logger.LogWarning("Slow query detected: {QueryName} took {Duration}ms and returned {ResultCount} results",
                    queryName, duration.TotalMilliseconds, resultCount);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging query performance for {QueryName}", queryName);
            return Task.CompletedTask; // Don't throw to avoid breaking the main operation
        }
    }

    public Task<IEnumerable<QueryPerformanceLog>> GetSlowQueriesAsync(int topCount = 10)
    {
        try
        {
            lock (_lockObject)
            {
                var slowQueries = _queryLogs
                    .Where(q => q.Duration.TotalMilliseconds > SlowQueryThresholdMs)
                    .OrderByDescending(q => q.Duration)
                    .Take(topCount)
                    .ToList();

                return Task.FromResult<IEnumerable<QueryPerformanceLog>>(slowQueries);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting slow queries");
            throw;
        }
    }

    public async Task OptimizePerformanceAsync()
    {
        try
        {
            logger.LogInformation("Starting performance optimization");

            // Analyze and rebuild indexes if needed (SQLite specific)
            await context.Database.ExecuteSqlRawAsync("ANALYZE");
            
            // Vacuum database to reclaim space (SQLite specific)
            await context.Database.ExecuteSqlRawAsync("VACUUM");
            
            // Update statistics
            await context.Database.ExecuteSqlRawAsync("PRAGMA optimize");

            logger.LogInformation("Performance optimization completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during performance optimization");
            throw;
        }
    }

    private List<string> GenerateRecommendations(List<QueryPerformanceLog> recentLogs, int totalPrompts, int totalTags)
    {
        var recommendations = new List<string>();

        try
        {
            // Analyze query patterns
            var slowQueries = recentLogs.Where(q => q.Duration.TotalMilliseconds > SlowQueryThresholdMs).ToList();
            var searchQueries = recentLogs.Where(q => q.QueryName.Contains("Search", StringComparison.OrdinalIgnoreCase)).ToList();
            var tagQueries = recentLogs.Where(q => q.QueryName.Contains("Tag", StringComparison.OrdinalIgnoreCase)).ToList();

            // Recommend pagination if large result sets
            var largeResultQueries = recentLogs.Where(q => q.ResultCount > 50).ToList();
            if (largeResultQueries.Any())
            {
                recommendations.Add("Consider implementing pagination for queries returning large result sets");
            }

            // Recommend search optimization
            if (searchQueries.Any(q => q.Duration.TotalMilliseconds > 500))
            {
                recommendations.Add("Search queries are slow - consider implementing full-text search or search indexing");
            }

            // Recommend database optimization
            if (slowQueries.Count > recentLogs.Count * 0.1) // More than 10% slow queries
            {
                recommendations.Add("High number of slow queries detected - consider running database optimization");
            }

            // Recommend caching
            if (recentLogs.Count > 100) // High query volume
            {
                recommendations.Add("High query volume detected - consider implementing caching for frequently accessed data");
            }

            // Data size recommendations
            if (totalPrompts > 1000)
            {
                recommendations.Add("Large number of prompts - ensure proper indexing and consider archiving old data");
            }

            if (totalTags > 100)
            {
                recommendations.Add("Large number of tags - consider tag cleanup and consolidation");
            }

            // Performance baseline recommendations
            if (!recentLogs.Any())
            {
                recommendations.Add("No recent query data available - performance monitoring is just starting");
            }
            else if (recentLogs.Average(q => q.Duration.TotalMilliseconds) > 200)
            {
                recommendations.Add("Average query time is high - review database configuration and query optimization");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating performance recommendations");
            recommendations.Add("Error generating recommendations - check logs for details");
        }

        return recommendations;
    }
}

// Extension methods for easy performance monitoring
public static class PerformanceMonitoringExtensions
{
    public static async Task<T> MonitorPerformanceAsync<T>(
        this IPerformanceMonitoringService performanceService,
        string queryName,
        Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            var resultCount = result switch
            {
                IEnumerable<object> enumerable => enumerable.Count(),
                _ => 1
            };
            
            await performanceService.LogQueryPerformanceAsync(queryName, stopwatch.Elapsed, resultCount);
            return result;
        }
        catch
        {
            stopwatch.Stop();
            await performanceService.LogQueryPerformanceAsync($"{queryName}_ERROR", stopwatch.Elapsed, 0);
            throw;
        }
    }

    public static async Task MonitorPerformanceAsync(
        this IPerformanceMonitoringService performanceService,
        string queryName,
        Func<Task> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await operation();
            stopwatch.Stop();
            await performanceService.LogQueryPerformanceAsync(queryName, stopwatch.Elapsed, 0);
        }
        catch
        {
            stopwatch.Stop();
            await performanceService.LogQueryPerformanceAsync($"{queryName}_ERROR", stopwatch.Elapsed, 0);
            throw;
        }
    }
}