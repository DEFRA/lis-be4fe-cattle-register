// <copyright file="SubmissionAnimalDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

public sealed class SubmissionAnimalDocument
{
    public required string AnimalId { get; init; }

    public required string Status { get; set; }

    public List<SubmissionDataItemDocument> Data { get; set; } = [];
}
