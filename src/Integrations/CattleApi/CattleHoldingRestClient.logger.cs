// <copyright file="CattleHoldingRestClient.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using Microsoft.Extensions.Logging;

public partial class CattleHoldingRestClient
{
    [LoggerMessage(LogLevel.Information, "Retrieved {Count} cattle from the cattle API. Cph: {Cph}, Filtered: {Filtered}")]
    partial void LogRetrievedCattleForHolding(int count, string cph, bool filtered);

    [LoggerMessage(LogLevel.Warning, "The cattle API does not know the holding. Cph: {Cph}")]
    partial void LogHoldingNotFound(string cph);

    [LoggerMessage(LogLevel.Warning, "The cattle API could not be reached. Action: {Action}")]
    partial void LogCattleApiUnreachable(string action, Exception exception);
}
