using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class EducationItemViewModelTests
{
    [Fact]
    public void Constructor_NullResourcesService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () => new EducationItemViewModel( null!, "prefix" ) );
    }

    [Fact]
    public void Constructor_NullPrefix_DefaultsToEmpty()
    {
        var lService = new ResourcesService();
        var lViewModel = new EducationItemViewModel( lService, null );

        Assert.NotNull( lViewModel.TitleText );
        Assert.NotNull( lViewModel.LocationDatesText );
        Assert.NotNull( lViewModel.NotesText );
    }

    [Fact]
    public void TitleText_AccessesResource()
    {
        var lService = new ResourcesService();
        var lViewModel = new EducationItemViewModel( lService, "TestPrefix" );

        Assert.NotNull( lViewModel.TitleText );
    }

    [Fact]
    public void HasNotes_WhenEmpty_ReturnsFalse()
    {
        var lService = new ResourcesService();
        var lViewModel = new EducationItemViewModel( lService, "NonExistent" );

        Assert.False( lViewModel.HasNotes );
    }

    [Fact]
    public void RefreshFromResources_RaisesPropertyChanged()
    {
        var lService = new ResourcesService();
        var lViewModel = new EducationItemViewModel( lService, "Test" );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lViewModel.RefreshFromResources();

        Assert.Contains( "TitleText", lRaisedProperties );
        Assert.Contains( "LocationDatesText", lRaisedProperties );
        Assert.Contains( "NotesText", lRaisedProperties );
        Assert.Contains( "HasNotes", lRaisedProperties );
    }

    [Fact]
    public void LanguageChange_TriggersRefresh()
    {
        var lService = new ResourcesService();
        var lViewModel = new EducationItemViewModel( lService, "Test" );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "TitleText", lRaisedProperties );
    }
}
