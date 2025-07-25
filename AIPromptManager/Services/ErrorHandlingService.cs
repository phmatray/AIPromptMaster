using Microsoft.EntityFrameworkCore;

namespace AIPromptManager.Services;

public interface IErrorHandlingService
{
    Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName, string? successMessage = null);
    Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName, string? successMessage = null);
}

public class ErrorHandlingService(
    IToastService toastService,
    ILogger<ErrorHandlingService> logger)
    : IErrorHandlingService
{
    public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName, string? successMessage = null)
    {
        try
        {
            var result = await operation();
            
            if (!string.IsNullOrEmpty(successMessage))
            {
                toastService.ShowSuccess(successMessage);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during {OperationName}", operationName);
            
            var userMessage = GetUserFriendlyErrorMessage(ex, operationName);
            toastService.ShowError(userMessage, "Error");
            
            throw;
        }
    }

    public async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName, string? successMessage = null)
    {
        try
        {
            await operation();
            
            if (!string.IsNullOrEmpty(successMessage))
            {
                toastService.ShowSuccess(successMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during {OperationName}", operationName);
            
            var userMessage = GetUserFriendlyErrorMessage(ex, operationName);
            toastService.ShowError(userMessage, "Error");
            
            throw;
        }
    }

    private static string GetUserFriendlyErrorMessage(Exception exception, string operationName)
    {
        return exception switch
        {
            ArgumentNullException => "Required information is missing. Please check your input and try again.",
            ArgumentException argEx => argEx.Message,
            InvalidOperationException invalidEx when invalidEx.Message.Contains("not found") => 
                "The requested item could not be found. It may have been deleted by another user.",
            InvalidOperationException invalidEx when invalidEx.Message.Contains("modified by another user") => 
                "This item was modified by another user. Please refresh the page and try again.",
            InvalidOperationException invalidEx when invalidEx.Message.Contains("deleted by another user") => 
                "This item was deleted by another user. Please refresh the page.",
            DbUpdateConcurrencyException => 
                "This item was modified by another user. Please refresh the page and try again.",
            DbUpdateException dbEx when dbEx.InnerException?.Message?.Contains("UNIQUE constraint failed") == true => 
                "An item with this information already exists.",
            DbUpdateException => 
                "Failed to save changes. Please check your input and try again.",
            TimeoutException => 
                "The operation timed out. Please try again.",
            _ => $"An unexpected error occurred during {operationName.ToLower()}. Please try again."
        };
    }
}