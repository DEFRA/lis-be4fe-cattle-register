// <copyright file="CattleSearchQuery.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Optional search filters for the cattle-on-holding lookup; each is passed through to the cattle API when supplied.
/// </summary>
public sealed record CattleSearchQuery(string? Eartag = null, string? Breed = null, string? Sex = null)
{
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Eartag) && string.IsNullOrWhiteSpace(Breed) && string.IsNullOrWhiteSpace(Sex);
}
