// <copyright file="SubmissionEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Submissions;

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Foundation.Submissions;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Submissions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

/// <summary>
/// Runs the BFF framework endpoints through the real pipeline. Submissions are held as BSON in memory, so each test
/// also covers the document's serialisation, but not MongoDB itself.
/// </summary>
public sealed class SubmissionEndpointsTest : IAsyncDisposable
{
    private const string Cph = "12/345/6789";
    private const string StartUrl = "/submission/12/345/6789/start";
    private const string ListUrl = "/submission/12/345/6789";
    private const string Animal = "UK123123123";

    private readonly SubmissionFactory factory = new();
    private readonly HttpClient client;

    public SubmissionEndpointsTest()
    {
        client = factory.CreateAuthenticatedClient();
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
    }

    [Fact]
    public async Task StartShouldCreateADraftWithNoAnimals()
    {
        using var body = await GetJsonAsync(StartUrl);

        body.RootElement.GetProperty("submission-id").GetString().ShouldNotBeNullOrWhiteSpace();
        body.RootElement.TryGetProperty("created-date", out _).ShouldBeTrue();
        body.RootElement.GetProperty("animals").GetArrayLength().ShouldBe(0);
        body.RootElement.TryGetProperty("submitted-date", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task StartShouldReturnTheExistingDraftWithItsAnimals()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, """[{"key":"dob","value":"2026-11-01","type":"DATE"}]""");

        using var body = await GetJsonAsync(StartUrl);

        body.RootElement.GetProperty("submission-id").GetString().ShouldBe(id);
        var animal = body.RootElement.GetProperty("animals").EnumerateArray().ShouldHaveSingleItem();
        animal.GetProperty("animal-id").GetString().ShouldBe(Animal);
        animal.GetProperty("status").GetString().ShouldBe("draft");
    }

    [Fact]
    public async Task StartShouldKeepDraftsSeparatePerCph()
    {
        var first = await StartAsync();

        using var other = await GetJsonAsync("/submission/98/765/4321/start");

        other.RootElement.GetProperty("submission-id").GetString().ShouldNotBe(first);
    }

    [Fact]
    public async Task StartShouldUseTheDraftAnotherRequestJustCreated()
    {
        var winner = new SubmissionDocument
        {
            Id = "winner-guid",
            HashId = "WIN-NER-AAA",
            Cph = Cph,
            CreatedDate = DateTime.UtcNow,
            Status = SubmissionStatus.Draft,
        };
        factory.Repository.DraftCreatedByAnotherRequest = winner;

        using var body = await GetJsonAsync(StartUrl);

        body.RootElement.GetProperty("submission-id").GetString().ShouldBe("WIN-NER-AAA");
    }

    [Fact]
    public async Task StartShouldUseAHashIdAndKeepTheGuidInternal()
    {
        var id = await StartAsync();

        id.ShouldMatch("^[0-9A-HJKMNP-TV-Z]{3}-[0-9A-HJKMNP-TV-Z]{3}-[0-9A-HJKMNP-TV-Z]{3}$");
        var stored = factory.Repository.Stored.ShouldHaveSingleItem();
        Guid.TryParse(stored.Id, out _).ShouldBeTrue();
        stored.HashId.ShouldBe(id);
        stored.HashId.ShouldBe(SubmissionHashId.Create(stored.Id));
    }

    [Fact]
    public async Task StartShouldTryAgainWhenTheHashIdClashes()
    {
        factory.Repository.HashIdClashes = 2;

        var id = await StartAsync();

        factory.Repository.Stored.ShouldHaveSingleItem().HashId.ShouldBe(id);
    }

