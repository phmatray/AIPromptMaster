using AIPromptManager.Components;
using AIPromptManager.Data;
using AIPromptManager.Middleware;
using AIPromptManager.Models;
using AIPromptManager.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire services to the container.
builder.AddServiceDefaults();

// Add Database context
builder.AddNpgsqlDbContext<PromptManagerContext>("prompt-manager-db");

// Add Identity services
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Password requirements
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<PromptManagerContext>();

// Configure cookie authentication for Blazor Server
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    
    // Cookie settings
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add authorization services with policies
builder.Services.AddAuthorization(options =>
{
    // Admin-only policy
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    // Admin or owner policy for resource-specific access
    options.AddPolicy("AdminOrOwner", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == 
            context.Resource?.ToString()));
    
    // Default user policy
    options.AddPolicy("UserOnly", policy =>
        policy.RequireRole("User", "Admin"));
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add API controllers
builder.Services.AddControllers();

// Add HttpContextAccessor for Blazor Server
builder.Services.AddHttpContextAccessor();

// Add services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IPromptService, PromptService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
builder.Services.AddScoped<IPerformanceMonitoringService, PerformanceMonitoringService>();
builder.Services.AddSingleton<IComponentOptimizationService, ComponentOptimizationService>();
builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<AIPromptManager.Services.IEmailSender<ApplicationUser>, EmailSender>();

// Add background services
builder.Services.AddHostedService<StorageCleanupService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Seed the database (both development and production)
using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;
    var dbContext = services.GetRequiredService<PromptManagerContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    // Seed roles and admin user first
    await DbSeeder.SeedRolesAndAdminAsync(roleManager, userManager, logger);
    
    // Then seed the database with sample data
    await DbSeeder.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API controllers
app.MapControllers();

// Map logout endpoint
app.MapPost("/Account/Logout", async (HttpContext context, SignInManager<ApplicationUser> signInManager, ILogger<Program> logger) =>
{
    if (signInManager.IsSignedIn(context.User))
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("User logged out.");
    }
    
    var returnUrl = context.Request.Form["ReturnUrl"].FirstOrDefault();
    if (!string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        return Results.Redirect(returnUrl);
    }
    
    return Results.Redirect("/");
});

app.Run();
