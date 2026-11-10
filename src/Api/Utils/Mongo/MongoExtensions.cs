// <copyright file="MongoExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Mongo;

using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;

public static class MongoExtensions
{
    private static int initialized;

    public static void Register()
    {
        if (Interlocked.Exchange(ref initialized, 1) == 1)
        {
            return;
        }

        MongoClientSettings.Extensions.AddAWSAuthentication();
    }
}
