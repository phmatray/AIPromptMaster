---
created: 2025-08-29T07:40:21Z
last_updated: 2025-08-29T07:40:21Z
version: 1.0
author: Claude Code PM System
---

# Technology Context

## Core Technologies

### Primary Stack
- **Framework**: .NET 9.0 (Preview)
- **UI Framework**: Blazor Server
- **Database**: SQLite (Development) / PostgreSQL (Production-ready)
- **ORM**: Entity Framework Core 9.0
- **CSS Framework**: Tailwind CSS 3.4.0
- **Cloud-Native**: .NET Aspire

### Language Versions
- **C#**: 12.0
- **JavaScript**: ES6+ (for Tailwind config)
- **HTML/Razor**: Latest Blazor syntax

## NuGet Dependencies

### AIPromptManager Project
```xml
<PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" Version="9.0.0-rc.2.24474.3" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0-rc.2.24474.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0-rc.2.24474.1" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0-rc.2" />
```

### Aspire Integration
```xml
<PackageReference Include="Aspire.Hosting.AppHost" Version="9.0.0-rc.1.24511.1" />
<PackageReference Include="Aspire.Hosting.PostgreSQL" Version="9.0.0-rc.1.24511.1" />
```

### Background Processor
```xml
<PackageReference Include="Hangfire.Core" Version="1.8.14" />
<PackageReference Include="Hangfire.InMemory" Version="1.0.0" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.14" />
```

## Node.js Dependencies

### Development Dependencies
```json
{
  "devDependencies": {
    "tailwindcss": "^3.4.0"
  }
}
```

## Development Tools

### Required Tools
- **.NET SDK**: 9.0 Preview
- **Node.js**: 18+ (for Tailwind CSS)
- **Git**: Version control
- **GitHub CLI**: Project management integration
- **Entity Framework CLI**: Database migrations

### IDE Support
- **Visual Studio 2022**: 17.11+ Preview
- **Visual Studio Code**: With C# Dev Kit
- **JetBrains Rider**: Latest version

### Command Line Tools
```bash
# .NET Tools
dotnet ef         # Entity Framework Core CLI
dotnet watch      # Hot reload development
dotnet aspire     # Aspire orchestration

# Node.js Tools
npm               # Package management
npx tailwindcss   # CSS compilation

# GitHub Tools
gh                # GitHub CLI
gh-sub-issue      # Issue management extension
```

## Database Technologies

### SQLite (Development)
- Version: Latest
- Connection: `Data Source=promptmanager.db`
- Migrations: Code-first approach
- Features: Lightweight, file-based

### PostgreSQL (Production-Ready)
- Version: 16+
- Connection: Via Aspire configuration
- Features: Full ACID compliance, concurrent access
- Extensions: None required

### Entity Framework Core 9
- Features Used:
  - Code-first migrations
  - Optimistic concurrency (RowVersion)
  - Many-to-many relationships
  - Compiled queries
  - Global query filters

## Frontend Technologies

### Blazor Server
- Rendering: Server-side
- Protocol: SignalR WebSockets
- State: Server-managed sessions
- Components: Razor syntax

### Tailwind CSS
- Version: 3.4.0
- Build: PostCSS pipeline
- Features:
  - Utility-first CSS
  - Custom breakpoints (xs: 475px)
  - Responsive design
  - Dark mode ready

### Font Awesome
- Icons: Web fonts
- Integration: CDN
- Usage: UI icons and indicators

## Cloud-Native Technologies

### .NET Aspire
- **Service Discovery**: Automatic service registration
- **Health Checks**: `/health` and `/alive` endpoints
- **Telemetry**: OpenTelemetry integration
- **Resilience**: HTTP client resilience policies
- **Dashboard**: Development-time observability

### Monitoring Stack
- **Metrics**: OpenTelemetry metrics
- **Tracing**: Distributed tracing
- **Logging**: Structured logging
- **Health**: Liveness and readiness probes

## Security Technologies

### Current Implementation
- **Input Validation**: Custom ValidationService
- **XSS Prevention**: Input sanitization
- **SQL Injection**: Parameterized queries
- **CORS**: Configurable policies
- **HTTPS**: Enforced in production

### Planned Security
- **Authentication**: ASP.NET Core Identity
- **Authorization**: Policy-based
- **Rate Limiting**: Built-in middleware
- **API Keys**: For external integrations

## Build & Deployment

### Build Tools
- **MSBuild**: .NET compilation
- **Tailwind CLI**: CSS compilation
- **npm scripts**: Build automation

### Container Support (Planned)
- **Docker**: Multi-stage builds
- **Docker Compose**: Local orchestration
- **Kubernetes**: Production deployment

### CI/CD (Planned)
- **GitHub Actions**: Automated workflows
- **Testing**: xUnit framework
- **Code Analysis**: Roslyn analyzers
- **Security Scanning**: Dependabot

## Package Management

### NuGet Configuration
- Source: nuget.org
- Framework: net9.0
- Runtime: Portable

### npm Configuration
- Registry: npmjs.org
- Package Manager: npm
- Lock File: package-lock.json

## Version Control

### Git Configuration
- Branch Strategy: GitHub Flow
- Main Branch: main
- Remote: GitHub (phmatray/AIPromptMaster)

### GitHub Integration
- Issues: Project tracking
- Pull Requests: Code review
- Actions: CI/CD (planned)
- Projects: Kanban boards