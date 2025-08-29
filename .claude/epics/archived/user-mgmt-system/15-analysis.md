---
issue: 15
analyzed: 2025-08-29T12:20:00Z
title: Admin Features - Add basic admin role and admin-only pages
---

# Issue #15 Analysis: Admin Features

## Work Streams Identified

### Stream A: Database Setup and Seeding
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Data/DbSeeder.cs (create)
- AIPromptManager/Program.cs (update for seeding)

**Work**:
1. Create DbSeeder class with role and admin user seeding
2. Seed Admin and User roles
3. Create default admin user (admin@promptmaster.com)
4. Update Program.cs to run seeding on startup
5. Add role checking/initialization

**Dependencies**: None (can start immediately)

### Stream B: Admin Service Layer
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Services/IAdminService.cs (create)
- AIPromptManager/Services/AdminService.cs (create)
- AIPromptManager/Models/SystemStatsModel.cs (create)
- AIPromptManager/Program.cs (register service)

**Work**:
1. Define IAdminService interface
2. Create SystemStatsModel
3. Implement AdminService with all methods
4. Add dependency injection registration
5. Implement system statistics gathering

**Dependencies**: Stream A should be complete (needs roles)

### Stream C: Admin UI Components
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Components/Pages/Admin/AdminDashboard.razor (create)
- AIPromptManager/Components/Pages/Admin/AdminUserManagement.razor (create)
- AIPromptManager/Components/Pages/Admin/AdminPromptManagement.razor (create)
- AIPromptManager/Components/Pages/Admin/AdminSystemStats.razor (create)

**Work**:
1. Create admin pages directory structure
2. Implement admin dashboard with statistics
3. Create user management interface
4. Create prompt management interface
5. Add proper authorization attributes

**Dependencies**: Streams A and B should be complete

### Stream D: Authorization and Navigation
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Program.cs (update authorization policies)
- AIPromptManager/Components/Layout/NavMenu.razor (update)
- AIPromptManager/Components/Layout/MainLayout.razor (update if needed)

**Work**:
1. Add authorization policies (AdminOnly, AdminOrOwner)
2. Update navigation with admin-only menu items
3. Add AuthorizeView for admin sections
4. Test authorization attributes

**Dependencies**: Stream A must be complete

## Execution Strategy

1. **Phase 1**: Complete Stream A (database and seeding)
2. **Phase 2**: Launch Streams B and D in parallel (service and auth)
3. **Phase 3**: Complete Stream C (UI components)

Streams B and D can run in parallel after A completes since they work on different files.

## Potential Conflicts

- Program.cs will be modified by multiple streams (coordinate updates)
- NavMenu.razor might have been modified in previous issues
- Ensure ApplicationUser is used consistently (not IdentityUser)

## Testing Requirements

After all streams complete:
1. Verify admin role is seeded on startup
2. Test admin login with default credentials
3. Verify non-admins cannot access admin pages
4. Test all admin service methods
5. Verify admin can see all prompts
6. Test user role assignment functionality
7. Verify system statistics are accurate