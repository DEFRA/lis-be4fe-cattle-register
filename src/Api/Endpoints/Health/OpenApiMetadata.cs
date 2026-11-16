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
        public const string Summary = "Gets the API health status.";
        public const string Description = "Returns the API health status. No dependency checks are currently registered, so MongoDB and the cattle API are not checked.";
    }
}
