﻿using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.WebActions.AasxLinkCreation;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace AasxServerBlazor.Tests.WebActions.AasxLinkCreation;

public class ExternalLinkCreatorTests
{
    private readonly Fixture _fixture;

    public ExternalLinkCreatorTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TryGetExternalLink_ShouldReturnFalse_ForNullOrWhiteSpace(string tag)
    {
        // Arrange
        var selectedNode = new TreeItem {Tag = tag};
        var externalLinkCreator = new ExternalLinkCreator();

        // Act
        var result = externalLinkCreator.TryGetExternalLink(selectedNode, out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetExternalLink_ShouldReturnTrue_ForHttpUrl()
    {
        // Arrange
        const string url = "https://example.com";
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        file.Value = url;
        var selectedNode = new TreeItem {Tag = file};
        var externalLinkCreator = new ExternalLinkCreator();

        // Act
        var result = externalLinkCreator.TryGetExternalLink(selectedNode, out var externalUrl);

        // Assert
        result.Should().BeTrue();
        externalUrl.Should().Be(url);
    }

    [Fact]
    public void TryGetExternalLink_ShouldReturnFalse_ForNonHttpNonAasxUrl()
    {
        // Arrange
        const string url = "ftp://example.com";
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        file.Value = url;
        var selectedNode = new TreeItem {Tag = file};
        var externalLinkCreator = new ExternalLinkCreator();

        // Act
        var result = externalLinkCreator.TryGetExternalLink(selectedNode, out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetExternalLink_ShouldReturnFalse_ForAasxUrlNotStartingWithSlash()
    {
        // Arrange
        const string urlWithoutLeadingSlash = "aasx/path";
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        file.Value = urlWithoutLeadingSlash;
        var selectedNode = new TreeItem {Tag = file};
        var externalLinkCreator = new ExternalLinkCreator();

        // Act
        var result = externalLinkCreator.TryGetExternalLink(selectedNode, out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetExternalLink_ShouldReturnFalse_ForNullOrEmptySubmodelIdOrSubmodelElementPath()
    {
        // Arrange
        var selectedNode = new TreeItem {Tag = _fixture.Create<ISubmodelElement>()};
        var externalLinkCreator = new ExternalLinkCreator();

        // Act
        var result = externalLinkCreator.TryGetExternalLink(selectedNode, out _);

        // Assert
        result.Should().BeFalse();
    }
}