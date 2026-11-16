// <copyright file="IDataService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Api.Services;

using Defra.Livestock.Be4Fe.Models.Data;

/// <summary>
/// Reference data the cattle-register UI reads to populate its forms.
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Lists the eartags issued to the holding that are not yet on an animal. When a submission is given, tags its
    /// animals already use are left out, so the same tag is not offered for two calves in one draft.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="submissionId">The optional submission id; when null or blank, no tags are left out.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentException">The CPH is missing or invalid, or the submission is for another CPH.</exception>
    /// <exception cref="Exceptions.NotFoundException">The submission is unknown.</exception>
    /// <returns>The unallocated eartags.</returns>
    Task<UnallocatedEartagsResponse> GetUnallocatedEarTagsAsync(string cph, string? submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the female animals on the holding. Whether a dam is eligible for a given birth is checked when the
    /// submission is validated, not here.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The animals that can be chosen as the dam.</returns>
    Task<HoldingAnimalsResponse> GetDamsAsync(string cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the animal on the holding whose eartag matches the animal id.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="animalId">The animal's eartag.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentException">The CPH is missing or invalid.</exception>
    /// <exception cref="Exceptions.NotFoundException">No animal on the holding has that eartag.</exception>
    /// <returns>The animal.</returns>
    Task<Animal> GetAnimalAsync(string cph, string animalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the male animals on the holding. Sires from off the holding (AI, a borrowed bull) are not covered yet.
    /// </summary>
    /// <param name="cph">The county/parish/holding number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The animals that can be chosen as the sire.</returns>
    Task<HoldingAnimalsResponse> GetSiresAsync(string cph, CancellationToken cancellationToken = default);
}
