// <copyright file="CdpLoggingTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Logging;

using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

public class CdpLoggingTests
{
    [Fact]
    public void Configuration_AppliesMinimumLevelOverridesFromSettings()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Information",
            ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Hosting.Diagnostics"] = "Warning",
        };
        var context = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
        };
        var configuration = new LoggerConfiguration();

        var services = new ServiceCollection().AddHttpContextAccessor().BuildServiceProvider();

        CdpLogging.Configuration(context, services, configuration);
        using var logger = configuration.CreateLogger();

        var overridden = logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Hosting.Diagnostics");
        Assert.False(overridden.IsEnabled(LogEventLevel.Information));
        Assert.True(overridden.IsEnabled(LogEventLevel.Warning));

        var other = logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, "Defra.Lis.Be4Fe.Api.Services.DataService");
        Assert.True(other.IsEnabled(LogEventLevel.Information));
        Assert.False(other.IsEnabled(LogEventLevel.Debug));
    }
}
