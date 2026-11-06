// <copyright file="SubmissionService.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

public sealed partial class SubmissionService
{
    [LoggerMessage(LogLevel.Information, "Created a draft submission. SubmissionId: {SubmissionId}, Cph: {Cph}")]
    partial void LogCreatedDraftSubmission(string submissionId, string cph);

    [LoggerMessage(LogLevel.Information, "Deleted an animal from a draft submission, leaving {AnimalCount} animals. SubmissionId: {SubmissionId}")]
    partial void LogDeletedAnimal(string submissionId, int animalCount);

    [LoggerMessage(LogLevel.Information, "Submitted a submission with {AnimalCount} animals. SubmissionId: {SubmissionId}")]
    partial void LogSubmittedSubmission(string submissionId, int animalCount);

    [LoggerMessage(LogLevel.Warning, "The submission could not be transferred to the cattle API and stays a draft. SubmissionId: {SubmissionId}")]
    partial void LogSubmissionTransferFailed(string submissionId, Exception exception);
}
