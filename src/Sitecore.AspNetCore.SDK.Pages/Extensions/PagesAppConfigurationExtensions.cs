﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Extensions;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Interfaces;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Request;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Serialization;
using Sitecore.AspNetCore.SDK.Pages.Configuration;
using Sitecore.AspNetCore.SDK.Pages.GraphQL;
using Sitecore.AspNetCore.SDK.Pages.Middleware;
using Sitecore.AspNetCore.SDK.Pages.Request.Handlers.GraphQL;
using Sitecore.AspNetCore.SDK.RenderingEngine.Configuration;
using Sitecore.AspNetCore.SDK.RenderingEngine.Extensions;
using Sitecore.AspNetCore.SDK.RenderingEngine.Interfaces;

namespace Sitecore.AspNetCore.SDK.Pages.Extensions;

/// <summary>
/// Configuration helpers for Pages functionality.
/// </summary>
public static class PagesAppConfigurationExtensions
{
    /// <summary>
    /// Registers the Sitecore Experience Editor middleware into the <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="app">The instance of the <see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> so that additional calls can be chained.</returns>
    public static IApplicationBuilder UseSitecorePages(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        object? experienceEditorMarker = app.ApplicationServices.GetService(typeof(PagesMarkerService));
        if (experienceEditorMarker != null)
        {
            app.UseMiddleware<PageSetupMiddleware>();
            app.UseMiddleware<PagesRenderMiddleware>();
        }

        return app;
    }

    /// <summary>
    /// Adds the Sitecore Experience Editor support services to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="serviceBuilder">The <see cref="ISitecoreRenderingEngineBuilder" /> to add services to.</param>
    /// <param name="contextId">The ContextId for the environment being used.</param>
    /// <param name="options">Configures the <see cref="PagesOptions" /> options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static ISitecoreRenderingEngineBuilder WithSitecorePages(this ISitecoreRenderingEngineBuilder serviceBuilder, string contextId, Action<PagesOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(serviceBuilder);

        IServiceCollection services = serviceBuilder.Services;
        if (services.Any(s => s.ServiceType == typeof(PagesMarkerService)))
        {
            return serviceBuilder;
        }

        services.AddSingleton<PagesMarkerService>();
        services.AddSingleton<IGraphQLClientFactory>(new GraphQLClientFactory(contextId));

        if (options != null)
        {
            services.Configure(options);
        }

        services.Configure((Action<RenderingEngineOptions>)(renderingOptions =>
        {
            renderingOptions.MapToRequest((httpRequest, layoutRequest) =>
            {
                MapRequest(httpRequest, layoutRequest, "mode");
                MapRequest(httpRequest, layoutRequest, "sc_itemid");
                MapRequest(httpRequest, layoutRequest, "sc_version");
                MapRequest(httpRequest, layoutRequest, "sc_lang");
                MapRequest(httpRequest, layoutRequest, "sc_site");
                MapRequest(httpRequest, layoutRequest, "sc_layoutKind");
                MapRequest(httpRequest, layoutRequest, "secret");
                MapRequest(httpRequest, layoutRequest, "tenant_id");
                MapRequest(httpRequest, layoutRequest, "route");
            });
        }));

        return serviceBuilder;
    }

    /// <summary>
    /// Registers an HTTP request handler for the Sitecore layout service client.
    /// </summary>
    /// <param name="builder">The <see cref="ISitecoreLayoutClientBuilder"/> to configure.</param>
    /// <returns>The <see cref="ILayoutRequestHandlerBuilder{HttpLayoutRequestHandler}"/> so that additional calls can be chained.</returns>

    public static ISitecoreLayoutClientBuilder AddSitecorePagesHandler(
        this ISitecoreLayoutClientBuilder builder)
    {
        string name = Constants.LayoutClients.Pages;
        builder.AddHandler(name, sp
            => ActivatorUtilities.CreateInstance<GraphQLEditingServiceHandler>(
                sp,
                sp.GetRequiredService<IGraphQLClientFactory>(),
                sp.GetRequiredService<ISitecoreLayoutSerializer>(),
                sp.GetRequiredService<ILogger<GraphQLEditingServiceHandler>>()));

        return builder;
    }

    private static void MapRequest(HttpRequest httpRequest, SitecoreLayoutRequest layoutRequest, string paramName)
    {
        if (httpRequest.Query == null || !httpRequest.Query.ContainsKey(paramName))
        {
            return;
        }

        string[]? modeQueryValue = httpRequest.Query[paramName];
        if (modeQueryValue == null)
        {
            return;
        }

        layoutRequest.AddHeader(paramName, modeQueryValue);
    }
}