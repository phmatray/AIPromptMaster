using PromptMaster.BackgroundProcessor.Data;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire services to the container.
builder.AddServiceDefaults();

// Add Database context
builder.AddNpgsqlDbContext<BackgroundProcessorContext>("bg-processor-db");

// The dashboard ships with a working test account so anyone cloning this repo can
// open /tickerq straight away - it is documented in the README on purpose. A real
// deployment overrides it through TickerQ__Dashboard__Username / __Password.
var dashboardUser = builder.Configuration["TickerQ:Dashboard:Username"] ?? "admin";
var dashboardPassword = builder.Configuration["TickerQ:Dashboard:Password"] ?? "tickerq";

// Add and configure TickerQ
builder.Services.AddTickerQ(options =>
{
    // 10.3 dropped the <TDbContext> type argument here: the store is now told which
    // context to use from the inside, which is what lets it reuse the one Aspire
    // already registered above instead of configuring a second connection.
    options.AddOperationalStore(efOptions =>
    {
        efOptions.UseApplicationDbContext<BackgroundProcessorContext>(
            ConfigurationType.IgnoreModelCustomizer);
    });

    // Replaces CancelMissedTickersOnAppStart(): occurrences whose window elapsed while
    // the processor was down are skipped rather than fired late in a burst.
    options.SkipStaleCronOccurrencesOnStartup();

    options.AddDashboard(dashboard =>
    {
        dashboard.SetBasePath("/tickerq");
        dashboard.WithBasicAuth(dashboardUser, dashboardPassword);
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
