using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class SkillSectionViewModelTests
{
    [Fact]
    public void Constructor_NullResourcesService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () => new SkillSectionViewModel( null!, "title", [ "key1" ] ) );
    }

    [Fact]
    public void Constructor_NullTitleResourceKey_DefaultsToEmpty()
    {
        var lService = new ResourcesService();
        var lViewModel = new SkillSectionViewModel( lService, null, null );

        Assert.NotNull( lViewModel.TitleText );
    }

    [Fact]
    public void Constructor_NullItemResourceKeys_DefaultsToEmpty()
    {
        var lService = new ResourcesService();
        var lViewModel = new SkillSectionViewModel( lService, "title", null );

        Assert.NotNull( lViewModel.Items );
        Assert.Empty( lViewModel.Items );
    }

    [Fact]
    public void Constructor_FiltersBlanks()
    {
        var lService = new ResourcesService();
        var lViewModel = new SkillSectionViewModel( lService, "title", [ "key1", "", "  ", "key2" ] );

        Assert.Equal( 2, lViewModel.Items.Count );
    }

    [Fact]
    public void RefreshFromResources_RaisesPropertyChanged()
    {
        var lService = new ResourcesService();
        var lViewModel = new SkillSectionViewModel( lService, "title", [ "key1" ] );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lViewModel.RefreshFromResources();

        Assert.Contains( "TitleText", lRaisedProperties );
    }

    [Fact]
    public void LanguageChange_RaisesTitleTextPropertyChanged()
    {
        var lService = new ResourcesService();
        var lViewModel = new SkillSectionViewModel( lService, "title", [ "key1" ] );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "TitleText", lRaisedProperties );
    }
}
