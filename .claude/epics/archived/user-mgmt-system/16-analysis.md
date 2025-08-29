---
issue: 16
analyzed: 2025-08-29T12:35:00Z
title: Email & Password Reset - Configure email service and password reset flow
---

# Issue #16 Analysis: Email & Password Reset

## Work Streams Identified

### Stream A: Email Service Foundation
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Services/IEmailService.cs (create)
- AIPromptManager/Services/EmailService.cs (create)
- AIPromptManager/appsettings.json (update)
- AIPromptManager/appsettings.Development.json (update)
- AIPromptManager/Program.cs (register service)

**Work**:
1. Create IEmailService interface with methods for various email types
2. Implement EmailService with SMTP configuration
3. Add email template loading and processing
4. Configure SMTP settings in appsettings
5. Register email service in DI container
6. Add development email settings (use MailDev or similar)

**Dependencies**: None (can start immediately)

### Stream B: Rate Limiting and Security
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Services/IRateLimitService.cs (create)
- AIPromptManager/Services/RateLimitService.cs (create)
- AIPromptManager/Models/RateLimitEntry.cs (create)
- AIPromptManager/Program.cs (register service)

**Work**:
1. Create rate limiting interface
2. Implement in-memory rate limiting service
3. Add configuration for rate limits
4. Track attempts per identifier
5. Clean up expired entries

**Dependencies**: None (can start immediately)

### Stream C: Password Reset API
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Controllers/PasswordResetController.cs (create)
- AIPromptManager/Models/PasswordResetRequest.cs (create)
- AIPromptManager/Models/PasswordResetModel.cs (create)
- AIPromptManager/Program.cs (map controllers)

**Work**:
1. Create API controller for password reset
2. Implement request reset endpoint
3. Implement reset password endpoint
4. Add token generation and validation
5. Integrate with email and rate limiting services

**Dependencies**: Streams A and B should be complete

### Stream D: Email Templates
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Templates/PasswordResetEmail.html (create)
- AIPromptManager/Templates/EmailConfirmation.html (create)
- AIPromptManager/Templates/WelcomeEmail.html (create)
- AIPromptManager/Templates/EmailLayout.html (create)

**Work**:
1. Create base email layout template
2. Create password reset email template
3. Create email confirmation template
4. Create welcome email template
5. Add template placeholders for dynamic content

**Dependencies**: None (can start immediately)

### Stream E: UI Components
**Agent Type**: general-purpose
**Files**:
- AIPromptManager/Components/Pages/Account/ForgotPassword.razor (create)
- AIPromptManager/Components/Pages/Account/ResetPassword.razor (create)
- AIPromptManager/Components/Pages/Account/ConfirmEmail.razor (create)
- AIPromptManager/Components/Shared/PasswordStrengthIndicator.razor (create)

**Work**:
1. Create forgot password page with email input
2. Create reset password page with token validation
3. Create email confirmation page
4. Create password strength indicator component
5. Add proper error handling and success messages

**Dependencies**: Stream C should be complete for API integration

## Execution Strategy

1. **Phase 1**: Launch Streams A, B, and D in parallel (independent services and templates)
2. **Phase 2**: Complete Stream C (API needs email and rate limiting)
3. **Phase 3**: Complete Stream E (UI needs API)

Streams A, B, and D can run completely in parallel as they work on different files.

## Potential Conflicts

- Program.cs will be modified by multiple streams (coordinate service registrations)
- Ensure ApplicationUser is used consistently
- Email service needs to work in development without real SMTP

## Testing Requirements

After all streams complete:
1. Test password reset flow end-to-end
2. Verify rate limiting blocks excessive requests
3. Test email templates render correctly
4. Verify tokens expire after configured time
5. Test email confirmation for new registrations
6. Verify security (no user enumeration)
7. Test in development with mock email service