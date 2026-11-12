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
            if (!Enum.IsDefined(source.Status))
            {
                throw new ArgumentException("The item status is not supported.", nameof(source));
            }

            if (source.Data is null)
            {
                throw new ArgumentException("Data must be an array of key, value and type items.", nameof(source));
            }

            var data = new List<SubmissionDataItemDocument>(source.Data.Length);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in source.Data)
            {
                if (item is null)
                {
                    throw new ArgumentException("Data items must not be null.", nameof(source));
                }

                var stored = item.ToSubmissionDataItemDocument();
                if (!keys.Add(stored.Key))
                {
                    throw new ArgumentException($"Data item '{stored.Key}' appears more than once.", nameof(source));
                }

                data.Add(stored);
            }

            return new SubmissionAnimalDocument
            {
                AnimalId = animalId,
                Status = source.Status,
                Data = data,
            };
        }
    }
}
