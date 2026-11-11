// <copyright file="PropertyTypes.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

public static class PropertyTypes
{
    public const string String = "STRING";
    public const string Date = "DATE";
    public const string Int = "INT";
    public const string Number = "NUMBER";
    public const string Bool = "BOOL";

    public static bool IsSupported(string type) => type is String or Date or Int or Number or Bool;
}
