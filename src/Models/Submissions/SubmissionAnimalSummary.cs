// <copyright file="SubmissionAnimalSummary.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using System.ComponentModel;
using System.Text.Json.Serialization;

public sealed class SubmissionAnimalSummary
{
    [JsonPropertyName("animal-id")]
    [Description("The animal's ID")]
    public required string AnimalId { get; init; }

    [Description("The animal's status")]
    public required ItemStatus Status { get; init; }
}
