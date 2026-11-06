// <copyright file="EartagModels.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

#pragma warning disable SA1402, SA1649 // The eartag contract is small and is easier to read in one place.

namespace Defra.Lis.Be4Fe.Models.Data;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// The result of <c>GET /data/eartags/unallocated</c>: tags issued to the keeper that are not yet on an animal.
/// </summary>
public sealed class UnallocatedEartagsResponse
{
    [Description("The CPH the eartags are for, as 12/345/6789.")]
    public required string Cph { get; init; }

    [Description("The eartags that can be given to a new animal.")]
    public required IReadOnlyCollection<UnallocatedEartag> Eartags { get; init; }
}

/// <summary>
/// One unallocated eartag. It is an object rather than a bare string so more detail can be added once the cattle API
/// endpoint is defined, without breaking callers.
/// </summary>
public sealed class UnallocatedEartag
{
    [Description("The eartag.")]
    public required string Eartag { get; init; }
}

/// <summary>
/// The result of <c>GET /data/eartags/dams</c> and <c>GET /data/eartags/sires</c>: the animals on a holding that can
/// be chosen as a parent.
/// </summary>
public sealed class ParentAnimalsResponse
{
    [Description("The CPH the animals are on, as 12/345/6789.")]
    public required string Cph { get; init; }

    [Description("The animals that can be chosen.")]
    public required IReadOnlyCollection<ParentAnimal> Animals { get; init; }
}

/// <summary>
/// An animal that can be chosen as a dam or sire.
/// </summary>
public sealed class ParentAnimal
{
    [Description("The animal's eartag.")]
    public required string Eartag { get; init; }

    [Description("The animal's breed name.")]
    public string? Breed { get; init; }

    [JsonPropertyName("breed-code")]
    [Description("The animal's breed code.")]
    public string? BreedCode { get; init; }

    [JsonPropertyName("date-of-birth")]
    [Description("The animal's date of birth.")]
    public DateOnly? DateOfBirth { get; init; }

    [Description("The animal's sex, as the cattle API reports it.")]
    public string? Sex { get; init; }
}
