// <copyright file="UnallocatedEarTag.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Data;

using System.ComponentModel;

/// <summary>
/// One unallocated eartag. It is an object rather than a bare string so more detail can be added once the cattle API
/// endpoint is defined, without breaking callers.
/// </summary>
public sealed class UnallocatedEarTag
{
    [Description("The eartag.")]
    public required string Eartag { get; init; }
}
