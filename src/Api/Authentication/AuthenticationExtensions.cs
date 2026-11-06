// <copyright file="AuthenticationExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Registers service-to-service authentication and describes it in the OpenAPI document.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds the API key scheme and the <see cref="AuthPolicies.ServiceToService"/> policy that requires it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding the <c>ApiKeyAuthentication</c> section.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddServiceToServiceAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApiKeyAuthenticationOptions>(ApiKeyAuthenticationOptions.SchemeName)
            .Bind(configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));
        services.AddAuthentication()
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.SchemeName, null);
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.ServiceToService, policy => policy
                .AddAuthenticationSchemes(ApiKeyAuthenticationOptions.SchemeName)
                .RequireAuthenticatedUser());

        return services;
    }

    /// <summary>
    /// Requires <see cref="AuthPolicies.ServiceToService"/> on the endpoints and documents the 401 they can return.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint or group builder.</param>
    /// <returns>The supplied builder.</returns>
    public static TBuilder RequireServiceToServiceAuthorization<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(AuthPolicies.ServiceToService);
        builder.Add(endpoint => endpoint.Metadata.Add(
            new ProducesResponseTypeMetadata(StatusCodes.Status401Unauthorized, typeof(ProblemDetails), ["application/problem+json"])));
        return builder;
    }

    /// <summary>
    /// Declares the API key security scheme and marks every operation that requires authorisation with it, so the
    /// document (and Scalar) show which header to send.
    /// </summary>
    /// <param name="options">The OpenAPI options.</param>
    /// <returns>The supplied options.</returns>
    public static OpenApiOptions AddApiKeySecurity(this OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[ApiKeyAuthenticationOptions.SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = new ApiKeyAuthenticationOptions().HeaderName,
                Description = "Service-to-service API key (interim until AWS STS).",
            };
            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, _) =>
        {
            var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<IAuthorizeData>()
                .Any();
            if (requiresAuthorization)
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(ApiKeyAuthenticationOptions.SchemeName, context.Document)] = [],
                });
            }

            return Task.CompletedTask;
        });

        return options;
    }
}
