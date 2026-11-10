// <copyright file="CorrelationIdEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Middleware;

using System.Net;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.Api.Tests.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Proves the production pipeline enforces x-cdp-request-id on every /submission endpoint and leaves health and OpenAPI alone.
/// </summary>
public sealed class CorrelationIdEndpointsTest : IClassFixture<CorrelationIdEndpointsTest.KeyedFactory>
{
    private readonly KeyedFactory factory;

    public CorrelationIdEndpointsTest(KeyedFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void EverySubmissionEndpointRequiresTheCorrelationId()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/submission", StringComparison.Ordinal) == true)
            .ToList();

        endpoints.ShouldNotBeEmpty();
        endpoints.ShouldAllBe(endpoint => endpoint.Metadata.GetMetadata<IgnoreCorrelationIdCheck>() == null);
    }

    [Theory]
    [InlineData("/submission")]
    [InlineData("/submission/start")]
    [InlineData("/submission/1234/UK200000000001/details")]
    public async Task ApiRequestWithoutTheCorrelationIdShouldBeRejectedWith400(string path)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(RequestHeaderNames.ApiKey, TestAuthentication.ApiKey);

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var error = document.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().ShouldBe("missing_header");
        error.GetProperty("details").GetProperty("header").GetString().ShouldBe(RequestHeaderNames.CorrelationId);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public async Task HealthAndOpenApiShouldNotNeedTheCorrelationId(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    public sealed class KeyedFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting($"{ApiKeyAuthenticationOptions.SectionName}:Keys:0", TestAuthentication.ApiKey);
        }
    }
}
