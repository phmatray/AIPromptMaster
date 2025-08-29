---
name: user-mgmt-system
status: completed
created: 2025-08-29T08:08:45Z
progress: 100%
completed: 2025-08-29T15:00:00Z
prd: .claude/prds/user-mgmt-system.md
github: https://github.com/phmatray/AIPromptMaster/issues/2
---

# Epic: User Management System

## Overview
Implement a lightweight, secure user authentication and authorization system using ASP.NET Core Identity, focusing on essential features first while leveraging existing application patterns and infrastructure. The system will reuse the existing service layer pattern, Blazor components structure, and database configuration to minimize new code.

## Architecture Decisions

### Core Decisions
- **ASP.NET Core Identity**: Use built-in Identity framework to avoid reinventing security features
- **Cookie Authentication**: Leverage Blazor Server's existing session management instead of JWT initially
- **Existing Database**: Extend current SQLite/PostgreSQL setup with Identity tables
- **Service Pattern Reuse**: Follow existing IService/Service pattern for user operations
- **Minimal UI Changes**: Add auth to existing layouts rather than creating new ones

### Simplification Strategy
- **No Custom Identity Provider**: Use default Identity with minimal customization
- **Leverage Scaffolding**: Use Identity UI scaffolding for login/register pages
- **Skip Team Management Initially**: Focus on user-level permissions first
- **Reuse Validation Service**: Extend existing ValidationService for user input
- **Built-in Email**: Use .NET's SmtpClient initially instead of third-party services

## Technical Approach

### Frontend Components
- **Reuse Existing Components**:
  - Extend MainLayout.razor with authentication state
  - Add AuthorizeView to existing pages
  - Reuse ToastService for auth notifications
  - Leverage existing form components for profile editing

- **Minimal New Components**:
  - LoginDisplay.razor (user info/logout in nav)
  - Simple profile page using existing form patterns

### Backend Services
- **Extend Existing Services**:
  - Add UserId to Prompt entity (migration)
  - Update PromptService to filter by current user
  - Extend ValidationService for email/password validation

- **New Services (Following Existing Pattern)**:
  - IUserService/UserService for profile management
  - Reuse existing ErrorHandlingService for auth errors

### Infrastructure
- **Database Changes**:
  - Single migration to add Identity tables
  - Add UserId foreign key to Prompts table
  - Reuse existing migration approach

- **Configuration**:
  - Add Identity configuration to Program.cs
  - Extend existing appsettings.json
  - Reuse existing health check pattern

## Implementation Strategy

### Phase 1: Core Authentication (Week 1-2)
Focus on getting basic auth working with minimal changes:
1. Add Identity to project and database
2. Scaffold Identity UI pages
3. Add user association to prompts
4. Implement basic authorization

### Phase 2: Enhancement (Week 3)
Polish and extend with essential features:
1. Email confirmation (optional initially)
2. Password reset functionality
3. Basic admin role
4. Profile management

### Risk Mitigation
- Use Identity defaults to avoid security mistakes
- Extensive testing with existing test patterns
- Gradual rollout with feature flags
- Keep non-auth path working during transition

## Task Breakdown Preview

High-level task categories (keeping it under 10 tasks):

- [ ] **Task 1: Identity Setup** - Add ASP.NET Core Identity packages and configure in Program.cs
- [ ] **Task 2: Database Migration** - Create migration for Identity tables and UserId in Prompts
- [ ] **Task 3: Identity UI Scaffolding** - Scaffold login, register, and manage pages
- [ ] **Task 4: Authentication Integration** - Add AuthenticationStateProvider to Blazor components
- [ ] **Task 5: Prompt Authorization** - Update PromptService to filter by authenticated user
- [ ] **Task 6: UI Auth Components** - Add LoginDisplay and update MainLayout with auth state
- [ ] **Task 7: User Profile Service** - Create IUserService for profile management operations
- [ ] **Task 8: Admin Features** - Add basic admin role and admin-only pages
- [ ] **Task 9: Email & Password Reset** - Configure email service and password reset flow

## Dependencies

### Internal Dependencies
- Existing service layer pattern
- Current database setup (EF Core)
- Blazor Server infrastructure
- Validation and error handling services

### External Dependencies (Minimal)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- Microsoft.AspNetCore.Identity.UI (for scaffolding)
- SMTP configuration for email (use existing email server)

### Prerequisite Work
- None - can build on current codebase as-is

## Success Criteria (Technical)

### Performance
- Authentication adds <100ms to page load
- No degradation of existing features
- Session management within Blazor Server limits

### Quality Gates
- All existing tests continue passing
- Auth-specific unit tests achieve 80% coverage
- No security warnings from code analysis

### Acceptance Criteria
- Users can register and log in
- Prompts are properly filtered by user
- Password reset works via email
- Admin can view all prompts
- Existing functionality remains intact

## Estimated Effort

### Timeline
- **Total Duration**: 3 weeks
- **Phase 1 (Core)**: 2 weeks
- **Phase 2 (Polish)**: 1 week

### Resource Requirements
- 1 developer (full-time)
- Existing infrastructure (no new services)
- Minimal additional hosting costs

### Critical Path
1. Identity setup and migration (blocks everything)
2. UI scaffolding (blocks user experience)
3. Service integration (blocks functionality)
4. Testing and deployment

## Simplification Notes

### What We're NOT Building (Initially)
- Complex team management (use simple roles instead)
- External auth providers (avoid OAuth complexity)
- JWT tokens (cookie auth is sufficient for Blazor Server)
- Custom identity stores (use defaults)
- Audit logging (can add later)
- 2FA (Phase 2 consideration)

### Leveraging Existing Code
- Service layer pattern already established
- Validation service can handle user input
- Error handling middleware works for auth errors
- Toast notifications ready for auth messages
- Database configuration and migrations pattern exists
- Responsive UI components can be reused

This approach minimizes new code while delivering core authentication functionality quickly and securely.

## Tasks Created
- [ ] 001.md - Identity Setup and Configuration (parallel: false)
- [ ] 002.md - Database Migration for Identity (parallel: false, depends on: 001)
- [ ] 003.md - Identity UI Scaffolding (parallel: false, depends on: 002)
- [ ] 004.md - Authentication Integration (parallel: true, depends on: 003)
- [ ] 005.md - Prompt Authorization (parallel: true, depends on: 002, 004)
- [ ] 006.md - UI Auth Components (parallel: true, depends on: 003, 004)
- [ ] 007.md - User Profile Service (parallel: true, depends on: 002)
- [ ] 008.md - Admin Features (parallel: true, depends on: 002, 005)
- [ ] 009.md - Email & Password Reset (parallel: true, depends on: 003)

**Total tasks**: 9
**Parallel tasks**: 6 (can run after dependencies met)
**Sequential tasks**: 3 (foundation tasks)
**Estimated total effort**: ~60-80 hours (2-3 weeks)