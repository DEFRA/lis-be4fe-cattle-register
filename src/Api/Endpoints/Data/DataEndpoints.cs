// <copyright file="v.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Data;

using Defra.Lis.Be4Fe.Api.Authentication;

/// <summary>
/// Reference data the cattle-register UI reads to populate its forms, under the <c>/data</c> root.
/// </summary>
public static class DataEndpoints
{
    public static IEndpointRouteBuilder UseDataEndpoints(this IEndpointRouteBuilder app)
    {
        var data = app
            .MapGroup("/" + RouteNames.Data)
            .RequireServiceToServiceAuthorization()
            .WithTags(OpenApiMetadata.Tag);

        app.UseDataAnimalEndpoints(data);
        app.UseDataEarTagEndpoints(data);
        return app;
    }
}
