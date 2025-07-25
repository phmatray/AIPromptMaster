# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an AI Prompt Manager built with Blazor Server (.NET 9) and Entity Framework Core with SQLite. The application helps users organize and manage AI prompts with tagging, search, and CRUD functionality.

## Technology Stack

- **Framework**: .NET 9 with Blazor Server
- **Database**: SQLite with Entity Framework Core 9.0.7
- **Styling**: Tailwind CSS with custom component classes
- **Project Structure**: Single project at `AIPromptManager/`

## Common Development Commands

### Build and Run
```bash
cd AIPromptManager
dotnet build
dotnet run

# Run with specific environment
dotnet run --environment Development
dotnet run --environment Production

# Watch mode for development (auto-rebuilds on changes)
dotnet watch run
```

### Database Operations
```bash
cd AIPromptManager

# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Reset database (removes and recreates)
dotnet ef database drop -f
dotnet ef database update
```

### CSS Development
```bash
cd AIPromptManager

# Install npm dependencies (first time setup)
npm install

# Watch mode for development
npm run build-css-watch

# Production build (minified)
npm run build-css-prod

# Standard build
npm run build-css
```

### Testing
```bash
# Run tests (when tests are added)
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test AIPromptManager.Tests
```

### Port Configuration
Default URLs: 
- HTTP: http://localhost:5291
- HTTPS: https://localhost:7134

Configured in `Properties/launchSettings.json`

## Architecture Overview

### Core Domain Models (`Models/`)
- **Prompt**: Main entity with Title, Description, Content, Tags collection, and optimistic concurrency control via RowVersion
- **Tag**: Name (unique), Prompts collection for many-to-many relationship, CreatedAt timestamp

### Data Access Layer (`Data/`)
- **PromptManagerContext**: EF Core DbContext configuring entities, relationships, and constraints
- **DbSeeder**: Populates initial sample data on application startup
- **Migrations**: Database schema versioning in `Migrations/` folder

### Service Layer Pattern (`Services/`)
All business logic flows through services with dependency injection:
- **IPromptService/PromptService**: CRUD operations, search, concurrency handling
- **ITagService/TagService**: Tag creation, cleanup of unused tags, duplicate prevention
- **IToastService/ToastService**: Cross-component notification system

### Component Architecture
- **Pages** (`Components/Pages/`): Route-based page components
  - Home: Landing page with quick actions
  - Prompts: List all prompts with filtering
  - Create: New prompt creation
  - Edit: Update existing prompts
  - Search: Advanced search functionality
- **Shared Components** (`Components/Shared/`): Reusable UI components
  - PromptForm: Unified create/edit form
  - TagInput: Tag management with autocomplete
  - PromptCard/PromptList: Display components
  - SearchBar: Real-time search filtering
  - ToastContainer/Toast: Notification system
  - ConfirmDialog: Deletion confirmation
- **Layout** (`Components/Layout/`): Application shell
  - MainLayout: Responsive sidebar/content layout
  - NavMenu: Navigation with mobile support

### Key Implementation Details

1. **Concurrency Control**: 
   - RowVersion byte[] property on Prompt entity
   - DbUpdateConcurrencyException handling in services
   - User-friendly conflict resolution messages

2. **Tag Management**:
   - Case-insensitive duplicate prevention
   - Automatic cleanup of orphaned tags
   - Many-to-many relationship via join table

3. **Responsive Design**:
   - Mobile-first approach with Tailwind CSS
   - Custom breakpoints (xs: 475px)
   - Collapsible sidebar navigation
   - Touch-friendly components

4. **State Management**:
   - Blazor Server-side state management
   - Service-scoped dependencies
   - Toast notifications via singleton service

### Database Schema

```sql
Prompts:
- Id (int, PK)
- Title (nvarchar(200), required)
- Description (nvarchar(500))
- Content (nvarchar(max), required)
- CreatedAt (datetime2, required)
- UpdatedAt (datetime2, required)
- RowVersion (varbinary(max), concurrency token)

Tags:
- Id (int, PK)
- Name (nvarchar(50), required, unique)
- CreatedAt (datetime2, required)

PromptTags (join table):
- PromptId (int, FK)
- TagId (int, FK)
- PK: (PromptId, TagId)
```

### Tailwind CSS Configuration

- Source: `wwwroot/app.css`
- Output: `wwwroot/dist/app.css`
- Config: `tailwind.config.js`
- Custom components documented in `TAILWIND_README.md`
- Auto-built via MSBuild targets

### Development Workflow

1. **Adding a new feature**:
   - Create/update domain models if needed
   - Add EF migration: `dotnet ef migrations add FeatureName`
   - Implement service interface and class
   - Register service in `Program.cs`
   - Create Razor components
   - Update navigation if needed

2. **Modifying the database**:
   - Update entity classes
   - Update `PromptManagerContext` configuration
   - Create migration: `dotnet ef migrations add MigrationName`
   - Update database: `dotnet ef database update`

3. **Adding new Tailwind styles**:
   - Edit `wwwroot/app.css`
   - Run `npm run build-css-watch` for development
   - Styles auto-build on dotnet build

### Common Issues and Solutions

1. **Database locked**: Stop all running instances of the application
2. **Migration conflicts**: Remove last migration and recreate
3. **CSS not updating**: Ensure npm packages installed and rebuild
4. **Concurrency errors**: Implement retry logic or user prompts

### Performance Considerations

- Blazor Server uses SignalR for real-time updates
- Database queries use async/await patterns
- Tag queries are optimized with includes
- Consider pagination for large datasets (not yet implemented)