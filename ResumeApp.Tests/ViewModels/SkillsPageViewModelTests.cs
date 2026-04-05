using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class SkillsPageViewModelTests
{
    [Fact]
    public void Constructor_InitializesSections()
    {
        var lViewModel = new SkillsPageViewModel( new ResourcesService(), new ThemeService() );

        Assert.NotNull( lViewModel.Sections );
        Assert.Equal( 4, lViewModel.Sections.Count );
    }

    [Fact]
    public void PageTitleText_ReturnsValue()
    {
        var lViewModel = new SkillsPageViewModel( new ResourcesService(), new ThemeService() );

        Assert.NotNull( lViewModel.PageTitleText );
    }

    [Fact]
    public void PageSubtitleText_ReturnsValue()
    {
        var lViewModel = new SkillsPageViewModel( new ResourcesService(), new ThemeService() );

        Assert.NotNull( lViewModel.PageSubtitleText );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_RaisesPropertyChanged()
    {
        var lResourcesService = new ResourcesService();
        var lViewModel = new SkillsPageViewModel( lResourcesService, new ThemeService() );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "PageTitleText", lRaisedProperties );
        Assert.Contains( "PageSubtitleText", lRaisedProperties );
    }

    [Fact]
    public void BuildKeys_ValidPrefixAndCount_ReturnsKeys()
    {
        var lResult = SkillsPageViewModel.BuildKeys( "Item", 3 );

        Assert.Equal( 3, lResult.Length );
        Assert.Equal( "Item1", lResult[0] );
    }

    [Fact]
    public void BuildKeys_EmptyPrefix_ReturnsEmpty()
    {
        Assert.Empty( SkillsPageViewModel.BuildKeys( "", 5 ) );
        Assert.Empty( SkillsPageViewModel.BuildKeys( "  ", 5 ) );
    }

    [Fact]
    public void BuildKeys_ZeroOrNegativeCount_ReturnsEmpty()
    {
        Assert.Empty( SkillsPageViewModel.BuildKeys( "Prefix", 0 ) );
        Assert.Empty( SkillsPageViewModel.BuildKeys( "Prefix", -1 ) );
    }
}
