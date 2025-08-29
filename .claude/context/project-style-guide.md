---
created: 2025-08-29T07:40:21Z
last_updated: 2025-08-29T07:40:21Z
version: 1.0
author: Claude Code PM System
---

# Project Style Guide

## Coding Standards

### C# Conventions

#### Naming Conventions
```csharp
// Classes: PascalCase
public class PromptService { }

// Interfaces: I + PascalCase
public interface IPromptService { }

// Methods: PascalCase
public async Task<Prompt> GetPromptAsync(int id) { }

// Parameters and variables: camelCase
public void ProcessPrompt(string promptText, int maxLength) 
{
    var processedText = promptText.Trim();
}

// Private fields: _camelCase
private readonly ILogger<PromptService> _logger;
private readonly PromptManagerContext _context;

// Constants: UPPER_CASE
public const int MAX_PROMPT_LENGTH = 5000;

// Properties: PascalCase
public string Title { get; set; }
```

#### File Organization
```csharp
// Standard file structure
using System;                      // System namespaces first
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;    // Third-party namespaces
using Microsoft.EntityFrameworkCore;

using AIPromptManager.Models;       // Project namespaces
using AIPromptManager.Services;

namespace AIPromptManager.Controllers
{
    // Class documentation
    /// <summary>
    /// Manages prompt operations
    /// </summary>
    public class PromptController : Controller
    {
        // Fields
        private readonly IPromptService _promptService;
        
        // Constructor
        public PromptController(IPromptService promptService)
        {
            _promptService = promptService;
        }
        
        // Public methods
        public async Task<IActionResult> Index() { }
        
        // Private methods
        private void ValidatePrompt(Prompt prompt) { }
    }
}
```

#### Async/Await Pattern
```csharp
// Always use Async suffix for async methods
public async Task<Prompt> GetPromptAsync(int id)
{
    return await _context.Prompts
        .Include(p => p.PromptTags)
        .FirstOrDefaultAsync(p => p.Id == id);
}

// Configure await where appropriate
await DoSomethingAsync().ConfigureAwait(false);
```

#### Exception Handling
```csharp
try
{
    // Specific operation
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Handle specific exception
    _logger.LogWarning(ex, "Concurrency conflict for prompt {Id}", id);
    throw new ConcurrencyException("The prompt was modified by another user.");
}
catch (Exception ex)
{
    // Log and wrap generic exceptions
    _logger.LogError(ex, "Error saving prompt {Id}", id);
    throw new ServiceException("Failed to save prompt", ex);
}
```

### Razor Component Conventions

#### Component Structure
```razor
@page "/prompts"
@using AIPromptManager.Models
@using AIPromptManager.Services
@inject IPromptService PromptService
@inject IToastService ToastService

<PageTitle>Prompts</PageTitle>

<div class="container mx-auto p-4">
    @if (isLoading)
    {
        <LoadingSpinner />
    }
    else
    {
        <PromptList Prompts="prompts" />
    }
</div>

@code {
    private List<Prompt> prompts = new();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadPrompts();
    }

    private async Task LoadPrompts()
    {
        isLoading = true;
        prompts = await PromptService.GetAllPromptsAsync();
        isLoading = false;
    }
}
```

#### Component Naming
- Components: PascalCase (e.g., `PromptList.razor`)
- Pages: Match route (e.g., `Create.razor` for `/create`)
- Shared: Descriptive names (e.g., `TagSelector.razor`)

### CSS/Tailwind Conventions

#### Class Organization
```html
<!-- Order: Layout → Spacing → Typography → Colors → Effects -->
<div class="flex flex-col gap-4 p-6 text-lg font-semibold text-gray-800 bg-white rounded-lg shadow-md hover:shadow-lg">
```

#### Custom CSS Structure
```css
/* app.css organization */

/* 1. Tailwind directives */
@tailwind base;
@tailwind components;
@tailwind utilities;

/* 2. Custom base styles */
@layer base {
    h1 {
        @apply text-3xl font-bold;
    }
}

/* 3. Custom components */
@layer components {
    .btn-primary {
        @apply px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600;
    }
}

/* 4. Custom utilities */
@layer utilities {
    .animation-delay-200 {
        animation-delay: 200ms;
    }
}
```

### Database Conventions

#### Entity Framework Patterns
```csharp
// Entity configuration
public class PromptConfiguration : IEntityTypeConfiguration<Prompt>
{
    public void Configure(EntityTypeBuilder<Prompt> builder)
    {
        builder.ToTable("Prompts");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.HasIndex(p => p.Title)
            .HasDatabaseName("IX_Prompts_Title");
    }
}
```

#### Migration Naming
```bash
# Descriptive migration names
dotnet ef migrations add AddPromptTable
dotnet ef migrations add AddIndexToPromptTitle
dotnet ef migrations add UpdatePromptAddRowVersion
```

