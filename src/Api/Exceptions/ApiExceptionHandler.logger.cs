// <copyright file="ApiExceptionHandler.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Exceptions;

public partial class ApiExceptionHandler
{
    [LoggerMessage(LogLevel.Error, "Unhandled exception while processing request {Method} {Path}")]
    partial void LogUnhandledExceptionWhileProcessingRequestMethodPath(string method, string path, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Request failed with {StatusCode} {Title} for {Method} {Path}")]
    partial void LogRequestFailedWithStatusCodeTitleForMethodPath(int statusCode, string title, string method, string path, Exception exception);
}
