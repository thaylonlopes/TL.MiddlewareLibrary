using Microsoft.AspNetCore.Builder;
using MiddlewareLibrary.Middlewares;
using System;

namespace MiddlewareLibrary.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<RequestTimingMiddleware>();
    }

    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    public static IApplicationBuilder UseStatusCodeMiddleware(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<StatusCodeMiddleware>();
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, int limit, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<RateLimitingMiddleware>(limit, period);
    }

    public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }

    public static IApplicationBuilder UseCachingMiddleware(this IApplicationBuilder builder, TimeSpan? cacheDuration = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<CachingMiddleware>(cacheDuration ?? TimeSpan.FromMinutes(1));
    }
}

