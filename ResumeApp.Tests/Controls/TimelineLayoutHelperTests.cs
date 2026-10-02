using System.Globalization;
using ResumeApp.AttachedProperties;
using ResumeApp.Controls;
using Xunit;

namespace ResumeApp.Tests.Controls;

public sealed class TimelineLayoutHelperTests
{
    private const string Separator = " \u00B7 ";

    private static readonly DateTime sToday = new( 2026, 10, 2 );

    private static TimelineSpan[] CreateCareerSpans() =>
    [
        new( new DateTime( 2024, 3, 1 ), sToday ),
        new( new DateTime( 2020, 2, 1 ), new DateTime( 2024, 3, 1 ) ),
        new( new DateTime( 2018, 5, 1 ), new DateTime( 2020, 2, 1 ) ),
        new( new DateTime( 2017, 5, 1 ), new DateTime( 2017, 8, 1 ) ),
        new( new DateTime( 2010, 6, 1 ), sToday )
    ];

    private static double FakeMeasure( string pText, bool pIsBold ) => pText.Length * ( pIsBold ? 8.0 : 7.0 );

    [Fact]
    public void AssignStableLanes_CurrentCareer_UsesTwoLanesWithEmploymentFirst()
    {
        var lLanes = TimelineLayoutHelper.AssignStableLanes( CreateCareerSpans(), out var lLaneCount );

        Assert.Equal( 2, lLaneCount );
        Assert.Equal( [ 0, 0, 0, 0, 1 ], lLanes );
    }

    [Fact]
    public void AssignStableLanes_IsIndependentOfInputOrder()
    {
        var lSpans = CreateCareerSpans().Reverse().ToArray();

        var lLanes = TimelineLayoutHelper.AssignStableLanes( lSpans, out var lLaneCount );

        Assert.Equal( 2, lLaneCount );
        Assert.Equal( [ 1, 0, 0, 0, 0 ], lLanes );
    }

    [Fact]
    public void AssignStableLanes_TouchingSpansShareALane_OverlappingSpansDoNot()
    {
        TimelineSpan[] lSpans =
        [
            new( new DateTime( 2020, 1, 1 ), new DateTime( 2021, 1, 1 ) ),
            new( new DateTime( 2021, 1, 1 ), new DateTime( 2022, 1, 1 ) ),
            new( new DateTime( 2021, 6, 1 ), new DateTime( 2023, 1, 1 ) )
        ];

        var lLanes = TimelineLayoutHelper.AssignStableLanes( lSpans, out var lLaneCount );

        Assert.Equal( 2, lLaneCount );
        Assert.Equal( lLanes[ 0 ], lLanes[ 1 ] );
        Assert.NotEqual( lLanes[ 1 ], lLanes[ 2 ] );
        Assert.Equal( 1, lLanes[ 0 ] );
        Assert.Equal( 0, lLanes[ 2 ] );
    }

    [Fact]
    public void AssignStableLanes_InvalidSpan_IsSkipped()
    {
        TimelineSpan[] lSpans =
        [
            new( new DateTime( 2030, 1, 1 ), new DateTime( 2020, 1, 1 ) ),
            new( new DateTime( 2020, 1, 1 ), new DateTime( 2021, 1, 1 ) )
        ];

        var lLanes = TimelineLayoutHelper.AssignStableLanes( lSpans, out var lLaneCount );

        Assert.Equal( 1, lLaneCount );
        Assert.Equal( [ -1, 0 ], lLanes );
    }

    [Fact]
    public void AssignStableLanes_NoSpans_ReturnsZeroLanes()
    {
        var lLanes = TimelineLayoutHelper.AssignStableLanes( [], out var lLaneCount );

        Assert.Empty( lLanes );
        Assert.Equal( 0, lLaneCount );
    }

    [Fact]
    public void FindTouchingNeighbors_CurrentCareer_LinksOnlyConsecutiveRolesOfTheEmploymentLane()
    {
        var lSpans = CreateCareerSpans();
        var lLanes = TimelineLayoutHelper.AssignStableLanes( lSpans, out _ );

        TimelineLayoutHelper.FindTouchingNeighbors( lSpans, lLanes, out var lPrevious, out var lNext );

        Assert.Equal( [ 1, 2, -1, -1, -1 ], lPrevious );
        Assert.Equal( [ -1, 0, 1, -1, -1 ], lNext );
    }

