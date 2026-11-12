// <copyright file="ParentAnimal.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Data;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// An animal that can be chosen as a dam or sire.
/// </summary>
public sealed class ParentAnimal
{
    [Description("The animal's ear tag.")]
    [JsonPropertyName("eartag")]
    public required string EarTag { get; init; }

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
