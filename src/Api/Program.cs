// <copyright file="Program.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Config;
using Defra.Lis.Be4Fe.Api.Endpoints;
using Defra.Lis.Be4Fe.Api.Endpoints.Data;
using Defra.Lis.Be4Fe.Api.Endpoints.Health;
using Defra.Lis.Be4Fe.Api.Endpoints.Submission;
using Defra.Lis.Be4Fe.Api.Exceptions;
using Defra.Lis.Be4Fe.Api.Foundation.Submissions;
using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Api.Middleware;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Api.Utils;
using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Defra.Lis.Be4Fe.Api.Utils.Mongo;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Livestock.Sdk.Api.Strategies;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

public class Program
{
    protected Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var app = CreateWebApplication(args);
        await app.RunAsync();
    }

    [ExcludeFromCodeCoverage]
    private static WebApplication CreateWebApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{builder.Environment.EnvironmentName}.json",
                optional: true,
                reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        ConfigureBuilder(builder, builder.Configuration);

        var app = builder.Build();
        return SetupApplication(app);
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureBuilder(
        WebApplicationBuilder builder,
        IConfigurationRoot configuration)
    {
        // Configure logging to use the CDP Platform standards.
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks();
        builder.Host.UseSerilog(CdpLogging.Configuration);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddStrategyFramework();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        });

        var services = builder.Services;

        // Trust material must be loaded before anything creates outbound connections.
        services.LoadCustomTrustStoreFromEnvironment();

        services.AddValidation();

        // Calls from the cattle-register UI: an API key for now, AWS STS later (LREG-560).
        services.AddServiceToServiceAuthentication(configuration);

        // Require x-cdp-request-id on every /submission endpoint (Correlation ID standard) and propagate it downstream.
        services.AddTransient<CorrelationIdMiddleware>();
        ConfigureHeaderPropagation(services, configuration);
        ConfigureExternalDependencies(services, configuration);
        ConfigureMongo(services, configuration);
        ConfigureOpenApi(services);
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureHeaderPropagation(IServiceCollection services, IConfiguration configuration)
    {
        var traceHeader = configuration.GetValue<string>("TraceHeader");

        services.AddHeaderPropagation(options =>
        {
            if (!string.IsNullOrWhiteSpace(traceHeader))
            {
                options.Headers.Add(traceHeader);
            }
        });
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureExternalDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        // Cattle API options are validated when first used, so the service boots (and answers /health)
        // before the base URL and API key are configured.
        services.AddOptions<CattleApiOptions>()
            .Bind(configuration.GetRequiredSection(CattleApiOptions.SectionName));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<CattleApiOptions>>().Value);

        // Strategies SDK REST client, registered before the factory so it carries header propagation
        // (x-cdp-request-id) to the cattle API.
        services.AddHttpClient<IRestHttpClient, RestHttpClient>().AddHeaderPropagation();
        services.AddRestStrategyFactory<CattleHoldingRestClient>();
        services.AddScoped<ICattleHoldingClient, CattleHoldingRestClient>();

        // BFF framework submissions. The transfer to the cattle API is a stand-in that always fails until it is built.
        services.AddSingleton<ISubmissionRepository, MongoSubmissionRepository>();
        services.AddSingleton<ISubmissionTransfer, NotConfiguredSubmissionTransfer>();
        services.AddScoped<ISubmissionService, SubmissionService>();

        // Reference data for the UI. Dams and sires come from the cattle-on-holding search; the cattle API endpoint for
        // unallocated eartags is not defined yet, so its source is a stand-in that always answers 503 until it is built.
        services.AddOptions<UnallocatedEarTagOptions>()
            .Bind(configuration.GetSection(UnallocatedEarTagOptions.SectionName));
        services.AddSingleton<IUnallocatedEarTagSource, NotConfiguredUnallocatedEarTagSource>();
        services.AddScoped<IDataService, DataService>();
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureMongo(IServiceCollection services, IConfiguration configuration)
    {
        MongoExtensions.Register();
        MongoConventions.Register();

        services
            .AddOptions<MongoConfig>()
            .Bind(configuration.GetRequiredSection("Mongo"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureOpenApi(IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Cattle Register API",
                    Version = "v1",
                    Description = "BE4FE API surface for the cattle-register Node application: the BFF framework endpoints for birth submissions and the reference data the UI's forms read.",
                };

                return Task.CompletedTask;
            });
            options.AddApiKeySecurity();
        });
    }

    [ExcludeFromCodeCoverage]
    private static WebApplication SetupApplication(WebApplication app)
    {
        app.UseCdpRequestLogging();
        app.UseHeaderPropagation();
        app.UseExceptionHandler();
        app.UseRouting();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapOpenApi("/openapi/{documentName}.json").WithMetadata(new IgnoreCorrelationIdCheck());
        app.MapScalarApiReference().WithMetadata(new IgnoreCorrelationIdCheck());
        app.MapGet("/openapi", () => Results.Redirect("/openapi/v1.json"))
            .ExcludeFromDescription()
            .WithMetadata(new IgnoreCorrelationIdCheck());

        // Every /submission and /data endpoint requires AuthPolicies.ServiceToService and x-cdp-request-id; health and
        // OpenAPI stay anonymous and need no correlation header.
        app.UseHealthEndpoints();
        app.UseSubmissionEndpoints();
        app.UseDataEndpoints();

        return app;
    }
}
