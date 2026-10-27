using System.Net;
using System.Text.Json;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            ForbiddenAccessException => HttpStatusCode.Forbidden,
            DomainException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "An unhandled exception occurred while processing request {Path}", context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Business rule validation failed ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var responsePayload = new
        {
            statusCode = (int)statusCode,
            error = statusCode == HttpStatusCode.InternalServerError ? "An unexpected internal server error occurred." : exception.Message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(responsePayload));
    }
}
