using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using ResumeApp.Controls;
using ResumeApp.Models;
using ResumeApp.Services;
using ResumeApp.Tests.Services;
using Xunit;

namespace ResumeApp.Tests.Controls;

[Collection( MotionPolicyCollection.Name )]
public sealed class TimelineControlTests
{
    private static FieldInfo GetField( string pName ) =>
        typeof( TimelineControl ).GetField( pName, BindingFlags.Instance | BindingFlags.NonPublic )!;

    private static void SetField( TimelineControl pControl, string pName, object pValue ) =>
        GetField( pName ).SetValue( pControl, pValue );

    private static bool GetBoolField( TimelineControl pControl, string pName ) =>
        ( bool )GetField( pName ).GetValue( pControl )!;

    [StaFact]
    public void Constructor_DoesNotThrow()
    {
        var lException = Record.Exception( () => new TimelineControl() );

        Assert.Null( lException );
    }

    [StaFact]
    public void DefaultPropertyValues_AreCorrect()
    {
        var lControl = new TimelineControl();

        Assert.Null( lControl.TimeFrames );
        Assert.Equal( DateTime.Today, lControl.SelectedDate );
        Assert.Null( lControl.SelectedTimeFrame );
    }

    [StaFact]
    public void TimeFrames_SetAndGet_RoundTrips()
    {
        var lControl = new TimelineControl();
        var lFrames = new ObservableCollection<TimelineTimeFrameItem>();

        lControl.TimeFrames = lFrames;

        Assert.Same( lFrames, lControl.TimeFrames );
    }

    [StaFact]
    public void MinDate_SetAndGet_RoundTrips()
    {
        var lControl = new TimelineControl();
        var lDate = new DateTime( 2020, 1, 1 );

        lControl.MinDate = lDate;

        Assert.Equal( lDate, lControl.MinDate );
    }

    [StaFact]
    public void SelectedDate_SetAndGet_RoundTrips()
    {
        var lControl = new TimelineControl();
        var lDate = DateTime.Today.AddDays( -30 );

        lControl.SelectedDate = lDate;

        Assert.Equal( lDate, lControl.SelectedDate );
    }

    [StaFact]
    public void SelectedTimeFrame_SetAndGet_RoundTrips()
    {
        var lControl = new TimelineControl();
        var lFrame = new TimelineTimeFrameItem( DateTime.Today, DateTime.Today.AddDays( 30 ), "Test", "Blue" );

        lControl.SelectedTimeFrame = lFrame;

        Assert.Same( lFrame, lControl.SelectedTimeFrame );
    }

    [StaFact]
    public void SelectedTimeFrame_SetNull()
    {
        var lControl = new TimelineControl();

        lControl.SelectedTimeFrame = null;

        Assert.Null( lControl.SelectedTimeFrame );
    }

    [StaFact]
    public void DependencyProperties_AreRegistered()
    {
        Assert.NotNull( TimelineControl.sTimeFramesProperty );
        Assert.NotNull( TimelineControl.sMinDateProperty );
        Assert.NotNull( TimelineControl.sSelectedDateProperty );
        Assert.NotNull( TimelineControl.sSelectedTimeFrameProperty );
        Assert.NotNull( TimelineControl.sZoomLevelProperty );
        Assert.NotNull( TimelineControl.sViewportStartTicksProperty );
    }

    [StaFact]
    public void ZoomLevel_SetAndGet_RoundTrips()
    {
        var lControl = new TimelineControl();

        lControl.ZoomLevel = 3.0;

        Assert.Equal( 3.0, lControl.ZoomLevel );
    }

    [StaFact]
    public void ViewportStartTicks_SetAndGet_RoundTrips()
    {
        var lControl = new TimelineControl();
        var lDate = DateTime.Today.AddDays( -30 );
        var lTicks = (double)lDate.Ticks;

        lControl.ViewportStartTicks = lTicks;

        Assert.Equal( lTicks, lControl.ViewportStartTicks );
    }

    [StaFact]
    public void TimeFrames_WithItems_DoesNotThrow()
    {
        var lControl = new TimelineControl();
        var lFrames = new ObservableCollection<TimelineTimeFrameItem>
        {
            new( new DateTime( 2020, 1, 1 ), new DateTime( 2023, 1, 1 ), "Job1", "Blue" ),
            new( new DateTime( 2018, 1, 1 ), new DateTime( 2020, 1, 1 ), "Job2", "Green" )
        };

        var lException = Record.Exception( () =>
        {
            lControl.MinDate = new DateTime( 2018, 1, 1 );
            lControl.TimeFrames = lFrames;
        } );

        Assert.Null( lException );
    }

