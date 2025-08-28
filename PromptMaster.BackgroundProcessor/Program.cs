using Microsoft.EntityFrameworkCore;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire services to the container.
builder.AddServiceDefaults();

// Add Database context
builder.AddNpgsqlDbContext<MyDbContext>("postgresdb");

// Add and configure TickerQ
builder.Services.AddTickerQ(options =>
{
    options.AddOperationalStore<MyDbContext>(efOptions =>
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
    var dbContext = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseTickerQ();

app.Run();