using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AIPromptManager.Middleware;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ErrorResponse();

        switch (exception)
        {
            case NpgsqlException npgsqlException:
                response.Message = "Database operation failed. Please try again.";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Details = npgsqlException.InnerException?.Message ?? npgsqlException.Message;
                break;
                
            case DbUpdateConcurrencyException concurrencyEx:
                response.Message = "The record was modified by another user. Please refresh and try again.";
                response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Details = "Concurrency conflict detected";
                break;
                
            case DbUpdateException dbEx:
                response.Message = "Failed to save changes. Please check your input and try again.";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Details = dbEx.InnerException?.Message ?? dbEx.Message;
                break;
                
            case ArgumentException argEx:
                response.Message = "Invalid input provided.";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Details = argEx.Message;
                break;
                
            case InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("storage", StringComparison.OrdinalIgnoreCase):
                response.Message = "Storage operation failed.";
                response.StatusCode = (int)HttpStatusCode.InsufficientStorage;
                response.Details = invalidOpEx.Message;
                break;
                
            case InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("full", StringComparison.OrdinalIgnoreCase):
                response.Message = "Storage is full. Please delete some items or contact support.";
                response.StatusCode = (int)HttpStatusCode.InsufficientStorage;
                response.Details = invalidOpEx.Message;
                break;
                
            case InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase):
                response.Message = "Service is temporarily unavailable. Please try again later.";
                response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                response.Details = invalidOpEx.Message;
                break;
                
            case UnauthorizedAccessException:
                response.Message = "Access denied.";
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;
                
            case TimeoutException:
                response.Message = "The operation timed out. Please try again.";
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                break;
                
            default:
                response.Message = "An unexpected error occurred. Please try again later.";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        context.Response.StatusCode = response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}