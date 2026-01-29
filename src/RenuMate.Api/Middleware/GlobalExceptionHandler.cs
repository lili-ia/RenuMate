using Auth0.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        var (statusCode, title, logAsError) = exception switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, "Business Validation Error", false),
            DomainConflictException => (StatusCodes.Status409Conflict, "Business Rule Conflict", false),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", false),
            RateLimitApiException => (StatusCodes.Status429TooManyRequests, "Too Many Requests", true),
            ErrorApiException => (StatusCodes.Status500InternalServerError, "Identity Service Error", true),
            _ => (StatusCodes.Status500InternalServerError, "Server Error", true)
        };

        if (logAsError)
        {
            logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        }
        else if (exception is UnauthorizedAccessException)
        {
            logger.LogWarning("Security access violation: {Message}. Instance: {Instance}", 
                exception.Message, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Business rule violation: {Message}", exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = logAsError ? "An internal error occurred. Please try later." : exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}