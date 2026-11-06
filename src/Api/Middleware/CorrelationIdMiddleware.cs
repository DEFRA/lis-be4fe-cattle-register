// <copyright file="CorrelationIdMiddleware.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware;

using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

/// <summary>
/// Requires a single x-cdp-request-id header on every endpoint not marked with <see cref="IgnoreCorrelationIdCheck"/>
/// (Correlation ID standard), rejecting the request with 400 otherwise. The value is stored in
/// <see cref="CorrelationIdContext"/>, added to every log line as <c>CorrelationId</c> and echoed on the response.
/// Must run after routing so the endpoint metadata is available.
/// </summary>
public partial class CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger) : JsonErrorMiddleware
{
    public override async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // check if this maps to an endpoint. If not, just call the next middleware.
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await next(context);
            return;
        }

        // check if the endpoint has the IgnoreCorrelationIdCheck metadata. If so, skip the correlation ID check.
        var ignoreCorrelationIdCheck = endpoint.Metadata.GetMetadata<IgnoreCorrelationIdCheck>() is not null;
        if (ignoreCorrelationIdCheck)
        {
            await next(context);
            return;
        }

        try
        {
            var values = context.Request.Headers[RequestHeaderNames.CorrelationId];
            var correlationId = values.Count == 1 ? NormalizeHeaderValue(values.ToString()) : null;
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                LogMissingCorrelationId(logger, context.Request.Method, LoggableRequestPath.For(context));
                await WriteJsonErrorAsync(
                    context,
                    statusCode: StatusCodes.Status400BadRequest,
                    code: "missing_header",
                    message: $"A single {RequestHeaderNames.CorrelationId} header is required.",
                    details: new { header = $"{RequestHeaderNames.CorrelationId}" });
                return;
            }

            CorrelationIdContext.Value = correlationId;
            context.Response.Headers[RequestHeaderNames.CorrelationId] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next(context);
            }
        }
        catch (Exception ex)
        {
            LogErrorInMiddleware(logger, nameof(CorrelationIdMiddleware), ex);
            throw;
        }
    }

    private static string? NormalizeHeaderValue(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();

        // Treat empty quotes as "missing": "", '' (and also values with spaces like "  ").
        trimmed = trimmed.Trim('\"', '\'');

        trimmed = trimmed.Trim();

        return trimmed.Length == 0 ? null : trimmed;
    }
}
