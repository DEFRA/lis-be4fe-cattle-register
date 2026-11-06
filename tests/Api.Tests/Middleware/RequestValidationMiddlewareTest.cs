// <copyright file="RequestValidationMiddlewareTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Middleware;

using System.Text.Json;
using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Api.Middleware;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

public sealed class RequestValidationMiddlewareTest
{
    [Fact]
    public async Task CorrelationIdValidationShouldContinueWhenCheckIsIgnored()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new IgnoreCorrelationIdCheck());
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\"\"")]
    [InlineData("'  '")]
    public async Task CorrelationIdValidationShouldRejectMissingValues(string? correlationId)
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        if (correlationId is not null)
        {
            context.Request.Headers[RequestHeaderNames.CorrelationId] = correlationId;
        }

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var payload = await ReadResponseAsync(context);
        payload.GetProperty("error").GetProperty("code").GetString().ShouldBe("missing_header");
    }

    [Fact]
    public async Task CorrelationIdValidationShouldContinueForAValueSurroundedByWhitespace()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Headers[RequestHeaderNames.CorrelationId] = "  correlation-id  ";
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task CorrelationIdValidationShouldRejectMoreThanOneValue()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Headers[RequestHeaderNames.CorrelationId] = new StringValues(["first", "second"]);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task CorrelationIdValidationShouldSetTheContextAndEchoTheHeader()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Headers[RequestHeaderNames.CorrelationId] = "corr-598";
        string? seen = null;

        await middleware.InvokeAsync(context, _ =>
        {
            seen = CorrelationIdContext.Value;
            return Task.CompletedTask;
        });

        seen.ShouldBe("corr-598");
        context.Response.Headers[RequestHeaderNames.CorrelationId].ToString().ShouldBe("corr-598");
    }

    [Fact]
    public async Task CorrelationIdValidationShouldIgnoreTheLegacyCorrelationHeader()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Headers["x-correlation-id"] = "legacy";

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ValidationMiddlewareShouldPropagateDownstreamExceptions()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Headers[RequestHeaderNames.CorrelationId] = "correlation-id";

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, _ => throw new InvalidOperationException("failure")));

        exception.Message.ShouldBe("failure");
    }

    private static DefaultHttpContext CreateContext(params object[] metadata)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (metadata.Length > 0)
        {
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "test"));
        }

        return context;
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        return document.RootElement.Clone();
    }
}
