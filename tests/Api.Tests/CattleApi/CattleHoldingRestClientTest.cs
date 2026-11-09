// <copyright file="CattleHoldingRestClientTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.CattleApi;

using System.Diagnostics;
using System.Net;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class CattleHoldingRestClientTest
{
    private const string TwoCattle = """
        [
          {"earTag":"UK200000000001","dateBirth":"2023-02-01","dateOnCph":"2023-02-15","sex":"Male","breed":"Aberdeen Angus","breedCode":"AA","breedName":"Aberdeen Angus","status":"Alive","errors":[]},
          {"earTag":"UK200000000002","dateBirth":null,"dateOnCph":null,"sex":null,"breed":null,"breedCode":null,"breedName":null,"status":"Alive","errors":[]}
        ]
        """;

    private readonly StubHttpMessageHandler handler = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ClientShouldFailClearlyWhenTheApiKeyIsNotConfigured(string? apiKey)
    {
        var client = CreateClient(apiKey: apiKey);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("CattleApi:ApiKey");
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchCattleShouldThrowArgumentExceptionWhenTheCattleApiRejectsTheCph()
    {
        handler.RespondWith(HttpStatusCode.BadRequest, """{"title":"Bad Request","status":400}""");
        var client = CreateClient();

        await Should.ThrowAsync<ArgumentException>(
            () => client.SearchCattleAsync("2/1/1", new CattleSearchQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SearchCattleShouldPropagateOtherFailures()
    {
        handler.RespondWith(HttpStatusCode.Unauthorized, """{"title":"Unauthorized","status":401}""");
        var client = CreateClient();

        await Should.ThrowAsync<RestResponseException>(
            () => client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("22/001")]
    [InlineData("22/001/0001/extra")]
    [InlineData(" ")]
    public async Task SearchCattleShouldRejectMalformedCphWithoutCallingTheApi(string cph)
    {
        var client = CreateClient();

        await Should.ThrowAsync<ArgumentException>(() => client.SearchCattleAsync(cph, new CattleSearchQuery(), TestContext.Current.CancellationToken));
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchCattleShouldCallTheCattleRouteWithoutFiltersByDefault()
    {
        handler.RespondWith(HttpStatusCode.OK, TwoCattle);
        var client = CreateClient();

        var cattle = await client.SearchCattleAsync("22-001-0001", new CattleSearchQuery(), TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().RequestUri!.ToString().ShouldBe("http://cattle-api.test/v1/holdings/22/001/0001/cattle");
        cattle.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchCattleShouldSendTheApiKey()
    {
        handler.RespondWith(HttpStatusCode.OK, "[]");
        var client = CreateClient(apiKey: "test-key");

        await client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), TestContext.Current.CancellationToken);

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.Headers.GetValues(CattleHoldingRestClient.ApiKeyHeaderName).ShouldBe(["test-key"]);
    }

    [Fact]
    public async Task SearchCattleShouldPassSuppliedFiltersAsQueryParameters()
    {
        handler.RespondWith(HttpStatusCode.OK, "[]");
        var client = CreateClient();

        var cattle = await client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(Eartag: " UK2000 ", Breed: "AA", Sex: "female"), TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().RequestUri!.Query.ShouldBe("?earTag=UK2000&breed=AA&sex=female");
        cattle.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchCattleShouldMapEachAnimalIncludingSparseRecords()
    {
        handler.RespondWith(HttpStatusCode.OK, TwoCattle);
        var client = CreateClient();

        var cattle = (await client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), TestContext.Current.CancellationToken)).ToList();

        cattle[0].CattleId.ShouldBe("UK200000000001");
        cattle[0].Eartag.ShouldBe("UK200000000001");
        cattle[0].Breed.ShouldBe("Aberdeen Angus");
        cattle[0].BreedCode.ShouldBe("AA");
        cattle[0].BreedName.ShouldBe("Aberdeen Angus");
        cattle[0].DateOfBirth.ShouldBe(new DateOnly(2023, 2, 1));
        cattle[0].DateOnCph.ShouldBe(new DateOnly(2023, 2, 15));
        cattle[0].Sex.ShouldBe("Male");
        cattle[0].Status.ShouldBe("Alive");

        cattle[1].Eartag.ShouldBe("UK200000000002");
        cattle[1].Breed.ShouldBe(string.Empty);
        cattle[1].DateOfBirth.ShouldBeNull();
        cattle[1].Sex.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task SearchCattleShouldThrowHoldingNotFoundOn404()
    {
        handler.RespondWith(HttpStatusCode.NotFound, """{"title":"Not Found","status":404}""");
        var client = CreateClient();

        await Should.ThrowAsync<HoldingNotFoundException>(
            () => client.SearchCattleAsync("22/050/0050", new CattleSearchQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ClientShouldFailClearlyWhenTheBaseUrlIsNotConfigured()
    {
        var client = CreateClient(baseUrl: string.Empty);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("CattleApi:BaseUrl");
        handler.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ERROR1")]
    [InlineData("ERROR2")]
    [InlineData("ERROR3")]
    public async Task SearchCattleAsync_ThrowsServiceUnavailable_WhenTheCattleApiCannotBeReached(string errorType)
    {
        // It's a hack but soanr has a fit as exceptions dont serialize well.
        Exception? failure = errorType switch
        {
            "ERROR1" => new HttpRequestException(
                "Connection refused (cattle-api.test:80)",
                new System.Net.Sockets.SocketException(111)),
            "ERROR2" => new HttpRequestException("Name or service not known (cattle-api.test:80)"),
            "ERROR3" => new TaskCanceledException(
                "The request was canceled due to the configured HttpClient.Timeout.",
                new TimeoutException()),
            _ => throw new NotImplementedException(),
        };

        handler.FailWith(failure);
        var client = CreateClient();

        var exception = await Should.ThrowAsync<ServiceUnavailableException>(
            () => client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), TestContext.Current.CancellationToken));

        exception.InnerException.ShouldBeOfType<RestRequestException>().InnerException.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task ClientShouldNotTreatCallerCancellationAsTheCattleApiBeingUnavailable()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        handler.FailWith(new TaskCanceledException("The operation was canceled.", null, cancellation.Token));
        var client = CreateClient();

        var exception = await Should.ThrowAsync<Exception>(() => client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), cancellation.Token));

        exception.ShouldNotBeOfType<ServiceUnavailableException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SearchCattleAsync_DoesNotTreatAuthOrServerFailuresAsUnavailable(HttpStatusCode statusCode)
    {
        // A rejected key or an outright cattle API fault is not retryable: it stays a 500 through ApiExceptionHandler.
        handler.RespondWith(statusCode, $$"""{"status":{{(int)statusCode}}}""");
        var client = CreateClient();

        await Should.ThrowAsync<RestResponseException>(
            () => client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), TestContext.Current.CancellationToken));
    }

    private CattleHoldingRestClient CreateClient(string baseUrl = "http://cattle-api.test/", string? apiKey = "key")
    {
        var options = new CattleApiOptions { BaseUrl = baseUrl, ApiKey = apiKey };
        var factory = new RestStrategyFactory<CattleHoldingRestClient>(new StubServiceProvider(handler));

        return new CattleHoldingRestClient(factory, options, NullLogger<CattleHoldingRestClient>.Instance);
    }

    /// <summary>
    /// Supplies a fresh SDK REST client over the stub transport each time the factory builds a strategy.
    /// </summary>
    private sealed class StubServiceProvider(StubHttpMessageHandler handler) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(IRestHttpClient)
                ? new RestHttpClient(new HttpClient(handler), NullLogger<RestHttpClient>.Instance)
                : null;
        }
    }
}
