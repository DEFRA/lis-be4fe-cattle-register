// <copyright file="SubmissionDataItemDocumentExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

public static class SubmissionDataItemDocumentExtensions
{
    extension(SubmissionDataItemDocument source)
    {
        public DataItem ToDataItem()
        {
            return new DataItem
            {
                Key = source.Key,
                Value = source.Value,
                Type = source.Type,
            };
        }
    }
}
