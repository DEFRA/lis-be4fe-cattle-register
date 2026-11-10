// <copyright file="TestAuthentication.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Authentication;

using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Wires the service-to-service API key scheme into the minimal test hosts the endpoint tests build.
/// </summary>
internal static class TestAuthentication
{
    public const string ApiKey = "test-api-key";

    public static IServiceCollection AddTestServiceToServiceAuthentication(this IServiceCollection services, params string[] keys)
    {
        var settings = (keys.Length == 0 ? [ApiKey] : keys)
            .Select((key, index) => new KeyValuePair<string, string?>($"{ApiKeyAuthenticationOptions.SectionName}:Keys:{index}", key));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        services.AddLogging();
        services.AddProblemDetails();
        return services.AddServiceToServiceAuthentication(configuration);
    }

    public static WebApplication UseTestServiceToServiceAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static HttpClient GetAuthenticatedTestClient(this WebApplication app)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(RequestHeaderNames.ApiKey, ApiKey);
        return client;
    }
}
