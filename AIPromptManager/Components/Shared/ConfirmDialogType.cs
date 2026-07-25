namespace AIPromptManager.Components.Shared;

/// <summary>
/// Severity of a <c>ConfirmDialog</c>, which drives its icon and button colours.
/// </summary>
/// <remarks>
/// Declared here rather than inside ConfirmDialog.razor's code block: nested in the
/// component it was a nested type of the component, so callers writing
/// <c>Type="ConfirmDialogType.Danger"</c> could not resolve it.
/// </remarks>
public enum ConfirmDialogType
{
    Info,
    Warning,
    Danger,
}
