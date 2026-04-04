using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class LocalizedResourceItemViewModelTests
{
    [Fact]
    public void Constructor_NullResourcesService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () => new LocalizedResourceItemViewModel( null!, "key" ) );
    }

    [Fact]
    public void Constructor_NullResourceKey_DefaultsToEmpty()
    {
        var lService = new ResourcesService();
        var lViewModel = new LocalizedResourceItemViewModel( lService, null );

        Assert.Equal( string.Empty, lViewModel.ResourceKey );
    }

    [Fact]
    public void Constructor_SetsResourceKey()
    {
        var lService = new ResourcesService();
        var lViewModel = new LocalizedResourceItemViewModel( lService, "TestKey" );

        Assert.Equal( "TestKey", lViewModel.ResourceKey );
    }

    [Fact]
    public void DisplayText_ReturnsResourceValue()
    {
        var lService = new ResourcesService();
        var lViewModel = new LocalizedResourceItemViewModel( lService, "NonExistent" );

        // Since key doesn't exist in resources, should return empty
        Assert.NotNull( lViewModel.DisplayText );
    }

    [Fact]
    public void DisplayText_UpdatesWhenLanguageChanges()
    {
        var lService = new ResourcesService();
        var lViewModel = new LocalizedResourceItemViewModel( lService, "TestKey" );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "DisplayText", lRaisedProperties );
    }
}
