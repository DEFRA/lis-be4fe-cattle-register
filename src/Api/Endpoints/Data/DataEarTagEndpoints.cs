// <copyright file="DataEarTagEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Api.Endpoints.Data;

using Defra.Livestock.Be4Fe.Api.Services;
using Defra.Livestock.Be4Fe.Models.Data;
using Defra.Livestock.Be4Fe.Models.Route;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Reference data the cattle-register UI reads to populate its forms, under the <c>/data</c> root.
/// </summary>
public static class DataEarTagEndpoints
{
    public static IEndpointRouteBuilder UseDataEarTagEndpoints(this IEndpointRouteBuilder app, RouteGroupBuilder group)
    {
        group.MapGet("/{county}/{parish}/{holding}/ear-tags/unallocated", GetUnallocatedEarTags)
            .WithName(OpenApiMetadata.GetUnallocatedEartags.Name)
            .WithSummary(OpenApiMetadata.GetUnallocatedEartags.Summary)
            .WithDescription(OpenApiMetadata.GetUnallocatedEartags.Description)
            .Produces<UnallocatedEartagsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{county}/{parish}/{holding}/ear-tags/dams", GetDams)
            .WithName(OpenApiMetadata.GetDams.Name)
            .WithSummary(OpenApiMetadata.GetDams.Summary)
            .WithDescription(OpenApiMetadata.GetDams.Description)
            .Produces<HoldingAnimalsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{county}/{parish}/{holding}/ear-tags/sires", GetSires)
            .WithName(OpenApiMetadata.GetSires.Name)
            .WithSummary(OpenApiMetadata.GetSires.Summary)
            .WithDescription(OpenApiMetadata.GetSires.Description)
            .Produces<HoldingAnimalsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> GetUnallocatedEarTags(
        [AsParameters] CphRoute cph,
        [FromQuery(Name = "submission-id")] string? submissionId,
        IDataService dataService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dataService.GetUnallocatedEarTagsAsync(
            cph.ToString(),
            submissionId,
            cancellationToken));
    }

    private static async Task<IResult> GetDams(
        [AsParameters] CphRoute cph,
        IDataService dataService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dataService.GetDamsAsync(
            cph.ToString(),
            cancellationToken));
    }

    private static async Task<IResult> GetSires(
        [AsParameters] CphRoute cph,
        IDataService dataService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dataService.GetSiresAsync(
            cph.ToString(),
            cancellationToken));
    }
}
