using System.Net;
using System.Text.Json;
using FoodDelivery.Shared.Common;

namespace AuthService.API.Middleware;

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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[AUTH MIDDLEWARE] Unauthorized access: {Message}, Path: {Path}", 
                ex.Message, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Unauthorized);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("[AUTH MIDDLEWARE] Bad request: {Message}, Path: {Path}", 
                ex.Message, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH MIDDLEWARE] Unhandled exception occurred, Path: {Path}", 
                context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(
            statusCode == HttpStatusCode.InternalServerError 
                ? "An internal server error occurred." 
                : exception.Message
        );

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
