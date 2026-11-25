// <copyright file="DataService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Api.Services;

using Defra.Livestock.Be4Fe.CattleApi;
using Defra.Livestock.Be4Fe.Models.Data;
using Defra.Livestock.Be4Fe.Models.Lookups.Models;
using Defra.Livestock.Sdk.Core.Exceptions;
using Defra.Livestock.Sdk.Submissions.Services;

/// <inheritdoc />
public sealed partial class DataService(
    IUnallocatedEarTagSource unallocatedEarTagSource,
    ICattleHoldingClient cattleHoldingClient,
    ISubmissionService submissionService,
    ILogger<DataService> logger)
    : IDataService
{
    // The values the cattle API reports for an animal's sex, and accepts as the sex filter.
    private const string Female = "Female";
    private const string Male = "Male";

    public async Task<UnallocatedEartagsResponse> GetUnallocatedEarTagsAsync(
        string cph,
        string? submissionId,
        CancellationToken cancellationToken = default)
    {
        var normalisedCph = CphQueryParameter.Normalise(cph);
        var usedInSubmission = await GetEarTagsUsedInSubmissionAsync(
            normalisedCph,
            submissionId,
            cancellationToken);

        var earTags = (await unallocatedEarTagSource.GetUnallocatedEarTagAsync(normalisedCph, cancellationToken))
            .Where(tag => !usedInSubmission.Contains(CompareKey(tag.Eartag)))
            .ToList();

        LogRetrievedUnallocatedEartags(earTags.Count, usedInSubmission.Count, normalisedCph);

        return new UnallocatedEartagsResponse
        {
            Cph = normalisedCph,
            Eartags = earTags,
        };
    }

    public async Task<Animal> GetAnimalAsync(
        string cph,
        string animalId,
        CancellationToken cancellationToken = default)
    {
        var holdingAnimals = await GetAnimalsAsync(cph, new CattleSearchQuery(Eartag: animalId), cancellationToken);
        var key = CompareKey(animalId);

        return holdingAnimals.Animals.FirstOrDefault(animal => CompareKey(animal.EarTag) == key)
            ?? throw new NotFoundException($"Animal '{animalId}' was not found on holding '{holdingAnimals.Cph}'.");
    }

    public Task<HoldingAnimalsResponse> GetDamsAsync(string cph, CancellationToken cancellationToken = default) =>
        GetAnimalsAsync(cph, new CattleSearchQuery(Sex: Female), cancellationToken);

    public Task<HoldingAnimalsResponse> GetSiresAsync(string cph, CancellationToken cancellationToken = default) =>
        GetAnimalsAsync(cph, new CattleSearchQuery(Sex: Male), cancellationToken);

    private static string CompareKey(string eartag) =>
        string.Concat(eartag.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

    private static Animal ToParentAnimal(CattleSummary cattle) => new()
    {
        EarTag = cattle.Eartag,
        Breed = cattle.BreedName ?? (string.IsNullOrWhiteSpace(cattle.Breed) ? null : cattle.Breed),
        BreedCode = cattle.BreedCode,
        DateOfBirth = cattle.DateOfBirth,
        Sex = string.IsNullOrWhiteSpace(cattle.Sex) ? null : cattle.Sex,
    };

    private async Task<HoldingAnimalsResponse> GetAnimalsAsync(
        string cph,
        CattleSearchQuery query,
        CancellationToken cancellationToken)
    {
        var normalisedCph = CphQueryParameter.Normalise(cph);
        var cattle = await cattleHoldingClient.SearchCattleAsync(
            normalisedCph,
            query,
            cancellationToken);

        var animals = cattle
            .Select(ToParentAnimal)
            .ToList();

        LogRetrievedParentAnimals(animals.Count, query.ToString(), normalisedCph);

        return new HoldingAnimalsResponse
        {
            Cph = normalisedCph,
            Animals = animals,
        };
    }

    private async Task<HashSet<string>> GetEarTagsUsedInSubmissionAsync(string cph, string? submissionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
        {
            return [];
        }

        // The submissions SDK normalises the id and only finds the submission within the given CPH.
        var submission = await submissionService.GetSubmissionAsync(cph, submissionId, cancellationToken);

        return submission.Animals.Select(animal => CompareKey(animal.AnimalId)).ToHashSet();
    }
}
