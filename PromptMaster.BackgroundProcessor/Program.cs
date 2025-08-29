using PromptMaster.BackgroundProcessor.Data;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire services to the container.
builder.AddServiceDefaults();

// Add Database context
builder.AddNpgsqlDbContext<BackgroundProcessorContext>("bg-processor-db");

// Add and configure TickerQ
builder.Services.AddTickerQ(options =>
{
    options.AddOperationalStore<BackgroundProcessorContext>(efOptions =>
    {
        efOptions.UseModelCustomizerForMigrations();
        efOptions.CancelMissedTickersOnAppStart();
    });
    
    options.AddDashboard(configuration =>
    {
        configuration.BasePath = "/tickerq";
        configuration.EnableBasicAuth = true;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var serviceScope = app.Services.CreateScope();
    var dbContext = serviceScope.ServiceProvider.GetRequiredService<BackgroundProcessorContext>();
    await DbSeeder.SeedAsync(dbContext);
}

app.UseTickerQ();

app.Run();