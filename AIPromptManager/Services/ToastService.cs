using static AIPromptManager.Components.Shared.Toast;

namespace AIPromptManager.Services;

public class ToastService : IToastService
{
    public event Action<string?, string?, ToastType>? OnShow;

    public void ShowSuccess(string message, string? title = null)
    {
        OnShow?.Invoke(title, message, ToastType.Success);
    }

    public void ShowError(string message, string? title = null)
    {
        OnShow?.Invoke(title, message, ToastType.Error);
    }

    public void ShowWarning(string message, string? title = null)
    {
        OnShow?.Invoke(title, message, ToastType.Warning);
    }

    public void ShowInfo(string message, string? title = null)
    {
        OnShow?.Invoke(title, message, ToastType.Info);
    }
}