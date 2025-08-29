using AIPromptManager.Components;
using AIPromptManager.Data;
using AIPromptManager.Middleware;
using AIPromptManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire services to the container.
builder.AddServiceDefaults();

// Add Database context
builder.AddNpgsqlDbContext<PromptManagerContext>("prompt-manager-db");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IPromptService, PromptService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
builder.Services.AddScoped<IPerformanceMonitoringService, PerformanceMonitoringService>();
builder.Services.AddSingleton<IComponentOptimizationService, ComponentOptimizationService>();

// Add background services
builder.Services.AddHostedService<StorageCleanupService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    
    // Seed the database
    using var serviceScope = app.Services.CreateScope();
    var dbContext = serviceScope.ServiceProvider.GetRequiredService<PromptManagerContext>();
    await DbSeeder.SeedAsync(dbContext);
}

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
