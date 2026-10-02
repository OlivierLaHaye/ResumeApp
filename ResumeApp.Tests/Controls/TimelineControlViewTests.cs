using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ResumeApp.AttachedProperties;
using ResumeApp.Controls;
using ResumeApp.Models;
using ResumeApp.Tests.Services;
using Xunit;

namespace ResumeApp.Tests.Controls;

[Collection( MotionPolicyCollection.Name )]
public sealed class TimelineControlViewTests
{
    private static readonly DateTime sCareerStart = new( 2010, 6, 1 );

    private static ObservableCollection<TimelineTimeFrameItem> CreateCareerFrames() =>
    [
        new( new DateTime( 2024, 3, 1 ), DateTime.Today, "FARO CREAFORM", "CommonBlueStrongBrush", "UI/UX Expert" ),
        new( new DateTime( 2020, 2, 1 ), new DateTime( 2024, 3, 1 ), "FARO CREAFORM", "CommonGreenStrongBrush", "Software Developer" ),
        new( new DateTime( 2018, 5, 1 ), new DateTime( 2020, 2, 1 ), "Arcane", "CommonYellowStrongBrush", "Developer" ),
        new( new DateTime( 2017, 5, 1 ), new DateTime( 2017, 8, 1 ), "iA", "CommonRedStrongBrush", "Intern" ),
        new( sCareerStart, DateTime.Today, "OLH PHOTOGRAPHIE", "CommonPurpleStrongBrush", "Photographer" )
    ];

    private static TimelineControl CreateArrangedControl( LayoutSizeClass pSizeClass = LayoutSizeClass.Regular, double pWidth = 1000.0 )
    {
        var lControl = new TimelineControl { Width = pWidth };
        AdaptiveLayout.SetSizeClass( lControl, pSizeClass );
        lControl.MinDate = sCareerStart;
        lControl.TimeFrames = CreateCareerFrames();

        Arrange( lControl );
        return lControl;
    }

    private static void Arrange( TimelineControl pControl )
    {
        pControl.InvalidateMeasure();
        pControl.Measure( new Size( pControl.Width, double.PositiveInfinity ) );
        pControl.Arrange( new Rect( 0.0, 0.0, pControl.DesiredSize.Width, pControl.DesiredSize.Height ) );
        pControl.UpdateLayout();
    }

    private static double GetPlotWidth( TimelineControl pControl )
    {
        var lGeometry = TimelineLayoutHelper.GetGeometry( pControl.SizeClass );
        return pControl.ActualWidth - lGeometry.PaddingLeft - lGeometry.PaddingRight;
    }

    private static double GetFitZoom( TimelineControl pControl ) =>
        TimelineLayoutHelper.ComputeFitRange( sCareerStart, DateTime.Today, GetPlotWidth( pControl ) ).Zoom;

    private static void RaiseKey( TimelineControl pControl, Key pKey, out bool pHandled )
    {
        using var lSource = new HwndSource( new HwndSourceParameters( "TimelineControlViewTests" ) );
        var lArgs = new KeyEventArgs( Keyboard.PrimaryDevice, lSource, Environment.TickCount, pKey )
        {
            RoutedEvent = Keyboard.KeyDownEvent
        };

        pControl.RaiseEvent( lArgs );
        pHandled = lArgs.Handled;
    }

    [StaFact]
    public void DesiredHeight_ForTwoLanes_FollowsTheSizeClass()
    {
        ( LayoutSizeClass SizeClass, double Height )[] lExpectations =
        [
            ( LayoutSizeClass.Compact, 106.0 ),
            ( LayoutSizeClass.Regular, 136.0 ),
            ( LayoutSizeClass.Wide, 148.0 )
        ];

        foreach ( var lExpectation in lExpectations )
        {
            var lControl = CreateArrangedControl( lExpectation.SizeClass );

            Assert.Equal( lExpectation.Height, lControl.DesiredSize.Height );
            Assert.Equal( lExpectation.Height, lControl.ActualHeight );
        }
    }

