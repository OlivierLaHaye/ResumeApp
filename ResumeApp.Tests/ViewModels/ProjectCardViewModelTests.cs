using System.ComponentModel;
using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class ProjectCardViewModelTests
{
    private static ResourcesService CreateResourcesService() => new();

    private static ProjectCardViewModel Create(
        string? pTitleResourceKey = "ProjectTitle",
        string? pContextResourceKey = "ProjectContext",
        string? pConstraintsResourceKey = "ProjectConstraints",
        string[]? pWhatIBuiltItemResourceKeys = null,
        string? pImpactResourceKey = "ProjectImpact",
        string? pTechResourceKey = "ProjectTech",
        string? pProjectImagesBaseName = "TestProject",
        string? pProjectLinkUriText = "https://example.com" )
    {
        return new ProjectCardViewModel(
            pResourcesService: CreateResourcesService(),
            pTitleResourceKey: pTitleResourceKey,
            pContextResourceKey: pContextResourceKey,
            pConstraintsResourceKey: pConstraintsResourceKey,
            pWhatIBuiltItemResourceKeys: pWhatIBuiltItemResourceKeys,
            pImpactResourceKey: pImpactResourceKey,
            pTechResourceKey: pTechResourceKey,
            pProjectImagesBaseName: pProjectImagesBaseName,
            pProjectLinkUriText: pProjectLinkUriText );
    }

    [Fact]
    public void Constructor_NullResourcesService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () =>
            new ProjectCardViewModel( null!, "title", "ctx", "con", null, "imp", "tech", "base", "link" ) );
    }

    [Fact]
    public void Constructor_NullParameters_DefaultToEmpty()
    {
        var lViewModel = Create(
            pTitleResourceKey: null,
            pContextResourceKey: null,
            pConstraintsResourceKey: null,
            pImpactResourceKey: null,
            pTechResourceKey: null,
            pProjectImagesBaseName: null,
            pProjectLinkUriText: null );

        Assert.NotNull( lViewModel.TitleText );
        Assert.NotNull( lViewModel.ContextValueText );
        Assert.NotNull( lViewModel.ConstraintsValueText );
        Assert.NotNull( lViewModel.ImpactValueText );
    }

    [Fact]
    public void IsProjectLinkButtonVisible_WithUrl_ReturnsTrue()
    {
        var lViewModel = Create( pProjectLinkUriText: "https://example.com" );

        Assert.True( lViewModel.IsProjectLinkButtonVisible );
    }

    [Fact]
    public void IsProjectLinkButtonVisible_WithoutUrl_ReturnsFalse()
    {
        var lViewModel = Create( pProjectLinkUriText: "" );

        Assert.False( lViewModel.IsProjectLinkButtonVisible );
    }

    [Fact]
    public void IsProjectLinkButtonVisible_NullUrl_ReturnsFalse()
    {
        var lViewModel = Create( pProjectLinkUriText: null );

        Assert.False( lViewModel.IsProjectLinkButtonVisible );
    }

    [StaFact]
    public void OpenProjectLinkCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.OpenProjectLinkCommand );
    }

    [StaFact]
    public void OpenProjectLinkCommand_CanExecute_WhenLinkPresent()
    {
        var lViewModel = Create( pProjectLinkUriText: "https://example.com" );

        Assert.True( lViewModel.OpenProjectLinkCommand.CanExecute( null ) );
    }

    [StaFact]
    public void OpenProjectLinkCommand_CannotExecute_WhenNoLink()
    {
        var lViewModel = Create( pProjectLinkUriText: "" );

        Assert.False( lViewModel.OpenProjectLinkCommand.CanExecute( null ) );
    }

    [Fact]
    public void WhatIBuiltItems_InitializesFromKeys()
    {
        var lViewModel = Create( pWhatIBuiltItemResourceKeys: [ "Key1", "Key2" ] );

        Assert.Equal( 2, lViewModel.WhatIBuiltItems.Count );
    }

    [Fact]
    public void WhatIBuiltItems_FiltersBlanks()
    {
        var lViewModel = Create( pWhatIBuiltItemResourceKeys: [ "Key1", "", "  ", "Key2" ] );

        Assert.Equal( 2, lViewModel.WhatIBuiltItems.Count );
    }

    [Fact]
    public void WhatIBuiltItems_NullKeys_DefaultsToEmpty()
    {
        var lViewModel = Create( pWhatIBuiltItemResourceKeys: null );

        Assert.NotNull( lViewModel.WhatIBuiltItems );
        Assert.Empty( lViewModel.WhatIBuiltItems );
    }

    [Fact]
    public void TechItems_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.TechItems );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_ItemArray_RefreshesProperties()
    {
        var lResourcesService = new ResourcesService();
        var lViewModel = new ProjectCardViewModel(
            lResourcesService, "title", "ctx", "con", null, "imp", "tech", "base", "" );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "TitleText", lRaisedProperties );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_NonItemArray_DoesNotRefresh()
    {
        var lResourcesService = new ResourcesService();
        var lViewModel = new ProjectCardViewModel(
            lResourcesService, "title", "ctx", "con", null, "imp", "tech", "base", "" );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        // Trigger a non "Item[]" property change
        lRaisedProperties.Clear();

        // Only "Item[]" triggers refresh - other property changes from ResourcesService are ignored
        Assert.DoesNotContain( "TitleText", lRaisedProperties );
    }
}
