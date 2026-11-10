// <copyright file="SubmissionEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Submission;

using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Models.Submissions;

/// <summary>
/// The BFF framework endpoints for submission. They sit at the <c>/submission</c> root the framework sets out,
/// rather than under <c>/api</c>.
/// </summary>
public static class SubmissionEndpoints
{
    public static IEndpointRouteBuilder UseSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/" + RouteNames.Submission)
            .RequireServiceToServiceAuthorization()
            .WithTags(OpenApiMetadata.Tag);

        group.MapGet("/start", StartSubmission)
            .WithName(OpenApiMetadata.StartSubmission.Name)
            .WithSummary(OpenApiMetadata.StartSubmission.Summary)
            .WithDescription(OpenApiMetadata.StartSubmission.Description)
            .Produces<SubmissionSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{submissionId}/{animalId}/{section}", GetAnimalSection)
            .WithName(OpenApiMetadata.GetAnimalSection.Name)
            .WithSummary(OpenApiMetadata.GetAnimalSection.Summary)
            .WithDescription(OpenApiMetadata.GetAnimalSection.Description)
            .Produces<AnimalSectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{submissionId}:update", UpdateAnimalSection)
            .WithName(OpenApiMetadata.UpdateAnimalSection.Name)
            .WithSummary(OpenApiMetadata.UpdateAnimalSection.Summary)
            .WithDescription(OpenApiMetadata.UpdateAnimalSection.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{submissionId}/{animalId}", DeleteAnimal)
            .WithName(OpenApiMetadata.DeleteAnimal.Name)
            .WithSummary(OpenApiMetadata.DeleteAnimal.Summary)
            .WithDescription(OpenApiMetadata.DeleteAnimal.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{submissionId}:submit", SubmitSubmission)
            .WithName(OpenApiMetadata.SubmitSubmission.Name)
            .WithSummary(OpenApiMetadata.SubmitSubmission.Summary)
            .WithDescription(OpenApiMetadata.SubmitSubmission.Description)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet(string.Empty, ListSubmissions)
            .WithName(OpenApiMetadata.ListSubmissions.Name)
            .WithSummary(OpenApiMetadata.ListSubmissions.Summary)
            .WithDescription(OpenApiMetadata.ListSubmissions.Description)
            .Produces<SubmissionListResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> StartSubmission(
        string? cph,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.StartAsync(cph, cancellationToken));
    }

    private static async Task<IResult> GetAnimalSection(
        string submissionId,
        string animalId,
        string section,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.GetSectionAsync(submissionId, animalId, section, cancellationToken));
    }

    private static async Task<IResult> UpdateAnimalSection(
        string submissionId,
        UpdateAnimalSectionRequest? request,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentException("A request body is required.", nameof(request));
        }

        await submissionService.UpdateSectionAsync(submissionId, request, cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<IResult> DeleteAnimal(
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

    private static async Task<IResult> ListSubmissions(
        string? cph,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await submissionService.ListAsync(cph, cancellationToken));
    }
}
