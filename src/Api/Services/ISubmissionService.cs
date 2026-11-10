// <copyright file="ISubmissionService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Models.Submissions;

public interface ISubmissionService
{
    /// <summary>
    /// Returns the CPH's draft submission, creating one if it has none.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The draft's id and summary.</returns>
    Task<SubmissionSummaryResponse> StartAsync(string? cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one section of an animal's data, with empty data if nothing has been saved for it yet.
    /// </summary>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="animalId">The animal id.</param>
    /// <param name="section">The section name, in any case.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The animal id, section name and data.</returns>
    Task<AnimalSectionResponse> GetSectionAsync(string submissionId, string animalId, string section, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves one section of an animal's data, adding the animal to the draft if it is new.
    /// </summary>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="request">The animal id, section name and data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateSectionAsync(string submissionId, UpdateAnimalSectionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an animal, and all of its sections, from a draft.
    /// </summary>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="animalId">The animal id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAnimalAsync(string submissionId, string animalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers a draft to the cattle API and, if that succeeds, marks it as submitted.
    /// </summary>
    /// <param name="submissionId">The submission id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SubmitAsync(string submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the CPH's submissions, newest first.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The submissions.</returns>
    Task<SubmissionListResponse> ListAsync(string? cph, CancellationToken cancellationToken = default);
}
