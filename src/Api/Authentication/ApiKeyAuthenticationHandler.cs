// <copyright file="ApiKeyAuthenticationHandler.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Authentication;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>
/// Authenticates a caller by comparing the API key header with the configured keys in constant time. The key itself
/// is never logged or placed on the principal.
/// </summary>
public sealed partial class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IProblemDetailsService problemDetailsService)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, loggerFactory, encoder)
{
    /// <summary>Gets the claim that identifies which configured key the caller presented.</summary>
    public const string ClientIdClaimType = "client_id";

    private string CdpRequestId => Request.Headers[RequestHeaderNames.CorrelationId].ToString();

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var presented) || string.IsNullOrEmpty(presented.ToString()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var keys = Options.Keys.Where(key => !string.IsNullOrWhiteSpace(key)).ToArray();
        if (keys.Length == 0)
        {
            LogNoApiKeysConfigured(Logger, LoggableRequestPath.For(Context), CdpRequestId);
            return Task.FromResult(AuthenticateResult.Fail("No API keys are configured."));
        }

        var keyIndex = presented.Count == 1 ? FindKey(presented[0], keys) : -1;
        if (keyIndex < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("The API key is not valid."));
        }

        var identity = new ClaimsIdentity([new Claim(ClientIdClaimType, $"api-key:{keyIndex}")], Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var reason = Request.Headers.ContainsKey(Options.HeaderName) ? "invalid" : "missing";
        LogApiKeyRejected(Logger, reason, Request.Method, LoggableRequestPath.For(Context), CdpRequestId);

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = $"{Scheme.Name} header=\"{Options.HeaderName}\"";
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails =
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Type = "https://httpstatuses.com/401",
                Detail = $"A valid {Options.HeaderName} header is required.",
            },
        });
    }

    /// <summary>
    /// Returns the index of the matching key, or -1. Both sides are hashed first so the comparison takes the same
    /// time whatever the lengths, and every key is checked so the position of a match is not revealed either.
    /// </summary>
    private static int FindKey(string presented, string[] keys)
    {
        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var match = -1;
        for (var index = 0; index < keys.Length; index++)
        {
            var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(keys[index]));
            if (CryptographicOperations.FixedTimeEquals(presentedHash, keyHash) && match < 0)
            {
                match = index;
            }
        }

        return match;
    }
}
