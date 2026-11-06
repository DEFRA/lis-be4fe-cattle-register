// <copyright file="HealthEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Health;

using Defra.Lis.Be4Fe.Api.MetaData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthStatusModel = Defra.Lis.Be4Fe.Models.Health.HealthStatus;

public static class HealthEndpoints
{
    public static RouteHandlerBuilder UseHealthEndpoints(this IEndpointRouteBuilder app)
    {
        return app.MapGet(RouteNames.Health, GetHealthRoute)
            .WithName(OpenApiMetadata.Get.Name)
            .WithTags(OpenApiMetadata.Tag)
            .WithSummary(OpenApiMetadata.Get.Summary)
            .WithDescription(OpenApiMetadata.Get.Description)
            .Produces<HealthStatusModel>()
            .WithMetadata(new IgnoreCorrelationIdCheck());
    }

    private static async Task<IResult> GetHealthRoute(
        HealthCheckService healthCheckService,
        CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);

        var response = new HealthStatusModel { Status = report.Status.ToString() };

        return TypedResults.Ok(response);
    }
}
