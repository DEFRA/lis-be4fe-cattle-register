// <copyright file="HoldingAnimalsResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Models.Data;

using System.ComponentModel;

/// <summary>
/// The result of <c>GET /data/eartags/dams</c> and <c>GET /data/eartags/sires</c>: the animals on a holding that can
/// be chosen as a parent.
/// </summary>
public sealed class HoldingAnimalsResponse
{
    [Description("The CPH the animals are on, as 12/345/6789.")]
    public required string Cph { get; init; }

    [Description("The animals that can be chosen.")]
    public required IReadOnlyCollection<Animal> Animals { get; init; }
}
