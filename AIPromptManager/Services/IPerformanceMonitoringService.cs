namespace AIPromptManager.Services;

public interface IPerformanceMonitoringService
{
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
    Task LogQueryPerformanceAsync(string queryName, TimeSpan duration, int resultCount = 0);
    Task<IEnumerable<QueryPerformanceLog>> GetSlowQueriesAsync(int topCount = 10);
    Task OptimizePerformanceAsync();
}

public class PerformanceMetrics
{
    public double AverageQueryTime { get; set; }
    public int TotalQueries { get; set; }
    public int SlowQueries { get; set; }
    public double DatabaseSize { get; set; }
    public int TotalPrompts { get; set; }
    public int TotalTags { get; set; }
    public DateTime LastOptimization { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

public class QueryPerformanceLog
{
    public string QueryName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int ResultCount { get; set; }
    public DateTime ExecutedAt { get; set; }
}