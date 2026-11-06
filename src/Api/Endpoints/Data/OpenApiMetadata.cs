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
        public const string Description = "Returns the eartags issued to the holding that are not yet on an animal, sourced from the cattle API. Requires the cph query parameter; when the optional submission-id is given, tags already used by that submission's animals are left out. 400 when the cph is missing or invalid or the submission is for another CPH, 404 when the holding or submission is unknown, 503 when the cattle API is unavailable.";
    }

    public static class GetDams
    {
        public const string Name = "GetDamEartags";
        public const string Summary = "Lists the animals that can be chosen as a dam for a CPH.";
        public const string Description = "Returns the female animals on the holding, sourced from the cattle API. Eligibility for a particular birth is checked when the submission is validated. Requires the cph query parameter. 400 when the cph is missing or invalid, 404 when the holding is unknown, 503 when the cattle API is unavailable.";
    }

    public static class GetSires
    {
        public const string Name = "GetSireEartags";
        public const string Summary = "Lists the animals that can be chosen as a sire for a CPH.";
        public const string Description = "Returns the male animals on the holding, sourced from the cattle API. Sires from off the holding are not included. Requires the cph query parameter. 400 when the cph is missing or invalid, 404 when the holding is unknown, 503 when the cattle API is unavailable.";
    }
}
