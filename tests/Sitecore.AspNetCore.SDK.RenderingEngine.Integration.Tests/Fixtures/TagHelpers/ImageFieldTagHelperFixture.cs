﻿using System.Net;
using FluentAssertions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.TestHost;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Extensions;
using Sitecore.AspNetCore.SDK.RenderingEngine.Extensions;
using Sitecore.AspNetCore.SDK.TestData;
using Xunit;

// ReSharper disable StringLiteralTypo
namespace Sitecore.AspNetCore.SDK.RenderingEngine.Integration.Tests.Fixtures.TagHelpers;

public class ImageFieldTagHelperFixture : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpLayoutClientMessageHandler _mockClientHandler;
    private readonly Uri _layoutServiceUri = new("http://layout.service");

    public ImageFieldTagHelperFixture()
    {
        TestServerBuilder testHostBuilder = new();
        _mockClientHandler = new HttpLayoutClientMessageHandler();
        testHostBuilder
            .ConfigureServices(builder =>
            {
                builder
                    .AddSitecoreLayoutService()
                    .AddHttpHandler("mock", _ => new HttpClient(_mockClientHandler) { BaseAddress = _layoutServiceUri })
                    .AsDefaultHandler();
                builder.AddSitecoreRenderingEngine(options =>
                {
                    options
                        .AddModelBoundView<ComponentModels.ComponentWithImages>("Component-With-Images", "ComponentWithImages")
                        .AddViewComponent("Component-1", "Component1")
                        .AddModelBoundView<ComponentModels.Component2>("Component-2", "Component2")
                        .AddDefaultComponentRenderer();
                });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseSitecoreRenderingEngine();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapDefaultControllerRoute();
                });
            });

        _server = testHostBuilder.BuildServer(new Uri("http://localhost"));
    }

    [Fact]
    public async Task ImgTagHelper_GeneratedProperImageWithCustomAttributes()
    {
        // Arrange
        _mockClientHandler.Responses.Push(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(Serializer.Serialize(CannedResponses.PageWithPreview))
        });

        HttpClient client = _server.CreateClient();

        // Act
        string response = await client.GetStringAsync(new Uri("/", UriKind.Relative));

        HtmlDocument doc = new();
        doc.LoadHtml(response);
        HtmlNode? sectionNode = doc.DocumentNode.ChildNodes.First(n => n.HasClass("component-with-images"));

        // Assert
        // check scenario that ImageTagHelper render proper image tag with custom attributes.
        sectionNode.ChildNodes[5].OuterHtml.Should().Contain(TestConstants.SecondImageTestValue);
    }

    [Fact]
    public async Task ImgTagHelper_GeneratesImageTags()
    {
        // Arrange
        _mockClientHandler.Responses.Push(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(Serializer.Serialize(CannedResponses.PageWithPreview))
        });

        HttpClient client = _server.CreateClient();

        // Act
        string response = await client.GetStringAsync(new Uri("/", UriKind.Relative));

        HtmlDocument doc = new();
        doc.LoadHtml(response);
        HtmlNode? sectionNode = doc.DocumentNode.ChildNodes.First(n => n.HasClass("component-with-images"));

        // Assert
        // check that there is proper number of 'img' tags generated.
        sectionNode.ChildNodes.Count(n => n.Name.Equals("img", StringComparison.OrdinalIgnoreCase)).Should().Be(2);
    }

    [Fact]
    public async Task ImgTagHelper_GeneratedProperHtmlWithoutTagName()
    {
        // Arrange
        _mockClientHandler.Responses.Push(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(Serializer.Serialize(CannedResponses.PageWithPreview))
        });

        HttpClient client = _server.CreateClient();

        // Act
        string response = await client.GetStringAsync(new Uri("/", UriKind.Relative));

        HtmlDocument doc = new();
        doc.LoadHtml(response);
        HtmlNode? sectionNode = doc.DocumentNode.ChildNodes.First(n => n.HasClass("component-with-images"));

        // Assert
        // check that link will contain user provided link text.
        sectionNode.ChildNodes[1].OuterHtml.Should().Contain(TestConstants.ImageFieldValue);
    }

    [Fact]
    public async Task ImgTagHelper_GeneratesProperImageUrlIncludingImageParams()
    {
        // Arrange
        _mockClientHandler.Responses.Push(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(Serializer.Serialize(CannedResponses.PageWithPreview))
        });

        HttpClient client = _server.CreateClient();

        // Act
        string response = await client.GetStringAsync(new Uri("/", UriKind.Relative));

        HtmlDocument doc = new();
        doc.LoadHtml(response);
        HtmlNode? sectionNode = doc.DocumentNode.ChildNodes.First(n => n.HasClass("component-with-images"));
        HtmlNode? lastImage = sectionNode.ChildNodes.Last(n => n.Name.Equals("img", StringComparison.OrdinalIgnoreCase));

        // Assert
        // check that image url contains mw and mh parameters
        lastImage.Attributes.Should().Contain(a => a.Name == "src");
        lastImage.Attributes["src"].Value.Should().Contain("mw=100&amp;mh=50");
    }

    [Fact]
    public async Task ImgTagHelper_GeneratesProperEditableImageMarkupWithCustomProperties()
    {
        // Arrange
        _mockClientHandler.Responses.Push(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(Serializer.Serialize(CannedResponses.EditablePage))
        });

        HttpClient client = _server.CreateClient();

        // Act
        string response = await client.GetStringAsync(new Uri("/", UriKind.Relative));

        HtmlDocument doc = new();
        doc.LoadHtml(response);
        HtmlNode? sectionNode = doc.DocumentNode.ChildNodes.First(n => n.HasClass("component-1")).ChildNodes.First(n => n.HasClass("component-2"));

        // Assert
        // check that editable markup contains all custom params
        sectionNode.InnerHtml.Should().Contain("height=\"50\"");
        sectionNode.InnerHtml.Should().Contain("width=\"94\"");
        sectionNode.InnerHtml.Should().Contain("class=\"image1\"");
        sectionNode.InnerHtml.Should().Contain("alt=\"customAlt\"");
        sectionNode.InnerHtml.Should().Contain("src=\"/sitecore/shell/-/jssmedia/styleguide/data/media/img/sc_logo.png?mw=100&mh=50\"");
    }

    public void Dispose()
    {
        _mockClientHandler.Dispose();
        _server.Dispose();
        GC.SuppressFinalize(this);
    }
}