    [StaFact]
    public void TodayMarkerText_DefaultsToToday()
    {
        var lControl = new TimelineControl();

        Assert.Equal( "Today", lControl.TodayMarkerText );
    }

    [StaFact]
    public void TodayMarkerText_SetAndGet_RoundTrips()
    {
        var lControl = new TimelineControl();

        lControl.TodayMarkerText = "Aujourd'hui";

        Assert.Equal( "Aujourd'hui", lControl.TodayMarkerText );
    }

    [StaFact]
    public void TodayMarkerText_NullAssignment_CoalescesToToday()
    {
        var lControl = new TimelineControl();

        lControl.TodayMarkerText = null!;

        Assert.Equal( "Today", lControl.TodayMarkerText );
    }

    [StaFact]
    public void FocusVisualStyle_IsNull()
    {
        var lControl = new TimelineControl();

        Assert.Null( lControl.FocusVisualStyle );
        Assert.True( lControl.Focusable );
    }

    [StaFact]
    public void LostMouseCapture_DuringDrag_ResetsPointerAndInertiaState()
    {
        var lControl = new TimelineControl();
        SetField( lControl, "mIsPointerDown", true );
        SetField( lControl, "mHasDragged", true );
        SetField( lControl, "mHasPendingPan", true );
        SetField( lControl, "mIsInertiaActive", true );

        lControl.RaiseEvent( new MouseEventArgs( Mouse.PrimaryDevice, Environment.TickCount )
        {
            RoutedEvent = Mouse.LostMouseCaptureEvent
        } );

        Assert.False( GetBoolField( lControl, "mIsPointerDown" ) );
        Assert.False( GetBoolField( lControl, "mHasDragged" ) );
        Assert.False( GetBoolField( lControl, "mHasPendingPan" ) );
        Assert.False( GetBoolField( lControl, "mIsInertiaActive" ) );
    }

    [StaFact]
    public void LostMouseCapture_WithoutDrag_KeepsInertiaState()
    {
        var lControl = new TimelineControl();
        SetField( lControl, "mIsInertiaActive", true );

        lControl.RaiseEvent( new MouseEventArgs( Mouse.PrimaryDevice, Environment.TickCount )
        {
            RoutedEvent = Mouse.LostMouseCaptureEvent
        } );

        Assert.True( GetBoolField( lControl, "mIsInertiaActive" ) );
    }

    [StaFact]
    public void StartInertia_WhenMotionReduced_DoesNotActivateInertia()
    {
        try
        {
            var lControl = new TimelineControl();
            MethodInfo lStartInertia = typeof( TimelineControl ).GetMethod( "StartInertiaIfPossible", BindingFlags.Instance | BindingFlags.NonPublic )!;

            SetField( lControl, "mIsInertiaActive", true );
            MotionPolicy.SetAnimationEnabledOverride( true );
            lStartInertia.Invoke( lControl, null );

            Assert.True( GetBoolField( lControl, "mIsInertiaActive" ) );

            MotionPolicy.SetAnimationEnabledOverride( false );
            lStartInertia.Invoke( lControl, null );

            Assert.False( GetBoolField( lControl, "mIsInertiaActive" ) );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }

    [StaFact]
    public void ThemeServiceSubscription_FollowsLoadedAndUnloadedLifecycle()
    {
        _ = new ThemeService();

        var lControl = new TimelineControl();
        FieldInfo lSubscribedField = GetField( "mSubscribedThemeService" );
        MethodInfo lSubscribe = typeof( TimelineControl ).GetMethod( "SubscribeToThemeService", BindingFlags.Instance | BindingFlags.NonPublic )!;
        MethodInfo lUnsubscribe = typeof( TimelineControl ).GetMethod( "UnsubscribeFromThemeService", BindingFlags.Instance | BindingFlags.NonPublic )!;

        lSubscribe.Invoke( lControl, null );

        Assert.Same( ThemeService.Instance, lSubscribedField.GetValue( lControl ) );

        lUnsubscribe.Invoke( lControl, null );

        Assert.Null( lSubscribedField.GetValue( lControl ) );
    }
}
