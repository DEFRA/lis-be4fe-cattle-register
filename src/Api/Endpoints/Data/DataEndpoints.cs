// <copyright file="DataEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Data;

using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Models.Data;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Reference data the cattle-register UI reads to populate its forms, under the <c>/data</c> root.
/// </summary>
public static class DataEndpoints
{
    public static IEndpointRouteBuilder UseDataEndpoints(this IEndpointRouteBuilder app)
    {
        var eartags = app
            .MapGroup("/" + RouteNames.Data + "/eartags")
            .RequireServiceToServiceAuthorization()
            .WithTags(OpenApiMetadata.Tag);

        eartags.MapGet("/unallocated", GetUnallocatedEartags)
            .WithName(OpenApiMetadata.GetUnallocatedEartags.Name)
            .WithSummary(OpenApiMetadata.GetUnallocatedEartags.Summary)
            .WithDescription(OpenApiMetadata.GetUnallocatedEartags.Description)
            .Produces<UnallocatedEartagsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        eartags.MapGet("/dams", GetDams)
            .WithName(OpenApiMetadata.GetDams.Name)
            .WithSummary(OpenApiMetadata.GetDams.Summary)
            .WithDescription(OpenApiMetadata.GetDams.Description)
            .Produces<ParentAnimalsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        eartags.MapGet("/sires", GetSires)
            .WithName(OpenApiMetadata.GetSires.Name)
            .WithSummary(OpenApiMetadata.GetSires.Summary)
            .WithDescription(OpenApiMetadata.GetSires.Description)
            .Produces<ParentAnimalsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> GetUnallocatedEartags(
        string? cph,
        [FromQuery(Name = "submission-id")] string? submissionId,
        IDataService dataService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dataService.GetUnallocatedEartagsAsync(cph, submissionId, cancellationToken));
    }

    private static async Task<IResult> GetDams(
        string? cph,
        IDataService dataService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dataService.GetDamsAsync(cph, cancellationToken));
    }

    private static async Task<IResult> GetSires(
        string? cph,
        IDataService dataService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dataService.GetSiresAsync(cph, cancellationToken));
    }
}
