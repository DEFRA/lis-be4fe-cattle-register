// <copyright file="ItemStatus.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Models.Submissions;

using System.Text.Json.Serialization;

[JsonConverter(typeof(ItemStatusJsonConverter))]
public enum ItemStatus
{
    Draft,
    Complete,
    Error,
}
