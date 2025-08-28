# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AI Prompt Manager - A Blazor Server application for organizing and managing AI prompts with enterprise-grade features including tagging, search, validation, performance monitoring, and storage management. The application has been recently integrated with .NET Aspire for cloud-native deployment and observability.

## Solution Structure

The solution consists of 4 projects:
- **AIPromptManager**: Main Blazor Server application (UI and business logic)
- **PromptMaster.AppHost**: Aspire orchestrator for local development
- **PromptMaster.BackgroundProcessor**: Background processing service
- **PromptMaster.ServiceDefaults**: Shared Aspire service defaults (health checks, telemetry)

## Common Development Commands

### Running the Application

```bash
# Standard run (AIPromptManager only)
cd AIPromptManager
dotnet run

# Run with Aspire orchestration (recommended for development)
dotnet run --project PromptMaster.AppHost

# Watch mode for development
cd AIPromptManager
dotnet watch run

# Run specific environment
dotnet run --environment Development
dotnet run --environment Production
```

### Database Management

```bash
cd AIPromptManager

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Reset database
dotnet ef database drop -f
dotnet ef database update
```

### CSS/Tailwind Development

```bash
cd AIPromptManager

# Install dependencies (first time)
npm install

# Watch mode for CSS development
npm run build-css-watch

# Production build (minified)
npm run build-css-prod

# Standard build
npm run build-css
```

### Testing & Quality

```bash
# Run tests (when implemented)
dotnet test

# Build solution
dotnet build

# Clean and rebuild
dotnet clean
dotnet build
```

## Architecture & Key Components

### Core Architecture Pattern
The application follows a Service Layer Pattern with dependency injection:
1. **Controllers/Components** → Call services via interfaces
2. **Services** → Implement business logic, handle errors
3. **DbContext** → Manages database operations
4. **Models** → Define domain entities

### Service Layer (`AIPromptManager/Services/`)
- **IPromptService**: CRUD operations, search, concurrency handling
- **ITagService**: Tag management, orphan cleanup, duplicate prevention
- **IValidationService**: Input validation, security checks
- **IStorageService**: Storage monitoring, cleanup operations
- **IPerformanceMonitoringService**: Performance metrics tracking
- **IToastService**: Cross-component notifications (Singleton)
- **ErrorHandlingService**: Global error handling

### Data Layer
- **Database**: SQLite with EF Core 9
- **Concurrency**: Optimistic concurrency using RowVersion
- **Relationships**: Many-to-many between Prompts and Tags
- **Indexes**: Performance indexes on frequently queried columns

### Aspire Integration
- **Health Checks**: Available at `/health` and `/alive` endpoints
- **OpenTelemetry**: Configured for metrics, tracing, and logging
- **Service Discovery**: Enabled for inter-service communication
- **Resilience**: Standard resilience handlers for HTTP clients

### Component Structure
```
Components/
├── Pages/           # Routable pages
├── Shared/          # Reusable components
├── Layout/          # Application layout
└── Features/        # Feature-specific components
```

## Key Implementation Notes

### Concurrent Edit Handling
The application uses optimistic concurrency control:
- `RowVersion` byte[] on Prompt entity
- Catches `DbUpdateConcurrencyException`
- Provides user-friendly conflict resolution

### Performance Optimizations
- Compiled queries for frequently accessed data
- Eager loading for related entities (Tags)
- Performance indexes on search columns
- Component-level optimization service

### Security & Validation
- XSS prevention through input sanitization
- SQL injection prevention via parameterized queries
- Size limits on all text inputs
- Global exception middleware

### Storage Management
- Automatic cleanup of orphaned data
- Storage monitoring and alerts
- Database size tracking
- File attachment management (when implemented)

### Responsive Design
- Mobile-first with Tailwind CSS
- Custom breakpoints (xs: 475px)
- Touch-friendly interface
- Collapsible navigation

## Development Workflow

### Adding New Features
1. Update domain models if needed
2. Create migration: `dotnet ef migrations add FeatureName`
3. Implement service interface and class
4. Register in `Program.cs`
5. Create Razor components
6. Update navigation if needed

### Modifying Database Schema
1. Update entity classes in `Models/`
2. Update `PromptManagerContext` configuration
3. Create migration: `dotnet ef migrations add MigrationName`
4. Apply: `dotnet ef database update`

### Working with Tailwind CSS
1. Edit `wwwroot/app.css` for custom styles
2. Run `npm run build-css-watch` during development
3. Styles auto-rebuild on `dotnet build`

## Port Configuration

- **AIPromptManager**: http://localhost:5291, https://localhost:7134
- **Aspire Dashboard**: http://localhost:15212 (when using AppHost)
- **Background Processor**: http://localhost:5289, https://localhost:7233

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Database locked | Stop all application instances |
| Migration conflicts | Remove last migration and recreate |
| CSS not updating | Run `npm install` then rebuild |
| Concurrency errors | Reload and retry the operation |
| Aspire dashboard not opening | Ensure running via AppHost project |

## Production Readiness Status

Current implementation includes:
- ✅ Core CRUD functionality
- ✅ Error handling and validation
- ✅ Performance monitoring
- ✅ Storage management
- ✅ Responsive design
- ✅ Aspire integration

Pending for production (see PRODUCTION_PLAN.md):
- ⚠️ Unit and integration tests
- ⚠️ Authentication/authorization
- ⚠️ Rate limiting
- ⚠️ Docker containerization
- ⚠️ CI/CD pipeline