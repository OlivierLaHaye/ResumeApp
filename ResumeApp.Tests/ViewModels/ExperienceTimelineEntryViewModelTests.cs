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
        ObservableCollection<string>? pAccomplishments = null,
        ResourcesService? pResourcesService = null,
        DateTime? pToday = null )
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
            pResourcesService: pResourcesService ?? CreateResourcesService(),
            pTodayProvider: pToday.HasValue ? () => pToday.Value : null );
    }

    private static ResourcesService CreateFrenchResourcesService()
    {
        var lResourcesService = new ResourcesService();
        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );
        return lResourcesService;
    }

    private static readonly DateTime sToday = new( 2026, 10, 2 );

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
    public void TechItems_SplitOnlyOnCommaAndSpace()
    {
        var lEntry = CreateEntry( pTechText: "A,B; C|D•E·F, G" );

        Assert.Equal( new[] { "A,B; C|D•E·F", "G" }, lEntry.TechItems.ToArray() );
    }

    [Fact]
    public void TechItems_KeepSlashInsideASingleChip()
    {
        var lEntry = CreateEntry( pTechText: "WPF, custom C# / WPF tools." );

        Assert.Equal( new[] { "WPF", "custom C# / WPF tools" }, lEntry.TechItems.ToArray() );
    }

    [Fact]
    public void TechItems_TrimTrailingPeriodFromEveryChip()
    {
        var lEntry = CreateEntry( pTechText: "C#., XAML. , Node.js." );

        Assert.Equal( new[] { "C#", "XAML", "Node.js" }, lEntry.TechItems.ToArray() );
    }

    [Fact]
    public void TechItems_FrenchLegacyTechTextKeepsCustomToolsAsOneChip()
    {
        var lEntry = CreateEntry( pTechText: "Adobe Photoshop, outils personnalisés en C# / WPF." );

        Assert.Equal( new[] { "Adobe Photoshop", "outils personnalisés en C# / WPF" }, lEntry.TechItems.ToArray() );
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
    public void DateRangeText_English_UsesIsoMonthsAndEnDash()
    {
        var lEntry = CreateEntry( pStartDate: new DateTime( 2020, 2, 1 ), pEndDate: new DateTime( 2024, 3, 1 ) );

        Assert.Equal( "2020-02 – 2024-03", lEntry.DateRangeText );
    }

    [Fact]
    public void DateRangeText_English_OpenEndUsesPresent()
    {
        var lEntry = CreateEntry( pStartDate: new DateTime( 2024, 3, 1 ), pEndDate: null );

        Assert.Equal( "2024-03 – Present", lEntry.DateRangeText );
    }

    [Fact]
    public void DateRangeText_French_UsesIsoMonthsAndEnDash()
    {
        var lEntry = CreateEntry(
            pStartDate: new DateTime( 2020, 2, 1 ),
            pEndDate: new DateTime( 2024, 3, 1 ),
            pResourcesService: CreateFrenchResourcesService() );

        Assert.Equal( "2020-02 – 2024-03", lEntry.DateRangeText );
    }

    [Fact]
    public void DateRangeText_French_OpenEndUsesLowercaseAujourdhui()
    {
        var lEntry = CreateEntry(
            pStartDate: new DateTime( 2024, 3, 1 ),
            pEndDate: null,
            pResourcesService: CreateFrenchResourcesService() );

        Assert.Equal( "2024-03 – aujourd'hui", lEntry.DateRangeText );
    }

    [Theory]
    [InlineData( 2024, 3, null, null, "2 yrs 7 mos", "2 ans 7 mois" )]
    [InlineData( 2020, 2, 2024, 3, "4 yrs 1 mo", "4 ans 1 mois" )]
    [InlineData( 2018, 5, 2020, 2, "1 yr 9 mos", "1 an 9 mois" )]
    [InlineData( 2017, 5, 2017, 8, "3 mos", "3 mois" )]
    [InlineData( 2010, 6, null, null, "16 yrs 4 mos", "16 ans 4 mois" )]
    [InlineData( 2025, 10, null, null, "1 yr", "1 an" )]
    [InlineData( 2024, 10, null, null, "2 yrs", "2 ans" )]
    [InlineData( 2026, 9, null, null, "1 mo", "1 mois" )]
    [InlineData( 2025, 9, 2026, 10, "1 yr 1 mo", "1 an 1 mois" )]
    [InlineData( 2026, 10, null, null, "", "" )]
    [InlineData( 2020, 5, 2020, 5, "", "" )]
    public void DurationText_CountsWholeMonthsAndFormatsPerLanguage(
        int pStartYear,
        int pStartMonth,
        int? pEndYear,
        int? pEndMonth,
        string pExpectedEnglish,
        string pExpectedFrench )
    {
        var lStart = new DateTime( pStartYear, pStartMonth, 1 );
        DateTime? lEnd = pEndYear.HasValue ? new DateTime( pEndYear.Value, pEndMonth!.Value, 1 ) : null;

        var lEnglish = CreateEntry( pStartDate: lStart, pEndDate: lEnd, pToday: sToday );
        var lFrench = CreateEntry( pStartDate: lStart, pEndDate: lEnd, pResourcesService: CreateFrenchResourcesService(), pToday: sToday );

        Assert.Equal( pExpectedEnglish, lEnglish.DurationText );
        Assert.Equal( pExpectedFrench, lFrench.DurationText );
    }

    [Fact]
    public void DurationText_IgnoresTheDayOfMonth()
    {
        var lEntry = CreateEntry(
            pStartDate: new DateTime( 2020, 1, 31 ),
            pEndDate: new DateTime( 2020, 3, 1 ),
            pToday: sToday );

        Assert.Equal( "2 mos", lEntry.DurationText );
    }

    [Fact]
    public void DurationText_EndBeforeStart_IsEmpty()
    {
        var lEntry = CreateEntry(
            pStartDate: new DateTime( 2020, 6, 1 ),
            pEndDate: new DateTime( 2019, 1, 1 ),
            pToday: sToday );

        Assert.Equal( string.Empty, lEntry.DurationText );
    }

    [Fact]
    public void DurationText_WithoutInjectedClock_UsesToday()
    {
        var lStart = new DateTime( DateTime.Today.Year - 1, DateTime.Today.Month, 1 );
        var lEntry = CreateEntry( pStartDate: lStart, pEndDate: null );

        Assert.Equal( "1 yr", lEntry.DurationText );
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

    [Fact]
    public void IsSelected_DefaultsToFalse()
    {
        var lEntry = CreateEntry();

        Assert.False( lEntry.IsSelected );
    }

    [Fact]
    public void IsSelected_SetTrue_ReturnsTrue()
    {
        var lEntry = CreateEntry();

        lEntry.IsSelected = true;

        Assert.True( lEntry.IsSelected );
    }

    [Fact]
    public void IsSelected_SetFalse_ReturnsFalse()
    {
        var lEntry = CreateEntry();
        lEntry.IsSelected = true;

        lEntry.IsSelected = false;

        Assert.False( lEntry.IsSelected );
    }

    [Fact]
    public void IsSelected_RaisesPropertyChanged()
    {
        var lEntry = CreateEntry();
        var lRaisedProperties = new List<string?>();
        lEntry.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lEntry.IsSelected = true;

        Assert.Contains( "IsSelected", lRaisedProperties );
    }

    [Fact]
    public void IsSelected_SameValue_DoesNotRaisePropertyChanged()
    {
        var lEntry = CreateEntry();
        lEntry.IsSelected = false;
        var lRaisedProperties = new List<string?>();
        lEntry.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lEntry.IsSelected = false;

        Assert.DoesNotContain( "IsSelected", lRaisedProperties );
    }
}
