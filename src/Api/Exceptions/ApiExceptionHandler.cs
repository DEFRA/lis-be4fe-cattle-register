// <copyright file="ApiExceptionHandler.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Exceptions;

using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Defra.Lis.Be4Fe.CattleApi;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

public sealed partial class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, type) = exception switch
        {
            NotFoundException or HoldingNotFoundException
                => (StatusCodes.Status404NotFound, "Not Found", "https://httpstatuses.com/404"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", "https://httpstatuses.com/409"),

            // A body that cannot be bound. ASP.NET throws this rather than answering 400 itself in Development.
            BadHttpRequestException or BusinessRuleException => (StatusCodes.Status400BadRequest, "Bad Request", "https://httpstatuses.com/400"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", "https://httpstatuses.com/400"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", "https://httpstatuses.com/403"),
            ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable", "https://httpstatuses.com/503"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "https://httpstatuses.com/500"),
        };

        // Put useful values into the Serilog LogContext (works with Enrich.FromLogContext()). The correlation
        // middleware's scope has already unwound here, so the ID is read from the header it enforced.
        var correlationId = httpContext.Request.Headers[RequestHeaderNames.CorrelationId].ToString();

        // The route template, not the raw path: the path can carry the Defra ID subject.
        var path = LoggableRequestPath.For(httpContext);
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", httpContext.TraceIdentifier))
        using (LogContext.PushProperty("Path", path))
        using (LogContext.PushProperty("StatusCode", statusCode))
        {
            if (statusCode >= 500)
            {
                LogUnhandledExceptionWhileProcessingRequestMethodPath(httpContext.Request.Method, path, exception);
            }
            else
            {
                LogRequestFailedWithStatusCodeTitleForMethodPath(statusCode, title, httpContext.Request.Method, path, exception);
            }
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier,
                ["correlationId"] = correlationId,
            },
        };

        httpContext.Response.StatusCode = statusCode;

        // The exception handler clears response headers, so echo the correlation ID again.
        if (!string.IsNullOrEmpty(correlationId))
        {
            httpContext.Response.Headers[RequestHeaderNames.CorrelationId] = correlationId;
        }

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true; // exception handled
    }
}
