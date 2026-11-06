// <copyright file="MongoDbClientFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Mongo;

using System.Diagnostics.CodeAnalysis;
using Defra.Lis.Be4Fe.Api.Config;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public interface IMongoDbClientFactory
{
    IMongoClient GetClient();

    IMongoCollection<T> GetCollection<T>(string collection);
}

[ExcludeFromCodeCoverage]
public class MongoDbClientFactory : IMongoDbClientFactory
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly IMongoClient client;

    public MongoDbClientFactory(IOptions<MongoConfig> config)
    {
        var uri = config.Value.DatabaseUri;
        var databaseName = config.Value.DatabaseName;

        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("MongoDB uri string cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("MongoDB database name cannot be empty");
        }

        var settings = MongoClientSettings.FromConnectionString(uri);
        client = new MongoClient(settings);
        mongoDatabase = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collection)
    {
        return mongoDatabase.GetCollection<T>(collection);
    }

    public IMongoClient GetClient()
    {
        return client;
    }
}
