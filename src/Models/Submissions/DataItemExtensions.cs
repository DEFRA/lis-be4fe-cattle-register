// <copyright file="DataItemExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

public static class DataItemExtensions
{
    extension(DataItem source)
    {
        public SubmissionDataItemDocument ToSubmissionDataItemDocument()
        {
            return new SubmissionDataItemDocument
            {
                Key = source.Key,
                Value = source.Value,
                Type = source.Type,
            };
        }
    }
}
