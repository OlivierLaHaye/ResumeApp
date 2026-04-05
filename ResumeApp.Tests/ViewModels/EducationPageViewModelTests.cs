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

    [Fact]
    public void BuildKeys_ValidPrefixAndCount_ReturnsKeys()
    {
        var lResult = EducationPageViewModel.BuildKeys( "Item", 2 ).ToList();

        Assert.Equal( 2, lResult.Count );
        Assert.Equal( "Item1", lResult[0] );
        Assert.Equal( "Item2", lResult[1] );
    }

    [Fact]
    public void BuildKeys_EmptyOrNullPrefix_ReturnsEmpty()
    {
        Assert.Empty( EducationPageViewModel.BuildKeys( "", 5 ) );
        Assert.Empty( EducationPageViewModel.BuildKeys( "  ", 5 ) );
    }

    [Fact]
    public void BuildKeys_ZeroOrNegativeCount_ReturnsEmpty()
    {
        Assert.Empty( EducationPageViewModel.BuildKeys( "Prefix", 0 ) );
        Assert.Empty( EducationPageViewModel.BuildKeys( "Prefix", -1 ) );
    }
}
