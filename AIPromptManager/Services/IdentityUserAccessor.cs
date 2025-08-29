using Microsoft.AspNetCore.Identity;

namespace AIPromptManager.Services;

public sealed class IdentityUserAccessor(UserManager<IdentityUser> userManager, IdentityRedirectManager redirectManager, IHttpContextAccessor httpContextAccessor)
{
    public async Task<IdentityUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }

    public async Task<IdentityUser> GetRequiredUserAsync()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}