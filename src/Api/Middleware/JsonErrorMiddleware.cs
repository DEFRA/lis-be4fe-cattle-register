// <copyright file="JsonErrorMiddleware.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware;

using System.Text.Json;
using Microsoft.AspNetCore.Http;

public abstract class JsonErrorMiddleware : IMiddleware
{
    public abstract Task InvokeAsync(HttpContext context, RequestDelegate next);

    protected static async Task WriteJsonErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        object? details = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            error = new
            {
                code,
                message,
                traceId = context.TraceIdentifier,
                path = context.Request.Path.Value,
                details,
            },
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
