using Microsoft.EntityFrameworkCore;

namespace PromptMaster.BackgroundProcessor.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(BackgroundProcessorContext dbContext)
    {
        await dbContext.Database.MigrateAsync();
    }
    
}