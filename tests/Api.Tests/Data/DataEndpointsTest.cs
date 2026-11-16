// <copyright file="DataEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Data;

using System.Net;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Foundation.Submissions;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Data;
using Defra.Lis.Be4Fe.Models.Lookups.Models;
using Defra.Lis.Be4Fe.Models.Submissions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Runs the <c>/data</c> endpoints through the real pipeline with the cattle API and submission store stubbed.
/// </summary>
public sealed class DataEndpointsTest : IAsyncDisposable
{
    private const string Cph = "12/345/6789";
    private const string UnallocatedUrl = "/data/12/345/6789/ear-tags/unallocated";
    private const string DamsUrl = "/data/12/345/6789/ear-tags/dams";
    private const string SiresUrl = "/data/12/345/6789/ear-tags/sires";
    private const string AnimalUrl = "/data/12/345/6789/animals/UK100000000001";

    private readonly DataFactory factory = new();
    private readonly HttpClient client;

    public DataEndpointsTest()
    {
        client = factory.CreateAuthenticatedClient();
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
    }

    [Fact]
    public async Task UnallocatedShouldReturnTheEartagsFromTheSource()
    {
        factory.Unallocated.Eartags = Tags("UK123456700001", "UK123456700002");

        using var body = await GetJsonAsync(UnallocatedUrl);

        body.RootElement.GetProperty("cph").GetString().ShouldBe(Cph);
        EartagsIn(body).ShouldBe(["UK123456700001", "UK123456700002"]);
    }

    [Fact]
    public async Task UnallocatedShouldPassTheCphFromTheRouteToTheSource()
    {
        await GetJsonAsync(UnallocatedUrl);

        factory.Unallocated.RequestedCphs.ShouldHaveSingleItem().ShouldBe(Cph);
    }

    [Fact]
    public async Task UnallocatedShouldLeaveOutTagsUsedInTheSubmission()
    {
        factory.Unallocated.Eartags = Tags("UK123456700001", "UK123456700002", "UK123456700003");
        factory.Submissions.Add(Draft("ABC-DEF-GHJ", Cph, "uk 123456 700002"));

        using var body = await GetJsonAsync(UnallocatedUrl + "?submission-id=abc-def-ghj");

        EartagsIn(body).ShouldBe(["UK123456700001", "UK123456700003"]);
    }

