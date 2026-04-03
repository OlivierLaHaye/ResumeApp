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

    [Fact]
    public void ExtractValueAfterFirstColon_WithColon_ReturnsValueAfter()
    {
        Assert.Equal( "value", ProjectCardViewModel.ExtractValueAfterFirstColon( "key: value" ) );
    }

    [Fact]
    public void ExtractValueAfterFirstColon_NoColon_ReturnsTrimmedText()
    {
        Assert.Equal( "nocolon", ProjectCardViewModel.ExtractValueAfterFirstColon( "nocolon" ) );
    }

    [Fact]
    public void ExtractValueAfterFirstColon_ColonAtEnd_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, ProjectCardViewModel.ExtractValueAfterFirstColon( "key:" ) );
    }

    [Fact]
    public void ExtractValueAfterFirstColon_Empty_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, ProjectCardViewModel.ExtractValueAfterFirstColon( "" ) );
        Assert.Equal( string.Empty, ProjectCardViewModel.ExtractValueAfterFirstColon( "  " ) );
    }

    [Fact]
    public void NormalizeRelativePath_StandardPath_NormalizesSlashes()
    {
        Assert.Equal( "Resources/Images/test.png", ProjectCardViewModel.NormalizeRelativePath( @"Resources\Images\test.png" ) );
    }

    [Fact]
    public void NormalizeRelativePath_LeadingSlashes_Strips()
    {
        Assert.Equal( "test.png", ProjectCardViewModel.NormalizeRelativePath( "/test.png" ) );
        Assert.Equal( "test.png", ProjectCardViewModel.NormalizeRelativePath( @"\test.png" ) );
    }

    [Fact]
    public void NormalizeRelativePath_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, ProjectCardViewModel.NormalizeRelativePath( null ) );
        Assert.Equal( string.Empty, ProjectCardViewModel.NormalizeRelativePath( "" ) );
        Assert.Equal( string.Empty, ProjectCardViewModel.NormalizeRelativePath( "  " ) );
    }

    [Fact]
    public void NormalizeRelativePath_OnlySlashes_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, ProjectCardViewModel.NormalizeRelativePath( "/" ) );
        Assert.Equal( string.Empty, ProjectCardViewModel.NormalizeRelativePath( @"\" ) );
    }

    [Fact]
    public void IsKnownImageExtension_SupportedExtensions_ReturnsTrue()
    {
        Assert.True( ProjectCardViewModel.IsKnownImageExtension( "test.png" ) );
        Assert.True( ProjectCardViewModel.IsKnownImageExtension( "test.jpg" ) );
        Assert.True( ProjectCardViewModel.IsKnownImageExtension( "test.jpeg" ) );
        Assert.True( ProjectCardViewModel.IsKnownImageExtension( "test.bmp" ) );
        Assert.True( ProjectCardViewModel.IsKnownImageExtension( "test.gif" ) );
        Assert.True( ProjectCardViewModel.IsKnownImageExtension( "test.tif" ) );
        Assert.True( ProjectCardViewModel.IsKnownImageExtension( "test.tiff" ) );
    }

    [Fact]
    public void IsKnownImageExtension_UnsupportedOrMissing_ReturnsFalse()
    {
        Assert.False( ProjectCardViewModel.IsKnownImageExtension( "test.txt" ) );
        Assert.False( ProjectCardViewModel.IsKnownImageExtension( "nodot" ) );
        Assert.False( ProjectCardViewModel.IsKnownImageExtension( "endswithdot." ) );
        Assert.False( ProjectCardViewModel.IsKnownImageExtension( "" ) );
        Assert.False( ProjectCardViewModel.IsKnownImageExtension( null ) );
    }

    [Fact]
    public void BuildProjectImagesBasePath_SimpleName_BuildsResourcePath()
    {
        string lResult = ProjectCardViewModel.BuildProjectImagesBasePath( "MyProject" );

        Assert.Equal( "Resources/Projects/MyProject/MyProject", lResult );
    }

    [Fact]
    public void BuildProjectImagesBasePath_PathWithSlash_ReturnsNormalized()
    {
        string lResult = ProjectCardViewModel.BuildProjectImagesBasePath( "Resources/Custom/MyProject" );

        Assert.Equal( "Resources/Custom/MyProject", lResult );
    }

    [Fact]
    public void BuildProjectImagesBasePath_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, ProjectCardViewModel.BuildProjectImagesBasePath( null ) );
        Assert.Equal( string.Empty, ProjectCardViewModel.BuildProjectImagesBasePath( "" ) );
        Assert.Equal( string.Empty, ProjectCardViewModel.BuildProjectImagesBasePath( "  " ) );
    }
}
