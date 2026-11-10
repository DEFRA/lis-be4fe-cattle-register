// <copyright file="SubmissionDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

#pragma warning disable SA1402 // The nested parts of the stored document are easier to read together.

namespace Defra.Lis.Be4Fe.Api.Foundation.Submissions;

using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// A submission as stored in MongoDB, following the sample document in the BFF framework.
/// </summary>
public sealed class SubmissionDocument
{
    /// <summary>
    /// Gets the GUID that identifies the document in MongoDB. It is never exposed by the API.
    /// </summary>
    [BsonId]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the id the API uses for this submission, in the format <c>XXX-XXX-XXX</c>.
    /// </summary>
    public required string HashId { get; init; }

    public required string Cph { get; init; }

    public required DateTime CreatedDate { get; init; }

    public DateTime? SubmittedDate { get; set; }

    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the version checked on every save, so two saves at once cannot silently overwrite each other.
    /// </summary>
    public int Version { get; set; }

    public List<SubmissionAnimalDocument> Animals { get; set; } = [];
}

public sealed class SubmissionAnimalDocument
{
    public required string AnimalId { get; init; }

    public required string Status { get; set; }

    public List<SubmissionSectionDocument> Sections { get; set; } = [];
}

public sealed class SubmissionSectionDocument
{
    public required string Section { get; init; }

    public List<SubmissionDataItemDocument> Data { get; set; } = [];
}

public sealed class SubmissionDataItemDocument
{
    public required string Key { get; init; }

    public required string Value { get; init; }

    public required string Type { get; init; }
}
