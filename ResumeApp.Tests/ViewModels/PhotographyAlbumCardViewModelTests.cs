using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class PhotographyAlbumCardViewModelTests
{
    [Fact]
    public void Constructor_NullResourcesService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () =>
            new PhotographyAlbumCardViewModel( null!, "title", "subtitle", "path/" ) );
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var lService = new ResourcesService();
        var lViewModel = new PhotographyAlbumCardViewModel( lService, "titleKey", "subtitleKey", "Resources/Photos/" );

        Assert.NotNull( lViewModel.TitleText );
        Assert.NotNull( lViewModel.SubtitleText );
    }

    [Fact]
    public void Constructor_NullTitleKey_DefaultsToEmpty()
    {
        var lService = new ResourcesService();
        var lViewModel = new PhotographyAlbumCardViewModel( lService, null, null, "path/" );

        Assert.NotNull( lViewModel.TitleText );
        Assert.NotNull( lViewModel.SubtitleText );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_ItemArray_RefreshesTexts()
    {
        var lService = new ResourcesService();
        var lViewModel = new PhotographyAlbumCardViewModel( lService, "titleKey", "subtitleKey", "path/" );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "TitleText", lRaisedProperties );
        Assert.Contains( "SubtitleText", lRaisedProperties );
    }

    [Fact]
    public void NormalizeFolderPath_AppendsTrailingSlash()
    {
        Assert.Equal( "Resources/Photos/", PhotographyAlbumCardViewModel.NormalizeFolderPath( "Resources/Photos" ) );
    }

    [Fact]
    public void NormalizeFolderPath_AlreadyHasSlash_NoDouble()
    {
        Assert.Equal( "Resources/Photos/", PhotographyAlbumCardViewModel.NormalizeFolderPath( "Resources/Photos/" ) );
    }

    [Fact]
    public void NormalizeFolderPath_BackslashToForward()
    {
        Assert.Equal( "Resources/Photos/", PhotographyAlbumCardViewModel.NormalizeFolderPath( @"Resources\Photos\" ) );
    }

    [Fact]
    public void NormalizeFolderPath_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, PhotographyAlbumCardViewModel.NormalizeFolderPath( null ) );
        Assert.Equal( string.Empty, PhotographyAlbumCardViewModel.NormalizeFolderPath( "" ) );
    }

    [Fact]
    public void NormalizeRelativePath_StandardPath()
    {
        Assert.Equal( "Resources/test.png", PhotographyAlbumCardViewModel.NormalizeRelativePath( @"Resources\test.png" ) );
    }

    [Fact]
    public void NormalizeRelativePath_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, PhotographyAlbumCardViewModel.NormalizeRelativePath( null ) );
        Assert.Equal( string.Empty, PhotographyAlbumCardViewModel.NormalizeRelativePath( "" ) );
    }

    [Fact]
    public void NormalizeRelativePathForPackUri_StandardPath()
    {
        string lResult = PhotographyAlbumCardViewModel.NormalizeRelativePathForPackUri( "Resources/test.png" );

        Assert.Equal( "Resources/test.png", lResult );
    }

    [Fact]
    public void NormalizeRelativePathForPackUri_EncodedPath_Unescapes()
    {
        string lResult = PhotographyAlbumCardViewModel.NormalizeRelativePathForPackUri( "Resources/My%20Photo.png" );

        Assert.Equal( "Resources/My Photo.png", lResult );
    }

    [Fact]
    public void NormalizeRelativePathForPackUri_Empty_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, PhotographyAlbumCardViewModel.NormalizeRelativePathForPackUri( "" ) );
    }

    [Fact]
    public void HasSupportedImageExtension_Supported_ReturnsTrue()
    {
        Assert.True( PhotographyAlbumCardViewModel.HasSupportedImageExtension( "photo.jpg" ) );
        Assert.True( PhotographyAlbumCardViewModel.HasSupportedImageExtension( "photo.jpeg" ) );
        Assert.True( PhotographyAlbumCardViewModel.HasSupportedImageExtension( "photo.png" ) );
        Assert.True( PhotographyAlbumCardViewModel.HasSupportedImageExtension( "photo.tiff" ) );
    }

    [Fact]
    public void HasSupportedImageExtension_Unsupported_ReturnsFalse()
    {
        Assert.False( PhotographyAlbumCardViewModel.HasSupportedImageExtension( "doc.pdf" ) );
        Assert.False( PhotographyAlbumCardViewModel.HasSupportedImageExtension( "noext" ) );
        Assert.False( PhotographyAlbumCardViewModel.HasSupportedImageExtension( "" ) );
        Assert.False( PhotographyAlbumCardViewModel.HasSupportedImageExtension( null ) );
    }

    [Fact]
    public void GetFileNameFromRelativePath_WithSlash_ReturnsFileName()
    {
        Assert.Equal( "photo.jpg", PhotographyAlbumCardViewModel.GetFileNameFromRelativePath( "Resources/Photos/photo.jpg" ) );
    }

    [Fact]
    public void GetFileNameFromRelativePath_NoSlash_ReturnsFullPath()
    {
        Assert.Equal( "photo.jpg", PhotographyAlbumCardViewModel.GetFileNameFromRelativePath( "photo.jpg" ) );
    }

    [Fact]
    public void GetFileNameFromRelativePath_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal( string.Empty, PhotographyAlbumCardViewModel.GetFileNameFromRelativePath( null ) );
        Assert.Equal( string.Empty, PhotographyAlbumCardViewModel.GetFileNameFromRelativePath( "" ) );
    }

    [Fact]
    public void GetCandidateFolderRelativePaths_NonPhotography_ReturnsSinglePath()
    {
        var lResults = PhotographyAlbumCardViewModel.GetCandidateFolderRelativePaths( "Resources/Other/" ).ToList();

        Assert.Single( lResults );
        Assert.Equal( "Resources/Other/", lResults[0] );
    }

    [Fact]
    public void GetCandidateFolderRelativePaths_PhotographyPath_ReturnsAlternate()
    {
        var lResults = PhotographyAlbumCardViewModel.GetCandidateFolderRelativePaths( "Resources/Photography/Album/" ).ToList();

        Assert.Equal( 2, lResults.Count );
        Assert.Equal( "Resources/Photography/Album/", lResults[0] );
        Assert.Equal( "Resources/Projects/Photography/Album/", lResults[1] );
    }

    [Fact]
    public void GetCandidateFolderRelativePaths_Empty_ReturnsEmpty()
    {
        Assert.Empty( PhotographyAlbumCardViewModel.GetCandidateFolderRelativePaths( "" ) );
    }

    [Fact]
    public void GetRandomIndex_ZeroOrOne_ReturnsZero()
    {
        Assert.Equal( 0, PhotographyAlbumCardViewModel.GetRandomIndex( 0 ) );
        Assert.Equal( 0, PhotographyAlbumCardViewModel.GetRandomIndex( 1 ) );
        Assert.Equal( 0, PhotographyAlbumCardViewModel.GetRandomIndex( -1 ) );
    }

    [Fact]
    public void GetRandomIndex_Positive_ReturnsInRange()
    {
        int lResult = PhotographyAlbumCardViewModel.GetRandomIndex( 10 );

        Assert.InRange( lResult, 0, 9 );
    }
}
