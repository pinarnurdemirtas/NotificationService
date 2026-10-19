using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Common;
namespace NotificationService.Api.ErrorHandling;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            AuthenticationRequiredException => (401, "Authentication required"),
            RequestValidationException => (400, "Invalid request"),
            ResourceNotFoundException => (404, "Resource not found"),
            ResourceConflictException => (409, "Resource conflict"),
            _ => (500, "An unexpected error occurred")
        };
        if (status == 500)
            logger.LogError("Request failed with {ExceptionType}; trace {TraceId}", exception.GetType().Name, context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title, Detail = status == 500 ? "Check server logs and database availability." : exception.Message,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, cancellationToken);
        return true;
    }
}
