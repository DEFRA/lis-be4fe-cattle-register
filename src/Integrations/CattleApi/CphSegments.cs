// <copyright file="CphSegments.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

/// <summary>
/// A county/parish/holding number split into its three segments for use in cattle API routes.
/// </summary>
public sealed record CphSegments(string County, string Parish, string Holding)
{
    private static readonly char[] Separators = ['/', '-'];

    public static CphSegments Parse(string cph)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cph);

        var parts = cph.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 3)
        {
            throw new ArgumentException("CPH must contain county, parish and holding segments.", nameof(cph));
        }

        return new CphSegments(parts[0], parts[1], parts[2]);
    }

    public string ToRoute() => $"{Uri.EscapeDataString(County)}/{Uri.EscapeDataString(Parish)}/{Uri.EscapeDataString(Holding)}";

    public override string ToString() => $"{County}/{Parish}/{Holding}";
}
