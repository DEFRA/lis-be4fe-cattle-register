// <copyright file="DataService.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

public partial class DataService
{
    [LoggerMessage(LogLevel.Information, "Retrieved {Count} unallocated eartags, leaving out {Excluded} used in the submission. Cph: {Cph}")]
    partial void LogRetrievedUnallocatedEartags(int count, int excluded, string cph);

    [LoggerMessage(LogLevel.Information, "Retrieved {Count} possible parent animals. Sex: {Sex}, Cph: {Cph}")]
    partial void LogRetrievedParentAnimals(int count, string sex, string cph);
}