    [Theory]
    [InlineData( LayoutSizeClass.Compact, 106.0 )]
    [InlineData( LayoutSizeClass.Regular, 136.0 )]
    [InlineData( LayoutSizeClass.Wide, 148.0 )]
    public void ComputeHeight_TwoLanes_FollowsSizeClassTable( LayoutSizeClass pSizeClass, double pExpectedHeight )
    {
        var lGeometry = TimelineLayoutHelper.GetGeometry( pSizeClass );

        Assert.Equal( pExpectedHeight, TimelineLayoutHelper.ComputeHeight( lGeometry, 2 ) );
    }

    [Theory]
    [InlineData( LayoutSizeClass.Compact )]
    [InlineData( LayoutSizeClass.Regular )]
    [InlineData( LayoutSizeClass.Wide )]
    public void ComputeHeight_GrowsByOneLanePitchPerLane( LayoutSizeClass pSizeClass )
    {
        var lGeometry = TimelineLayoutHelper.GetGeometry( pSizeClass );

        var lOneLane = TimelineLayoutHelper.ComputeHeight( lGeometry, 1 );
        var lThreeLanes = TimelineLayoutHelper.ComputeHeight( lGeometry, 3 );

        Assert.Equal( lGeometry.LanePitch * 2.0, lThreeLanes - lOneLane );
    }

    [Theory]
    [InlineData( LayoutSizeClass.Compact )]
    [InlineData( LayoutSizeClass.Regular )]
    [InlineData( LayoutSizeClass.Wide )]
    public void ComputePillHeight_FitsInsideTheLabelRow( LayoutSizeClass pSizeClass )
    {
        var lGeometry = TimelineLayoutHelper.GetGeometry( pSizeClass );
        var lBaseline = TimelineLayoutHelper.ComputeBaselineY( lGeometry, 2 );
        var lHeight = TimelineLayoutHelper.ComputeHeight( lGeometry, 2 );

        var lPillBottom = lBaseline + 0.5 + TimelineLayoutHelper.PillTopGap + TimelineLayoutHelper.ComputePillHeight( lGeometry );

        Assert.True( lPillBottom <= lHeight );
    }

    [Fact]
    public void ComputeFitRange_AddsTwelveAndTwentyPixelMarginsAroundTheCareer()
    {
        var lMin = new DateTime( 2010, 6, 1 );
        const double Width = 984.0;

        var lFit = TimelineLayoutHelper.ComputeFitRange( lMin, sToday, Width );

        var lRangeDays = ( sToday - lMin ).TotalDays;
        Assert.Equal( ( Width - 32.0 ) / lRangeDays, lFit.Zoom, 9 );
        Assert.Equal( 12.0, ( lMin - lFit.Start ).TotalDays * lFit.Zoom, 3 );
        Assert.Equal( 20.0, ( lFit.End - sToday ).TotalDays * lFit.Zoom, 3 );
        Assert.Equal( Width, ( lFit.End - lFit.Start ).TotalDays * lFit.Zoom, 3 );
        Assert.Equal( Width / lFit.Zoom, lFit.SpanDays, 6 );
    }

    [Fact]
    public void ComputeFitRange_MaximumZoomShowsAtLeastOneHundredEightyDays()
    {
        var lFit = TimelineLayoutHelper.ComputeFitRange( new DateTime( 2010, 6, 1 ), sToday, 1260.0 );

        Assert.Equal( 1260.0 / 180.0, lFit.MaxZoom, 9 );
        Assert.True( lFit.MaxZoom > lFit.Zoom );
    }

    [Fact]
    public void ComputeFitRange_TinyCareerNeverZoomsBelowTheFit()
    {
        var lFit = TimelineLayoutHelper.ComputeFitRange( sToday.AddDays( -30 ), sToday, 984.0 );

        Assert.True( lFit.MaxZoom >= lFit.Zoom );
    }

