// <copyright file="ItemStatusJsonConverter.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serializes animal statuses as strings and rejects integer representations.
/// </summary>
public sealed class ItemStatusJsonConverter()
    : JsonStringEnumConverter<ItemStatus>(
        JsonNamingPolicy.CamelCase,
        allowIntegerValues: false);
