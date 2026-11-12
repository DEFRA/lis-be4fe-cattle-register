// <copyright file="TestInitialization.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests;

using System.Runtime.CompilerServices;
using Defra.Lis.Be4Fe.Api.Utils.Mongo;

internal static class TestInitialization
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // BSON class maps are cached on first use. Register the production conventions
        // before any test can create a map, even without starting the application.
        MongoConventions.Register();
    }
}
