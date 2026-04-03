using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class ExperiencePageViewModelTests
{
    private static ExperiencePageViewModel Create()
    {
        return new ExperiencePageViewModel( new ResourcesService(), new ThemeService() );
    }

    [Fact]
    public void Constructor_InitializesTimelineEntries()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.TimelineEntries );
        Assert.True( lViewModel.TimelineEntries.Count > 0 );
    }

    [Fact]
    public void Constructor_InitializesExperienceTimeFrames()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.ExperienceTimeFrames );
        Assert.Equal( lViewModel.TimelineEntries.Count, lViewModel.ExperienceTimeFrames.Count );
    }

    [Fact]
    public void TimelineMinDate_IsSet()
    {
        var lViewModel = Create();

        Assert.True( lViewModel.TimelineMinDate > DateTime.MinValue );
    }

    [Fact]
    public void SelectedDate_DefaultIsSet()
    {
        var lViewModel = Create();

        Assert.True( lViewModel.SelectedDate > DateTime.MinValue );
    }

    [Fact]
    public void SelectedTimelineEntry_DefaultIsSet()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.SelectedTimelineEntry );
    }

    [Fact]
    public void SetSelectedDate_ClampsToRange()
    {
        var lViewModel = Create();

        lViewModel.SelectedDate = DateTime.MinValue;

        Assert.Equal( lViewModel.TimelineMinDate, lViewModel.SelectedDate );
    }

    [Fact]
    public void SetSelectedDate_ClampsToToday()
    {
        var lViewModel = Create();

        lViewModel.SelectedDate = DateTime.MaxValue;

        Assert.Equal( DateTime.Today, lViewModel.SelectedDate );
    }

    [Fact]
    public void SetSelectedDate_SameValue_DoesNotChangeAgain()
    {
        var lViewModel = Create();
        var lInitialDate = lViewModel.SelectedDate;

        lViewModel.SelectedDate = lInitialDate;

        Assert.Equal( lInitialDate, lViewModel.SelectedDate );
    }

    [Fact]
    public void SetSelectedDate_SynchronizesTimelineEntry()
    {
        var lViewModel = Create();
        Assert.True( lViewModel.TimelineEntries.Count > 1, "Test requires multiple timeline entries" );

        var lSecondEntry = lViewModel.TimelineEntries[1];
        lViewModel.SelectedDate = lSecondEntry.StartDate;

        Assert.NotNull( lViewModel.SelectedTimelineEntry );
    }

    [Fact]
    public void SetSelectedDate_AtExactMinDate_DoesNotClampFurther()
    {
        var lViewModel = Create();

        lViewModel.SelectedDate = lViewModel.TimelineMinDate;

        Assert.Equal( lViewModel.TimelineMinDate, lViewModel.SelectedDate );
    }

    [Fact]
    public void SetSelectedDate_AtExactToday_DoesNotClampFurther()
    {
        var lViewModel = Create();

        lViewModel.SelectedDate = DateTime.Today;

        Assert.Equal( DateTime.Today, lViewModel.SelectedDate );
    }

    [Fact]
    public void SetSelectedDate_WithinRange_AcceptsValue()
    {
        var lViewModel = Create();
        var lMidDate = lViewModel.TimelineMinDate.AddDays( 30 );

        lViewModel.SelectedDate = lMidDate;

        Assert.Equal( lMidDate.Date, lViewModel.SelectedDate );
    }

    [Fact]
    public void SetSelectedTimeFrame_SynchronizesDate()
    {
        var lViewModel = Create();
        Assert.True( lViewModel.ExperienceTimeFrames.Count > 1, "Test requires multiple time frames" );

        var lSecondFrame = lViewModel.ExperienceTimeFrames[1];
        lViewModel.SelectedTimeFrame = lSecondFrame;

        Assert.Equal( lSecondFrame.StartDate, lViewModel.SelectedDate );
    }

    [Fact]
    public void SetSelectedTimeFrame_NullValue_DoesNotCrash()
    {
        var lViewModel = Create();

        var lException = Record.Exception( () => lViewModel.SelectedTimeFrame = null );

        Assert.Null( lException );
    }

    [Fact]
    public void SetSelectedTimelineEntry_NullValue_DoesNotCrash()
    {
        var lViewModel = Create();

        var lException = Record.Exception( () => lViewModel.SelectedTimelineEntry = null );

        Assert.Null( lException );
    }

    [Fact]
    public void SetSelectedTimeFrame_SameValue_DoesNothing()
    {
        var lViewModel = Create();
        var lCurrentFrame = lViewModel.SelectedTimeFrame;

        lViewModel.SelectedTimeFrame = lCurrentFrame;

        Assert.Same( lCurrentFrame, lViewModel.SelectedTimeFrame );
    }

    [Fact]
    public void SetSelectedTimelineEntry_SameValue_DoesNothing()
    {
        var lViewModel = Create();
        var lCurrentEntry = lViewModel.SelectedTimelineEntry;

        lViewModel.SelectedTimelineEntry = lCurrentEntry;

        Assert.Same( lCurrentEntry, lViewModel.SelectedTimelineEntry );
    }

    [StaFact]
    public void SelectExperienceCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.SelectExperienceCommand );
    }

    [StaFact]
    public void SelectDateCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.SelectDateCommand );
    }

    [StaFact]
    public void SelectExperienceCommand_WithEntry_SetsSelectedTimelineEntry()
    {
        var lViewModel = Create();
        Assert.True( lViewModel.TimelineEntries.Count > 1, "Test requires multiple entries" );

        var lEntry = lViewModel.TimelineEntries[1];
        lViewModel.SelectExperienceCommand.Execute( lEntry );

        Assert.Same( lEntry, lViewModel.SelectedTimelineEntry );
        Assert.Equal( lEntry.StartDate, lViewModel.SelectedDate );
    }

    [StaFact]
    public void SelectExperienceCommand_WithNonEntry_DoesNothing()
    {
        var lViewModel = Create();
        var lBefore = lViewModel.SelectedTimelineEntry;

        lViewModel.SelectExperienceCommand.Execute( "notanentry" );

        Assert.Same( lBefore, lViewModel.SelectedTimelineEntry );
    }

    [StaFact]
    public void SelectDateCommand_WithDateTime_SetsDate()
    {
        var lViewModel = Create();
        var lDate = lViewModel.TimelineMinDate.AddDays( 10 );

        lViewModel.SelectDateCommand.Execute( lDate );

        Assert.Equal( lDate.Date, lViewModel.SelectedDate );
    }

    [StaFact]
    public void SelectDateCommand_WithString_ParsesAndSetsDate()
    {
        var lViewModel = Create();
        string lDateString = DateTime.Today.ToString( "yyyy-MM-dd" );

        lViewModel.SelectDateCommand.Execute( lDateString );

        Assert.Equal( DateTime.Today, lViewModel.SelectedDate );
    }

    [StaFact]
    public void SelectDateCommand_WithNonDateObject_DoesNothing()
    {
        var lViewModel = Create();
        var lBefore = lViewModel.SelectedDate;

        lViewModel.SelectDateCommand.Execute( 42 );

        Assert.Equal( lBefore, lViewModel.SelectedDate );
    }

    [StaFact]
    public void SelectDateCommand_WithUnparseableString_DoesNothing()
    {
        var lViewModel = Create();
        var lBefore = lViewModel.SelectedDate;

        lViewModel.SelectDateCommand.Execute( "not-a-date" );

        Assert.Equal( lBefore, lViewModel.SelectedDate );
    }

    [StaFact]
    public void SelectDateCommand_WithNull_DoesNothing()
    {
        var lViewModel = Create();
        var lBefore = lViewModel.SelectedDate;

        lViewModel.SelectDateCommand.Execute( null );

        Assert.Equal( lBefore, lViewModel.SelectedDate );
    }

    [StaFact]
    public void SelectExperienceCommand_WithNull_DoesNothing()
    {
        var lViewModel = Create();
        var lBefore = lViewModel.SelectedTimelineEntry;

        lViewModel.SelectExperienceCommand.Execute( null );

        Assert.Same( lBefore, lViewModel.SelectedTimelineEntry );
    }

    [Fact]
    public void TimelineControlInteractionsHelpText_ReturnsValue()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.TimelineControlInteractionsHelpText );
    }

    [Fact]
    public void SetSelectedTimelineEntry_WithDifferentEntry_SynchronizesDate()
    {
        var lViewModel = Create();
        Assert.True( lViewModel.TimelineEntries.Count > 1, "Test requires multiple entries" );

        var lNewEntry = lViewModel.TimelineEntries[1];
        lViewModel.SelectedTimelineEntry = lNewEntry;

        Assert.Same( lNewEntry, lViewModel.SelectedTimelineEntry );
        Assert.Equal( lNewEntry.StartDate, lViewModel.SelectedDate );
    }

    [Fact]
    public void SelectedDate_RaisesPropertyChanged()
    {
        var lViewModel = Create();
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );
        var lNewDate = lViewModel.TimelineMinDate.AddDays( 60 );

        lViewModel.SelectedDate = lNewDate;

        Assert.Contains( "SelectedDate", lRaisedProperties );
    }

    [Fact]
    public void SelectedTimeFrame_RaisesPropertyChanged()
    {
        var lViewModel = Create();
        Assert.True( lViewModel.ExperienceTimeFrames.Count > 1, "Test requires multiple frames" );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lViewModel.SelectedTimeFrame = lViewModel.ExperienceTimeFrames[1];

        Assert.Contains( "SelectedTimeFrame", lRaisedProperties );
    }

    [Fact]
    public void SelectedTimelineEntry_RaisesPropertyChanged()
    {
        var lViewModel = Create();
        Assert.True( lViewModel.TimelineEntries.Count > 1, "Test requires multiple entries" );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lViewModel.SelectedTimelineEntry = lViewModel.TimelineEntries[1];

        Assert.Contains( "SelectedTimelineEntry", lRaisedProperties );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_RebuildsEntries()
    {
        var lResourcesService = new ResourcesService();
        var lViewModel = new ExperiencePageViewModel( lResourcesService, new ThemeService() );
        int lInitialCount = lViewModel.TimelineEntries.Count;
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "TimelineControlInteractionsHelpText", lRaisedProperties );
        Assert.True( lViewModel.TimelineEntries.Count > 0 );
    }
}
