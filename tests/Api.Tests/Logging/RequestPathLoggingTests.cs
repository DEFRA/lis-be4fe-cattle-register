// <copyright file="RequestPathLoggingTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Logging;

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Defra.Lis.Be4Fe.Api.Endpoints.Submission;
using Defra.Lis.Be4Fe.Api.Exceptions;
using Defra.Lis.Be4Fe.Api.Middleware;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Api.Tests.Authentication;
using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Defra.Lis.Be4Fe.Models.Submissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Runs the birth endpoints through the logging pipeline from <c>Program</c> and checks that request paths are
/// logged as route templates, so identifiers in a path never reach the logs.
/// </summary>
public class RequestPathLoggingTests
{
    private const string PathIdentifier = "5e0c8f8a-4b1d-4c7e-9a3f-6d2b8e1f0a47";
    private const string CorrelationId = "corr-path";

    private readonly CapturingSink sink = new();

    [Fact]
    public async Task GetSection_LogsTheRouteTemplate()
    {
        await using var app = await StartAppAsync();

        var response = await AuthenticatedClient(app).GetAsync($"/submission/{PathIdentifier}/UK1/details", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertRequestLoggedWithRouteTemplate("GET", "/submission/{submissionId}/{animalId}/{section}");
        AssertNoEventContainsThePathIdentifier();
    }

    [Fact]
    public async Task GetSection_LogsNoRawPath_WhenTheRequestFails()
    {
        await using var app = await StartAppAsync();

        var response = await AuthenticatedClient(app).GetAsync($"/submission/{PathIdentifier}/UK1/unknown", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AssertRequestLoggedWithRouteTemplate("GET", "/submission/{submissionId}/{animalId}/{section}");
        sink.Events.ShouldAllBe(e => !e.RenderMessage().Contains($"/submission/{PathIdentifier}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetSection_LogsNoRawPath_WhenTheCorrelationIdIsMissing()
    {
        await using var app = await StartAppAsync();

        var response = await app.GetAuthenticatedTestClient().GetAsync($"/submission/{PathIdentifier}/UK1/details", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        sink.Events.ShouldContain(e => e.MessageTemplate.Text.Contains(RequestHeaderNames.CorrelationId, StringComparison.Ordinal));
        AssertNoEventContainsThePathIdentifier();
    }

    [Fact]
    public async Task GetSection_LogsNoRawPath_WhenTheApiKeyIsMissing()
    {
        await using var app = await StartAppAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, CorrelationId);

        var response = await client.GetAsync($"/submission/{PathIdentifier}/UK1/details", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        sink.Events.ShouldContain(e => e.MessageTemplate.Text.Contains("API key", StringComparison.Ordinal));
        AssertNoEventContainsThePathIdentifier();
    }

    [Fact]
    public async Task UnmatchedRoute_LogsNoRawPath()
    {
        await using var app = await StartAppAsync();

        var response = await AuthenticatedClient(app).GetAsync($"/unknown/{PathIdentifier}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AssertRequestLoggedWithRouteTemplate("GET", LoggableRequestPath.UnmatchedRoute);
        AssertNoEventContainsThePathIdentifier();
    }

    private static HttpClient AuthenticatedClient(WebApplication app)
    {
        var client = app.GetAuthenticatedTestClient();
        client.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, CorrelationId);
        return client;
    }

    private static string Describe(LogEvent logEvent)
    {
        var text = new StringBuilder(logEvent.RenderMessage());
        foreach (var property in logEvent.Properties)
        {
            text.Append(' ').Append(property.Key).Append('=').Append(property.Value);
        }

        return text.Append(' ').Append(logEvent.Exception).ToString();
    }

    private void AssertRequestLoggedWithRouteTemplate(string method, string template)
    {
        var request = sink.Events.Where(e => e.Properties.ContainsKey("Elapsed")).ShouldHaveSingleItem();
        ((ScalarValue)request.Properties["RequestMethod"]).Value.ShouldBe(method);
        ((ScalarValue)request.Properties["RequestPath"]).Value.ShouldBe(template);
    }

    private void AssertNoEventContainsThePathIdentifier()
    {
        sink.Events.ShouldNotBeEmpty();
        sink.Events.ShouldAllBe(e => !Describe(e).Contains(PathIdentifier, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<WebApplication> StartAppAsync()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();

        // Information and above, as in production, but without the overrides from appsettings, and everything from
        // this service. The overrides CdpLogging enforces in code still apply. The static Log.Logger is left alone, as
        // tests running in parallel would otherwise write to each other's sinks.
        builder.Services.AddHttpContextAccessor();
        builder.Host.UseSerilog(
            (context, services, configuration) =>
            {
                CdpLogging.Configuration(context, services, configuration);
                configuration.MinimumLevel.Information()
                    .MinimumLevel.Override("Defra", LogEventLevel.Verbose)
                    .WriteTo.Sink(sink);
            },
            preserveStaticLogger: true);

        builder.Services.AddRouting();
        builder.Services.AddTestServiceToServiceAuthentication();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddTransient<CorrelationIdMiddleware>();
        builder.Services.AddSingleton<ISubmissionService, StubSubmissionService>();

        var app = builder.Build();
        app.UseCdpRequestLogging();
        app.UseExceptionHandler();
        app.UseRouting();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseTestServiceToServiceAuthentication();
        app.UseSubmissionEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private sealed class StubSubmissionService : ISubmissionService
    {
        public Task<AnimalSectionResponse> GetSectionAsync(string submissionId, string animalId, string section, CancellationToken cancellationToken = default)
        {
            if (section == "unknown")
            {
                throw new NotFoundException("The section was not found.");
            }

            return Task.FromResult(new AnimalSectionResponse
            {
                AnimalId = animalId,
                Section = section,
                Data = [],
            });
        }

        public Task<SubmissionSummaryResponse> StartAsync(string? cph, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateSectionAsync(string submissionId, UpdateAnimalSectionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAnimalAsync(string submissionId, string animalId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SubmitAsync(string submissionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SubmissionListResponse> ListAsync(string? cph, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogEvent> events = new();

        public IReadOnlyCollection<LogEvent> Events => events;

        public void Emit(LogEvent logEvent) => events.Enqueue(logEvent);
    }
}
