// <copyright file="AnimalRequest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using System.ComponentModel;

public sealed class AnimalRequest
{
    [Description("The animal's status")]
    public required string Status { get; init; }

    [Description("The data items of the animal.")]
    public required DataItem[] Data { get; init; }
}
