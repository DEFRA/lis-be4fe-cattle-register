// <copyright file="ServiceToServiceEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Authentication;

using System.Net;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceToServiceEndpointsTest : IClassFixture<ServiceToServiceEndpointsTest.KeyedFactory>
{
    private readonly KeyedFactory factory;

    public ServiceToServiceEndpointsTest(KeyedFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void EverySubmissionEndpointRequiresTheServiceToServicePolicy()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/submission", StringComparison.Ordinal) == true)
            .ToList();

        endpoints.ShouldNotBeEmpty();
        endpoints.ShouldAllBe(endpoint => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(data => data.Policy == AuthPolicies.ServiceToService));
    }

    [Theory]
    [InlineData("/submission/12/345/6789")]
    [InlineData("/submission/12/345/6789/start")]
    [InlineData("/submission/1234/UK200000000001")]
    public async Task ApiRequestWithoutTheKeyShouldBeRejected(string path)
    {
        var response = await CreateCorrelatedClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task ApiRequestWithTheWrongKeyShouldBeRejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/submission/12/345/6789/start");
        request.Headers.Add("x-api-key", "not-the-key");

        var response = await CreateCorrelatedClient().SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/openapi/v1.json")]
    public async Task HealthAndOpenApiShouldStayAnonymous(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // The correlation check runs before authentication, so these requests carry the header to reach it.
    private HttpClient CreateCorrelatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, "corr-auth-test");
        return client;
    }

    public sealed class KeyedFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting($"{ApiKeyAuthenticationOptions.SectionName}:Keys:0", TestAuthentication.ApiKey);
        }
    }
}
