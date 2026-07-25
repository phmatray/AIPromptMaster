using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Configurations;
using TickerQ.Utilities.Entities;

namespace PromptMaster.BackgroundProcessor.Data;

public class BackgroundProcessorContext(
    DbContextOptions<BackgroundProcessorContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // TickerQ 10.3 made these configurations generic over the ticker entity types,
        // so a host can persist its own subclasses. This app uses the built-in ones.
        // They stay applied by hand rather than through TickerQ's model customizer:
        // InitialCreate was generated from this model, and handing ownership to the
        // customizer would move the three ticker tables out of the snapshot.
        builder.ApplyConfiguration(new TimeTickerConfigurations<TimeTickerEntity>());
        builder.ApplyConfiguration(new CronTickerConfigurations<CronTickerEntity>());
        builder.ApplyConfiguration(new CronTickerOccurrenceConfigurations<CronTickerEntity>());
    }
}
