// <copyright file="MongoSubmissionRepository.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Foundation.Submissions;

using Defra.Lis.Be4Fe.Api.Utils.Mongo;
using Defra.Lis.Be4Fe.Models.Submissions;
using MongoDB.Driver;

public sealed class MongoSubmissionRepository : ISubmissionRepository
{
    private const int IndexesNotCreated = 0;
    private const int IndexesBeingCreated = 1;
    private const int IndexesCreated = 2;
    private readonly IMongoCollection<SubmissionDocument> collection;
    private readonly ILogger<MongoSubmissionRepository> logger;
    private int indexState = IndexesNotCreated;

    public MongoSubmissionRepository(
        IMongoDbClientFactory mongoDbClientFactory,
        ILogger<MongoSubmissionRepository> logger)
    {
        collection = mongoDbClientFactory.GetCollection<SubmissionDocument>("submissions");
        this.logger = logger;
    }

    public async Task<SubmissionDocument?> FindDraftAsync(string cph, CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        return await collection
            .Find(submission => submission.Cph == cph && submission.Status == SubmissionStatus.Draft)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryInsertDraftAsync(SubmissionDocument submission, CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        try
        {
            await collection.InsertOneAsync(submission, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task<SubmissionDocument?> GetByHashIdAsync(string hashId, CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        return await collection
            .Find(submission => submission.HashId == hashId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SubmissionDocument>> ListAsync(string cph, CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        return await collection
            .Find(submission => submission.Cph == cph)
            .SortByDescending(submission => submission.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryReplaceDraftAsync(SubmissionDocument submission, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var result = await collection.ReplaceOneAsync(
            stored => stored.Id == submission.Id
                && stored.Status == SubmissionStatus.Draft
                && stored.Version == expectedVersion,
            submission,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        // Same approach as the cache repository: one caller creates the indexes, and a failure is retried later.
        if (Interlocked.CompareExchange(ref indexState, IndexesBeingCreated, IndexesNotCreated) != IndexesNotCreated)
        {
            return;
        }

        var keys = Builders<SubmissionDocument>.IndexKeys;

        // A CPH has at most one draft, so two "start" calls at once cannot each create one.
        var oneDraftPerCph = new CreateIndexModel<SubmissionDocument>(
            keys.Ascending(submission => submission.Cph),
            new CreateIndexOptions<SubmissionDocument>
            {
                Name = "unique_draft_per_cph",
                Unique = true,
                PartialFilterExpression = Builders<SubmissionDocument>.Filter.Eq(submission => submission.Status, SubmissionStatus.Draft),
            });

        var uniqueHashId = new CreateIndexModel<SubmissionDocument>(
            keys.Ascending(submission => submission.HashId),
            new CreateIndexOptions { Name = "unique_hashId", Unique = true });

        var listByCph = new CreateIndexModel<SubmissionDocument>(
            keys.Ascending(submission => submission.Cph).Descending(submission => submission.CreatedDate),
            new CreateIndexOptions { Name = "cph_createdDate" });

        try
        {
            await collection.Indexes.CreateManyAsync([oneDraftPerCph, uniqueHashId, listByCph], cancellationToken);
            Volatile.Write(ref indexState, IndexesCreated);
        }
        catch (Exception ex)
        {
            Volatile.Write(ref indexState, IndexesNotCreated);
            logger.LogWarning(ex, "Unable to create Mongo submission indexes. Continuing without them; the next call retries.");
        }
    }
}
