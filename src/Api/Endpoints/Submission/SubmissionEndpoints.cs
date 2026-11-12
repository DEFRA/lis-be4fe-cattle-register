// <copyright file="SubmissionEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Submission;

using System.Diagnostics.CodeAnalysis;
using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Models.Submissions;

/// <summary>
/// The BFF framework endpoints for submission. They sit at the <c>/submission</c> root the framework sets out,
/// rather than under <c>/api</c>.
/// </summary>
public static class SubmissionEndpoints
{
    [SuppressMessage(
        "S1192",
        "S1192:String literals should not be duplicated",
        Justification = "I want the routes to be fully written.")]
    public static IEndpointRouteBuilder UseSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/" + RouteNames.Submission)
            .RequireServiceToServiceAuthorization()
            .WithTags(OpenApiMetadata.Tag);

        group.MapGet("/{county}/{parish}/{holding}", ListSubmissions)
            .WithName(OpenApiMetadata.ListSubmissions.Name)
            .WithSummary(OpenApiMetadata.ListSubmissions.Summary)
            .WithDescription(OpenApiMetadata.ListSubmissions.Description)
            .Produces<SubmissionListResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{county}/{parish}/{holding}/start", StartSubmission)
            .WithName(OpenApiMetadata.StartSubmission.Name)
            .WithSummary(OpenApiMetadata.StartSubmission.Summary)
            .WithDescription(OpenApiMetadata.StartSubmission.Description)
            .Produces<SubmissionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{submissionId}", GetSubmission)
            .WithName(OpenApiMetadata.GetSubmission.Name)
            .WithSummary(OpenApiMetadata.GetSubmission.Summary)
            .WithDescription(OpenApiMetadata.GetSubmission.Description)
            .Produces<SubmissionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{submissionId}/{animalId}", GetSubmissionAnimal)
            .WithName(OpenApiMetadata.GetAnimal.Name)
            .WithSummary(OpenApiMetadata.GetAnimal.Summary)
            .WithDescription(OpenApiMetadata.GetAnimal.Description)
            .Produces<AnimalResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{submissionId}/{animalId}", UpsertSubmissionAnimal)
            .WithName(OpenApiMetadata.UpsertAnimal.Name)
            .WithSummary(OpenApiMetadata.UpsertAnimal.Summary)
            .WithDescription(OpenApiMetadata.UpsertAnimal.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{submissionId}/{animalId}", UpsertSubmissionAnimal)
            .WithName(OpenApiMetadata.PatchAnimal.Name)
            .WithSummary(OpenApiMetadata.PatchAnimal.Summary)
            .WithDescription(OpenApiMetadata.PatchAnimal.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{submissionId}/{animalId}", DeleteSubmissionAnimal)
            .WithName(OpenApiMetadata.DeleteAnimal.Name)
            .WithSummary(OpenApiMetadata.DeleteAnimal.Summary)
            .WithDescription(OpenApiMetadata.DeleteAnimal.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{submissionId}", SubmitSubmission)
            .WithName(OpenApiMetadata.SubmitSubmission.Name)
            .WithSummary(OpenApiMetadata.SubmitSubmission.Summary)
            .WithDescription(OpenApiMetadata.SubmitSubmission.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> ListSubmissions(
        string county,
        string parish,
        string holding,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var cph = $"{county}/{parish}/{holding}";
        return TypedResults.Ok(await submissionService.ListAsync(cph, cancellationToken));
    }

    private static async Task<IResult> StartSubmission(
        string county,
        string parish,
        string holding,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var cph = $"{county}/{parish}/{holding}";
        return TypedResults.Ok(await submissionService.StartAsync(cph, cancellationToken));
    }

    private static async Task<IResult> GetSubmission(
        string submissionId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.GetSubmissionAsync(submissionId, cancellationToken));
    }

    private static async Task<IResult> GetSubmissionAnimal(
        string submissionId,
        string animalId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.GetAnimalAsync(submissionId, animalId, cancellationToken));
    }

    private static async Task<IResult> UpsertSubmissionAnimal(
        string submissionId,
        string animalId,
        AnimalRequest? request,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await submissionService.UpsertAnimalAsync(submissionId, animalId, request, cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<IResult> DeleteSubmissionAnimal(
        string submissionId,
        string animalId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        await submissionService.DeleteAnimalAsync(submissionId, animalId, cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<IResult> SubmitSubmission(
        string submissionId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        await submissionService.SubmitAsync(submissionId, cancellationToken);
        return TypedResults.Ok();
    }
}