    [Fact]
    public void IsShowingAll_ReflectsWhetherTheWholeFitRangeIsVisible()
    {
        const double Width = 984.0;
        var lFit = TimelineLayoutHelper.ComputeFitRange( new DateTime( 2010, 6, 1 ), sToday, Width );

        Assert.True( TimelineLayoutHelper.IsShowingAll( Width, lFit.Zoom, lFit ) );
        Assert.True( TimelineLayoutHelper.IsShowingAll( Width, Width / ( lFit.SpanDays - 0.5 ), lFit ) );
        Assert.False( TimelineLayoutHelper.IsShowingAll( Width, lFit.Zoom * TimelineLayoutHelper.ZoomStepFactor, lFit ) );
    }

    [Theory]
    [InlineData( 0.40, false, 6 )]
    [InlineData( 1.00, false, 3 )]
    [InlineData( 2.00, false, 1 )]
    [InlineData( 0.25, true, 1 )]
    [InlineData( 0.10, true, 2 )]
    [InlineData( 0.04, true, 5 )]
    [InlineData( 0.001, true, 10 )]
    public void ChooseTickSchedule_PrefersMonthsThenYearsByPixelDensity( double pPixelsPerDay, bool pIsYears, int pStep )
    {
        var lSchedule = TimelineLayoutHelper.ChooseTickSchedule( pPixelsPerDay, 50.0 );

        Assert.Equal( pIsYears ? TimelineTickGranularity.Years : TimelineTickGranularity.Months, lSchedule.Granularity );
        Assert.Equal( pStep, lSchedule.Step );
    }

    [Theory]
    [InlineData( "en-CA" )]
    [InlineData( "fr-CA" )]
    public void BuildAxisTicks_MonthSchedule_LabelsJanuaryWithTheYearAndOtherMonthsWithAbbreviatedNames( string pCultureName )
    {
        var lCulture = CultureInfo.GetCultureInfo( pCultureName );
        var lSchedule = new TimelineTickSchedule( TimelineTickGranularity.Months, 1 );

        var lTicks = TimelineLayoutHelper.BuildAxisTicks( new DateTime( 2023, 12, 1 ), new DateTime( 2024, 4, 15 ), sToday, 3.0, lSchedule, lCulture );

        Assert.Equal( 5, lTicks.Count );

        var lJanuary = lTicks.Single( pTick => pTick.Date == new DateTime( 2024, 1, 1 ) );
        Assert.Equal( "2024", lJanuary.Label );
        Assert.True( lJanuary.IsEmphasized );
        Assert.True( lJanuary.IsGrid );

        var lMarch = lTicks.Single( pTick => pTick.Date == new DateTime( 2024, 3, 1 ) );
        Assert.Equal( lCulture.DateTimeFormat.GetAbbreviatedMonthName( 3 ), lMarch.Label );
        Assert.False( lMarch.IsEmphasized );
        Assert.False( lMarch.IsGrid );
        Assert.True( lMarch.IsMajor );
    }

    [Fact]
    public void FormatAxisLabel_MarchIsLocalizedForEnglishAndFrenchCanada()
    {
        var lMarch = new DateTime( 2024, 3, 1 );

        Assert.Equal( "Mar", TimelineLayoutHelper.FormatAxisLabel( lMarch, TimelineTickGranularity.Months, CultureInfo.GetCultureInfo( "en-CA" ) ) );
        Assert.Equal( "mars", TimelineLayoutHelper.FormatAxisLabel( lMarch, TimelineTickGranularity.Months, CultureInfo.GetCultureInfo( "fr-CA" ) ) );
        Assert.Equal( "2024", TimelineLayoutHelper.FormatAxisLabel( lMarch, TimelineTickGranularity.Years, CultureInfo.GetCultureInfo( "fr-CA" ) ) );
    }

    [Fact]
    public void BuildAxisTicks_MonthScheduleWithStepThree_LabelsOnlyEveryThirdMonthAndKeepsMinorTicks()
    {
        var lSchedule = new TimelineTickSchedule( TimelineTickGranularity.Months, 3 );

        var lTicks = TimelineLayoutHelper.BuildAxisTicks( new DateTime( 2023, 12, 1 ), new DateTime( 2024, 8, 31 ), sToday, 1.0, lSchedule, CultureInfo.GetCultureInfo( "en-CA" ) );

        Assert.Equal( [ "2024", "Apr", "Jul" ], lTicks.Where( pTick => pTick.Label != null ).Select( pTick => pTick.Label! ) );
        Assert.Equal( 9, lTicks.Count );
        Assert.Equal( 6, lTicks.Count( pTick => !pTick.IsMajor ) );
    }

