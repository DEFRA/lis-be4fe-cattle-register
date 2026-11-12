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
            if (string.IsNullOrWhiteSpace(source.Key))
            {
                throw new ArgumentException("Every data item needs a key.", nameof(source));
            }

            var type = source.Type?.Trim().ToUpperInvariant();
            if (type is null || !PropertyTypes.IsSupported(type))
            {
                throw new ArgumentException($"Data item '{source.Key}' has an unsupported type '{source.Type}'.", nameof(source));
            }

            return new SubmissionDataItemDocument
            {
                Key = source.Key,
                Value = source.Value,
                Type = type,
            };
        }
    }
}
