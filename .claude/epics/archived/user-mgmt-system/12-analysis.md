---
issue: 12
analyzed: 2025-08-29T09:40:00Z
title: Prompt Authorization - Update PromptService to filter by authenticated user
---

# Issue #12 Analysis: Prompt Authorization

## Work Streams Identified

### Stream A: Service Configuration & User Context
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Services/PromptService.cs
- AIPromptManager/Services/IPromptService.cs
- AIPromptManager/Program.cs (register IHttpContextAccessor)

**Work**:
1. Add IHttpContextAccessor to PromptService constructor
2. Implement GetCurrentUserId() helper method
3. Register IHttpContextAccessor in DI container

**Dependencies**: None (can start immediately)

### Stream B: CRUD Method Authorization
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Services/PromptService.cs (all CRUD methods)

**Work**:
1. Update GetAllPromptsAsync to filter by UserId
2. Update GetPromptByIdAsync to verify ownership
3. Update CreatePromptAsync to set UserId
4. Update UpdatePromptAsync to verify ownership
5. Update DeletePromptAsync to verify ownership
6. Update SearchPromptsAsync to filter by user

**Dependencies**: Stream A must complete first (needs GetCurrentUserId method)

### Stream C: Error Handling & Validation
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Services/PromptService.cs
- AIPromptManager/Services/ErrorHandlingService.cs (if updates needed)

**Work**:
1. Add proper exception throwing for unauthorized access
2. Implement consistent error messages
3. Add logging for security events
4. Handle edge cases (null user, missing claims)

**Dependencies**: Stream B should be mostly complete

## Execution Strategy

1. **Phase 1**: Complete Stream A (service configuration)
2. **Phase 2**: Complete Stream B (CRUD authorization)
3. **Phase 3**: Complete Stream C (error handling)

Streams must run sequentially due to interdependencies.

## Potential Conflicts

- All streams modify PromptService.cs
- Sequential execution avoids conflicts

## Testing Requirements

After all streams complete:
1. Verify users can only see their own prompts
2. Test that create operations set correct UserId
3. Confirm update/delete fail for non-owned prompts
4. Check that exceptions are properly thrown and handled
5. Ensure performance is acceptable with filtering