## Code Documentation

### XML Documentation
```csharp
/// <summary>
/// Retrieves a prompt by its identifier.
/// </summary>
/// <param name="id">The unique identifier of the prompt.</param>
/// <returns>The prompt if found; otherwise, null.</returns>
/// <exception cref="ArgumentException">Thrown when id is less than 1.</exception>
public async Task<Prompt?> GetPromptAsync(int id)
{
    if (id < 1)
        throw new ArgumentException("Invalid prompt ID", nameof(id));
        
    return await _context.Prompts.FindAsync(id);
}
```

### Inline Comments
```csharp
// Use comments sparingly for complex logic
public void ProcessPrompt(string text)
{
    // Remove potential XSS vectors while preserving legitimate content
    var sanitized = HtmlEncoder.Default.Encode(text);
    
    // Apply business rules for prompt validation
    if (sanitized.Length > MAX_PROMPT_LENGTH)
    {
        // Truncate but preserve word boundaries
        var lastSpace = sanitized.LastIndexOf(' ', MAX_PROMPT_LENGTH);
        sanitized = sanitized.Substring(0, lastSpace) + "...";
    }
}
```

## Git Conventions

### Branch Naming
```bash
feature/add-prompt-export       # New features
bugfix/fix-search-pagination    # Bug fixes
hotfix/security-patch-xss       # Emergency fixes
refactor/optimize-queries        # Code improvements
docs/update-readme               # Documentation
```

### Commit Messages
```bash
# Format: <type>: <description>

feat: Add prompt export functionality
fix: Resolve search pagination issue
docs: Update installation instructions
style: Format code according to standards
refactor: Optimize database queries
test: Add unit tests for PromptService
chore: Update dependencies
```

### Pull Request Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guide
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No warnings in build
```

## Project Structure Standards

### Directory Naming
- Use PascalCase for C# project folders
- Use lowercase for web assets folders
- Use kebab-case for script folders

### File Placement Rules
1. Controllers in `Controllers/`
2. Services in `Services/`
3. Models in `Models/`
4. Components in `Components/`
5. Shared components in `Components/Shared/`
6. Pages in `Components/Pages/`
7. Static files in `wwwroot/`

## Testing Standards

### Test Naming Convention
```csharp
[Fact]
public async Task GetPromptAsync_WithValidId_ReturnsPrompt()
{
    // Arrange
    var id = 1;
    
    // Act
    var result = await _service.GetPromptAsync(id);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(id, result.Id);
}
```

### Test Organization
```
Tests/
├── Unit/
│   ├── Services/
│   ├── Models/
│   └── Validators/
├── Integration/
│   ├── Database/
│   └── API/
└── E2E/
    └── Scenarios/
```

## Performance Guidelines

### Query Optimization
```csharp
// Use projection for read-only operations
var promptTitles = await _context.Prompts
    .Where(p => p.IsActive)
    .Select(p => new { p.Id, p.Title })
    .ToListAsync();

// Use Include for related data
var promptWithTags = await _context.Prompts
    .Include(p => p.PromptTags)
    .ThenInclude(pt => pt.Tag)
    .FirstOrDefaultAsync(p => p.Id == id);

// Use compiled queries for frequently used queries
private static readonly Func<PromptManagerContext, int, Task<Prompt>> GetPromptById =
    EF.CompileAsyncQuery((PromptManagerContext context, int id) =>
        context.Prompts.FirstOrDefault(p => p.Id == id));
```

### Memory Management
```csharp
// Dispose resources properly
using var context = new PromptManagerContext();
await using var transaction = await context.Database.BeginTransactionAsync();

// Use streaming for large datasets
await foreach (var prompt in GetPromptsAsyncEnumerable())
{
    ProcessPrompt(prompt);
}
```

## Security Standards

### Input Validation
```csharp
// Always validate and sanitize input
public IActionResult Create([FromBody] PromptDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
        
    var sanitized = _validationService.SanitizeInput(dto);
    // Process sanitized input
}
```

### Authentication Patterns (Future)
```csharp
[Authorize]
[RequireHttps]
public class SecureController : Controller
{
    [Authorize(Roles = "Admin")]
    public IActionResult AdminAction() { }
}
```

## Accessibility Standards

### HTML Semantics
```html
<!-- Use semantic HTML -->
<nav aria-label="Main navigation">
<main role="main">
<article>
<section aria-labelledby="section-title">

<!-- Provide alt text -->
<img src="logo.png" alt="AI Prompt Manager Logo">

<!-- Use ARIA labels -->
<button aria-label="Delete prompt" title="Delete">
```

### Keyboard Navigation
- All interactive elements keyboard accessible
- Tab order logical and predictable
- Focus indicators visible
- Skip links for navigation