// <copyright file="ICattleHoldingClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Cattle-on-holding lookups passed through to the cattle API. Every member throws
/// <see cref="ServiceUnavailableException"/> when the cattle API cannot be reached (connection refused, DNS failure or
/// timeout), so callers can retry.
/// </summary>
public interface ICattleHoldingClient
{
    /// <exception cref="ArgumentException">The CPH is not three segments.</exception>
    /// <exception cref="HoldingNotFoundException">The cattle API does not know the holding.</exception>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<IReadOnlyCollection<CattleSummary>> SearchCattleAsync(string cph, CattleSearchQuery query, CancellationToken cancellationToken = default);
}
