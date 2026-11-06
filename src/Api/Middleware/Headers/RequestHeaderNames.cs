// <copyright file="RequestHeaderNames.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware.Headers;

public static class RequestHeaderNames
{
    // The CDP platform request ID (Correlation ID standard), not the legacy x-correlation-id.
    public const string CorrelationId = "x-cdp-request-id";
    public const string OperatorId = "x-operator-id";
    public const string ApiKey = "x-api-key";
}
