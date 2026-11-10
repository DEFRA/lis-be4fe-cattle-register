// <copyright file="ApiKeyAuthenticationHandlerTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Authentication;

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class ApiKeyAuthenticationHandlerTests
{
    private const string HeaderName = "x-api-key";

    private readonly CapturingLoggerProvider logs = new();

    [Fact]
    public async Task Request_WithoutTheHeader_Returns401ProblemDetails()
    {
        await using var app = await StartAppAsync();

        var response = await app.GetTestClient().GetAsync("/protected", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("ApiKey header=\"x-api-key\"", response.Headers.WwwAuthenticate.ToString());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
        Assert.Contains(logs.Messages, message => message.Contains("missing", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("test-api-key-but-longer")]
    [InlineData("TEST-API-KEY")]
    [InlineData("test-api-ke")]
    public async Task Request_WithAKeyThatDoesNotMatch_Returns401(string key)
    {
        await using var app = await StartAppAsync();

        var response = await SendAsync(app, "/protected", key);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(logs.Messages, message => message.Contains("invalid", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("current-key", "api-key:0")]
    [InlineData("next-key", "api-key:1")]
    public async Task Request_WithAnyConfiguredKey_IsAuthenticatedAsThatKey(string key, string expectedClient)
    {
        await using var app = await StartAppAsync("current-key", "next-key");

        var response = await SendAsync(app, "/protected", key);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedClient, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Request_WithTheHeaderRepeated_Returns401()
    {
        await using var app = await StartAppAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Add(HeaderName, [TestAuthentication.ApiKey, TestAuthentication.ApiKey]);

        var response = await app.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WhenNoKeysAreConfigured_IsRejectedWithAWarning()
    {
        await using var app = await StartAppAsync(" ");

        var response = await SendAsync(app, "/protected", " ");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(logs.Messages, message => message.Contains("No API keys are configured", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnonymousEndpoint_DoesNotRequireTheKey()
    {
        await using var app = await StartAppAsync();

        var response = await app.GetTestClient().GetAsync("/anonymous", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Keys_AreNeverWrittenToTheLogs()
    {
        await using var app = await StartAppAsync();

        await SendAsync(app, "/protected", "presented-secret-value");
        await SendAsync(app, "/protected", TestAuthentication.ApiKey);
        await app.GetTestClient().GetAsync("/protected", TestContext.Current.CancellationToken);

        Assert.NotEmpty(logs.Messages);
        Assert.DoesNotContain(logs.Messages, message => message.Contains("presented-secret-value", StringComparison.Ordinal));
        Assert.DoesNotContain(logs.Messages, message => message.Contains(TestAuthentication.ApiKey, StringComparison.Ordinal));
    }

    private static async Task<HttpResponseMessage> SendAsync(WebApplication app, string path, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(HeaderName, key);
        return await app.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<WebApplication> StartAppAsync(params string[] keys)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddTestServiceToServiceAuthentication(keys);
        builder.Logging.AddProvider(logs);
        var app = builder.Build();
        app.UseTestServiceToServiceAuthentication();
        app.MapGet("/protected", (HttpContext context) => context.User.FindFirst(ApiKeyAuthenticationHandler.ClientIdClaimType)?.Value)
            .RequireServiceToServiceAuthorization();
        app.MapGet("/anonymous", () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> messages = new();

        public IReadOnlyCollection<string> Messages => messages;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                messages.Enqueue(formatter(state, exception));
            }
        }
    }
}
