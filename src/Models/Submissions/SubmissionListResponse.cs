// <copyright file="SubmissionListResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using System.ComponentModel;

public sealed class SubmissionListResponse
{
    [Description("The list of submissions for the CPH.")]
    public IReadOnlyCollection<SubmissionResponse> Submissions { get; init; } = [];
}
