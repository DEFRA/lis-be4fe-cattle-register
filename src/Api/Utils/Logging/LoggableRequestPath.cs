// <copyright file="LoggableRequestPath.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Logging;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// The request path to write to the logs. The raw path can carry personal data, such as the Defra ID subject in
/// <c>/submission/{submissionId}/{animalId}/{section}</c>, so the route template of the matched endpoint is logged instead.
/// </summary>
public static class LoggableRequestPath
{
    /// <summary>The value logged when the request matched no route, as its raw path could still carry personal data.</summary>
    public const string UnmatchedRoute = "(unmatched route)";

    /// <summary>
    /// Returns the route template of the endpoint that matched the request, for example <c>/submission/{submissionId}/{animalId}/{section}</c>.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <returns>The route template, or <see cref="UnmatchedRoute"/> when no route matched.</returns>
    public static string For(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The exception handler middleware clears the endpoint before its handlers run, but keeps it on the feature.
        var endpoint = context.GetEndpoint() ?? context.Features.Get<IExceptionHandlerFeature>()?.Endpoint;

        return endpoint is RouteEndpoint { RoutePattern.RawText: { Length: > 0 } template }
            ? template
            : UnmatchedRoute;
    }
}
