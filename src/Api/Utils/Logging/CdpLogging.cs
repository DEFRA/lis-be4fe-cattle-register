// <copyright file="CdpLogging.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Logging;

using System.Diagnostics.CodeAnalysis;
using Defra.Lis.Be4Fe.Api.Utils.Auditing;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

public static class CdpLogging
{
    /// <summary>
    /// Log sources that write raw request URLs at Information. The URLs can carry the Defra ID subject, as in
    /// <c>/submission/{submissionId}/{animalId}/{section}</c> inbound and <c>/v1/users/{userId}</c> to the cattle API, so they are held at Warning.
    /// </summary>
    public static readonly IReadOnlyList<string> RawUrlLoggers =
    [
        "Microsoft.AspNetCore.Hosting.Diagnostics",
        "System.Net.Http.HttpClient",
        typeof(RestHttpClient).FullName!,
    ];

    [ExcludeFromCodeCoverage]
    public static void Configuration(HostBuilderContext ctx, IServiceProvider services, LoggerConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        var traceIdHeader = ctx.Configuration.GetValue<string>("TraceHeader");

        // No ECS HttpContext enricher: it writes the full request URL onto every event.
        var mainLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Filter.With<AuditLogger.Filters.ExcludeAuditEvents>()
            .CreateLogger();

        // Serilog applies MinimumLevel overrides where an event is created, never in a sub-logger, so the
        // Serilog:MinimumLevel settings must be read onto this logger too or the overrides do nothing.
        config.ReadFrom.Configuration(MinimumLevelSettings(ctx.Configuration));

        // Applied after the settings so configuration cannot turn the URL logging back on.
        foreach (var source in RawUrlLoggers)
        {
            config.MinimumLevel.Override(source, LogEventLevel.Warning);
        }

        config.Enrich.With(new LoggableRequestPathEnricher(services.GetRequiredService<IHttpContextAccessor>()));

        if (traceIdHeader != null)
        {
            config.Enrich.WithCorrelationId(traceIdHeader);
        }

        var auditLogger = AuditLogger.CreateAuditLogger();

        config
            .WriteTo.Logger(mainLogger)
            .WriteTo.Logger(auditLogger);
    }

    /// <summary>
    /// Logs one line per request with the route template from <see cref="LoggableRequestPath"/> in place of the raw
    /// path.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder.</returns>
    public static IApplicationBuilder UseCdpRequestLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseSerilogRequestLogging(options =>
        {
            // The host's logger rather than the static Log.Logger the middleware falls back to.
            options.Logger = app.ApplicationServices.GetService<ILogger>();
            options.GetMessageTemplateProperties = (httpContext, _, elapsedMs, statusCode) =>
            [
                new LogEventProperty("RequestMethod", new ScalarValue(httpContext.Request.Method)),
                new LogEventProperty("RequestPath", new ScalarValue(LoggableRequestPath.For(httpContext))),
                new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
                new LogEventProperty("Elapsed", new ScalarValue(elapsedMs)),
            ];
        });
    }

    private static IConfiguration MinimumLevelSettings(IConfiguration configuration) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(configuration.GetSection("Serilog:MinimumLevel").AsEnumerable())
            .Build();
}
