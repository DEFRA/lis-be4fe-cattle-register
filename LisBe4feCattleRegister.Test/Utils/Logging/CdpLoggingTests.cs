using LisBe4feCattleRegister.Utils.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace LisBe4feCattleRegister.Test.Utils.Logging;

public class CdpLoggingTests
{
    [Fact]
    public void Configuration_AppliesMinimumLevelOverridesFromSettings()
    {
        using var logger = CreateLogger(new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Information",
            ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Hosting.Diagnostics"] = "Warning",
        });

        var overridden = logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Hosting.Diagnostics");
        Assert.False(overridden.IsEnabled(LogEventLevel.Information));
        Assert.True(overridden.IsEnabled(LogEventLevel.Warning));

        var other = logger.ForContext(Constants.SourceContextPropertyName, "LisBe4feCattleRegister.Services.SomeService");
        Assert.True(other.IsEnabled(LogEventLevel.Information));
        Assert.False(other.IsEnabled(LogEventLevel.Debug));
    }

    [Fact]
    public void Configuration_AppliesTheDefaultLevelFromSettings()
    {
        using var logger = CreateLogger(new Dictionary<string, string?> { ["Serilog:MinimumLevel:Default"] = "Debug" });

        Assert.True(logger.IsEnabled(LogEventLevel.Debug));
        Assert.False(logger.IsEnabled(LogEventLevel.Verbose));
    }

    [Theory]
    [InlineData("Serilog:MinimumLevel:Default")]
    [InlineData("Serilog:MinimumLevel")]
    public void Configuration_KeepsTheRootAtInformation_WhenTheDefaultIsHigher(string key)
    {
        // Audit events are logged at Information; the root logger must let them through to the audit logger.
        using var logger = CreateLogger(new Dictionary<string, string?> { [key] = "Warning" });

        Assert.True(logger.IsEnabled(LogEventLevel.Information));
        Assert.False(logger.IsEnabled(LogEventLevel.Debug));
    }

    [Fact]
    public void RootMinimumLevelSettings_KeepsOverridesAndCapsOnlyTheDefault()
    {
        var settings = CdpLogging.RootMinimumLevelSettings(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:MinimumLevel:Default"] = "Error",
                ["Serilog:MinimumLevel:Override:Polly"] = "Fatal",
                ["Serilog:WriteTo:0:Name"] = "Console",
            })
            .Build());

        Assert.Equal("Information", settings["Serilog:MinimumLevel:Default"]);
        Assert.Equal("Fatal", settings["Serilog:MinimumLevel:Override:Polly"]);
        Assert.Null(settings["Serilog:WriteTo:0:Name"]);
    }

    private static Logger CreateLogger(Dictionary<string, string?> settings)
    {
        var context = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
        };
        var configuration = new LoggerConfiguration();

        CdpLogging.Configuration(context, configuration);

        return configuration.CreateLogger();
    }
}
