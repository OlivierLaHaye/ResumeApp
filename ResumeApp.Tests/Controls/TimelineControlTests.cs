using System.Collections.ObjectModel;
using ResumeApp.Controls;
using ResumeApp.Models;
using Xunit;

namespace ResumeApp.Tests.Controls;

public sealed class TimelineControlTests
{
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
}