    [StaFact]
    public void DesiredHeight_ChangesWhenTheSizeClassChanges()
    {
        var lControl = CreateArrangedControl( LayoutSizeClass.Regular );
        Assert.Equal( 136.0, lControl.ActualHeight );

        AdaptiveLayout.SetSizeClass( lControl, LayoutSizeClass.Compact );
        Arrange( lControl );

        Assert.Equal( 106.0, lControl.ActualHeight );
    }

    [StaFact]
    public void DesiredHeight_WithoutFrames_ReservesNoLanes()
    {
        var lControl = new TimelineControl { Width = 1000.0 };

        Arrange( lControl );

        var lGeometry = TimelineLayoutHelper.GetGeometry( LayoutSizeClass.Regular );
        Assert.Equal( TimelineLayoutHelper.ComputeHeight( lGeometry, 0 ), lControl.DesiredSize.Height );
    }

    [StaFact]
    public void SizeClass_IsInheritedFromTheLogicalParent()
    {
        var lHost = new Grid();
        AdaptiveLayout.SetSizeClass( lHost, LayoutSizeClass.Compact );
        var lControl = new TimelineControl();

        lHost.Children.Add( lControl );

        Assert.Equal( LayoutSizeClass.Compact, lControl.SizeClass );
        Assert.Equal( LayoutSizeClass.Compact, AdaptiveLayout.GetSizeClass( lControl ) );
    }

