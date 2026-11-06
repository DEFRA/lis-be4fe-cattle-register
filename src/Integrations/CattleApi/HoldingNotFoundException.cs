// <copyright file="HoldingNotFoundException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

public sealed class HoldingNotFoundException(string cph)
    : Exception($"Holding '{cph}' was not found.")
{
    public string Cph { get; } = cph;
}
