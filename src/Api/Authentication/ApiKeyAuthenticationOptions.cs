// <copyright file="ApiKeyAuthenticationOptions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Authentication;

using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Microsoft.AspNetCore.Authentication;

/// <summary>
/// Options for the interim API key scheme that guards calls from the cattle-register UI until AWS STS replaces it (LREG-560).
/// </summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Gets the authentication scheme name.</summary>
    public const string SchemeName = "ApiKey";

    /// <summary>Gets the configuration section the options are bound from.</summary>
    public const string SectionName = "ApiKeyAuthentication";

    /// <summary>
    /// Gets or sets the request header that carries the key.
    /// </summary>
    public string HeaderName { get; set; } = RequestHeaderNames.ApiKey;

    /// <summary>
    /// Gets or sets the accepted keys. More than one key may be active at once so a key can be rotated without
    /// downtime. When no key is configured every protected request is rejected.
    /// </summary>
    public string[] Keys { get; set; } = [];
}
