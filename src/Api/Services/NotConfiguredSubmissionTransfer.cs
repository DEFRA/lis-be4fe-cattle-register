// <copyright file="NotConfiguredSubmissionTransfer.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Api.Foundation.Submissions;
using Defra.Lis.Be4Fe.CattleApi;

/// <summary>
/// Stands in until the transfer to the cattle API is built. It always fails, so a submission stays a draft rather than
/// being marked as submitted when nothing was sent.
/// </summary>
public sealed class NotConfiguredSubmissionTransfer : ISubmissionTransfer
{
    public Task TransferAsync(SubmissionDocument submission, CancellationToken cancellationToken = default)
    {
        throw new ServiceUnavailableException(
            "Submitting to the cattle API is not available yet.",
            new NotSupportedException("The submission transfer to the cattle API has not been implemented."));
    }
}
