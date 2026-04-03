using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class EducationPageViewModelTests
{
    [Fact]
    public void Constructor_InitializesItems()
    {
        var lViewModel = new EducationPageViewModel( new ResourcesService(), new ThemeService() );

        Assert.NotNull( lViewModel.Items );
        Assert.Equal( 2, lViewModel.Items.Count );
    }

    [Fact]
    public void PageTitleText_ReturnsValue()
    {
        var lViewModel = new EducationPageViewModel( new ResourcesService(), new ThemeService() );

        Assert.NotNull( lViewModel.PageTitleText );
    }

    [Fact]
    public void PageSubtitleText_ReturnsValue()
    {
        var lViewModel = new EducationPageViewModel( new ResourcesService(), new ThemeService() );

        Assert.NotNull( lViewModel.PageSubtitleText );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_RaisesPropertyChanged()
    {
        var lResourcesService = new ResourcesService();
        var lViewModel = new EducationPageViewModel( lResourcesService, new ThemeService() );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "PageTitleText", lRaisedProperties );
        Assert.Contains( "PageSubtitleText", lRaisedProperties );
    }
}
