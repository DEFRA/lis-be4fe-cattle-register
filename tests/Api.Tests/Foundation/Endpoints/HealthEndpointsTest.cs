// <copyright file="HealthEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Foundation.Endpoints;

using System.Net;
using System.Net.Http.Json;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Api.Endpoints.Health;
using Defra.Lis.Be4Fe.Api.MetaData;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthStatusModel = Defra.Lis.Be4Fe.Models.Health.HealthStatus;

public class HealthEndpointsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public HealthEndpointsTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthShouldReturnOkAndHealthStatusObject()
    {
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        var content =
            await response.Content.ReadFromJsonAsync<HealthStatusModel>(TestContext.Current.CancellationToken);
        content.ShouldNotBeNull();
        content.Status.ShouldBe("Healthy");
    }

    [Fact]
    public async Task GetHealthShouldBypassApiKeyAndCorrelationIdValidation()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Clear();

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content =
            await response.Content.ReadFromJsonAsync<HealthStatusModel>(TestContext.Current.CancellationToken);
        content.ShouldNotBeNull();
        content.Status.ShouldBe("Healthy");
    }

    [Fact]
    public async Task HealthCheckServiceShouldReportHealthyStatus()
    {
        using var scope = factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync(TestContext.Current.CancellationToken);

        report.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void UseHealthEndpointsShouldRegisterHealthRouteWithExpectedMetadata()
    {
        var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();
        var healthEndpoint = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .FirstOrDefault(e => e.RoutePattern.RawText is "health" or "/health");

        healthEndpoint.ShouldNotBeNull();
        healthEndpoint.Metadata.GetMetadata<IgnoreCorrelationIdCheck>().ShouldNotBeNull();
        healthEndpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName.ShouldBe(OpenApiMetadata.Get.Name);
    }
}
