// <copyright file="IDataService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Models.Data;

/// <summary>
/// Reference data the cattle-register UI reads to populate its forms.
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Lists the eartags issued to the holding that are not yet on an animal. When a submission is given, tags its
    /// animals already use are left out, so the same tag is not offered for two calves in one draft.
    /// </summary>
    /// <exception cref="ArgumentException">The CPH is missing or invalid, or the submission is for another CPH.</exception>
    /// <exception cref="Exceptions.NotFoundException">The submission is unknown.</exception>
    /// <returns>The unallocated eartags.</returns>
    Task<UnallocatedEartagsResponse> GetUnallocatedEartagsAsync(string? cph, string? submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the female animals on the holding. Whether a dam is eligible for a given birth is checked when the
    /// submission is validated, not here.
    /// </summary>
    /// <exception cref="ArgumentException">The CPH is missing or invalid.</exception>
    /// <returns>The animals that can be chosen as the dam.</returns>
    Task<ParentAnimalsResponse> GetDamsAsync(string? cph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the male animals on the holding. Sires from off the holding (AI, a borrowed bull) are not covered yet.
    /// </summary>
    /// <exception cref="ArgumentException">The CPH is missing or invalid.</exception>
    /// <returns>The animals that can be chosen as the sire.</returns>
    Task<ParentAnimalsResponse> GetSiresAsync(string? cph, CancellationToken cancellationToken = default);
}
