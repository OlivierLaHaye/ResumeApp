using System.Globalization;
using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

public sealed class ResourcesServiceTests
{
    [Fact]
    public void Constructor_DefaultCulture_IsEnCA()
    {
        var lService = new ResourcesService();

        Assert.Equal( "en-CA", lService.ActiveCulture.Name );
    }

    [Fact]
    public void ActiveLanguageDisplayName_ReturnsNonEmpty()
    {
        var lService = new ResourcesService();

        string lDisplayName = lService.ActiveLanguageDisplayName;

        Assert.NotNull( lDisplayName );
        Assert.NotEmpty( lDisplayName );
    }

    [Fact]
    public void Indexer_NullKey_ReturnsEmpty()
    {
        var lService = new ResourcesService();

        string lResult = lService[ null! ];

        Assert.Equal( string.Empty, lResult );
    }

    [Fact]
    public void Indexer_EmptyKey_ReturnsEmpty()
    {
        var lService = new ResourcesService();

        string lResult = lService[ "" ];

        Assert.Equal( string.Empty, lResult );
    }

    [Fact]
    public void Indexer_WhitespaceKey_ReturnsEmpty()
    {
        var lService = new ResourcesService();

        string lResult = lService[ "   " ];

        Assert.Equal( string.Empty, lResult );
    }

    [Fact]
    public void Indexer_UnknownKey_ReturnsEmpty()
    {
        var lService = new ResourcesService();

        string lResult = lService[ "NonExistentResourceKey_12345" ];

        Assert.Equal( string.Empty, lResult );
    }

    [Fact]
    public void SetLanguage_FrenchCanada_ChangesCulture()
    {
        var lService = new ResourcesService();

        lService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Equal( "fr-CA", lService.ActiveCulture.Name );
    }

    [Fact]
    public void SetLanguage_EnglishCanada_ChangesCulture()
    {
        var lService = new ResourcesService();
        lService.SetLanguage( AppLanguage.FrenchCanada );

        lService.SetLanguage( AppLanguage.EnglishCanada );

        Assert.Equal( "en-CA", lService.ActiveCulture.Name );
    }

    [Fact]
    public void SetLanguage_SameLanguage_DoesNotRaisePropertyChanged()
    {
        var lService = new ResourcesService();
        int lRaiseCount = 0;
        lService.PropertyChanged += ( _, _ ) => lRaiseCount++;

        lService.SetLanguage( AppLanguage.EnglishCanada );

        Assert.Equal( 0, lRaiseCount );
    }

    [Fact]
    public void SetLanguage_DifferentLanguage_RaisesPropertyChanged()
    {
        var lService = new ResourcesService();
        var lRaisedProperties = new List<string?>();
        lService.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "ActiveCulture", lRaisedProperties );
        Assert.Contains( "ActiveLanguageDisplayName", lRaisedProperties );
        Assert.Contains( "Item[]", lRaisedProperties );
    }

    [Fact]
    public void Initialize_DoesNotThrow()
    {
        var lService = new ResourcesService();

        var lException = Record.Exception( () => lService.Initialize() );

        Assert.Null( lException );
    }
}
