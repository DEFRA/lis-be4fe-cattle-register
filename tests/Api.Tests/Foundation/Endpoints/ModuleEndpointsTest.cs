// <copyright file="ModuleEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Foundation.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Models.Health;
using Microsoft.AspNetCore.Mvc.Testing;

public class ModuleEndpointsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ModuleEndpointsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthShouldReturnHealthy()
    {
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var health = await response.Content.ReadFromJsonAsync<HealthStatus>(TestContext.Current.CancellationToken);
        health.ShouldNotBeNull();
        health.Status.ShouldBe("Healthy");
    }

    [Fact]
    public async Task GetOpenApiDocumentShouldReturnDocumentedSpecification()
    {
        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        document.RootElement.GetProperty("info").GetProperty("title").GetString().ShouldBe("Cattle Register API");
        document.RootElement.GetProperty("paths").TryGetProperty("/submission/{county}/{parish}/{holding}/start", out _).ShouldBeTrue();
        document.RootElement.GetProperty("paths").TryGetProperty("/submission/{submissionId}/{animalId}", out _).ShouldBeTrue();

        var animalOperation = document.RootElement.GetProperty("paths").GetProperty("/submission/{submissionId}/{animalId}").GetProperty("get");
        animalOperation.GetProperty("operationId").GetString().ShouldBe("GetSubmissionAnimal");
        animalOperation.GetProperty("summary").GetString().ShouldBe("Gets an animal's data.");
        animalOperation.GetProperty("responses").TryGetProperty("404", out _).ShouldBeTrue();
        animalOperation.GetProperty("responses").TryGetProperty("401", out _).ShouldBeTrue();
        animalOperation.GetProperty("security")[0].TryGetProperty("ApiKey", out _).ShouldBeTrue();

        var apiKey = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("ApiKey");
        apiKey.GetProperty("type").GetString().ShouldBe("apiKey");
        apiKey.GetProperty("in").GetString().ShouldBe("header");
        apiKey.GetProperty("name").GetString().ShouldBe("x-api-key");

        var health = document.RootElement.GetProperty("paths").GetProperty("/health").GetProperty("get");
        health.TryGetProperty("security", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task GetScalarUiShouldReturnInteractiveDocumentation()
    {
        var response = await client.GetAsync("/scalar/v1", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Scalar API Reference");
    }
}
