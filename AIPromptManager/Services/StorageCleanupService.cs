namespace AIPromptManager.Services;

public class StorageCleanupService(
    IServiceProvider serviceProvider,
    ILogger<StorageCleanupService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cleanupIntervalDays = configuration.GetValue("Storage:CleanupIntervalDays", 30);
        var autoCleanupEnabled = configuration.GetValue("Storage:AutoCleanupEnabled", true);
            
        if (!autoCleanupEnabled)
        {
            logger.LogInformation("Automatic storage cleanup is disabled");
            return;
        }

        logger.LogInformation("Storage cleanup service started with {IntervalDays} day interval", cleanupIntervalDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
                    
                using var scope = serviceProvider.CreateScope();
                var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
                    
                // Check storage health
                var healthStatus = await storageService.CheckStorageHealthAsync();
                    
                if (healthStatus == StorageHealthStatus.Warning || healthStatus == StorageHealthStatus.Critical)
                {
                    logger.LogInformation("Storage health is {HealthStatus}, performing cleanup", healthStatus);
                        
                    // Cleanup unused tags first (less risky)
                    var tagCleanupResult = await storageService.CleanupUnusedTagsAsync();
                    if (tagCleanupResult.Success)
                    {
                        logger.LogInformation("Tag cleanup completed: {ItemsRemoved} items removed, {SpaceFreed} bytes freed", 
                            tagCleanupResult.ItemsRemoved, tagCleanupResult.SpaceFreed);
                    }
                    else
                    {
                        logger.LogWarning("Tag cleanup failed: {Errors}", string.Join("; ", tagCleanupResult.Errors));
                    }
                        
                    // If still critical, cleanup old data
                    if (healthStatus == StorageHealthStatus.Critical)
                    {
                        var dataCleanupResult = await storageService.CleanupOldDataAsync(365); // Keep 1 year
                        if (dataCleanupResult.Success)
                        {
                            logger.LogInformation("Data cleanup completed: {ItemsRemoved} items removed, {SpaceFreed} bytes freed", 
                                dataCleanupResult.ItemsRemoved, dataCleanupResult.SpaceFreed);
                        }
                        else
                        {
                            logger.LogError("Data cleanup failed: {Errors}", string.Join("; ", dataCleanupResult.Errors));
                        }
                    }
                }
                else
                {
                    logger.LogDebug("Storage health is {HealthStatus}, no cleanup needed", healthStatus);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during automatic storage cleanup");
                // Continue running despite errors
            }
        }
            
        logger.LogInformation("Storage cleanup service stopped");
    }
}