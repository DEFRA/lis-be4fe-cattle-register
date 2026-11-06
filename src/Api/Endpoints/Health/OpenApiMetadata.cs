// <copyright file="OpenApiMetadata.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Health;

using Defra.Lis.Be4Fe.Api;

public static class OpenApiMetadata
{
    public const string Tag = nameof(RouteNames.Health);

    public static class Get
    {
        public const string Name = "GetHealth";
        public const string Summary = "Get health status";
        public const string Description = "Retrieves the health status of the API and its dependencies";
    }
}