    [Fact]
    public async Task StartShouldReturn409WhenTheHashIdKeepsClashing()
    {
        factory.Repository.HashIdClashes = 3;

        var response = await client.GetAsync(StartUrl, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        factory.Repository.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task EveryResponseShouldUseTheHashIdAndNeverTheGuid()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");
        var guid = factory.Repository.Stored.ShouldHaveSingleItem().Id;

        var start = await client.GetStringAsync(StartUrl, TestContext.Current.CancellationToken);
        var list = await client.GetStringAsync(ListUrl, TestContext.Current.CancellationToken);
        var animalResponse = await client.GetStringAsync($"/submission/{id}/{Animal}", TestContext.Current.CancellationToken);

        start.ShouldContain(id);
        list.ShouldContain(id);
        start.ShouldNotContain(guid);
        list.ShouldNotContain(guid);
        animalResponse.ShouldNotContain(guid);
    }

    [Fact]
    public async Task EndpointsShouldNotFindASubmissionByItsGuid()
    {
        await StartAsync();
        var guid = factory.Repository.Stored.ShouldHaveSingleItem().Id;

        var get = await client.GetAsync($"/submission/{guid}/{Animal}", TestContext.Current.CancellationToken);
        var update = await PostAsync($"/submission/{guid}/{Animal}", UpdatePayload("[]"));
        var submit = await PostAsync($"/submission/{guid}");

        get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        update.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        submit.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EndpointsShouldAcceptTheHashIdInLowerCase()
    {
        var id = await StartAsync();

        var response = await PostAsync($"/submission/{id.ToLowerInvariant()}/{Animal}", UpdatePayload("[]"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public void HashIdShouldBeTheSameForTheSameGuidAndDifferForAnother()
    {
        const string Guid1 = "196ee823-db52-493e-b9c1-face71dd6be2";
        const string Guid2 = "8e22ec7c-cdee-4cff-8953-ef6b4bdc936a";

        SubmissionHashId.Create(Guid1).ShouldBe(SubmissionHashId.Create(Guid1));
        SubmissionHashId.Create(Guid1).ShouldNotBe(SubmissionHashId.Create(Guid2));
    }

    [Theory]
    [InlineData("196ee823-db52-493e-b9c1-face71dd6be2", "PRS-PJY-WV8")]
    [InlineData("8e22ec7c-cdee-4cff-8953-ef6b4bdc936a", "X3K-WHY-YSM")]
    public void HashIdShouldBeCrockfordBase32OfTheGuidsHash(string guid, string expected)
    {
        SubmissionHashId.Create(guid).ShouldBe(expected);
    }

    [Theory]
    [InlineData("prs-pjy-wv8", "PRS-PJY-WV8")]
    [InlineData(" PRS-PJY-WV8 ", "PRS-PJY-WV8")]
    [InlineData("oO0-iI1-lL1", "000-111-111")]
    public void HashIdShouldNormaliseWhatACallerMayHaveTyped(string received, string expected)
    {
        SubmissionHashId.Normalise(received).ShouldBe(expected);
    }

    [Fact]
    public async Task EndpointsShouldReadLookAlikeLettersAsTheirDigits()
    {
        factory.Repository.DraftCreatedByAnotherRequest = new SubmissionDocument
        {
            Id = "look-alike-guid",
            HashId = "011-ABC-DEF",
            Cph = Cph,
            CreatedDate = DateTime.UtcNow,
            Status = SubmissionStatus.Draft,
        };
        await StartAsync();

        var response = await client.GetAsync($"/submission/oil-abc-def", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/submission/1/2/3/start")]
    [InlineData("/submission/aa/bbb/cccc")]
    public async Task EndpointsScopedToACphShouldRejectAMissingOrInvalidCph(string url)
    {
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAnimalShouldReturnEmptyDataWhenSavedWithoutData()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");

        using var body = await GetJsonAsync($"/submission/{id}/{Animal}");

        body.RootElement.GetProperty("animal-id").GetString().ShouldBe(Animal);
        body.RootElement.GetProperty("status").GetString().ShouldBe("draft");
        body.RootElement.GetProperty("data").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task GetAnimalShouldReturn404ForAnUnknownSubmission()
    {
        var response = await client.GetAsync($"/submission/unknown/{Animal}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateShouldStoreEachItemWithItsKeyValueAndType()
    {
        var id = await StartAsync();

        await UpdateAsync(id, Animal, """[{"key":"dateOfBirth","value":"2026-11-01","type":"DATE"},{"key":"weight","value":"42","type":"NUMBER"}]""");

        using var body = await GetJsonAsync($"/submission/{id}/{Animal}");
        var items = body.RootElement.GetProperty("data").EnumerateArray().ToList();
        items.Count.ShouldBe(2);
        items[0].GetProperty("key").GetString().ShouldBe("dateOfBirth");
        items[0].GetProperty("value").GetString().ShouldBe("2026-11-01");
        items[0].GetProperty("type").GetString().ShouldBe("DATE");
        items[1].GetProperty("key").GetString().ShouldBe("weight");
        items[1].GetProperty("value").GetString().ShouldBe("42");
        items[1].GetProperty("type").GetString().ShouldBe("NUMBER");
    }

    [Theory]
    [InlineData("STRING")]
    [InlineData("DATE")]
    [InlineData("INT")]
    [InlineData("NUMBER")]
    [InlineData("BOOL")]
    public async Task UpdateShouldStoreAndReturnANullValue(string type)
    {
        var id = await StartAsync();

        await UpdateAsync(id, Animal, $$"""[{"key":"k","value":null,"type":"{{type}}"}]""");

        var stored = factory.Repository.Stored.ShouldHaveSingleItem().Animals.ShouldHaveSingleItem().Data.ShouldHaveSingleItem();
        stored.Value.ShouldBeNull();
        using var body = await GetJsonAsync($"/submission/{id}/{Animal}");
        var item = body.RootElement.GetProperty("data")[0];
        item.GetProperty("key").GetString().ShouldBe("k");
        item.GetProperty("value").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("type").GetString().ShouldBe(type);
    }

    [Theory]
    [InlineData("STRING")]
    [InlineData("DATE")]
    [InlineData("INT")]
    [InlineData("NUMBER")]
    [InlineData("BOOL")]
    [InlineData("string")]
    public async Task UpdateShouldAcceptEverySupportedType(string type)
    {
        var id = await StartAsync();

        await UpdateAsync(id, Animal, $$"""[{"key":"k","value":"v","type":"{{type}}"}]""");

        using var body = await GetJsonAsync($"/submission/{id}/{Animal}");
        body.RootElement.GetProperty("data")[0].GetProperty("type").GetString().ShouldBe(type.ToUpperInvariant());
    }

    [Fact]
    public async Task UpdateShouldReplaceAllDataAndLeaveOtherAnimalsAlone()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, """[{"key":"a","value":"first","type":"STRING"},{"key":"b","value":"gone","type":"STRING"}]""");
        await UpdateAsync(id, "UK2", """[{"key":"a","value":"kept","type":"STRING"}]""");

        await UpdateAsync(id, Animal, """[{"key":"a","value":"second","type":"STRING"}]""");

        using var updatedAnimal = await GetJsonAsync($"/submission/{id}/{Animal}");
        var item = updatedAnimal.RootElement.GetProperty("data").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("key").GetString().ShouldBe("a");
        item.GetProperty("value").GetString().ShouldBe("second");
        using var otherAnimal = await GetJsonAsync($"/submission/{id}/UK2");
        otherAnimal.RootElement.GetProperty("data")[0].GetProperty("value").GetString().ShouldBe("kept");
    }

    [Theory]
    [InlineData("""{"data":[]}""")]
    [InlineData("""{"status":"DRAFT"}""")]
    [InlineData("""{"status":"DRAFT","data":"text"}""")]
    [InlineData("""{"status":"DRAFT","data":{"key":"k"}}""")]
    [InlineData("""{"status":"DRAFT","data":[null]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"value":"v","type":"STRING"}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":" ","value":"v","type":"STRING"}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":"k","type":"STRING"}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":"k","value":1,"type":"INT"}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":"k","value":"v"}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":"k","value":null,"type":null}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":"k","value":null,"type":""}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":"k","value":null,"type":" "}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":"k","value":"v","type":"OTHER"}]}""")]
    [InlineData("""{"status":"DRAFT","data":[{"key":"k","value":"v","type":"STRING"},{"key":"k","value":"w","type":"STRING"}]}""")]
    [InlineData("""{"status":"OTHER","data":[]}""")]
    [InlineData("""{"status":" ","data":[]}""")]
    [InlineData("""{"status":1,"data":[]}""")]
    [InlineData("""{"status":0,"data":[]}""")]
    [InlineData("""{"status":999,"data":[]}""")]
    [InlineData("""{"status":"0","data":[]}""")]
    [InlineData("""{"status":"999","data":[]}""")]
    [InlineData("""{"status":null,"data":[]}""")]
    [InlineData("null")]
    public async Task UpdateShouldRejectAnInvalidPayload(string payload)
    {
        var id = await StartAsync();

        var response = await PostAsync($"/submission/{id}/{Animal}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("COMPLETE", "complete")]
    [InlineData("complete", "complete")]
    [InlineData("DRAFT", "draft")]
    public async Task UpdateShouldSetTheStatusOfANewAnimal(string status, string expected)
    {
        var id = await StartAsync();

        await PostUpdateWithStatusAsync(id, Animal, status);

        (await GetAnimalStatusAsync(Animal)).ShouldBe(expected);
    }

    [Fact]
    public async Task UpdateShouldSaveANewAnimalWithDraftStatus()
    {
        var id = await StartAsync();

        await UpdateAsync(id, Animal, "[]");

        (await GetAnimalStatusAsync(Animal)).ShouldBe("draft");
    }

    [Fact]
    public async Task UpdateShouldChangeTheStatusOfAnExistingAnimalInBothDirections()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");

        await PostUpdateWithStatusAsync(id, Animal, "COMPLETE");
        (await GetAnimalStatusAsync(Animal)).ShouldBe("complete");

        await PostUpdateWithStatusAsync(id, Animal, "DRAFT");
        (await GetAnimalStatusAsync(Animal)).ShouldBe("draft");
    }

    [Fact]
    public async Task UpdateShouldReplaceTheExistingStatusWithDraft()
    {
        var id = await StartAsync();
        await PostUpdateWithStatusAsync(id, Animal, "COMPLETE");

        await UpdateAsync(id, Animal, "[]");

        (await GetAnimalStatusAsync(Animal)).ShouldBe("draft");
    }

    [Fact]
    public async Task UpdateShouldOnlyChangeTheStatusOfTheAnimalInTheRequest()
    {
        var id = await StartAsync();
        await UpdateAsync(id, "UK1", "[]");

        await PostUpdateWithStatusAsync(id, "UK2", "COMPLETE");

        (await GetAnimalStatusAsync("UK1")).ShouldBe("draft");
        (await GetAnimalStatusAsync("UK2")).ShouldBe("complete");
    }

    [Fact]
    public async Task UpdateShouldNotStoreAnythingWhenTheStatusIsInvalid()
    {
        var id = await StartAsync();

        var response = await PostAsync($"/submission/{id}/{Animal}", $$"""{"status":"OTHER","data":[]}""");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Repository.Stored.ShouldHaveSingleItem().Animals.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateShouldReturn404ForAnUnknownSubmission()
    {
        var response = await PostAsync($"/submission/unknown/{Animal}", UpdatePayload("[]"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateShouldReturn409WhenAnotherRequestChangedTheSubmission()
    {
        var id = await StartAsync();
        factory.Repository.RejectNextReplace = true;

        var response = await PostAsync($"/submission/{id}/{Animal}", UpdatePayload("[]"));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteAnimalShouldRemoveTheAnimalAndItsDataAndLeaveTheOthersAlone()
    {
        var id = await StartAsync();
        await UpdateAsync(id, "UK1", """[{"key":"a","value":"gone","type":"STRING"}]""");
        await UpdateAsync(id, "UK2", """[{"key":"a","value":"kept","type":"STRING"}]""");

        var response = await client.DeleteAsync($"/submission/{id}/UK1", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var animal = factory.Repository.Stored.ShouldHaveSingleItem().Animals.ShouldHaveSingleItem();
        animal.AnimalId.ShouldBe("UK2");
        using var animalResponse = await GetJsonAsync($"/submission/{id}/UK2");
        animalResponse.RootElement.GetProperty("data")[0].GetProperty("value").GetString().ShouldBe("kept");
    }

    [Fact]
    public async Task DeleteAnimalShouldLeaveAnEmptyDraftWhenItRemovesTheLastAnimal()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");

        await client.DeleteAsync($"/submission/{id}/{Animal}", TestContext.Current.CancellationToken);

        using var start = await GetJsonAsync(StartUrl);
        start.RootElement.GetProperty("submission-id").GetString().ShouldBe(id);
        start.RootElement.GetProperty("animals").GetArrayLength().ShouldBe(0);
    }

    [Theory]
    [InlineData("unknown", Animal)]
    [InlineData("{id}", "UK-NOT-ADDED")]
    public async Task DeleteAnimalShouldReturn404ForAnUnknownSubmissionOrAnimal(string submission, string animalId)
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");

        var response = await client.DeleteAsync($"/submission/{submission.Replace("{id}", id, StringComparison.Ordinal)}/{animalId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        factory.Repository.Stored.ShouldHaveSingleItem().Animals.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DeleteAnimalShouldReturn409OnceSubmitted()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");
        await PostAsync($"/submission/{id}");

        var response = await client.DeleteAsync($"/submission/{id}/{Animal}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        factory.Repository.Stored.ShouldHaveSingleItem().Animals.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DeleteAnimalShouldReturn409WhenAnotherRequestChangedTheSubmission()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");
        factory.Repository.RejectNextReplace = true;

        var response = await client.DeleteAsync($"/submission/{id}/{Animal}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        factory.Repository.Stored.ShouldHaveSingleItem().Animals.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DeleteAnimalShouldNotFindASubmissionByItsGuid()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");
        var guid = factory.Repository.Stored.ShouldHaveSingleItem().Id;

        var response = await client.DeleteAsync($"/submission/{guid}/{Animal}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitShouldTransferTheSubmissionAndMarkItSubmitted()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");

        var response = await PostAsync($"/submission/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Transfer.TransferredIds.ShouldBe([id]);
        using var list = await GetJsonAsync(ListUrl);
        var item = list.RootElement.GetProperty("submissions").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("status").GetString().ShouldBe("SUBMITTED");
        item.GetProperty("submitted-date").GetDateTimeOffset().ShouldBe(factory.Repository.Stored.ShouldHaveSingleItem().SubmittedDate!.Value);
    }

    [Fact]
    public async Task StartShouldCreateANewDraftOnceTheLastOneIsSubmitted()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");
        await PostAsync($"/submission/{id}");

        var next = await StartAsync();

        next.ShouldNotBe(id);
    }

    [Fact]
    public async Task SubmitShouldLeaveTheSubmissionADraftWhenTheTransferFails()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");
        factory.Transfer.FailWith = new ServiceUnavailableException("Unavailable", new HttpRequestException());

        var response = await PostAsync($"/submission/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        using var list = await GetJsonAsync(ListUrl);
        var item = list.RootElement.GetProperty("submissions").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("status").GetString().ShouldBe(SubmissionStatus.Draft);
        item.TryGetProperty("submitted-date", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task SubmitShouldRejectASubmissionWithNoAnimals()
    {
        var id = await StartAsync();

        var response = await PostAsync($"/submission/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Transfer.TransferredIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task SubmitAndUpdateShouldReturn409OnceSubmitted()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");
        await PostAsync($"/submission/{id}");

        var submit = await PostAsync($"/submission/{id}");
        var update = await PostAsync($"/submission/{id}/{Animal}", UpdatePayload("[]"));

        submit.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        update.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        factory.Transfer.TransferredIds.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SubmitShouldReturn404ForAnUnknownSubmission()
    {
        var response = await PostAsync("/submission/unknown");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListShouldReturnTheSubmissionsForTheCphWithTheAnimalCount()
    {
        var id = await StartAsync();
        await UpdateAsync(id, "UK1", "[]");
        await UpdateAsync(id, "UK2", "[]");
        await GetJsonAsync("/submission/98/765/4321/start");

        using var list = await GetJsonAsync(ListUrl);

        var item = list.RootElement.GetProperty("submissions").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("submission-id").GetString().ShouldBe(id);
        item.GetProperty("animal-count").GetInt32().ShouldBe(2);
        item.GetProperty("status").GetString().ShouldBe(SubmissionStatus.Draft);
        item.TryGetProperty("created-date", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task ListShouldReturnOnlyTheIdAndStatusOfEachAnimal()
    {
        var id = await StartAsync();
        await UpdateAsync(id, "UK1", """[{"key":"a","value":"private","type":"STRING"}]""");
        await UpdateAsync(id, "UK2", "[]");

        using var list = await GetJsonAsync(ListUrl);

        var item = list.RootElement.GetProperty("submissions").EnumerateArray().ShouldHaveSingleItem();
        var animals = item.GetProperty("animals").EnumerateArray().ToList();
        animals.Select(animal => animal.GetProperty("animal-id").GetString()).ShouldBe(["UK1", "UK2"]);
        animals.ShouldAllBe(animal => animal.GetProperty("status").GetString() == "draft");
        animals.ShouldAllBe(animal => animal.EnumerateObject().Count() == 2);
    }

    [Fact]
    public async Task ListShouldReturnNoAnimalsForAnEmptyDraft()
    {
        await StartAsync();

        using var list = await GetJsonAsync(ListUrl);

        var item = list.RootElement.GetProperty("submissions").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("animals").GetArrayLength().ShouldBe(0);
    }

    [Theory]
    [InlineData("GET", "/submission/12/345/6789")]
    [InlineData("GET", "/submission/12/345/6789/start")]
    [InlineData("GET", "/submission/1/UK1")]
    [InlineData("DELETE", "/submission/1/UK1")]
    [InlineData("POST", "/submission/1/UK1")]
    [InlineData("POST", "/submission/1")]
    public async Task EveryEndpointShouldRequireTheApiKey(string method, string url)
    {
        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, "corr-submission-test");
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        var response = await anonymous.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndpointsShouldRequireTheCorrelationId()
    {
        using var uncorrelated = factory.CreateClient();
        uncorrelated.DefaultRequestHeaders.Add(RequestHeaderNames.ApiKey, SubmissionFactory.ApiKey);

        var response = await uncorrelated.GetAsync(StartUrl, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSubmissionShouldReturnTheDraftAndItsAnimalSummary()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");

        using var body = await GetJsonAsync($"/submission/{id}");

        body.RootElement.GetProperty("submission-id").GetString().ShouldBe(id);
        body.RootElement.GetProperty("animal-count").GetInt32().ShouldBe(1);
        body.RootElement.GetProperty("animals")[0].GetProperty("animal-id").GetString().ShouldBe(Animal);
    }

    [Fact]
    public async Task GetAnimalShouldReturn404ForAnUnknownAnimal()
    {
        var id = await StartAsync();
        var response = await client.GetAsync($"/submission/{id}/{Animal}", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchShouldReplaceTheAnimalData()
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, "[]");
        using var content = new StringContent("""{"status":"complete","data":[{"key":"weight","value":"42","type":"number"}]}""", Encoding.UTF8, "application/json");
        var response = await client.PatchAsync($"/submission/{id}/{Animal}", content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = await GetJsonAsync($"/submission/{id}/{Animal}");
        body.RootElement.GetProperty("status").GetString().ShouldBe("complete");
        body.RootElement.GetProperty("data")[0].GetProperty("type").GetString().ShouldBe("NUMBER");
    }

    [Theory]
    [InlineData("POST", """{"status":"draft","data":null}""")]
    [InlineData("PATCH", """{"status":"draft","data":[null]}""")]
    [InlineData("POST", """{"status":"draft","data":[{"key":"k","value":null}]}""")]
    [InlineData("PATCH", """{"status":"draft","data":[{"key":"k","value":"v","type":null}]}""")]
    [InlineData("POST", """{"status":"draft","data":[{"key":"k","value":"v","type":"OTHER"}]}""")]
    [InlineData("PATCH", """{"status":"draft","data":[{"key":"k","value":"v","type":"STRING"},{"key":"k","value":"w","type":"STRING"}]}""")]
    public async Task InvalidUpdatesShouldPreserveTheExistingAnimal(string method, string payload)
    {
        var id = await StartAsync();
        await UpdateAsync(id, Animal, """[{"key":"original","value":"kept","type":"STRING"}]""");
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/submission/{id}/{Animal}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var animal = await GetJsonAsync($"/submission/{id}/{Animal}");
        var item = animal.RootElement.GetProperty("data").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("key").GetString().ShouldBe("original");
        item.GetProperty("value").GetString().ShouldBe("kept");
    }

    private static string UpdatePayload(string data) =>
        $$"""{"status":"DRAFT","data":{{data}}}""";

    private async Task<JsonDocument> GetJsonAsync(string url)
    {
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private async Task<string> StartAsync()
    {
        using var body = await GetJsonAsync(StartUrl);
        return body.RootElement.GetProperty("submission-id").GetString()!;
    }

    private async Task UpdateAsync(string id, string animalId, string data)
    {
        var response = await PostAsync($"/submission/{id}/{animalId}", UpdatePayload(data));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task PostUpdateWithStatusAsync(string id, string animalId, string status)
    {
        var response = await PostAsync($"/submission/{id}/{animalId}", $$"""{"status":"{{status}}","data":[]}""");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<string?> GetAnimalStatusAsync(string animalId)
    {
        using var body = await GetJsonAsync(StartUrl);
        return body.RootElement.GetProperty("animals").EnumerateArray()
            .Single(animal => animal.GetProperty("animal-id").GetString() == animalId)
            .GetProperty("status").GetString();
    }

    private async Task<HttpResponseMessage> PostAsync(string url, string? json = null)
    {
        using var content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content, TestContext.Current.CancellationToken);
    }

    private sealed class SubmissionFactory : WebApplicationFactory<Program>
    {
        public const string ApiKey = "test-api-key";

        public InMemorySubmissionRepository Repository { get; } = new();

        public RecordingSubmissionTransfer Transfer { get; } = new();

        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add(RequestHeaderNames.ApiKey, ApiKey);
            client.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, "corr-submission-test");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting($"{ApiKeyAuthenticationOptions.SectionName}:Keys:0", ApiKey);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISubmissionRepository>();
                services.RemoveAll<ISubmissionTransfer>();
                services.AddSingleton<ISubmissionRepository>(Repository);
                services.AddSingleton<ISubmissionTransfer>(Transfer);
            });
        }
    }

    private sealed class RecordingSubmissionTransfer : ISubmissionTransfer
    {
        public Exception? FailWith { get; set; }

        public List<string> TransferredIds { get; } = [];

        public Task TransferAsync(SubmissionDocument submission, CancellationToken cancellationToken = default)
        {
            if (FailWith is not null)
            {
                throw FailWith;
            }

            TransferredIds.Add(submission.HashId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySubmissionRepository : ISubmissionRepository
    {
        private readonly ConcurrentDictionary<string, byte[]> stored = new();

        public SubmissionDocument? DraftCreatedByAnotherRequest { get; set; }

        public int HashIdClashes { get; set; }

        public List<SubmissionDocument> Stored => All();

        public bool RejectNextReplace { get; set; }

        public Task<SubmissionDocument?> FindDraftAsync(string cph, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(All().FirstOrDefault(s => s.Cph == cph && s.Status == SubmissionStatus.Draft));
        }

        public Task<bool> TryInsertDraftAsync(SubmissionDocument submission, CancellationToken cancellationToken = default)
        {
            if (DraftCreatedByAnotherRequest is { } winner)
            {
                DraftCreatedByAnotherRequest = null;
                stored[winner.Id] = winner.ToBson();
                return Task.FromResult(false);
            }

            if (HashIdClashes > 0)
            {
                HashIdClashes--;
                return Task.FromResult(false);
            }

            stored[submission.Id] = submission.ToBson();
            return Task.FromResult(true);
        }

        public Task<SubmissionDocument?> GetByHashIdAsync(string hashId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(All().FirstOrDefault(s => s.HashId == hashId));
        }

        public Task<IReadOnlyCollection<SubmissionDocument>> ListAsync(string cph, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<SubmissionDocument> submissions = All()
                .Where(s => s.Cph == cph)
                .OrderByDescending(s => s.CreatedDate)
                .ToList();
            return Task.FromResult(submissions);
        }

        public Task<bool> TryReplaceDraftAsync(SubmissionDocument submission, int expectedVersion, CancellationToken cancellationToken = default)
        {
            if (RejectNextReplace)
            {
                RejectNextReplace = false;
                return Task.FromResult(false);
            }

            var current = BsonSerializer.Deserialize<SubmissionDocument>(stored[submission.Id]);
            if (current.Status != SubmissionStatus.Draft || current.Version != expectedVersion)
            {
                return Task.FromResult(false);
            }

            stored[submission.Id] = submission.ToBson();
            return Task.FromResult(true);
        }

        private List<SubmissionDocument> All() =>
            stored.Values.Select(bytes => BsonSerializer.Deserialize<SubmissionDocument>(bytes)).ToList();
    }
}
