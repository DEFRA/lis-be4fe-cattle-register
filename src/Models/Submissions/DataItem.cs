// <copyright file="DataItem.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using System.ComponentModel;

public sealed class DataItem
{
    [Description("The key of the data item.")]
    public required string Key { get; init; }

    [Description("The value of the data item.")]
    public required string? Value { get; init; }

    [Description("The type of the data item.")]
    public required string Type { get; init; }
}
