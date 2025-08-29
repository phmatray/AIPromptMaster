---
created: 2025-08-29T07:40:21Z
last_updated: 2025-08-29T07:40:21Z
version: 1.0
author: Claude Code PM System
---

# Project Structure

## Solution Architecture
```
PromptMaster.sln
├── AIPromptManager/              # Main Blazor Server application
├── PromptMaster.AppHost/         # Aspire orchestrator
├── PromptMaster.BackgroundProcessor/  # Background job service
└── PromptMaster.ServiceDefaults/ # Shared service configurations
```

## AIPromptManager Structure
```
AIPromptManager/
├── Components/
│   ├── Layout/               # Application layout components
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   ├── Pages/               # Routable page components
│   │   ├── Home.razor
│   │   ├── Create.razor
│   │   ├── Edit.razor
│   │   └── Error.razor
│   └── Shared/              # Reusable components
│       ├── PromptList.razor
│       ├── PromptForm.razor
│       ├── TagSelector.razor
│       └── ToastNotification.razor
├── Data/                    # Database context and configurations
│   ├── PromptManagerContext.cs
│   └── Configurations/
├── Migrations/              # EF Core database migrations
├── Models/                  # Domain entities
│   ├── Prompt.cs
│   ├── Tag.cs
│   └── PromptTag.cs
├── Services/                # Business logic layer
│   ├── IPromptService.cs
│   ├── PromptService.cs
│   ├── ITagService.cs
│   ├── TagService.cs
│   ├── IValidationService.cs
│   ├── ValidationService.cs
│   ├── IStorageService.cs
│   ├── StorageService.cs
│   ├── IPerformanceMonitoringService.cs
│   ├── PerformanceMonitoringService.cs
│   ├── IToastService.cs
│   ├── ToastService.cs
│   └── ErrorHandlingService.cs
├── wwwroot/                 # Static assets
│   ├── css/
│   ├── dist/               # Compiled Tailwind CSS
│   └── app.css            # Source CSS
├── Properties/
│   └── launchSettings.json
├── appsettings.json        # Application configuration
├── appsettings.Development.json
├── Program.cs              # Application entry point
├── package.json            # Node.js dependencies (Tailwind)
├── tailwind.config.js      # Tailwind configuration
└── AIPromptManager.csproj  # Project file
```

## PromptMaster.AppHost Structure
```
PromptMaster.AppHost/
├── AppHost.cs              # Aspire application model
├── Properties/
│   └── launchSettings.json
├── appsettings.json
└── PromptMaster.AppHost.csproj
```

## PromptMaster.BackgroundProcessor Structure
```
PromptMaster.BackgroundProcessor/
├── Data/
│   ├── BackgroundProcessorContext.cs
│   └── DbSeeder.cs
├── Jobs/
│   └── MyJobs.cs           # Background job definitions
├── Migrations/             # Service-specific migrations
├── Program.cs
├── appsettings.json
└── PromptMaster.BackgroundProcessor.csproj
```

## PromptMaster.ServiceDefaults Structure
```
PromptMaster.ServiceDefaults/
├── Extensions.cs           # Service extension methods
└── PromptMaster.ServiceDefaults.csproj
```

## Configuration Files
```
Root/
├── .gitignore             # Git ignore patterns
├── LICENSE                # MIT License
├── README.md              # Claude Code PM documentation
├── CLAUDE.md              # Claude Code instructions
├── PRODUCTION_PLAN.md     # Production deployment guide
├── AGENTS.md              # Agent documentation
├── COMMANDS.md            # Command reference
└── PromptMaster.sln.DotSettings  # IDE settings
```

## Claude Code PM Structure
```
.claude/
├── context/               # Project context files
│   └── *.md              # Context documentation
├── docs/                  # Additional documentation
├── prd/                   # Product requirement documents
├── scripts/               # PM automation scripts
│   └── pm/               # Project management scripts
│       ├── init.sh
│       └── *.sh
└── CLAUDE.md             # Development rules
```

## File Naming Conventions

### C# Files
- **Interfaces**: `I{Name}.cs` (e.g., `IPromptService.cs`)
- **Services**: `{Name}Service.cs` (e.g., `PromptService.cs`)
- **Models**: `{Name}.cs` (e.g., `Prompt.cs`)
- **Components**: `{Name}.razor` (e.g., `PromptList.razor`)

### Configuration
- **JSON**: `appsettings.{Environment}.json`
- **Launch**: `launchSettings.json`

### Web Assets
- **CSS**: lowercase with hyphens (e.g., `app.css`)
- **JavaScript**: camelCase (e.g., `tailwind.config.js`)

## Module Organization

### Separation of Concerns
1. **Models**: Pure domain entities
2. **Data**: Database context and configurations
3. **Services**: Business logic and operations
4. **Components**: UI presentation and interaction
5. **wwwroot**: Static assets and client resources

### Dependency Flow
```
Components → Services → Data/Models
    ↓           ↓
  wwwroot    Database
```

### Service Registration Pattern
All services registered in `Program.cs`:
- Scoped services for per-request lifetime
- Singleton for cross-component communication (ToastService)
- Transient for stateless operations