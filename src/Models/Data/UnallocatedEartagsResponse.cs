// <copyright file="UnallocatedEartagsResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Models.Data;

using System.ComponentModel;

/// <summary>
/// The result of <c>GET /data/eartags/unallocated</c>: tags issued to the keeper that are not yet on an animal.
/// </summary>
public sealed class UnallocatedEartagsResponse
{
    [Description("The CPH the eartags are for, as 12/345/6789.")]
    public required string Cph { get; init; }

    [Description("The eartags that can be given to a new animal.")]
    public required IReadOnlyCollection<UnallocatedEarTag> Eartags { get; init; }
}
