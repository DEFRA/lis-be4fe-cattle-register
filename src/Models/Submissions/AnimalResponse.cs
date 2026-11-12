// <copyright file="AnimalResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using System.ComponentModel;
using System.Text.Json.Serialization;

public sealed class AnimalResponse
{
    [JsonPropertyName("animal-id")]
    [Description("The animal's ID.")]
    public required string AnimalId { get; init; }

    [Description("The animal's status")]
    public required ItemStatus Status { get; init; }

    [Description("The data items of the animal.")]
    public required DataItem[] Data { get; init; }
}
