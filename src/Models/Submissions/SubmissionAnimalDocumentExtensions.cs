// <copyright file="SubmissionAnimalDocumentExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

public static class SubmissionAnimalDocumentExtensions
{
    extension(SubmissionAnimalDocument source)
    {
        public AnimalResponse ToAnimalResponse()
        {
            return new AnimalResponse
            {
                AnimalId = source.AnimalId.Trim(),
                Status = source.Status,
                Data =
                [
                    .. (source.Data ?? [])
                    .Select(item => item.ToDataItem())
                ],
            };
        }
    }
}