    [Fact]
    public void BuildAxisTicks_YearSchedule_UsesYearLabelsGridLinesAndQuarterMinorTicks()
    {
        var lSchedule = new TimelineTickSchedule( TimelineTickGranularity.Years, 1 );

        var lTicks = TimelineLayoutHelper.BuildAxisTicks( new DateTime( 2020, 6, 1 ), new DateTime( 2022, 6, 1 ), sToday, 0.3, lSchedule, CultureInfo.GetCultureInfo( "fr-CA" ) );

        var lLabelled = lTicks.Where( pTick => pTick.Label != null ).ToList();
        Assert.Equal( [ "2021", "2022" ], lLabelled.Select( pTick => pTick.Label! ) );
        Assert.All( lLabelled, pTick => Assert.True( pTick.IsGrid && !pTick.IsEmphasized ) );
        Assert.Equal( 6, lTicks.Count( pTick => !pTick.IsMajor ) );
    }

    [Fact]
    public void BuildAxisTicks_YearScheduleWithStepTwo_LabelsEvenYearsOnly()
    {
        var lSchedule = new TimelineTickSchedule( TimelineTickGranularity.Years, 2 );

        var lTicks = TimelineLayoutHelper.BuildAxisTicks( new DateTime( 2010, 1, 1 ), new DateTime( 2014, 6, 1 ), sToday, 0.1, lSchedule, CultureInfo.GetCultureInfo( "en-CA" ) );

        Assert.Equal( [ "2010", "2012", "2014" ], lTicks.Where( pTick => pTick.Label != null ).Select( pTick => pTick.Label! ) );
        Assert.Equal( 5, lTicks.Count );
    }

    [Fact]
    public void BuildAxisTicks_NeverDrawsTicksAfterToday()
    {
        var lSchedule = new TimelineTickSchedule( TimelineTickGranularity.Months, 1 );

        var lTicks = TimelineLayoutHelper.BuildAxisTicks( new DateTime( 2026, 5, 1 ), new DateTime( 2027, 3, 1 ), sToday, 3.0, lSchedule, CultureInfo.GetCultureInfo( "en-CA" ) );

        Assert.NotEmpty( lTicks );
        Assert.All( lTicks, pTick => Assert.True( pTick.Date <= sToday ) );
        Assert.Equal( new DateTime( 2026, 10, 1 ), lTicks[ ^1 ].Date );
    }

    [Theory]
    [InlineData( "en-CA", "Mar 2024" )]
    [InlineData( "fr-CA", "mars 2024" )]
    public void FormatPillLabel_UsesMonthPrecision( string pCultureName, string pExpected )
    {
        var lLabel = TimelineLayoutHelper.FormatPillLabel( new DateTime( 2024, 3, 29 ), CultureInfo.GetCultureInfo( pCultureName ) );

        Assert.Equal( pExpected, lLabel );
    }

    [Fact]
    public void ChooseBarLabel_EnoughRoom_ReturnsBoldCompanyThenRegularRole()
    {
        var lSegments = TimelineLayoutHelper.ChooseBarLabel( "FARO CREAFORM", "UI/UX Expert", Separator, false, 300.0, FakeMeasure );

        Assert.Equal( 2, lSegments.Count );
        Assert.Equal( "FARO CREAFORM", lSegments[ 0 ].Text );
        Assert.True( lSegments[ 0 ].IsBold );
        Assert.Equal( Separator + "UI/UX Expert", lSegments[ 1 ].Text );
        Assert.False( lSegments[ 1 ].IsBold );
    }

    [Fact]
    public void ChooseBarLabel_RoleDoesNotFit_FallsBackToCompanyOnly()
    {
        var lSegments = TimelineLayoutHelper.ChooseBarLabel( "FARO CREAFORM", "UI/UX Expert", Separator, false, 150.0, FakeMeasure );

        var lSegment = Assert.Single( lSegments );
        Assert.Equal( "FARO CREAFORM", lSegment.Text );
        Assert.True( lSegment.IsBold );
    }

