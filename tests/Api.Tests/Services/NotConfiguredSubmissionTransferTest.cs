// <copyright file="NotConfiguredSubmissionTransferTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Services;

using Defra.Lis.Be4Fe.Api.Foundation.Submissions;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Submissions;

public sealed class NotConfiguredSubmissionTransferTest
{
    [Fact]
    public async Task ShouldAlwaysFailAsUnavailableSoTheSubmissionStaysADraft()
    {
        var submission = new SubmissionDocument
        {
            Id = Guid.NewGuid().ToString(),
            HashId = "ABC-DEF-GHJ",
            Cph = "12/345/6789",
            CreatedDate = DateTime.UtcNow,
            Status = SubmissionStatus.Draft,
        };

        var exception = await Should.ThrowAsync<ServiceUnavailableException>(
            () => new NotConfiguredSubmissionTransfer().TransferAsync(submission, TestContext.Current.CancellationToken));

        exception.InnerException.ShouldBeOfType<NotSupportedException>();
    }
}
