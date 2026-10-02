using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using ResumeApp.Controls;
using ResumeApp.Models;
using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Controls;

public sealed class TimelineControlAutomationPeerTests
{
    private static TimelineControlAutomationPeer CreatePeer( TimelineControl pControl ) =>
        Assert.IsType<TimelineControlAutomationPeer>( UIElementAutomationPeer.CreatePeerForElement( pControl ) );

    private static TimelineTimeFrameItem CreateFrame( string pDescription = "" ) =>
        new( new DateTime( 2024, 3, 1 ), DateTime.Today, "FARO CREAFORM", "CommonBlueStrongBrush", "UI/UX Expert" )
        {
            DescriptionText = pDescription
        };

    [StaFact]
    public void Control_CreatesTheTimelinePeer()
    {
        var lPeer = CreatePeer( new TimelineControl() );

        Assert.Equal( nameof( TimelineControl ), lPeer.GetClassName() );
    }

    [StaFact]
    public void Name_ComesFromTheLocalizedResourceWhenNoAutomationNameIsSet()
    {
        var lControl = new TimelineControl { DataContext = new ResourcesService() };

        Assert.Equal( "Career timeline", CreatePeer( lControl ).GetName() );
    }

    [StaFact]
    public void Name_IsResolvedThroughAResourcesServicePropertyOfTheDataContext()
    {
        var lControl = new TimelineControl { DataContext = new ResourcesServiceHost() };

        Assert.Equal( "Career timeline", CreatePeer( lControl ).GetName() );
    }

    [StaFact]
    public void Name_PrefersAnExplicitAutomationName()
    {
        var lControl = new TimelineControl { DataContext = new ResourcesService() };
        AutomationProperties.SetName( lControl, "Explicit" );

        Assert.Equal( "Explicit", CreatePeer( lControl ).GetName() );
    }

    [StaFact]
    public void HelpText_ComesFromTheInteractionsResource()
    {
        var lControl = new TimelineControl { DataContext = new ResourcesService() };

        var lHelpText = CreatePeer( lControl ).GetHelpText();

        Assert.Contains( "Enter", lHelpText );
        Assert.Contains( "zoom", lHelpText );
        Assert.Equal( new ResourcesService()[ "TimelineControlInteractionsHelpText" ], lHelpText );
    }

    [StaFact]
    public void HelpText_PrefersAnExplicitHelpText()
    {
        var lControl = new TimelineControl { DataContext = new ResourcesService() };
        AutomationProperties.SetHelpText( lControl, "Explicit help" );

        Assert.Equal( "Explicit help", CreatePeer( lControl ).GetHelpText() );
    }

    [StaFact]
    public void NameAndHelpText_WithoutAResourcesService_AreEmpty()
    {
        var lPeer = CreatePeer( new TimelineControl() );

        Assert.Equal( string.Empty, lPeer.GetName() );
        Assert.Equal( string.Empty, lPeer.GetHelpText() );
    }

    [StaFact]
    public void ValuePattern_IsExposedAsReadOnlyAndRejectsWrites()
    {
        var lPeer = CreatePeer( new TimelineControl() );

        var lProvider = Assert.IsAssignableFrom<IValueProvider>( lPeer.GetPattern( PatternInterface.Value ) );

        Assert.True( lProvider.IsReadOnly );
        Assert.Throws<InvalidOperationException>( () => lProvider.SetValue( "anything" ) );
    }

    [StaFact]
    public void Value_IsEmptyWithoutASelectedFrame()
    {
        var lPeer = CreatePeer( new TimelineControl() );

        Assert.Equal( string.Empty, Assert.IsAssignableFrom<IValueProvider>( lPeer.GetPattern( PatternInterface.Value ) ).Value );
    }

    [StaFact]
    public void Value_DescribesTheSelectedFrameWithTitleSubtitleAndDescription()
    {
        var lControl = new TimelineControl { DataContext = new ResourcesService() };
        var lPeer = CreatePeer( lControl );
        var lProvider = Assert.IsAssignableFrom<IValueProvider>( lPeer.GetPattern( PatternInterface.Value ) );

        lControl.SelectedTimeFrame = CreateFrame();
        Assert.Equal( "FARO CREAFORM \u00B7 UI/UX Expert", lProvider.Value );

        lControl.SelectedTimeFrame = CreateFrame( "Leads UI/UX delivery." );
        Assert.Equal( "FARO CREAFORM \u00B7 UI/UX Expert, Leads UI/UX delivery.", lProvider.Value );
    }

    [StaFact]
    public void ChangingTheSelectedFrame_DoesNotThrowWhenNoClientListens()
    {
        var lControl = new TimelineControl();
        _ = CreatePeer( lControl );

        var lException = Record.Exception( () =>
        {
            lControl.SelectedTimeFrame = CreateFrame();
            lControl.SelectedTimeFrame = null;
        } );

        Assert.Null( lException );
    }

    [StaFact]
    public void RaiseValueChanged_WithoutListeners_DoesNotThrow()
    {
        var lPeer = CreatePeer( new TimelineControl() );

        var lException = Record.Exception( () => lPeer.RaiseValueChanged( "old", "new" ) );

        Assert.Null( lException );
    }

    private sealed class ResourcesServiceHost
    {
        public ResourcesService ResourcesService { get; } = new();
    }
}
