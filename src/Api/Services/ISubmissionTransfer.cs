// <copyright file="ISubmissionTransfer.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Api.Services;

using Defra.Livestock.Be4Fe.Models.Submissions;

/// <summary>
/// Transfers a completed submission to the cattle API.
/// </summary>
public interface ISubmissionTransfer
{
    Task TransferAsync(SubmissionDocument submission, CancellationToken cancellationToken = default);
}
