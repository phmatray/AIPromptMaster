using AIPromptManager.Services;

namespace AIPromptManager.Services
{
    public class StorageCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StorageCleanupService> _logger;
        private readonly IConfiguration _configuration;
        
        public StorageCleanupService(IServiceProvider serviceProvider, ILogger<StorageCleanupService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cleanupIntervalDays = _configuration.GetValue<int>("Storage:CleanupIntervalDays", 30);
            var autoCleanupEnabled = _configuration.GetValue<bool>("Storage:AutoCleanupEnabled", true);
            
            if (!autoCleanupEnabled)
            {
                _logger.LogInformation("Automatic storage cleanup is disabled");
                return;
            }

            _logger.LogInformation("Storage cleanup service started with {IntervalDays} day interval", cleanupIntervalDays);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
                    
                    using var scope = _serviceProvider.CreateScope();
                    var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
                    
                    // Check storage health
                    var healthStatus = await storageService.CheckStorageHealthAsync();
                    
                    if (healthStatus == StorageHealthStatus.Warning || healthStatus == StorageHealthStatus.Critical)
                    {
                        _logger.LogInformation("Storage health is {HealthStatus}, performing cleanup", healthStatus);
                        
                        // Cleanup unused tags first (less risky)
                        var tagCleanupResult = await storageService.CleanupUnusedTagsAsync();
                        if (tagCleanupResult.Success)
                        {
                            _logger.LogInformation("Tag cleanup completed: {ItemsRemoved} items removed, {SpaceFreed} bytes freed", 
                                tagCleanupResult.ItemsRemoved, tagCleanupResult.SpaceFreed);
                        }
                        else
                        {
                            _logger.LogWarning("Tag cleanup failed: {Errors}", string.Join("; ", tagCleanupResult.Errors));
                        }
                        
                        // If still critical, cleanup old data
                        if (healthStatus == StorageHealthStatus.Critical)
                        {
                            var dataCleanupResult = await storageService.CleanupOldDataAsync(365); // Keep 1 year
                            if (dataCleanupResult.Success)
                            {
                                _logger.LogInformation("Data cleanup completed: {ItemsRemoved} items removed, {SpaceFreed} bytes freed", 
                                    dataCleanupResult.ItemsRemoved, dataCleanupResult.SpaceFreed);
                            }
                            else
                            {
                                _logger.LogError("Data cleanup failed: {Errors}", string.Join("; ", dataCleanupResult.Errors));
                            }
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Storage health is {HealthStatus}, no cleanup needed", healthStatus);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic storage cleanup");
                    // Continue running despite errors
                }
            }
            
            _logger.LogInformation("Storage cleanup service stopped");
        }
    }
}