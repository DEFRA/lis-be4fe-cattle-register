// <copyright file="ISubmissionTransfer.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Api.Foundation.Submissions;

/// <summary>
/// Transfers a completed submission to the cattle API.
/// </summary>
public interface ISubmissionTransfer
{
    Task TransferAsync(SubmissionDocument submission, CancellationToken cancellationToken = default);
}
