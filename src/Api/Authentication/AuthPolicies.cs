// <copyright file="AuthPolicies.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Authentication;

/// <summary>
/// Authorisation policy names.
/// </summary>
public static class AuthPolicies
{
    /// <summary>
    /// Gets the policy every service-to-service endpoint requires. It accepts the API key scheme today; AWS STS is
    /// added to this policy as a further scheme when it arrives, without touching the endpoints.
    /// </summary>
    public const string ServiceToService = "ServiceToService";
}
