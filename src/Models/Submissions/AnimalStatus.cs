// <copyright file="AnimalStatus.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

public static class AnimalStatus
{
    public const string Draft = "DRAFT";
    public const string Complete = "COMPLETE";

    public static bool IsSupported(string status) => status is Draft or Complete;
}
