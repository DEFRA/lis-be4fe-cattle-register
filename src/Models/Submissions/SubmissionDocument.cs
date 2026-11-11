// <copyright file="SubmissionDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using MongoDB.Bson.Serialization.Attributes;

public sealed class SubmissionDocument
{
    [BsonId]
    public required string Id { get; init; }

    public required string HashId { get; init; }

    public required string Cph { get; init; }

    public required DateTime CreatedDate { get; init; }

    public DateTime? SubmittedDate { get; set; }

    public required string Status { get; set; }

    public int Version { get; set; }

    public List<SubmissionAnimalDocument> Animals { get; set; } = [];
}
