// <copyright file="ModuleInfoResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Foundation.Models;

public sealed class ModuleInfoResponse
{
    public required string Service { get; init; }

    public required string Environment { get; init; }

    public required string Version { get; init; }

    public required DependencyConfiguration Dependencies { get; init; }
}
