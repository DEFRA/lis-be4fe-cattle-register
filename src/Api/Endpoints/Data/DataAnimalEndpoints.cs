// <copyright file="DataAnimalEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Data;

using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Models.Data;
using Defra.Lis.Be4Fe.Models.Route;

/// <summary>
/// Reference data the cattle-register UI reads to populate its forms, under the <c>/data</c> root.
/// </summary>
public static class DataAnimalEndpoints
{
    public static IEndpointRouteBuilder UseDataAnimalEndpoints(this IEndpointRouteBuilder app, RouteGroupBuilder group)
    {
        group.MapGet("/{county}/{parish}/{holding}/animals/{animalId}", GetAnimal)
            .WithName(OpenApiMetadata.GetAnimal.Name)
            .WithSummary(OpenApiMetadata.GetAnimal.Summary)
            .WithDescription(OpenApiMetadata.GetAnimal.Description)
            .Produces<Animal>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> GetAnimal(
        [AsParameters] CphRoute cph,
        string animalId,
        IDataService dataService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dataService.GetAnimalAsync(
            cph.ToString(),
            animalId,
            cancellationToken));
    }
}
