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
```

### Database Operations
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

### CSS Development
```bash
cd AIPromptManager

# Watch mode for development
npm run build-css-watch

# Production build (minified)
npm run build-css-prod
```

### Testing
```bash
# Run tests (when tests are added)
dotnet test
```

## Architecture Overview

### Core Domain Models
- **Prompt**: Main entity with Title, Description, Content, Tags, and optimistic concurrency (RowVersion)
- **Tag**: Many-to-many relationship with Prompts, unique constraint on Name

### Service Layer Pattern
All business logic flows through services:
- **IPromptService/PromptService**: Handles all prompt operations with concurrency control
- **ITagService/TagService**: Manages tags with automatic creation and cleanup
- **IToastService/ToastService**: Manages user notifications

### Component Architecture
- **Pages**: Located in `Components/Pages/` - main route handlers
- **Shared Components**: Reusable components in `Components/Shared/`
- **Layout**: Navigation and main layout in `Components/Layout/`

### Key Workflows

1. **Prompt Creation/Edit**: 
   - Uses `PromptForm` component for both create and edit modes
   - Supports tag management with `TagInput` component
   - Implements optimistic concurrency for updates

2. **Search and List**:
   - `SearchBar` component provides real-time filtering
   - `PromptList` displays results with `PromptCard` components
   - Supports edit/delete operations with immediate UI updates

3. **Database Seeding**:
   - `DbSeeder` runs on startup to populate sample data
   - Ensures database migrations are applied

### Tailwind CSS Integration
- Custom components defined in `wwwroot/app.css`
- Built automatically via MSBuild targets
- See `TAILWIND_README.md` for component reference

### Important Patterns
- **Optimistic Concurrency**: All updates check RowVersion to prevent conflicts
- **Cascading Delete**: Deleting prompts automatically cleans up tag relationships
- **Toast Notifications**: All user actions provide feedback via toast system
- **Responsive Design**: Mobile-first approach with custom breakpoints