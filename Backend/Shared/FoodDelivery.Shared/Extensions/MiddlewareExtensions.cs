using FoodDelivery.Shared.Middleware;
using Microsoft.AspNetCore.Builder;

namespace FoodDelivery.Shared.Extensions;

/// <summary>
/// Extension methods for registering custom middleware.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds global exception handling middleware to the pipeline.
    /// Should be added early in the pipeline to catch all exceptions.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }

    /// <summary>
    /// Adds request/response logging middleware to the pipeline.
    /// Logs all incoming requests and outgoing responses with timing.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
