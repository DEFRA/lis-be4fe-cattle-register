// <copyright file="OpenApiMetadata.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Data;

using Defra.Lis.Be4Fe.Api;

public static class OpenApiMetadata
{
    public const string Tag = nameof(RouteNames.Data);

    public static class GetUnallocatedEartags
    {
        public const string Name = "GetUnallocatedEartags";
        public const string Summary = "Lists the unallocated eartags for a CPH.";
        public const string Description = "Returns the eartags issued to the holding that are not yet on an animal, sourced from the cattle API. The CPH is supplied as county, parish and holding route segments; when the optional submission-id query parameter is given, tags already used by that submission's animals are left out. 400 when the CPH is invalid or the submission is for another CPH, 404 when the holding or submission is unknown, 503 when the cattle API is unavailable.";
    }

    public static class GetDams
    {
        public const string Name = "GetDamEartags";
        public const string Summary = "Lists the animals that can be chosen as a dam for a CPH.";
        public const string Description = "Returns the female animals on the holding, sourced from the cattle API. The CPH is supplied as county, parish and holding route segments. 400 when the CPH is invalid, 404 when the holding is unknown, 503 when the cattle API is unavailable.";
    }

    public static class GetSires
    {
        public const string Name = "GetSireEartags";
        public const string Summary = "Lists the animals that can be chosen as a sire for a CPH.";
        public const string Description = "Returns the male animals on the holding, sourced from the cattle API. Sires from off the holding are not included. The CPH is supplied as county, parish and holding route segments. 400 when the CPH is invalid, 404 when the holding is unknown, 503 when the cattle API is unavailable.";
    }

    public static class GetAnimal
    {
        public const string Name = "GetAnimal";
        public const string Summary = "Gets an animal on a CPH by its eartag.";
        public const string Description = "Returns the animal on the holding whose eartag matches the animalId route parameter, sourced from the cattle API. The CPH is supplied as county, parish and holding route segments. 400 when the CPH is invalid, 404 when the holding is unknown or no animal on it has that eartag, 503 when the cattle API is unavailable.";
    }
}
