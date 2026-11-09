// <copyright file="CattleHoldingRestClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using System.Net;
using System.Text.Json;
using Defra.Lis.Be4Fe.CattleApi.Dto;
using Defra.Lis.Be4Fe.Models.Lookups.Models;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest;
using Microsoft.Extensions.Logging;

/// <summary>
/// Passes cattle-on-holding queries through to the cattle API using the strategies SDK REST strategy. The API key travels as <c>x-api-key</c>; the
/// correlation header is added by header propagation on the SDK's HTTP client.
/// </summary>
public sealed partial class CattleHoldingRestClient(
    IRestStrategyFactory<CattleHoldingRestClient> strategyFactory,
    CattleApiOptions options,
    ILogger<CattleHoldingRestClient> logger)
    : ICattleHoldingClient
{
    public const string ApiKeyHeaderName = "x-api-key";

    private const string ApiDescription = "Cattle API";

    // The cattle API serves every endpoint under a URL-segment version.
    private const string ApiVersion = "v1";

    // The cattle API emits camelCase; the SDK's default is snake_case.
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<CattleSummary>> SearchCattleAsync(string cph, CattleSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var segments = CphSegments.Parse(cph);

        try
        {
            var cattle = await ExecuteAsync(
                "Search cattle on holding",
                strategy => strategy
                    .WithResourceUrl($"{ApiVersion}/holdings/{segments.ToRoute()}/cattle")
                    .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Eartag), "earTag", query.Eartag?.Trim() ?? string.Empty)
                    .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Breed), "breed", query.Breed?.Trim() ?? string.Empty)
                    .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Sex), "sex", query.Sex?.Trim() ?? string.Empty)
                    .ExecuteAndTransform<List<CattleApiCattle>, IReadOnlyCollection<CattleSummary>>(list => list.Select(ToCattleSummary).ToList()),
                cancellationToken);

            LogRetrievedCattleForHolding(cattle.Count, segments.ToString(), !query.IsEmpty);

            return cattle;
        }
        catch (RestResponseException ex) when (IsNotFound(ex))
        {
            LogHoldingNotFound(segments.ToString());
            throw new HoldingNotFoundException(segments.ToString());
        }
        catch (RestResponseException ex) when (IsBadRequest(ex))
        {
            throw new ArgumentException($"The cattle API rejected CPH '{segments}'.", nameof(cph), ex);
        }
    }

    private static CattleSummary ToCattleSummary(CattleApiCattle cattle)
    {
        ArgumentNullException.ThrowIfNull(cattle);

        var earTag = cattle.EarTag ?? string.Empty;

        return new CattleSummary
        {
            CattleId = earTag,
            Eartag = earTag,
            Breed = cattle.Breed ?? cattle.BreedName ?? string.Empty,
            BreedCode = cattle.BreedCode,
            BreedName = cattle.BreedName,
            DateOfBirth = cattle.DateBirth,
            DateOnCph = cattle.DateOnCph,
            Sex = cattle.Sex ?? string.Empty,
            Status = cattle.Status ?? string.Empty,
        };
    }

    private static bool IsNotFound(RestResponseException exception) => HasStatus(exception, HttpStatusCode.NotFound);

    private static bool IsBadRequest(RestResponseException exception) => HasStatus(exception, HttpStatusCode.BadRequest);

    private static bool HasStatus(RestResponseException exception, HttpStatusCode statusCode) =>
        exception.InnerException is HttpRequestException { StatusCode: { } actual } && actual == statusCode;

    // The SDK wraps a transport failure (connection refused, DNS failure, timeout) in a RestRequestException. A
    // cancelled caller token is the caller giving up, not the cattle API being unavailable, so it is left alone.
    private static bool IsUnreachable(RestRequestException exception, CancellationToken cancellationToken) =>
        (exception.InnerException is HttpRequestException or OperationCanceledException) && !cancellationToken.IsCancellationRequested;

    /// <summary>
    /// Builds the strategy for <paramref name="action"/> and runs <paramref name="execute"/> against it, turning an
    /// unreachable cattle API into a <see cref="ServiceUnavailableException"/> (503) so the caller can retry.
    /// </summary>
    private async Task<TResult> ExecuteAsync<TResult>(
        string action,
        Func<IRestStrategy<CattleHoldingRestClient>, Task<TResult>> execute,
        CancellationToken cancellationToken)
    {
        try
        {
            return await execute(BuildStrategy(action, cancellationToken));
        }
        catch (RestRequestException ex) when (IsUnreachable(ex, cancellationToken))
        {
            LogCattleApiUnreachable(action, ex);
            throw new ServiceUnavailableException("The cattle API is temporarily unavailable, try again later.", ex);
        }
    }

    private IRestStrategy<CattleHoldingRestClient> BuildStrategy(string action, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException($"{CattleApiOptions.SectionName}:BaseUrl must be configured to call the cattle API.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException($"{CattleApiOptions.SectionName}:ApiKey must be configured to call the cattle API.");
        }

        return strategyFactory
            .BuildRestStrategy()
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithApiDescription(ApiDescription)
            .WithActionDescription(action)
            .WithBaseUrl(options.BaseUrl)
            .WithJsonSerializerOptions(SerializerOptions)
            .WithGet()
            .WithHeader(ApiKeyHeaderName, options.ApiKey);
    }
}
