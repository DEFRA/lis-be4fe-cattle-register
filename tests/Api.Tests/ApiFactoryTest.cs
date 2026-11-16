// <copyright file="ApiFactoryTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class ApiFactoryTest : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public ApiFactoryTest(ApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void TestHostShouldLoadConfigFilesWithoutWatchingThem()
    {
        var configuration = (IConfigurationRoot)factory.Services.GetRequiredService<IConfiguration>();

        var files = configuration.Providers.OfType<FileConfigurationProvider>().ToList();

        files.ShouldNotBeEmpty();
        files.ShouldAllBe(provider => !provider.Source.ReloadOnChange);
    }
}
