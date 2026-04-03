using System.Windows.Controls;
using ResumeApp.Behaviors;
using Xunit;

namespace ResumeApp.Tests.Behaviors;

public sealed class ExperienceTimelineScrollSyncBehaviorTests
{
    [StaFact]
    public void DependencyProperties_AreRegistered()
    {
        Assert.NotNull( ExperienceTimelineScrollSyncBehavior.sIsEnabledProperty );
        Assert.NotNull( ExperienceTimelineScrollSyncBehavior.sItemsControlProperty );
        Assert.NotNull( ExperienceTimelineScrollSyncBehavior.sSelectedDateProperty );
        Assert.NotNull( ExperienceTimelineScrollSyncBehavior.sSelectedItemProperty );
    }

    [StaFact]
    public void SetIsEnabled_AndGetIsEnabled_RoundTrips()
    {
        var lScrollViewer = new ScrollViewer();

        ExperienceTimelineScrollSyncBehavior.SetIsEnabled( lScrollViewer, true );

        Assert.True( ExperienceTimelineScrollSyncBehavior.GetIsEnabled( lScrollViewer ) );
    }

    [StaFact]
    public void SetIsEnabled_False_Detaches()
    {
        var lScrollViewer = new ScrollViewer();
        ExperienceTimelineScrollSyncBehavior.SetIsEnabled( lScrollViewer, true );

        ExperienceTimelineScrollSyncBehavior.SetIsEnabled( lScrollViewer, false );

        Assert.False( ExperienceTimelineScrollSyncBehavior.GetIsEnabled( lScrollViewer ) );
    }

    [StaFact]
    public void SetIsEnabled_OnNonScrollViewer_DoesNotThrow()
    {
        var lBorder = new Border();

        var lException = Record.Exception( () =>
            ExperienceTimelineScrollSyncBehavior.SetIsEnabled( lBorder, true ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void SetItemsControl_AndGetItemsControl_RoundTrips()
    {
        var lScrollViewer = new ScrollViewer();
        var lItemsControl = new ItemsControl();

        ExperienceTimelineScrollSyncBehavior.SetItemsControl( lScrollViewer, lItemsControl );

        Assert.Same( lItemsControl, ExperienceTimelineScrollSyncBehavior.GetItemsControl( lScrollViewer ) );
    }

    [StaFact]
    public void SetItemsControl_Null_ReturnsNull()
    {
        var lScrollViewer = new ScrollViewer();

        ExperienceTimelineScrollSyncBehavior.SetItemsControl( lScrollViewer, null );

        Assert.Null( ExperienceTimelineScrollSyncBehavior.GetItemsControl( lScrollViewer ) );
    }

    [StaFact]
    public void SetSelectedDate_AndGetSelectedDate_RoundTrips()
    {
        var lScrollViewer = new ScrollViewer();
        var lDate = new DateTime( 2023, 1, 15 );

        ExperienceTimelineScrollSyncBehavior.SetSelectedDate( lScrollViewer, lDate );

        Assert.Equal( lDate, ExperienceTimelineScrollSyncBehavior.GetSelectedDate( lScrollViewer ) );
    }

    [StaFact]
    public void SetSelectedItem_AndGetSelectedItem_RoundTrips()
    {
        var lScrollViewer = new ScrollViewer();
        var lItem = new object();

        ExperienceTimelineScrollSyncBehavior.SetSelectedItem( lScrollViewer, lItem );

        Assert.Same( lItem, ExperienceTimelineScrollSyncBehavior.GetSelectedItem( lScrollViewer ) );
    }

    [StaFact]
    public void SetSelectedItem_Null_ReturnsNull()
    {
        var lScrollViewer = new ScrollViewer();

        ExperienceTimelineScrollSyncBehavior.SetSelectedItem( lScrollViewer, null );

        Assert.Null( ExperienceTimelineScrollSyncBehavior.GetSelectedItem( lScrollViewer ) );
    }

    [StaFact]
    public void IsEnabled_WithItemsControl_DoesNotThrow()
    {
        var lScrollViewer = new ScrollViewer();
        var lItemsControl = new ItemsControl();

        ExperienceTimelineScrollSyncBehavior.SetIsEnabled( lScrollViewer, true );
        ExperienceTimelineScrollSyncBehavior.SetItemsControl( lScrollViewer, lItemsControl );

        Assert.True( ExperienceTimelineScrollSyncBehavior.GetIsEnabled( lScrollViewer ) );
    }

    [StaFact]
    public void IsEnabled_FalseWithoutPriorTrue_DoesNotThrow()
    {
        var lScrollViewer = new ScrollViewer();

        var lException = Record.Exception( () =>
            ExperienceTimelineScrollSyncBehavior.SetIsEnabled( lScrollViewer, false ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void SelectedDateChanged_WhenNotEnabled_DoesNotThrow()
    {
        var lScrollViewer = new ScrollViewer();

        var lException = Record.Exception( () =>
            ExperienceTimelineScrollSyncBehavior.SetSelectedDate( lScrollViewer, DateTime.Today ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void SelectedItemChanged_WhenNotEnabled_DoesNotThrow()
    {
        var lScrollViewer = new ScrollViewer();

        var lException = Record.Exception( () =>
            ExperienceTimelineScrollSyncBehavior.SetSelectedItem( lScrollViewer, new object() ) );

        Assert.Null( lException );
    }
}
