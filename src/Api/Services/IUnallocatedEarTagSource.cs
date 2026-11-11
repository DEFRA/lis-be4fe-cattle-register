// <copyright file="IUnallocatedEarTagSource.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Models.Data;

/// <summary>
/// Reads the eartags issued to a holding that are not yet on an animal, from the cattle API.
/// </summary>
public interface IUnallocatedEarTagSource
{
    /// <param name="cph">The CPH, already normalised to 12/345/6789.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <exception cref="Defra.Lis.Be4Fe.CattleApi.HoldingNotFoundException">The cattle API does not know the holding.</exception>
    /// <exception cref="Defra.Lis.Be4Fe.CattleApi.ServiceUnavailableException">The cattle API cannot be reached.</exception>
    /// <returns>The unallocated eartags on the holding.</returns>
    Task<IReadOnlyCollection<UnallocatedEarTag>> GetUnallocatedEarTagAsync(string cph, CancellationToken cancellationToken = default);
}
