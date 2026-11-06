// <copyright file="CphQueryParameter.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using System.Text.RegularExpressions;
using Defra.Lis.Be4Fe.CattleApi;

/// <summary>
/// Validates and normalises the <c>cph</c> query parameter shared by the endpoints scoped to a holding.
/// </summary>
public static partial class CphQueryParameter
{
    /// <exception cref="ArgumentException">The CPH is missing or not in the format 12/345/6789.</exception>
    /// <returns>The CPH as <c>12/345/6789</c>, whether it was given with slashes or dashes.</returns>
    public static string Normalise(string? cph)
    {
        if (string.IsNullOrWhiteSpace(cph))
        {
            throw new ArgumentException("A cph query parameter is required.", nameof(cph));
        }

        var normalised = CphSegments.Parse(cph).ToString();

        return CphFormat().IsMatch(normalised)
            ? normalised
            : throw new ArgumentException("The cph query parameter must be in the format 12/345/6789.", nameof(cph));
    }

    [GeneratedRegex(@"^\d{2}/\d{3}/\d{4}$")]
    private static partial Regex CphFormat();
}
