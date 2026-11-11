// <copyright file="SubmissionResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using System.ComponentModel;
using System.Text.Json.Serialization;

public sealed class SubmissionResponse
{
    [JsonPropertyName("submission-id")]
    [Description("The submission's ID.")]
    public required string SubmissionId { get; init; }

    [JsonPropertyName("animal-count")]
    [Description("The number of animals in the submission.")]
    public required int AnimalCount { get; init; }

    [Description("The submission's animals.")]
    public IReadOnlyCollection<SubmissionAnimalSummary> Animals { get; init; } = [];

    [Description("The submission's status.")]
    public required string Status { get; init; }

    [JsonPropertyName("created-date")]
    [Description("The date and time the submission was created.")]
    public required DateTimeOffset CreatedDate { get; init; }

    [JsonPropertyName("submitted-date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("The date and time the submission was submitted.")]
    public DateTimeOffset? SubmittedDate { get; init; }
}
