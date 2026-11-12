// <copyright file="SubmissionAnimalDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public sealed class SubmissionAnimalDocument
{
    public required string AnimalId { get; init; }

    [BsonRepresentation(BsonType.String)]
    public required ItemStatus Status { get; set; }

    public List<SubmissionDataItemDocument> Data { get; set; } = [];
}
