// <copyright file="SubmissionService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Api.Exceptions;
using Defra.Lis.Be4Fe.Api.Foundation.Submissions;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Submissions;

public sealed partial class SubmissionService(
    ISubmissionRepository repository,
    ISubmissionTransfer transfer,
    TimeProvider timeProvider,
    ILogger<SubmissionService> logger)
    : ISubmissionService
{
    private const int MaxCreateDraftAttempts = 3;

    public async Task<SubmissionResponse> StartAsync(string? cph, CancellationToken cancellationToken = default)
    {
        var normalisedCph = CphQueryParameter.Normalise(cph);

        var draft = await repository.FindDraftAsync(normalisedCph, cancellationToken);
        draft ??= await CreateDraftAsync(normalisedCph, cancellationToken);

        return draft.ToSubmissionResponse();
    }

    public async Task<AnimalResponse> GetAnimalAsync(string submissionId, string animalId, CancellationToken cancellationToken = default)
    {
        var submission = await GetRequiredAsync(submissionId, cancellationToken);
        var normalisedAnimalId = animalId.Trim();

        var stored = submission
            .Animals
            .First(animal => animal.AnimalId == normalisedAnimalId);

        return stored.ToAnimalResponse();
    }

    public async Task UpsertAnimalAsync(
        string submissionId,
        string animalId,
        AnimalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var submission = await GetRequiredDraftAsync(submissionId, cancellationToken);
        var normalisedAnimalId = animalId.Trim();

        submission.Animals.RemoveAll(animal => animal.AnimalId == normalisedAnimalId);
        submission.Animals.Add(request.ToSubmissionAnimalDocument(normalisedAnimalId));
        await SaveAsync(submission, cancellationToken);
    }

    public async Task DeleteAnimalAsync(string submissionId, string animalId, CancellationToken cancellationToken = default)
    {
        var submission = await GetRequiredDraftAsync(submissionId, cancellationToken);
        var normalisedAnimalId = animalId.Trim();

        if (submission.Animals.RemoveAll(animal => animal.AnimalId == normalisedAnimalId) == 0)
        {
            throw new NotFoundException($"Animal '{normalisedAnimalId}' was not found in submission '{submissionId}'.");
        }

        await SaveAsync(submission, cancellationToken);

        LogDeletedAnimal(submission.HashId, submission.Animals.Count);
    }

    public async Task SubmitAsync(string submissionId, CancellationToken cancellationToken = default)
    {
        var submission = await GetRequiredDraftAsync(submissionId, cancellationToken);

        if (submission.Animals.Count == 0)
        {
            throw new BusinessRuleException($"Submission '{submissionId}' has no animals to submit.");
        }

        try
        {
            await transfer.TransferAsync(submission, cancellationToken);
        }
        catch (ServiceUnavailableException ex)
        {
            // The framework leaves the retry loop to be decided. For now it stays a draft and the caller can try again.
            LogSubmissionTransferFailed(submissionId, ex);
            throw;
        }

        submission.Status = SubmissionStatus.Submitted;
        submission.SubmittedDate = timeProvider.GetUtcNow().UtcDateTime;
        await SaveAsync(submission, cancellationToken);

        LogSubmittedSubmission(submissionId, submission.Animals.Count);
    }

    public async Task<SubmissionListResponse> ListAsync(string? cph, CancellationToken cancellationToken = default)
    {
        var submissions = await repository
            .ListAsync(
                CphQueryParameter.Normalise(cph),
                cancellationToken);

        return new SubmissionListResponse
        {
            Submissions =
            [
                .. submissions
                    .Select(submission => submission.ToSubmissionResponse())
            ],
        };
    }

    private static string? ToStoredAnimalStatus(string? status)
    {
        if (status is null)
        {
            return null;
        }

        var normalised = status.Trim().ToUpperInvariant();

        return AnimalStatus.IsSupported(normalised)
            ? normalised
            : throw new ArgumentException($"The animal status must be {AnimalStatus.Draft} or {AnimalStatus.Complete}.", nameof(status));
    }

    private static List<SubmissionDataItemDocument> ToStoredData(DataItem[]? data)
    {
        if (data is null)
        {
            throw new ArgumentException("Data must be an array of key, value and type items.", nameof(data));
        }

        var stored = new List<SubmissionDataItemDocument>(data.Length);
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in data)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Key))
            {
                throw new ArgumentException("Every data item needs a key.", nameof(data));
            }

            var type = item.Type.Trim().ToUpperInvariant();
            if (!PropertyTypes.IsSupported(type))
            {
                throw new ArgumentException($"Data item '{item.Key}' has an unsupported type '{item.Type}'.", nameof(data));
            }

            if (!keys.Add(item.Key))
            {
                throw new ArgumentException($"Data item '{item.Key}' appears more than once.", nameof(data));
            }

            stored.Add(new SubmissionDataItemDocument { Key = item.Key, Value = item.Value, Type = type });
        }

        return stored;
    }

    private async Task<SubmissionDocument> CreateDraftAsync(string cph, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCreateDraftAttempts; attempt++)
        {
            var guid = Guid.NewGuid().ToString();
            var created = new SubmissionDocument
            {
                Id = guid,
                HashId = SubmissionHashId.Create(guid),
                Cph = cph,
                CreatedDate = timeProvider.GetUtcNow().UtcDateTime,
                Status = SubmissionStatus.Draft,
            };

            if (await repository.TryInsertDraftAsync(created, cancellationToken))
            {
                LogCreatedDraftSubmission(created.HashId, cph);
                return created;
            }

            // Another request created the draft between the look-up and the insert. If it did not, the hash id
            // clashed with another submission's, so try again with a new GUID.
            var existing = await repository.FindDraftAsync(cph, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        throw new ConflictException($"A draft submission for CPH '{cph}' could not be started. Try again.");
    }

    private async Task<SubmissionDocument> GetRequiredAsync(string submissionId, CancellationToken cancellationToken)
    {
        return await repository.GetByHashIdAsync(SubmissionHashId.Normalise(submissionId), cancellationToken)
            ?? throw new NotFoundException($"Submission '{submissionId}' was not found.");
    }

    private async Task<SubmissionDocument> GetRequiredDraftAsync(string submissionId, CancellationToken cancellationToken)
    {
        var submission = await GetRequiredAsync(submissionId, cancellationToken);

        return submission.Status == SubmissionStatus.Draft
            ? submission
            : throw new ConflictException($"Submission '{submissionId}' has already been submitted.");
    }

    private async Task SaveAsync(SubmissionDocument submission, CancellationToken cancellationToken)
    {
        var expectedVersion = submission.Version;
        submission.Version = expectedVersion + 1;

        if (!await repository.TryReplaceDraftAsync(submission, expectedVersion, cancellationToken))
        {
            throw new ConflictException($"Submission '{submission.HashId}' was changed by another request. Reload it and try again.");
        }
    }
}
