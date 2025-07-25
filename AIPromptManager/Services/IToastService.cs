using static AIPromptManager.Components.Shared.Toast;

namespace AIPromptManager.Services;

public interface IToastService
{
    event Action<string?, string?, ToastType>? OnShow;
    void ShowSuccess(string message, string? title = null);
    void ShowError(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
    void ShowInfo(string message, string? title = null);
}