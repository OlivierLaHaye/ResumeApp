using ResumeApp.Models;
using Xunit;

namespace ResumeApp.Tests.Models;

public sealed class TimelineTimeFrameItemTests
{
    [Fact]
    public void Constructor_NormalDates_SetsStartAndEnd()
    {
        var lStart = new DateTime( 2020, 1, 15 );
        var lEnd = new DateTime( 2023, 6, 20 );
        var lItem = new TimelineTimeFrameItem( lStart, lEnd, "Job", "Blue" );

        Assert.Equal( new DateTime( 2020, 1, 15 ), lItem.StartDate );
        Assert.Equal( new DateTime( 2023, 6, 20 ), lItem.EndDate );
        Assert.Equal( "Job", lItem.Title );
        Assert.Equal( "Blue", lItem.AccentColorKey );
    }

    [Fact]
    public void Constructor_ReversedDates_SwapsStartAndEnd()
    {
        var lEarlier = new DateTime( 2020, 1, 1 );
        var lLater = new DateTime( 2023, 1, 1 );
        var lItem = new TimelineTimeFrameItem( lLater, lEarlier, "Job", "Blue" );

        Assert.Equal( lEarlier, lItem.StartDate );
        Assert.Equal( lLater, lItem.EndDate );
    }

    [Fact]
    public void Constructor_StripsTimes_UsesDateOnly()
    {
        var lStart = new DateTime( 2020, 1, 15, 14, 30, 0 );
        var lEnd = new DateTime( 2023, 6, 20, 10, 0, 0 );
        var lItem = new TimelineTimeFrameItem( lStart, lEnd, "Job", "Blue" );

        Assert.Equal( new DateTime( 2020, 1, 15 ), lItem.StartDate );
        Assert.Equal( new DateTime( 2023, 6, 20 ), lItem.EndDate );
    }

    [Fact]
    public void Constructor_NullTitle_DefaultsToEmpty()
    {
        var lItem = new TimelineTimeFrameItem( DateTime.Today, DateTime.Today, null, null );

        Assert.Equal( string.Empty, lItem.Title );
        Assert.Equal( string.Empty, lItem.AccentColorKey );
    }

    [Fact]
    public void SetStartDate_RaisesPropertyChanged()
    {
        var lItem = new TimelineTimeFrameItem( DateTime.Today, DateTime.Today.AddDays( 1 ), "Job", "Blue" );
        string? lRaisedName = null;
        lItem.PropertyChanged += ( _, pArgs ) => lRaisedName = pArgs.PropertyName;

        lItem.StartDate = DateTime.Today.AddYears( -1 );

        Assert.Equal( "StartDate", lRaisedName );
    }

    [Fact]
    public void SetEndDate_RaisesPropertyChanged()
    {
        var lItem = new TimelineTimeFrameItem( DateTime.Today, DateTime.Today.AddDays( 1 ), "Job", "Blue" );
        string? lRaisedName = null;
        lItem.PropertyChanged += ( _, pArgs ) => lRaisedName = pArgs.PropertyName;

        lItem.EndDate = DateTime.Today.AddYears( 1 );

        Assert.Equal( "EndDate", lRaisedName );
    }

    [Fact]
    public void SetTitle_RaisesPropertyChanged()
    {
        var lItem = new TimelineTimeFrameItem( DateTime.Today, DateTime.Today, "Old", "Blue" );
        string? lRaisedName = null;
        lItem.PropertyChanged += ( _, pArgs ) => lRaisedName = pArgs.PropertyName;

        lItem.Title = "New";

        Assert.Equal( "Title", lRaisedName );
    }

    [Fact]
    public void SetAccentColorKey_RaisesPropertyChanged()
    {
        var lItem = new TimelineTimeFrameItem( DateTime.Today, DateTime.Today, "Job", "Old" );
        string? lRaisedName = null;
        lItem.PropertyChanged += ( _, pArgs ) => lRaisedName = pArgs.PropertyName;

        lItem.AccentColorKey = "New";

        Assert.Equal( "AccentColorKey", lRaisedName );
    }

    [Fact]
    public void SetStartDate_SameValue_DoesNotRaise()
    {
        var lDate = DateTime.Today;
        var lItem = new TimelineTimeFrameItem( lDate, lDate.AddDays( 1 ), "Job", "Blue" );
        bool lRaised = false;
        lItem.PropertyChanged += ( _, _ ) => lRaised = true;

        lItem.StartDate = lDate;

        Assert.False( lRaised );
    }

    [Fact]
    public void SetStartDate_StripesTime()
    {
        var lItem = new TimelineTimeFrameItem( DateTime.Today, DateTime.Today.AddDays( 1 ), "Job", "Blue" );

        lItem.StartDate = new DateTime( 2020, 6, 15, 14, 30, 0 );

        Assert.Equal( new DateTime( 2020, 6, 15 ), lItem.StartDate );
    }
}
