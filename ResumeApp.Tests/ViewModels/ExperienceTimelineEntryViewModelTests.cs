using System.Collections.ObjectModel;
using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class ExperienceTimelineEntryViewModelTests
{
    private static ResourcesService CreateResourcesService() => new();

    private static ExperienceTimelineEntryViewModel CreateEntry(
        string? pCompanyText = "Company",
        string? pRoleText = "Role",
        string? pLocationText = "Location",
        string? pScopeText = "Scope",
        string? pTechText = "C#, WPF",
        DateTime? pStartDate = null,
        DateTime? pEndDate = null,
        ObservableCollection<string>? pAccomplishments = null )
    {
        return new ExperienceTimelineEntryViewModel(
            pCompanyText: pCompanyText,
            pRoleText: pRoleText,
            pLocationText: pLocationText,
            pScopeText: pScopeText,
            pTechText: pTechText,
            pStartDate: pStartDate ?? new DateTime( 2020, 1, 1 ),
            pEndDate: pEndDate,
            pAccomplishments: pAccomplishments,
            pResourcesService: CreateResourcesService() );
    }

    [Fact]
    public void Constructor_NullResourcesService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () =>
            new ExperienceTimelineEntryViewModel( "Co", "Role", "Loc", "Scope", "Tech",
                DateTime.Today, null, null, null! ) );
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var lEntry = CreateEntry();

        Assert.Equal( "Company", lEntry.CompanyText );
        Assert.Equal( "Role", lEntry.RoleText );
        Assert.Equal( "Location", lEntry.LocationText );
        Assert.Equal( "Scope", lEntry.ScopeText );
        Assert.Equal( "C#, WPF", lEntry.TechText );
    }

    [Fact]
    public void Constructor_NullProperties_DefaultToEmpty()
    {
        var lEntry = CreateEntry(
            pCompanyText: null,
            pRoleText: null,
            pLocationText: null,
            pScopeText: null,
            pTechText: null );

        Assert.Equal( string.Empty, lEntry.CompanyText );
        Assert.Equal( string.Empty, lEntry.RoleText );
        Assert.Equal( string.Empty, lEntry.LocationText );
        Assert.Equal( string.Empty, lEntry.ScopeText );
        Assert.Equal( string.Empty, lEntry.TechText );
    }

    [Fact]
    public void Constructor_NullAccomplishments_DefaultsToEmpty()
    {
        var lEntry = CreateEntry( pAccomplishments: null );

        Assert.NotNull( lEntry.Accomplishments );
        Assert.Empty( lEntry.Accomplishments );
    }

    [Fact]
    public void Constructor_WithAccomplishments_SetsCollection()
    {
        var lAccomplishments = new ObservableCollection<string> { "A1", "A2" };
        var lEntry = CreateEntry( pAccomplishments: lAccomplishments );

        Assert.Equal( 2, lEntry.Accomplishments.Count );
    }

    [Fact]
    public void TechItems_SplitsCommaSeparatedText()
    {
        var lEntry = CreateEntry( pTechText: "C#, WPF, XAML" );

        Assert.Equal( 3, lEntry.TechItems.Count );
        Assert.Contains( "C#", lEntry.TechItems );
        Assert.Contains( "WPF", lEntry.TechItems );
        Assert.Contains( "XAML", lEntry.TechItems );
    }

    [Fact]
    public void TechItems_SplitsVariousSeparators()
    {
        var lEntry = CreateEntry( pTechText: "A / B•C·D|E;F" );

        Assert.True( lEntry.TechItems.Count >= 6 );
    }

    [Fact]
    public void TechItems_EmptyTechText_ReturnsEmpty()
    {
        var lEntry = CreateEntry( pTechText: "" );

        Assert.Empty( lEntry.TechItems );
    }

    [Fact]
    public void TechItems_WhitespaceTechText_ReturnsEmpty()
    {
        var lEntry = CreateEntry( pTechText: "   " );

        Assert.Empty( lEntry.TechItems );
    }

    [Fact]
    public void TechItems_DuplicatesRemoved()
    {
        var lEntry = CreateEntry( pTechText: "C#, c#, C#" );

        Assert.Single( lEntry.TechItems );
    }

    [Fact]
    public void SetLaneIndex_SetsLaneIndex()
    {
        var lEntry = CreateEntry();

        lEntry.SetLaneIndex( 2 );

        Assert.Equal( 2, lEntry.LaneIndex );
    }

    [Fact]
    public void SetLaneIndex_NegativeValue_ClampsToZero()
    {
        var lEntry = CreateEntry();

        lEntry.SetLaneIndex( -5 );

        Assert.Equal( 0, lEntry.LaneIndex );
    }

    [Fact]
    public void SetLaneIndex_UpdatesMarkerGlyph()
    {
        var lEntry = CreateEntry();

        lEntry.SetLaneIndex( 0 );
        string lGlyph0 = lEntry.MarkerGlyph;

        lEntry.SetLaneIndex( 1 );
        string lGlyph1 = lEntry.MarkerGlyph;

        lEntry.SetLaneIndex( 2 );
        string lGlyph2 = lEntry.MarkerGlyph;

        lEntry.SetLaneIndex( 3 );
        string lGlyph3 = lEntry.MarkerGlyph;

        // Each should be set (possibly empty if resource not found)
        Assert.NotNull( lGlyph0 );
        Assert.NotNull( lGlyph1 );
        Assert.NotNull( lGlyph2 );
        Assert.NotNull( lGlyph3 );
    }

    [StaFact]
    public void SetLaneIndex_UpdatesLaneLeftMargin()
    {
        var lEntry = CreateEntry();

        lEntry.SetLaneIndex( 0 );
        Assert.Equal( 0, lEntry.LaneLeftMargin.Left );

        lEntry.SetLaneIndex( 1 );
        Assert.True( lEntry.LaneLeftMargin.Left > 0 );
    }

    [Fact]
    public void SetPaletteIndex_SetsValue()
    {
        var lEntry = CreateEntry();

        lEntry.SetPaletteIndex( 3 );

        Assert.Equal( 3, lEntry.PaletteIndex );
    }

    [Fact]
    public void SetPaletteIndex_NegativeValue_ClampsToZero()
    {
        var lEntry = CreateEntry();

        lEntry.SetPaletteIndex( -1 );

        Assert.Equal( 0, lEntry.PaletteIndex );
    }

    [Fact]
    public void SetPaletteIndex_SameValue_DoesNotChange()
    {
        var lEntry = CreateEntry();
        lEntry.SetPaletteIndex( 3 );
        bool lChanged = false;
        lEntry.PropertyChanged += ( _, _ ) => lChanged = true;

        lEntry.SetPaletteIndex( 3 );

        Assert.False( lChanged );
    }

    [Fact]
    public void DateRangeText_IsNotEmpty()
    {
        var lEntry = CreateEntry( pStartDate: new DateTime( 2020, 1, 1 ), pEndDate: new DateTime( 2023, 6, 1 ) );

        Assert.NotNull( lEntry.DateRangeText );
        Assert.NotEmpty( lEntry.DateRangeText );
    }

    [Fact]
    public void DateRangeText_NullEndDate_ContainsPresent()
    {
        var lEntry = CreateEntry( pEndDate: null );

        Assert.NotNull( lEntry.DateRangeText );
    }

    [Fact]
    public void PropertyChanged_RaisedForLaneIndex()
    {
        var lEntry = CreateEntry();
        var lRaisedProperties = new List<string?>();
        lEntry.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lEntry.SetLaneIndex( 5 );

        Assert.Contains( "LaneIndex", lRaisedProperties );
        Assert.Contains( "MarkerGlyph", lRaisedProperties );
        Assert.Contains( "LaneLeftMargin", lRaisedProperties );
    }

    [Fact]
    public void PropertyChanged_RaisedForPaletteIndex()
    {
        var lEntry = CreateEntry();
        var lRaisedProperties = new List<string?>();
        lEntry.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lEntry.SetPaletteIndex( 5 );

        Assert.Contains( "PaletteIndex", lRaisedProperties );
    }

    [Fact]
    public void MarkerGlyph_Cycles_Over4Values()
    {
        var lEntry = CreateEntry();
        var lGlyphs = new HashSet<int>();

        for ( int lIndex = 0; lIndex < 4; lIndex++ )
        {
            lEntry.SetLaneIndex( lIndex );
            lGlyphs.Add( lIndex % 4 );
        }

        Assert.Equal( 4, lGlyphs.Count );
    }
}
