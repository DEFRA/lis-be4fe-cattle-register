// <copyright file="SubmissionModels.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

#pragma warning disable SA1402, SA1649 // The BFF contract is small and is easier to read in one place.

namespace Defra.Lis.Be4Fe.Models.Submissions;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// Statuses of a submission.
/// </summary>
public static class SubmissionStatus
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
}

/// <summary>
/// Statuses of an animal in a submission.
/// </summary>
public static class AnimalStatus
{
    public const string Draft = "DRAFT";
    public const string Complete = "COMPLETE";

    public static bool IsSupported(string status) => status is Draft or Complete;
}

/// <summary>
/// The supported types of a property.
/// </summary>
public static class PropertyTypes
{
    public const string String = "STRING";
    public const string Date = "DATE";
    public const string Int = "INT";
    public const string Number = "NUMBER";
    public const string Bool = "BOOL";

    public static bool IsSupported(string type) => type is String or Date or Int or Number or Bool;
}

/// <summary>
/// The result of <c>GET /{sub-taxonomy}/start</c>: the open draft for the CPH, or a new one with no animals.
/// </summary>
public sealed class SubmissionSummaryResponse
{
    [JsonPropertyName("submission-id")]
    [Description("The submission's ID")]
    public required string SubmissionId { get; init; }

    [JsonPropertyName("created-date")]
    [Description("The submission's creation date")]
    public required DateTimeOffset CreatedDate { get; init; }

    [Description("The submission's animals")]
    public IReadOnlyCollection<SubmissionAnimalSummary> Animals { get; init; } = [];
}

public sealed class SubmissionAnimalSummary
{
    [JsonPropertyName("animal-id")]
    [Description("The animal's ID")]
    public required string AnimalId { get; init; }

    [Description("The animal's status")]
    public required string Status { get; init; }
}

/// <summary>
/// One section of an animal's data. It is both the result of <c>GET /{sub-taxonomy}/{submission-id}/{animal-id}/{section}</c>
/// and the shape of the payload of <c>POST /{sub-taxonomy}/{submission-id}:update</c>.
/// </summary>
public sealed class AnimalSectionResponse
{
    [JsonPropertyName("animal-id")]
    [Description("The animal's ID.")]
    public required string AnimalId { get; init; }

    [Description("The section of the animal's data.")]
    public required string Section { get; init; }

    [Description("The data items of the section.")]
    public required DataItem[] Data { get; init; }
}

/// <summary>
/// One property of a section. The value is always a string; <see cref="Type"/> says how to read it.
/// </summary>
public sealed class DataItem
{
    [Description("The key of the data item.")]
    public required string Key { get; init; }

    [Description("The value of the data item.")]
    public required string Value { get; init; }

    [Description("The type of the data item.")]
    public required string Type { get; init; }
}

/// <summary>
/// The payload of <c>POST /{sub-taxonomy}/{submission-id}:update</c>.
/// </summary>
public sealed class UpdateAnimalSectionRequest
{
    [JsonPropertyName("animal-id")]
    [Description("The animal's ID.")]
    public string? AnimalId { get; init; }

    [Description("The section of the animal's data.")]
    public string? Section { get; init; }

    [Description("The animal's status, DRAFT or COMPLETE. If omitted, an existing animal keeps its status and a new animal is DRAFT.")]
    public string? Status { get; init; }

    [Description("The data items of the section that will replace any existing data.")]
    public required DataItem[] Data { get; init; }
}

/// <summary>
/// The result of <c>GET /{sub-taxonomy}</c>.
/// </summary>
public sealed class SubmissionListResponse
{
    [Description("The list of submissions for the CPH.")]
    public IReadOnlyCollection<SubmissionListItem> Submissions { get; init; } = [];
}

public sealed class SubmissionListItem
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
