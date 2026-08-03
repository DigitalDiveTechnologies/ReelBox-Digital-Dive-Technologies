using System.Text.Json;
using FluentValidation;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
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
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            UnauthorizedAppException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            _ => (StatusCodes.Status500InternalServerError, "Server error"),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
            await TryPersistErrorAsync(context, exception, status);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode}", status);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        object body = exception switch
        {
            ValidationException validation => new
            {
                type = $"https://httpstatuses.com/{status}",
                title,
                status,
                errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            },
            BadRequestException badRequest => new
            {
                type = $"https://httpstatuses.com/{status}",
                title,
                status,
                detail = badRequest.Message,
                code = badRequest.Code,
            },
            _ => new
            {
                type = $"https://httpstatuses.com/{status}",
                title,
                status,
                detail = status >= 500 ? "An unexpected error occurred." : exception.Message,
            },
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static async Task TryPersistErrorAsync(HttpContext context, Exception exception, int status)
    {
        try
        {
            var writer = context.RequestServices.GetService<IAppErrorLogWriter>();
            if (writer is null) return;

            await writer.WriteAsync(
                "Error",
                exception.Message,
                exception.ToString(),
                exception.GetType().FullName,
                context.TraceIdentifier,
                context.Request.Path.Value,
                status);
        }
        catch
        {
            // Never let error persistence break the response path.
        }
    }
}
