using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;

namespace AIPromptManager.Services;

public interface IComponentOptimizationService
{
    void RegisterComponentRender(string componentName, TimeSpan renderTime);
    ComponentPerformanceReport GetPerformanceReport();
    void OptimizeComponentRendering();
}

public class ComponentOptimizationService : IComponentOptimizationService
{
    private readonly ConcurrentDictionary<string, List<TimeSpan>> _renderTimes = new();
    private readonly object _lockObject = new();
    private const int MaxRenderTimeEntries = 100;

    public void RegisterComponentRender(string componentName, TimeSpan renderTime)
    {
        _renderTimes.AddOrUpdate(componentName, 
            new List<TimeSpan> { renderTime },
            (key, existing) =>
            {
                lock (_lockObject)
                {
                    existing.Add(renderTime);
                    if (existing.Count > MaxRenderTimeEntries)
                    {
                        existing.RemoveAt(0);
                    }
                    return existing;
                }
            });
    }

    public ComponentPerformanceReport GetPerformanceReport()
    {
        var report = new ComponentPerformanceReport();
        
        foreach (var kvp in _renderTimes)
        {
            var componentName = kvp.Key;
            var renderTimes = kvp.Value.ToList();
            
            if (renderTimes.Any())
            {
                var componentStats = new ComponentStats
                {
                    ComponentName = componentName,
                    TotalRenders = renderTimes.Count,
                    AverageRenderTime = TimeSpan.FromMilliseconds(renderTimes.Average(t => t.TotalMilliseconds)),
                    MaxRenderTime = renderTimes.Max(),
                    MinRenderTime = renderTimes.Min(),
                    SlowRenders = renderTimes.Count(t => t.TotalMilliseconds > 100) // > 100ms is considered slow
                };
                
                report.ComponentStats.Add(componentStats);
            }
        }
        
        report.ComponentStats = report.ComponentStats
            .OrderByDescending(c => c.AverageRenderTime)
            .ToList();
            
        return report;
    }

    public void OptimizeComponentRendering()
    {
        // In a real implementation, this could:
        // 1. Identify components with slow render times
        // 2. Suggest optimizations (memoization, virtualization, etc.)
        // 3. Clear old performance data
        // 4. Trigger garbage collection if needed
        
        var report = GetPerformanceReport();
        var slowComponents = report.ComponentStats
            .Where(c => c.AverageRenderTime.TotalMilliseconds > 50)
            .ToList();
            
        // Log recommendations for slow components
        foreach (var component in slowComponents)
        {
            Console.WriteLine($"Component '{component.ComponentName}' has slow render time: {component.AverageRenderTime.TotalMilliseconds:F1}ms average");
        }
        
        // Clear old data to free memory
        foreach (var kvp in _renderTimes.ToList())
        {
            if (kvp.Value.Count > MaxRenderTimeEntries / 2)
            {
                lock (_lockObject)
                {
                    var toKeep = kvp.Value.Skip(kvp.Value.Count - MaxRenderTimeEntries / 2).ToList();
                    _renderTimes.TryUpdate(kvp.Key, toKeep, kvp.Value);
                }
            }
        }
    }
}

public class ComponentPerformanceReport
{
    public List<ComponentStats> ComponentStats { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class ComponentStats
{
    public string ComponentName { get; set; } = string.Empty;
    public int TotalRenders { get; set; }
    public TimeSpan AverageRenderTime { get; set; }
    public TimeSpan MaxRenderTime { get; set; }
    public TimeSpan MinRenderTime { get; set; }
    public int SlowRenders { get; set; }
    
    public double SlowRenderPercentage => TotalRenders > 0 ? (double)SlowRenders / TotalRenders * 100 : 0;
}

// Base component class with performance monitoring
public abstract class OptimizedComponentBase : ComponentBase
{
    [Inject] protected IComponentOptimizationService? ComponentOptimization { get; set; }
    
    private DateTime _renderStartTime;
    
    protected override void OnInitialized()
    {
        _renderStartTime = DateTime.UtcNow;
        base.OnInitialized();
    }
    
    protected override void OnAfterRender(bool firstRender)
    {
        if (ComponentOptimization != null)
        {
            var renderTime = DateTime.UtcNow - _renderStartTime;
            ComponentOptimization.RegisterComponentRender(GetType().Name, renderTime);
        }
        
        base.OnAfterRender(firstRender);
    }
    
    protected override void OnParametersSet()
    {
        _renderStartTime = DateTime.UtcNow;
        base.OnParametersSet();
    }
}