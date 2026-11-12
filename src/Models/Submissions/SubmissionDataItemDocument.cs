// <copyright file="SubmissionDataItemDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

public sealed class SubmissionDataItemDocument
{
    public required string Key { get; init; }

    public required string? Value { get; init; }

    public required string Type { get; init; }
}