    [Fact]
    public async Task UnallocatedShouldReturn404ForAnUnknownSubmission()
    {
        var response = await client.GetAsync(UnallocatedUrl + "?submission-id=ABC-DEF-GHJ", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnallocatedShouldReturn400WhenTheSubmissionIsForAnotherCph()
    {
        factory.Submissions.Add(Draft("ABC-DEF-GHJ", "98/765/4321", "UK123456700001"));

        var response = await client.GetAsync(UnallocatedUrl + "?submission-id=ABC-DEF-GHJ", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Unallocated.RequestedCphs.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnallocatedShouldReturn404WhenTheHoldingIsUnknown()
    {
        factory.Unallocated.FailWith = new HoldingNotFoundException(Cph);

        var response = await client.GetAsync(UnallocatedUrl, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DamsShouldSearchTheHoldingForFemalesAndReturnTheirDetails()
    {
        factory.Cattle.Cattle = [Animal("UK100000000001", "Female", "AA", "Aberdeen Angus", new DateOnly(2020, 3, 1))];

        using var body = await GetJsonAsync(DamsUrl);

        var (cph, query) = factory.Cattle.Searches.ShouldHaveSingleItem();
        cph.ShouldBe(Cph);
        query.Sex.ShouldBe("Female");
        body.RootElement.GetProperty("cph").GetString().ShouldBe(Cph);
        var dam = body.RootElement.GetProperty("animals").EnumerateArray().ShouldHaveSingleItem();
        dam.GetProperty("eartag").GetString().ShouldBe("UK100000000001");
        dam.GetProperty("breed").GetString().ShouldBe("Aberdeen Angus");
        dam.GetProperty("breed-code").GetString().ShouldBe("AA");
        dam.GetProperty("date-of-birth").GetString().ShouldBe("2020-03-01");
        dam.GetProperty("sex").GetString().ShouldBe("Female");
    }

    [Fact]
    public async Task SiresShouldSearchTheHoldingForMales()
    {
        factory.Cattle.Cattle = [Animal("UK100000000002", "Male")];

        using var body = await GetJsonAsync(SiresUrl);

        factory.Cattle.Searches.ShouldHaveSingleItem().Query.Sex.ShouldBe("Male");
        body.RootElement.GetProperty("animals").EnumerateArray().ShouldHaveSingleItem()
            .GetProperty("eartag").GetString().ShouldBe("UK100000000002");
    }

    [Fact]
    public async Task AnimalShouldSearchTheHoldingByEartagAndReturnTheAnimal()
    {
        factory.Cattle.Cattle = [Animal("UK100000000001", "Female", "AA", "Aberdeen Angus", new DateOnly(2020, 3, 1))];

        using var body = await GetJsonAsync(AnimalUrl);

        var (cph, query) = factory.Cattle.Searches.ShouldHaveSingleItem();
        cph.ShouldBe(Cph);
        query.Eartag.ShouldBe("UK100000000001");
        body.RootElement.GetProperty("eartag").GetString().ShouldBe("UK100000000001");
        body.RootElement.GetProperty("breed").GetString().ShouldBe("Aberdeen Angus");
        body.RootElement.GetProperty("breed-code").GetString().ShouldBe("AA");
        body.RootElement.GetProperty("date-of-birth").GetString().ShouldBe("2020-03-01");
        body.RootElement.GetProperty("sex").GetString().ShouldBe("Female");
    }

    [Fact]
    public async Task AnimalShouldReturnTheMatchingAnimalWhenTheCattleApiReturnsOthers()
    {
        factory.Cattle.Cattle =
        [
            Animal("UK100000000011", "Male"),
            Animal("uk 100000 000001", "Female"),
        ];

        using var body = await GetJsonAsync(AnimalUrl);

        body.RootElement.GetProperty("eartag").GetString().ShouldBe("uk 100000 000001");
    }

    [Fact]
    public async Task AnimalShouldReturn404WhenNoAnimalOnTheHoldingHasTheEartag()
    {
        factory.Cattle.Cattle = [Animal("UK100000000011", "Male")];

        var response = await client.GetAsync(AnimalUrl, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/data/1/2/3/ear-tags/unallocated")]
    [InlineData("/data/aa/bbb/cccc/ear-tags/unallocated")]
    [InlineData("/data/1/2/3/ear-tags/dams")]
    [InlineData("/data/1/2/3/ear-tags/sires")]
    [InlineData("/data/1/2/3/animals/UK100000000001")]
    public async Task EndpointsShouldRejectAnInvalidCph(string url)
    {
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Unallocated.RequestedCphs.ShouldBeEmpty();
        factory.Cattle.Searches.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(DamsUrl)]
    [InlineData(SiresUrl)]
    [InlineData(AnimalUrl)]
    public async Task CattleEndpointsShouldReturn404WhenTheHoldingIsUnknown(string url)
    {
        factory.Cattle.FailWith = new HoldingNotFoundException(Cph);

        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(DamsUrl)]
    [InlineData(SiresUrl)]
    [InlineData(AnimalUrl)]
    public async Task CattleEndpointsShouldReturn503WhenTheCattleApiIsUnavailable(string url)
    {
        factory.Cattle.FailWith = new ServiceUnavailableException("down", new HttpRequestException());

        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData(UnallocatedUrl)]
    [InlineData(DamsUrl)]
    [InlineData(SiresUrl)]
    [InlineData(AnimalUrl)]
    public async Task EndpointsShouldRequireAnApiKey(string url)
    {
        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, "corr-data-test");

        var response = await anonymous.GetAsync(url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(UnallocatedUrl)]
    [InlineData(DamsUrl)]
    [InlineData(SiresUrl)]
    [InlineData(AnimalUrl)]
    public async Task EndpointsShouldRequireACorrelationId(string url)
    {
        using var noCorrelation = factory.CreateClient();
        noCorrelation.DefaultRequestHeaders.Add(RequestHeaderNames.ApiKey, DataFactory.ApiKey);

        var response = await noCorrelation.GetAsync(url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static List<UnallocatedEarTag> Tags(params string[] eartags) =>
        eartags.Select(eartag => new UnallocatedEarTag { Eartag = eartag }).ToList();

    private static List<string?> EartagsIn(JsonDocument body, string property = "eartags") =>
        body.RootElement.GetProperty(property).EnumerateArray().Select(item => item.GetProperty("eartag").GetString()).ToList();

    private static CattleSummary Animal(string eartag, string sex, string? breedCode = null, string? breedName = null, DateOnly? dateOfBirth = null) => new()
    {
        CattleId = eartag,
        Eartag = eartag,
        Breed = breedName ?? string.Empty,
        BreedCode = breedCode,
        BreedName = breedName,
        DateOfBirth = dateOfBirth,
        Sex = sex,
        Status = "Alive",
    };

    private static SubmissionDocument Draft(string hashId, string cph, params string[] animalIds) => new()
    {
        Id = Guid.NewGuid().ToString(),
        HashId = hashId,
        Cph = cph,
        CreatedDate = DateTime.UtcNow,
        Status = SubmissionStatus.Draft,
        Animals = animalIds.Select(id => new SubmissionAnimalDocument { AnimalId = id, Status = ItemStatus.Draft }).ToList(),
    };

    private async Task<JsonDocument> GetJsonAsync(string url)
    {
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private sealed class DataFactory(bool stubUnallocated = true) : ApiFactory
    {
        public const string ApiKey = "test-api-key";

        public StubUnallocatedEarTagSource Unallocated { get; } = new();

        public StubCattleHoldingClient Cattle { get; } = new();

        public StubSubmissionRepository Submissions { get; } = new();

        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add(RequestHeaderNames.ApiKey, ApiKey);
            client.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, "corr-data-test");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting($"{ApiKeyAuthenticationOptions.SectionName}:Keys:0", ApiKey);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICattleHoldingClient>();
                services.RemoveAll<ISubmissionRepository>();
                services.AddSingleton<ICattleHoldingClient>(Cattle);
                services.AddSingleton<ISubmissionRepository>(Submissions);

                if (stubUnallocated)
                {
                    services.RemoveAll<IUnallocatedEarTagSource>();
                    services.AddSingleton<IUnallocatedEarTagSource>(Unallocated);
                }
            });
        }
    }

    private sealed class StubUnallocatedEarTagSource : IUnallocatedEarTagSource
    {
        public IReadOnlyCollection<UnallocatedEarTag> Eartags { get; set; } = [];

        public Exception? FailWith { get; set; }

        public List<string> RequestedCphs { get; } = [];

        public Task<IReadOnlyCollection<UnallocatedEarTag>> GetUnallocatedEarTagAsync(string cph, CancellationToken cancellationToken = default)
        {
            RequestedCphs.Add(cph);

            if (FailWith is not null)
            {
                throw FailWith;
            }

            return Task.FromResult(Eartags);
        }
    }

    private sealed class StubCattleHoldingClient : ICattleHoldingClient
    {
        public IReadOnlyCollection<CattleSummary> Cattle { get; set; } = [];

        public Exception? FailWith { get; set; }

        public List<(string Cph, CattleSearchQuery Query)> Searches { get; } = [];

        public Task<IReadOnlyCollection<CattleSummary>> SearchCattleAsync(string cph, CattleSearchQuery query, CancellationToken cancellationToken = default)
        {
            Searches.Add((cph, query));

            if (FailWith is not null)
            {
                throw FailWith;
            }

            return Task.FromResult(Cattle);
        }
    }

    private sealed class StubSubmissionRepository : ISubmissionRepository
    {
        private readonly List<SubmissionDocument> submissions = [];

        public void Add(SubmissionDocument submission) => submissions.Add(submission);

        public Task<SubmissionDocument?> GetByHashIdAsync(string hashId, CancellationToken cancellationToken = default) =>
            Task.FromResult(submissions.FirstOrDefault(submission => submission.HashId == hashId));

        public Task<SubmissionDocument?> FindDraftAsync(string cph, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryInsertDraftAsync(SubmissionDocument submission, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<SubmissionDocument>> ListAsync(string cph, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryReplaceDraftAsync(SubmissionDocument submission, int expectedVersion, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
