// <copyright file="ApiFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Api.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// The base for every test host of the API. Config reload is turned off because each reloading config file holds a
/// file watcher, and on Linux each one uses an inotify instance. Test classes that start a host per test would otherwise
/// exhaust the default limit of 128 when they run in parallel.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("hostBuilder:reloadConfigOnChange", "false");
    }
}
