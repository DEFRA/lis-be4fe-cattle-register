// <copyright file="ISubmissionRepository.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Api.Foundation.Submissions;

using Defra.Livestock.Be4Fe.Models.Submissions;

public interface ISubmissionRepository
{
    /// <summary>
    /// Finds the draft for a CPH.
    /// </summary>
    /// <param name="cph">The county parish holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The draft, or null when the CPH has no draft.</returns>
    Task<SubmissionDocument?> FindDraftAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new draft.
    /// </summary>
    /// <param name="submission">The draft.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>False when the CPH already has a draft, or another submission already has the hash id.</returns>
    Task<bool> TryInsertDraftAsync(SubmissionDocument submission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a submission by its hash id.
    /// </summary>
    /// <param name="hashId">The submission hash id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The submission, or null when no matching submission exists.</returns>
    Task<SubmissionDocument?> GetByHashIdAsync(string hashId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists submissions for a CPH, ordered by creation date from newest to oldest.
    /// </summary>
    /// <param name="cph">The county parish holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The submissions for the CPH, or an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<SubmissionDocument>> ListAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a draft, as long as it is still a draft at <paramref name="expectedVersion"/>.
    /// </summary>
    /// <param name="submission">The changed draft.</param>
    /// <param name="expectedVersion">The version that was read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>False when the stored submission has changed since it was read.</returns>
    Task<bool> TryReplaceDraftAsync(SubmissionDocument submission, int expectedVersion, CancellationToken cancellationToken = default);
}
