using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace AIPromptManager.Services;

public sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private readonly NavigationManager _navigationManager = navigationManager;

    [DoesNotReturn]
    public void RedirectTo(string? uri)
    {
        uri ??= "";
        
        // Prevent open redirects by checking if the URI is relative
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = "";
        }

        _navigationManager.NavigateTo(uri);
        throw new InvalidOperationException($"The redirect to '{uri}' could not be completed.");
    }

    [DoesNotReturn]
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = _navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = _navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    [DoesNotReturn]
    public void RedirectToWithStatus(string uri, string message, HttpContext httpContext)
    {
        httpContext.Response.Cookies.Append(StatusCookieName, message, new CookieOptions
        {
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,
            IsEssential = true
        });
        RedirectTo(uri);
    }

    private string CurrentPath => _navigationManager.ToAbsoluteUri(_navigationManager.Uri).GetLeftPart(UriPartial.Path);
}