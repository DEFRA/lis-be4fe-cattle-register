// <copyright file="CorrelationIdContext.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware;

/// <summary>
/// Holds the correlation ID for the current execution flow so it follows async calls (Correlation ID standard).
/// </summary>
public static class CorrelationIdContext
{
    private static readonly AsyncLocal<string?> CorrelationId = new();

    /// <summary>Gets or sets the correlation ID for the current execution flow.</summary>
    public static string? Value
    {
        get => CorrelationId.Value;
        set => CorrelationId.Value = value;
    }
}
