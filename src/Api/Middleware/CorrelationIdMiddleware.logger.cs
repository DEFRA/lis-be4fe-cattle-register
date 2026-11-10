// <copyright file="CorrelationIdMiddleware.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware;

public partial class CorrelationIdMiddleware
{
    [LoggerMessage(LogLevel.Error, "Error in {MiddlewareName}")]
    static partial void LogErrorInMiddleware(ILogger logger, string middlewareName, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Rejected request without a single x-cdp-request-id header for {Method} {Path}")]
    static partial void LogMissingCorrelationId(ILogger logger, string method, string path);
}
