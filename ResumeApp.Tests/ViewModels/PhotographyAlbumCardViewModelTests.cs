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
}
