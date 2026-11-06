// <copyright file="AuditLoggingExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Auditing;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class AuditLoggingExtensions
{
    private static readonly Dictionary<string, object> AuditLogLevel = new()
    {
        [AuditLogger.AuditPropertyName] = true,
    };

    public static void Audit(
        this ILogger logger,
        string message,
        params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(logger);
        using (logger.BeginScope(AuditLogLevel))
        {
            logger.LogInformation(message, args);
        }
    }

    public static void Audit(
        this ILogger logger,
        Exception exception,
        string message,
        params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(logger);
        using (logger.BeginScope(AuditLogLevel))
        {
            logger.LogInformation(exception, message, args);
        }
    }
}
