// <copyright file="DataService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Api.Exceptions;
using Defra.Lis.Be4Fe.Api.Foundation.Submissions;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Data;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <inheritdoc />
public sealed partial class DataService(
    IUnallocatedEarTagSource unallocatedEarTagSource,
    ICattleHoldingClient cattleHoldingClient,
    ISubmissionRepository submissionRepository,
    ILogger<DataService> logger)
    : IDataService
{
    // The values the cattle API reports for an animal's sex, and accepts as the sex filter.
    public const string Female = "Female";
    public const string Male = "Male";

    public async Task<UnallocatedEartagsResponse> GetUnallocatedEartagsAsync(string? cph, string? submissionId, CancellationToken cancellationToken = default)
    {
        var normalisedCph = CphQueryParameter.Normalise(cph);
        var usedInSubmission = await GetEartagsUsedInSubmissionAsync(normalisedCph, submissionId, cancellationToken);

        var eartags = (await unallocatedEarTagSource.GetUnallocatedEarTagAsync(normalisedCph, cancellationToken))
            .Where(tag => !usedInSubmission.Contains(CompareKey(tag.Eartag)))
            .ToList();

        LogRetrievedUnallocatedEartags(eartags.Count, usedInSubmission.Count, normalisedCph);

        return new UnallocatedEartagsResponse
        {
            Cph = normalisedCph,
            Eartags = eartags,
        };
    }

    public Task<ParentAnimalsResponse> GetDamsAsync(string? cph, CancellationToken cancellationToken = default) =>
        GetParentAnimalsAsync(cph, Female, cancellationToken);

    public Task<ParentAnimalsResponse> GetSiresAsync(string? cph, CancellationToken cancellationToken = default) =>
        GetParentAnimalsAsync(cph, Male, cancellationToken);

    // Eartags are written with and without spaces and in either case, so they are compared without either.
    private static string CompareKey(string eartag) =>
        string.Concat(eartag.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

    private static ParentAnimal ToParentAnimal(CattleSummary cattle) => new()
    {
        Eartag = cattle.Eartag,
        Breed = cattle.BreedName ?? (string.IsNullOrWhiteSpace(cattle.Breed) ? null : cattle.Breed),
        BreedCode = cattle.BreedCode,
        DateOfBirth = cattle.DateOfBirth,
        Sex = string.IsNullOrWhiteSpace(cattle.Sex) ? null : cattle.Sex,
    };

    private async Task<ParentAnimalsResponse> GetParentAnimalsAsync(string? cph, string sex, CancellationToken cancellationToken)
    {
        var normalisedCph = CphQueryParameter.Normalise(cph);

        var cattle = await cattleHoldingClient.SearchCattleAsync(normalisedCph, new CattleSearchQuery(Sex: sex), cancellationToken);

        // The filter is passed to the cattle API, but is applied here too so a looser match there cannot offer a bull
        // as a dam. Animals with no recorded sex are left out for the same reason.
        var animals = cattle
            .Where(animal => string.Equals(animal.Sex, sex, StringComparison.OrdinalIgnoreCase))
            .Select(ToParentAnimal)
            .ToList();

        LogRetrievedParentAnimals(animals.Count, sex, normalisedCph);

        return new ParentAnimalsResponse
        {
            Cph = normalisedCph,
            Animals = animals,
        };
    }

    private async Task<HashSet<string>> GetEartagsUsedInSubmissionAsync(string cph, string? submissionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
        {
            return [];
        }

        var submission = await submissionRepository.GetByHashIdAsync(SubmissionHashId.Normalise(submissionId), cancellationToken)
            ?? throw new NotFoundException($"Submission '{submissionId}' was not found.");

        if (submission.Cph != cph)
        {
            throw new ArgumentException($"Submission '{submissionId}' is not for CPH '{cph}'.", nameof(submissionId));
        }

        return submission.Animals.Select(animal => CompareKey(animal.AnimalId)).ToHashSet();
    }
}
