using System.Globalization;
using System.Resources;
using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Controls;

public sealed class TimelineResourcesTests
{
    private static readonly string[] sNewKeys =
    [
        "TimelineAutomationName",
        "TimelineBarLabelSeparator",
        "TimelineShowAllButtonText",
        "TimelineZoomInButtonAutomationName",
        "TimelineZoomOutButtonAutomationName",
        "TimelineHintText",
        "TimelineControlInteractionsHelpText"
    ];

    private static readonly ResourceManager sManager = new( "ResumeApp.Properties.Resources", typeof( ResourcesService ).Assembly );

    private static Dictionary<string, string> LoadStrings( CultureInfo pCulture )
    {
        var lSet = sManager.GetResourceSet( pCulture, true, false );
        Assert.NotNull( lSet );

        var lResult = new Dictionary<string, string>( StringComparer.Ordinal );
        foreach ( System.Collections.DictionaryEntry lEntry in lSet )
        {
            lResult[ ( string )lEntry.Key ] = lEntry.Value as string ?? string.Empty;
        }

        return lResult;
    }

    [Fact]
    public void EnglishAndFrenchCanada_HaveIdenticalKeySets()
    {
        var lEnglish = LoadStrings( CultureInfo.InvariantCulture );
        var lFrench = LoadStrings( CultureInfo.GetCultureInfo( "fr-CA" ) );

        Assert.Equal( lEnglish.Keys.OrderBy( pKey => pKey, StringComparer.Ordinal ), lFrench.Keys.OrderBy( pKey => pKey, StringComparer.Ordinal ) );
    }

    [Fact]
    public void TimelineKeys_ExistInBothLanguages()
    {
        var lEnglish = LoadStrings( CultureInfo.InvariantCulture );
        var lFrench = LoadStrings( CultureInfo.GetCultureInfo( "fr-CA" ) );

        Assert.All( sNewKeys, pKey =>
        {
            Assert.True( lEnglish.TryGetValue( pKey, out var lEnglishValue ) && lEnglishValue.Length > 0, $"{pKey} missing in English" );
            Assert.True( lFrench.TryGetValue( pKey, out var lFrenchValue ) && lFrenchValue.Length > 0, $"{pKey} missing in French" );
        } );
    }

    [Fact]
    public void TimelineTexts_MatchTheApprovedWording()
    {
        var lEnglish = LoadStrings( CultureInfo.InvariantCulture );
        var lFrench = LoadStrings( CultureInfo.GetCultureInfo( "fr-CA" ) );

        Assert.Equal( "Career timeline", lEnglish[ "TimelineAutomationName" ] );
        Assert.Equal( "Chronologie du parcours", lFrench[ "TimelineAutomationName" ] );
        Assert.Equal( "Show all", lEnglish[ "TimelineShowAllButtonText" ] );
        Assert.Equal( "Tout afficher", lFrench[ "TimelineShowAllButtonText" ] );
        Assert.Equal( "Zoom in", lEnglish[ "TimelineZoomInButtonAutomationName" ] );
        Assert.Equal( "Zoom avant", lFrench[ "TimelineZoomInButtonAutomationName" ] );
        Assert.Equal( "Zoom out", lEnglish[ "TimelineZoomOutButtonAutomationName" ] );
        Assert.Equal( "Zoom arri\u00E8re", lFrench[ "TimelineZoomOutButtonAutomationName" ] );
        Assert.Equal( "Drag to pan \u00B7 Wheel or +/\u2212 to zoom \u00B7 Arrows to move", lEnglish[ "TimelineHintText" ] );
        Assert.Equal( "Glissez pour parcourir \u00B7 Molette ou +/\u2212 pour zoomer \u00B7 Fl\u00E8ches pour naviguer", lFrench[ "TimelineHintText" ] );
        Assert.Equal( " \u00B7 ", lEnglish[ "TimelineBarLabelSeparator" ] );
        Assert.Equal( " \u00B7 ", lFrench[ "TimelineBarLabelSeparator" ] );
    }

    [Fact]
    public void InteractionsHelpText_MentionsZoomKeysShowAllAndEnterInBothLanguages()
    {
        var lEnglish = LoadStrings( CultureInfo.InvariantCulture )[ "TimelineControlInteractionsHelpText" ];
        var lFrench = LoadStrings( CultureInfo.GetCultureInfo( "fr-CA" ) )[ "TimelineControlInteractionsHelpText" ];

        Assert.Contains( "plus and minus to zoom", lEnglish );
        Assert.Contains( "0 to show everything", lEnglish );
        Assert.Contains( "Enter to open the selected role card", lEnglish );
        Assert.Contains( "plus et moins pour zoomer", lFrench );
        Assert.Contains( "0 pour tout afficher", lFrench );
        Assert.Contains( "Entr\u00E9e pour ouvrir la carte du r\u00F4le s\u00E9lectionn\u00E9", lFrench );
    }

    [Fact]
    public void FrenchTimelineTexts_AreNotCopiesOfTheEnglishOnes()
    {
        var lEnglish = LoadStrings( CultureInfo.InvariantCulture );
        var lFrench = LoadStrings( CultureInfo.GetCultureInfo( "fr-CA" ) );

        string[] lTranslatedKeys =
        [
            "TimelineAutomationName",
            "TimelineShowAllButtonText",
            "TimelineZoomInButtonAutomationName",
            "TimelineZoomOutButtonAutomationName",
            "TimelineHintText",
            "TimelineControlInteractionsHelpText"
        ];

        Assert.All( lTranslatedKeys, pKey => Assert.NotEqual( lEnglish[ pKey ], lFrench[ pKey ] ) );
    }
}
