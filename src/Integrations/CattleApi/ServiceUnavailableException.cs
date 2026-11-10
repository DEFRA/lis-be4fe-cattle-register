// <copyright file="ServiceUnavailableException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

/// <summary>
/// The cattle API could not be reached, or it (or a service behind it) reported that it is temporarily unavailable
/// (HTTP 503), so the caller can retry later. Mapped to 503 rather than 500, which signals an outright failure.
/// </summary>
public sealed class ServiceUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