    [StaFact]
    public void DefaultView_IsTheWholeCareer()
    {
        var lControl = CreateArrangedControl();

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( GetFitZoom( lControl ), lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void DefaultView_StaysWholeCareerWhenTheSelectedDateIsAssignedAfterLayout()
    {
        var lControl = CreateArrangedControl();
        var lViewportBefore = lControl.ViewportStartTicks;

        lControl.SelectedDate = new DateTime( 2024, 3, 1 );
        Arrange( lControl );

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( GetFitZoom( lControl ), lControl.ZoomLevel, 9 );
        Assert.Equal( lViewportBefore, lControl.ViewportStartTicks );
    }

    [StaFact]
    public void DefaultView_IsAppliedWhenTheFramesArriveBeforeTheFirstLayout()
    {
        var lControl = new TimelineControl { Width = 1000.0 };
        lControl.SelectedDate = new DateTime( 2024, 3, 1 );
        lControl.TimeFrames = CreateCareerFrames();
        lControl.MinDate = sCareerStart;

        Arrange( lControl );

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( GetFitZoom( lControl ), lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void ZoomIn_ZoomsByOnePointTwoFiveAndClearsIsShowingAll()
    {
        var lControl = CreateArrangedControl();
        var lZoomBefore = lControl.ZoomLevel;

        lControl.ZoomIn();

        Assert.False( lControl.IsShowingAll );
        Assert.Equal( lZoomBefore * 1.25, lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void ZoomOut_AfterZoomIn_ReturnsToShowingAll()
    {
        var lControl = CreateArrangedControl();

        lControl.ZoomIn();
        lControl.ZoomOut();

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( GetFitZoom( lControl ), lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void ZoomOut_WhileShowingAll_DoesNotZoomBelowTheFit()
    {
        var lControl = CreateArrangedControl();
        var lZoomBefore = lControl.ZoomLevel;

        lControl.ZoomOut();

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( lZoomBefore, lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void ZoomIn_NeverShowsLessThanOneHundredEightyDays()
    {
        var lControl = CreateArrangedControl();

        for ( var lStep = 0; lStep < 40; lStep++ )
        {
            lControl.ZoomIn();
        }

        var lVisibleDays = GetPlotWidth( lControl ) / lControl.ZoomLevel;
        Assert.Equal( 180.0, lVisibleDays, 6 );
    }

    [StaFact]
    public void ZoomIn_KeepsTheSelectedDateAtTheSamePixel()
    {
        var lControl = CreateArrangedControl();
        lControl.SelectedDate = new DateTime( 2020, 2, 1 );
        var lGeometry = TimelineLayoutHelper.GetGeometry( lControl.SizeClass );

        double PixelOf( DateTime pDate ) => lGeometry.PaddingLeft + ( ( pDate - lControl.ViewportStartDate ).TotalDays * lControl.ZoomLevel );

        var lPixelBefore = PixelOf( lControl.SelectedDate );
        lControl.ZoomIn();
        var lPixelAfter = PixelOf( lControl.SelectedDate );

        Assert.Equal( lPixelBefore, lPixelAfter, 3 );
    }

    [StaFact]
    public void ShowAll_AfterZooming_RestoresTheFitViewAndIsShowingAll()
    {
        var lControl = CreateArrangedControl();
        var lFitViewport = lControl.ViewportStartTicks;
        lControl.ZoomIn();
        lControl.ZoomIn();
        Assert.False( lControl.IsShowingAll );

        lControl.ShowAll();

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( GetFitZoom( lControl ), lControl.ZoomLevel, 9 );
        Assert.Equal( lFitViewport, lControl.ViewportStartTicks );
    }

    [StaFact]
    public void SelectedDateFarFromTheView_PansWithoutChangingTheZoom()
    {
        var lControl = CreateArrangedControl();
        lControl.SelectedDate = DateTime.Today;
        lControl.ZoomIn();
        lControl.ZoomIn();
        lControl.ZoomIn();
        var lZoomBefore = lControl.ZoomLevel;
        var lViewportBefore = lControl.ViewportStartTicks;

        lControl.SelectedDate = sCareerStart;

        Assert.Equal( lZoomBefore, lControl.ZoomLevel, 12 );
        Assert.NotEqual( lViewportBefore, lControl.ViewportStartTicks );
        Assert.False( lControl.IsShowingAll );

        var lVisibleDays = GetPlotWidth( lControl ) / lControl.ZoomLevel;
        Assert.InRange( ( sCareerStart - lControl.ViewportStartDate ).TotalDays, 0.0, lVisibleDays );
    }

    [StaFact]
    public void ReplacingTheFrames_ReturnsToTheWholeCareer()
    {
        var lControl = CreateArrangedControl();
        lControl.ZoomIn();
        lControl.ZoomIn();
        Assert.False( lControl.IsShowingAll );

        lControl.TimeFrames = CreateCareerFrames();

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( GetFitZoom( lControl ), lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void ChangingTheFramesCollection_ReturnsToTheWholeCareer()
    {
        var lControl = CreateArrangedControl();
        lControl.ZoomIn();
        lControl.ZoomIn();
        Assert.False( lControl.IsShowingAll );

        lControl.TimeFrames!.Clear();
        foreach ( var lFrame in CreateCareerFrames() )
        {
            lControl.TimeFrames.Add( lFrame );
        }

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( GetFitZoom( lControl ), lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void Resizing_WhileShowingAll_KeepsShowingAll()
    {
        var lControl = CreateArrangedControl();

        lControl.Width = 1300.0;
        Arrange( lControl );

        Assert.True( lControl.IsShowingAll );
        Assert.Equal( GetFitZoom( lControl ), lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void Resizing_WhileZoomedIn_KeepsTheZoomAndStaysInsideTheValidRange()
    {
        var lControl = CreateArrangedControl();
        lControl.ZoomIn();
        lControl.ZoomIn();
        lControl.ZoomIn();
        var lZoomBefore = lControl.ZoomLevel;

        lControl.Width = 1100.0;
        Arrange( lControl );

        Assert.False( lControl.IsShowingAll );
        Assert.Equal( lZoomBefore, lControl.ZoomLevel, 9 );
    }

    [StaFact]
    public void IsShowingAll_IsReadOnlyAndRegisteredAsADependencyProperty()
    {
        Assert.True( TimelineControl.sIsShowingAllProperty.ReadOnly );
        Assert.Equal( typeof( bool ), TimelineControl.sIsShowingAllProperty.PropertyType );
    }

    [StaFact]
    public void SelectionActivated_IsABubblingRoutedEvent()
    {
        Assert.Equal( RoutingStrategy.Bubble, TimelineControl.sSelectionActivatedEvent.RoutingStrategy );

        var lControl = new TimelineControl();
        var lRaised = 0;
        RoutedEventHandler lHandler = ( _, _ ) => lRaised++;

        lControl.SelectionActivated += lHandler;
        lControl.RaiseEvent( new RoutedEventArgs( TimelineControl.sSelectionActivatedEvent, lControl ) );
        lControl.SelectionActivated -= lHandler;
        lControl.RaiseEvent( new RoutedEventArgs( TimelineControl.sSelectionActivatedEvent, lControl ) );

        Assert.Equal( 1, lRaised );
    }

    [StaFact]
    public void PlusKeys_ZoomIn()
    {
        foreach ( var lKey in new[] { Key.OemPlus, Key.Add } )
        {
            var lControl = CreateArrangedControl();

            RaiseKey( lControl, lKey, out var lHandled );

            Assert.True( lHandled );
            Assert.False( lControl.IsShowingAll );
        }
    }

    [StaFact]
    public void MinusKeys_ZoomOut()
    {
        foreach ( var lKey in new[] { Key.OemMinus, Key.Subtract } )
        {
            var lControl = CreateArrangedControl();
            lControl.ZoomIn();
            Assert.False( lControl.IsShowingAll );

            RaiseKey( lControl, lKey, out var lHandled );

            Assert.True( lHandled );
            Assert.True( lControl.IsShowingAll );
        }
    }

    [StaFact]
    public void ZeroKeys_ShowAll()
    {
        foreach ( var lKey in new[] { Key.D0, Key.NumPad0 } )
        {
            var lControl = CreateArrangedControl();
            lControl.ZoomIn();
            lControl.ZoomIn();
            Assert.False( lControl.IsShowingAll );

            RaiseKey( lControl, lKey, out var lHandled );

            Assert.True( lHandled );
            Assert.True( lControl.IsShowingAll );
        }
    }

    [StaFact]
    public void EnterKey_RaisesSelectionActivated()
    {
        var lControl = CreateArrangedControl();
        var lRaised = 0;
        lControl.SelectionActivated += ( _, _ ) => lRaised++;

        RaiseKey( lControl, Key.Enter, out var lHandled );

        Assert.True( lHandled );
        Assert.Equal( 1, lRaised );
    }

    [StaFact]
    public void FormatFrameSummary_JoinsTitleSubtitleAndDescription()
    {
        var lItem = new TimelineTimeFrameItem( sCareerStart, DateTime.Today, "FARO CREAFORM", "CommonBlueStrongBrush", "UI/UX Expert" )
        {
            DescriptionText = "Leads UI/UX delivery."
        };

        Assert.Equal( "FARO CREAFORM \u00B7 UI/UX Expert, Leads UI/UX delivery.", TimelineControl.FormatFrameSummary( lItem ) );
        Assert.Equal( "FARO CREAFORM \u00B7 UI/UX Expert\nLeads UI/UX delivery.", TimelineControl.FormatFrameToolTip( lItem, " \u00B7 " ) );
    }

    [StaFact]
    public void FormatFrameSummary_WithoutDescription_HasNoTrailingSeparator()
    {
        var lItem = new TimelineTimeFrameItem( sCareerStart, DateTime.Today, "iA", "CommonRedStrongBrush", "Intern" );

        Assert.Equal( "iA \u00B7 Intern", TimelineControl.FormatFrameSummary( lItem ) );
        Assert.Equal( "iA \u00B7 Intern", TimelineControl.FormatFrameToolTip( lItem, " \u00B7 " ) );
        Assert.Equal( string.Empty, TimelineControl.FormatFrameSummary( null ) );
    }
}
