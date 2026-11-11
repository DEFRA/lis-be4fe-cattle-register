// <copyright file="AnimalRequestExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Submissions;

public static class AnimalRequestExtensions
{
    extension(AnimalRequest source)
    {
        public SubmissionAnimalDocument ToSubmissionAnimalDocument(string animalId)
        {
            return new SubmissionAnimalDocument
            {
                AnimalId = animalId,
                Status = source.Status,
                Data =
                [
                    .. (source.Data ?? [])
                    .Select(item => item.ToSubmissionDataItemDocument())
                ],
            };
        }
    }
}
