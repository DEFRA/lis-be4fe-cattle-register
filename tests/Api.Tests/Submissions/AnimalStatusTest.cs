// <copyright file="AnimalStatusTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Submissions;

using System.Text.Json;
using Defra.Lis.Be4Fe.Models.Submissions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

public sealed class AnimalStatusTest
{
    [Theory]
    [InlineData(ItemStatus.Draft, "draft")]
    [InlineData(ItemStatus.Complete, "complete")]
    [InlineData(ItemStatus.Error, "error")]
    [InlineData(ItemStatus.InProgress, "inProgress")]
    public void ShouldWriteCanonicalJsonStrings(ItemStatus status, string expected)
    {
        JsonSerializer.Serialize(status).ShouldBe($"\"{expected}\"");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    [InlineData("\"0\"")]
    [InlineData("\"999\"")]
    [InlineData("\"OTHER\"")]
    [InlineData("null")]
    public void ShouldRejectInvalidJsonValues(string json)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ItemStatus>(json));
    }

    [Theory]
    [InlineData("DRAFT", ItemStatus.Draft)]
    [InlineData("dRAfT", ItemStatus.Draft)]
    [InlineData("COMPLETE", ItemStatus.Complete)]
    [InlineData("COmPLEtE", ItemStatus.Complete)]
    public void ShouldPreserveExistingBsonStrings(string storedStatus, ItemStatus expected)
    {
        var bson = new BsonDocument
        {
            { "AnimalId", "UK1" },
            { "Status", storedStatus },
            { "Data", new BsonArray() },
        };

        var animal = BsonSerializer.Deserialize<SubmissionAnimalDocument>(bson);

        animal.Status.ShouldBe(expected);
        animal.ToBsonDocument()["Status"].AsString.ShouldBe(expected.ToString());
    }
}
