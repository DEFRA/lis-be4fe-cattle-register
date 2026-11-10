// <copyright file="UnallocatedEarTagOptions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Config;

/// <summary>
/// Options for sourcing unallocated ear tags from the environment variable.
/// </summary>
public sealed class UnallocatedEarTagOptions
{
    /// <summary>Gets the configuration section the options are bound from.</summary>
    public const string SectionName = "UnallocatedEarTag";

    public string? Data { get; set; }
}
