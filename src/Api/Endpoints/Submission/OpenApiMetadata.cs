// <copyright file="OpenApiMetadata.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Submission;

using Defra.Lis.Be4Fe.Api;

public static class OpenApiMetadata
{
    public const string Tag = nameof(RouteNames.Submission);

    public static class StartSubmission
    {
        public const string Name = "StartSubmission";
        public const string Summary = "Starts or continues a submission for a CPH.";
        public const string Description = "Returns the submission id and summary of the CPH's draft submission, creating a new draft if it has none. The CPH is supplied as county, parish and holding route segments. 400 when the CPH is invalid, 409 when a draft cannot be created after retrying.";
    }

    public static class GetSubmission
    {
        public const string Name = "GetSubmission";
        public const string Summary = "Gets a submission.";
        public const string Description = "Returns the submission and animal summaries for the CPH supplied as county, parish and holding route segments. 404 when the submission is unknown or belongs to another CPH.";
    }

    public static class GetAnimal
    {
        public const string Name = "GetSubmissionAnimal";
        public const string Summary = "Gets an animal's data.";
        public const string Description = "Returns the animal's status and data from the submission for the CPH supplied as county, parish and holding route segments. 404 when the submission or animal is unknown or the submission belongs to another CPH.";
    }

    public static class UpsertAnimal
    {
        public const string Name = "UpsertSubmissionAnimal";
        public const string Summary = "Saves an animal's data.";
        public const string Description = "Replaces the animal's complete status and data, adding it if new. Both POST and PATCH replace the full animal data; omitted data items are removed. The CPH is supplied as county, parish and holding route segments. 400 for a missing or invalid body or a blank animal id, 404 when the submission is unknown or belongs to another CPH, 409 for a submitted or concurrently changed submission.";
    }

    public static class PatchAnimal
    {
        public const string Name = "PatchSubmissionAnimal";
        public const string Summary = UpsertAnimal.Summary;
        public const string Description = UpsertAnimal.Description;
    }

    public static class DeleteAnimal
    {
        public const string Name = "DeleteAnimal";
        public const string Summary = "Removes an animal from a draft.";
        public const string Description = "Removes the animal and all of its data from the draft for the CPH supplied as county, parish and holding route segments. 404 when the submission or animal is unknown or the submission belongs to another CPH, 409 when the submission has already been submitted or was changed by another request.";
    }

    public static class SubmitSubmission
    {
        public const string Name = "SubmitSubmission";
        public const string Summary = "Submits a draft to the cattle API.";
        public const string Description = "Transfers the draft for the CPH supplied as county, parish and holding route segments to the cattle API and marks it as submitted. 400 when it has no animals, 404 when it is unknown or belongs to another CPH, 409 when it has already been submitted or was changed by another request. The transfer is not yet configured, so it currently returns 503 and the submission stays a draft.";
    }

    public static class ListSubmissions
    {
        public const string Name = "ListSubmissions";
        public const string Summary = "Lists the submissions for a CPH.";
        public const string Description = "Returns the CPH's submissions, newest first, each with the id and status of its animals. The CPH is supplied as county, parish and holding route segments. 400 when the CPH is invalid.";
    }
}