    [Fact]
    public void ChooseBarLabel_CompanyDoesNotFit_EllipsizesCompany()
    {
        var lSegments = TimelineLayoutHelper.ChooseBarLabel( "FARO CREAFORM", "UI/UX Expert", Separator, false, 100.0, FakeMeasure );

        var lSegment = Assert.Single( lSegments );
        Assert.Equal( "FARO CREAFO\u2026", lSegment.Text );
        Assert.True( lSegment.IsBold );
    }

    [Fact]
    public void ChooseBarLabel_SameCompanyAsLeftNeighbour_ShowsRoleOnly()
    {
        var lSegments = TimelineLayoutHelper.ChooseBarLabel( "FARO CREAFORM", "UI/UX Expert", Separator, true, 300.0, FakeMeasure );

        var lSegment = Assert.Single( lSegments );
        Assert.Equal( "UI/UX Expert", lSegment.Text );
        Assert.False( lSegment.IsBold );
    }

    [Fact]
    public void ChooseBarLabel_RoleOnlyThatDoesNotFit_IsEllipsized()
    {
        var lSegments = TimelineLayoutHelper.ChooseBarLabel( "FARO CREAFORM", "UI/UX Expert", Separator, true, 60.0, FakeMeasure );

        var lSegment = Assert.Single( lSegments );
        Assert.Equal( "UI/UX E\u2026", lSegment.Text );
        Assert.False( lSegment.IsBold );
    }

    [Theory]
    [InlineData( 55.9, false )]
    [InlineData( 56.0, true )]
    [InlineData( 20.0, false )]
    [InlineData( 0.0, false )]
    public void ChooseBarLabel_EllipsisNeedsAtLeastFiftySixPixels( double pAvailableWidth, bool pExpectsLabel )
    {
        var lSegments = TimelineLayoutHelper.ChooseBarLabel( "OLH PHOTOGRAPHIE", string.Empty, Separator, false, pAvailableWidth, FakeMeasure );

        Assert.Equal( pExpectsLabel, lSegments.Count > 0 );
    }

    [Fact]
    public void ChooseBarLabel_MissingRole_ShowsCompanyOnly()
    {
        var lSegments = TimelineLayoutHelper.ChooseBarLabel( "iA", null, Separator, false, 300.0, FakeMeasure );

        var lSegment = Assert.Single( lSegments );
        Assert.Equal( "iA", lSegment.Text );
    }

    [Fact]
    public void ChooseBarLabel_MissingCompany_ShowsRoleOnly()
    {
        var lSegments = TimelineLayoutHelper.ChooseBarLabel( string.Empty, "Developer", Separator, false, 300.0, FakeMeasure );

        var lSegment = Assert.Single( lSegments );
        Assert.Equal( "Developer", lSegment.Text );
        Assert.False( lSegment.IsBold );
    }

    [Theory]
    [InlineData( "CommonBlueStrongBrush", "CommonBlueEdgeBrush" )]
    [InlineData( "CommonPinkStrongBrush", "CommonPinkEdgeBrush" )]
    [InlineData( "CommonBlueBrush", null )]
    [InlineData( "", null )]
    [InlineData( null, null )]
    public void GetEdgeBrushKey_MapsStrongBrushKeysToEdgeBrushKeys( string? pAccentKey, string? pExpected )
    {
        Assert.Equal( pExpected, TimelineLayoutHelper.GetEdgeBrushKey( pAccentKey ) );
    }

    [Theory]
    [InlineData( "FARO CREAFORM", "UI/UX Expert", "FARO CREAFORM \u00B7 UI/UX Expert" )]
    [InlineData( "FARO CREAFORM", "", "FARO CREAFORM" )]
    [InlineData( "", "UI/UX Expert", "UI/UX Expert" )]
    public void FormatFrameHeadline_JoinsTitleAndSubtitle( string pTitle, string pSubtitle, string pExpected )
    {
        Assert.Equal( pExpected, TimelineLayoutHelper.FormatFrameHeadline( pTitle, pSubtitle, Separator ) );
    }
}
