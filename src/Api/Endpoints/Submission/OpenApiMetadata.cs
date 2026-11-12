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
        public const string Description = "Returns the submission id and summary of the CPH's draft submission, creating a new draft if it has none. The CPH is supplied in the route.";
    }

    public static class GetSubmission
    {
        public const string Name = "GetSubmission";
        public const string Summary = "Gets a submission.";
        public const string Description = "Returns the submission and animal summaries. 404 when the submission is unknown.";
    }

    public static class GetAnimal
    {
        public const string Name = "GetSubmissionAnimal";
        public const string Summary = "Gets an animal's data.";
        public const string Description = "Returns the animal's status and data. 404 when the submission or animal is unknown.";
    }

    public static class UpsertAnimal
    {
        public const string Name = "UpsertSubmissionAnimal";
        public const string Summary = "Saves an animal's data.";
        public const string Description = "Replaces the animal's status and data, adding it if new. 400 for invalid data, 404 for an unknown submission, 409 for a submitted or concurrently changed submission.";
    }

    public static class PatchAnimal
    {
        public const string Name = "PatchSubmissionAnimal";
        public const string Summary = UpsertAnimal.Summary;
        public const string Description = UpsertAnimal.Description;
    }

    public static class GetAnimalSection
    {
        public const string Name = "GetAnimalSection";
        public const string Summary = "Gets one section of an animal's data.";
        public const string Description = "Returns the animal id, section name and stored data, with an empty data array if nothing has been saved for that section. 404 when the submission is unknown.";
    }

    public static class UpdateAnimalSection
    {
        public const string Name = "UpdateAnimalSection";
        public const string Summary = "Saves one section of an animal's data.";
        public const string Description = "Saves the data against the animal id and section, adding the animal to the draft if it is new, and sets the animal's status if one is given. 400 when the status is not DRAFT or COMPLETE, 404 when the submission is unknown, 409 when it has already been submitted or was changed by another request.";
    }

    public static class DeleteAnimal
    {
        public const string Name = "DeleteAnimal";
        public const string Summary = "Removes an animal from a draft.";
        public const string Description = "Removes the animal and all of its data from the draft. 404 when the submission or animal is unknown, 409 when the submission has already been submitted or was changed by another request.";
    }

    public static class SubmitSubmission
    {
        public const string Name = "SubmitSubmission";
        public const string Summary = "Submits a draft to the cattle API.";
        public const string Description = "Transfers the draft to the cattle API and marks it as submitted. 400 when it has no animals, 404 when it is unknown, 409 when it has already been submitted, 503 when the transfer fails, in which case it stays a draft.";
    }

    public static class ListSubmissions
    {
        public const string Name = "ListSubmissions";
        public const string Summary = "Lists the submissions for a CPH.";
        public const string Description = "Returns the CPH's submissions, newest first, each with the id and status of its animals. The CPH is supplied in the route.";
    }
}
