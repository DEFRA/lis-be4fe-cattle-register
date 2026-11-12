// <copyright file="ISubmissionRepository.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Foundation.Submissions;

using Defra.Lis.Be4Fe.Models.Submissions;

public interface ISubmissionRepository
{
    Task<SubmissionDocument?> FindDraftAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new draft.
    /// </summary>
    /// <param name="submission">The draft.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>False when the CPH already has a draft, or another submission already has the hash id.</returns>
    Task<bool> TryInsertDraftAsync(SubmissionDocument submission, CancellationToken cancellationToken = default);

    Task<SubmissionDocument?> GetByHashIdAsync(string hashId, CancellationToken cancellationToken = default);

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
