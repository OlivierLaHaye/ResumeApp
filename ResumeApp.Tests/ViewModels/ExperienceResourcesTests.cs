using System.Collections;
using System.Globalization;
using System.Resources;
using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class ExperienceResourcesTests
{
    private static readonly string[] sNewResourceNames =
    [
        "ExperienceDateRangeOpenEnd",
        "ExperienceDurationYearOne",
        "ExperienceDurationYearMany",
        "ExperienceDurationMonthOne",
        "ExperienceDurationMonthMany"
    ];

    private static readonly ResourceManager sManager = new( "ResumeApp.Properties.Resources", typeof( ResourcesService ).Assembly );

    private static Dictionary<string, string> LoadStrings( CultureInfo pCulture )
    {
        var lSet = sManager.GetResourceSet( pCulture, true, false );
        Assert.NotNull( lSet );

        var lResult = new Dictionary<string, string>( StringComparer.Ordinal );
        foreach ( DictionaryEntry lEntry in lSet )
        {
            lResult[ ( string )lEntry.Key ] = lEntry.Value as string ?? string.Empty;
        }

        return lResult;
    }

    private static Dictionary<string, string> English() => LoadStrings( CultureInfo.InvariantCulture );

    private static Dictionary<string, string> French() => LoadStrings( CultureInfo.GetCultureInfo( "fr-CA" ) );

    [Fact]
    public void EnglishAndFrenchCanada_HaveIdenticalNameSetsAndCounts()
    {
        var lEnglish = English();
        var lFrench = French();

        Assert.Equal( lEnglish.Count, lFrench.Count );
        Assert.Equal( lEnglish.Keys.OrderBy( pName => pName, StringComparer.Ordinal ), lFrench.Keys.OrderBy( pName => pName, StringComparer.Ordinal ) );
    }

    [Fact]
    public void NewExperienceResources_ExistInBothLanguages()
    {
        var lEnglish = English();
        var lFrench = French();

        Assert.All( sNewResourceNames, pName =>
        {
            Assert.True( lEnglish.TryGetValue( pName, out string? lEnglishValue ) && lEnglishValue.Length > 0, $"{pName} missing in English" );
            Assert.True( lFrench.TryGetValue( pName, out string? lFrenchValue ) && lFrenchValue.Length > 0, $"{pName} missing in French" );
        } );
    }

    [Fact]
    public void DateRangeAndDurationTexts_MatchTheApprovedWording()
    {
        var lEnglish = English();
        var lFrench = French();

        Assert.Equal( " – ", lEnglish[ "ExperienceDateRangeSeparator" ] );
        Assert.Equal( " – ", lFrench[ "ExperienceDateRangeSeparator" ] );
        Assert.Equal( "Present", lEnglish[ "ExperienceDateRangeOpenEnd" ] );
        Assert.Equal( "aujourd'hui", lFrench[ "ExperienceDateRangeOpenEnd" ] );
        Assert.Equal( "{0} yr", lEnglish[ "ExperienceDurationYearOne" ] );
        Assert.Equal( "{0} yrs", lEnglish[ "ExperienceDurationYearMany" ] );
        Assert.Equal( "{0} mo", lEnglish[ "ExperienceDurationMonthOne" ] );
        Assert.Equal( "{0} mos", lEnglish[ "ExperienceDurationMonthMany" ] );
        Assert.Equal( "{0} an", lFrench[ "ExperienceDurationYearOne" ] );
        Assert.Equal( "{0} ans", lFrench[ "ExperienceDurationYearMany" ] );
        Assert.Equal( "{0} mois", lFrench[ "ExperienceDurationMonthOne" ] );
        Assert.Equal( "{0} mois", lFrench[ "ExperienceDurationMonthMany" ] );
    }

    [Fact]
    public void LabelPresent_IsKeptUnchanged()
    {
        Assert.Equal( "Present", English()[ "LabelPresent" ] );
        Assert.Equal( "Présent", French()[ "LabelPresent" ] );
    }

    [Fact]
    public void FrenchUiUxExpertScopeAndLocation_ReuseTheOwnersLegacyFrenchText()
    {
        var lFrench = French();

        string lLegacyScope = lFrench[ "Experience1Scope" ];
        const string ScopePrefix = "Portée: ";
        Assert.StartsWith( ScopePrefix, lLegacyScope, StringComparison.Ordinal );
        Assert.Equal( lLegacyScope[ ScopePrefix.Length.. ], lFrench[ "ExperienceCreaformUiUxExpertScope" ] );
        Assert.Equal(
            "Leadership UI/UX et livraison front-end sur plusieurs projets, en assurant une UI desktop cohérente et de haute qualité.",
            lFrench[ "ExperienceCreaformUiUxExpertScope" ] );

        string lLegacyLocation = lFrench[ "Experience1LocationDates" ].Split( " | " )[ 0 ];
        Assert.Equal( lLegacyLocation, lFrench[ "ExperienceCreaformUiUxExpertLocation" ] );
        Assert.Equal( "Lévis, QC, Canada (Hybride)", lFrench[ "ExperienceCreaformUiUxExpertLocation" ] );
    }

    [Fact]
    public void EnglishUiUxExpertScopeAndLocation_AreUntouched()
    {
        var lEnglish = English();

        Assert.Equal( "Lead UI/UX and front-end delivery across projects, ensuring consistent, high-quality desktop UI.", lEnglish[ "ExperienceCreaformUiUxExpertScope" ] );
        Assert.Equal( "Lévis, QC, Canada (Hybrid)", lEnglish[ "ExperienceCreaformUiUxExpertLocation" ] );
    }

    [Fact]
    public void CompanyLabelsStayUppercase()
    {
        Assert.Equal( "FARO CREAFORM", English()[ "ExperienceCreaformUiUxExpertCompany" ] );
        Assert.Equal( "FARO CREAFORM", French()[ "ExperienceCreaformUiUxExpertCompany" ] );
    }
}
