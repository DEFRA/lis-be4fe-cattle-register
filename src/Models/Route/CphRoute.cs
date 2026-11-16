// <copyright file="CphRoute.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Models.Route;

using Microsoft.AspNetCore.Mvc;

public sealed class CphRoute
{
    [FromRoute(Name = "county")]
    public required string County { get; init; }

    [FromRoute(Name = "parish")]
    public required string Parish { get; init; }

    [FromRoute(Name = "holding")]
    public required string Holding { get; init; }

    public override string ToString() => $"{County}/{Parish}/{Holding}";
}
