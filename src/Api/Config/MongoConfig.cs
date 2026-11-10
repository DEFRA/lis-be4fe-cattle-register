// <copyright file="MongoConfig.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Config;

using System.ComponentModel.DataAnnotations;

public class MongoConfig
{
    public const string SectionName = "Mongo";

    [Required]
    public required string DatabaseUri { get; init; }

    [Required]
    public required string DatabaseName { get; init; }
}
