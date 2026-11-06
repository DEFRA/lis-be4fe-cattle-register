// <copyright file="CattleApiCattle.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

/// <summary>
/// One animal from the cattle API's cattle-on-holding response (camelCase JSON).
/// </summary>
public sealed record CattleApiCattle(
    string? EarTag,
    DateOnly? DateBirth,
    DateOnly? DateOnCph,
    string? Sex,
    string? Breed,
    string? BreedCode,
    string? BreedName,
    string? Status);
