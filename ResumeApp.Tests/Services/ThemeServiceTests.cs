using System.Windows.Media;
using ResumeApp.Helpers;
using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

public sealed class ThemeServiceTests
{
    [Fact]
    public void Constructor_SetsDefaultActiveThemeToLight()
    {
        var lService = new ThemeService();

        Assert.Equal( AppTheme.Light, lService.ActiveTheme );
    }

    [Fact]
    public void IsDarkThemeActive_WhenLight_ReturnsFalse()
    {
        var lService = new ThemeService();

        Assert.False( lService.IsDarkThemeActive );
    }

    [Fact]
    public void Constructor_SetsInstance()
    {
        var lService = new ThemeService();

        Assert.NotNull( ThemeService.Instance );
    }

    [Fact]
    public void ActiveTheme_SetToDark_RaisesPropertyChanged()
    {
        var lService = new ThemeService();
        var lRaisedProperties = new List<string?>();
        lService.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lService.ActiveTheme = AppTheme.Dark;

        Assert.Equal( AppTheme.Dark, lService.ActiveTheme );
        Assert.Contains( "ActiveTheme", lRaisedProperties );
    }

    [Fact]
    public void IsDarkThemeActive_WhenDark_ReturnsTrue()
    {
        var lService = new ThemeService();

        lService.ActiveTheme = AppTheme.Dark;

        Assert.True( lService.IsDarkThemeActive );
    }

    [Fact]
    public void ActiveTheme_SameValue_DoesNotRaisePropertyChanged()
    {
        var lService = new ThemeService();
        var lRaisedProperties = new List<string?>();
        lService.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lService.ActiveTheme = AppTheme.Light; // same as default

        Assert.Empty( lRaisedProperties );
    }

    [StaFact]
    public void Initialize_NullApplication_ThrowsArgumentNullException()
    {
        var lService = new ThemeService();

        Assert.Throws<ArgumentNullException>( () => lService.Initialize( null! ) );
    }

    [StaFact]
    public void ToggleTheme_NullApplication_ThrowsArgumentNullException()
    {
        var lService = new ThemeService();

        Assert.Throws<ArgumentNullException>( () => lService.ToggleTheme( null! ) );
    }

    [Theory]
    [InlineData( "TextPrimaryColor", "TextPrimaryBrush" )]
    [InlineData( "CommonWhiteColor", "CommonWhiteBrush" )]
    [InlineData( "AccentText", "AccentTextBrush" )]
    public void GetBrushKeyForColorKey_MapsColorKeyToBrushKey( string pColorKey, string pExpectedBrushKey )
    {
        Assert.Equal( pExpectedBrushKey, ThemeService.GetBrushKeyForColorKey( pColorKey ) );
    }

    [StaFact]
    public void CreateHighContrastDictionary_MapsEverySemanticKeyToSystemColorRoles()
    {
        Color lWindow = Colors.Black;
        Color lWindowText = Colors.White;
        Color lHighlight = Colors.Cyan;
        Color lHighlightText = Colors.Navy;
        Color lHotTrack = Colors.Yellow;

        var lDictionary = ThemeService.CreateHighContrastDictionary( lWindow, lWindowText, lHighlight, lHighlightText, lHotTrack );

        AssertRole( lDictionary, ThemeService.HighContrastSurfaceColorKeys, lWindow );
        AssertRole( lDictionary, ThemeService.HighContrastTextColorKeys, lWindowText );
        AssertRole( lDictionary, ThemeService.HighContrastHighlightColorKeys, lHighlight );
        AssertRole( lDictionary, ThemeService.HighContrastHighlightTextColorKeys, lHighlightText );
        AssertRole( lDictionary, ThemeService.HighContrastHotTrackColorKeys, lHotTrack );
    }

    private static string[] GetAllHighContrastColorKeys() =>
    [
        .. ThemeService.HighContrastSurfaceColorKeys,
        .. ThemeService.HighContrastTextColorKeys,
        .. ThemeService.HighContrastHighlightColorKeys,
        .. ThemeService.HighContrastHighlightTextColorKeys,
        .. ThemeService.HighContrastHotTrackColorKeys
    ];

    [Fact]
    public void HighContrastKeys_CoverSemanticTokens()
    {
        string[] lAllKeys = GetAllHighContrastColorKeys();

        string[] lSemanticKeys = [ "TextPrimaryColor", "TextSecondaryColor", "AccentColor", "AccentTextColor", "FocusRingColor", "BorderSubtleColor", "SurfaceHoverColor", "SurfaceSelectedColor", "TextOnSelectedColor" ];

        Assert.All( lSemanticKeys, pKey => Assert.Contains( pKey, lAllKeys ) );
        Assert.Equal( lAllKeys.Length, lAllKeys.Distinct( StringComparer.Ordinal ).Count() );
    }

    [StaFact]
    public void HighContrastKeys_ExistAsColorAndBrushInBothThemeDictionaries()
    {
        string[] lThemeFileNames = [ "Theme.Dark.xaml", "Theme.Light.xaml" ];

        foreach ( string lThemeFileName in lThemeFileNames )
        {
            var lDictionary = Assert.IsType<System.Windows.ResourceDictionary>(
                System.Windows.Application.LoadComponent( new Uri( $"/ResumeApp;component/Resources/{lThemeFileName}", UriKind.Relative ) ) );

            Assert.All( GetAllHighContrastColorKeys(), pColorKey =>
            {
                Assert.IsType<Color>( lDictionary[ pColorKey ] );
                Assert.IsType<SolidColorBrush>( lDictionary[ ThemeService.GetBrushKeyForColorKey( pColorKey ) ] );
            } );
        }
    }

    [Fact]
    public void HighContrastKeys_MapShellPillTokensToReadableRoles()
    {
        Assert.Contains( "SurfacePillSelectedColor", ThemeService.HighContrastSurfaceColorKeys );
        Assert.Contains( "SurfacePillSelectedBorderColor", ThemeService.HighContrastHighlightColorKeys );
        Assert.Contains( "BorderStrongColor", ThemeService.HighContrastTextColorKeys );
    }

    [Fact]
    public void HighContrastHighlightKeys_IncludeEveryTimelineLaneColor()
    {
        Assert.Equal( ColorHelper.sAccentBrushKeys.Length, ColorHelper.sAccentColorKeys.Length );

        for ( int lIndex = 0; lIndex < ColorHelper.sAccentBrushKeys.Length; lIndex++ )
        {
            string lColorKey = ColorHelper.sAccentColorKeys[ lIndex ];

            Assert.EndsWith( "Color", lColorKey );
            Assert.Equal( ColorHelper.sAccentBrushKeys[ lIndex ], ThemeService.GetBrushKeyForColorKey( lColorKey ) );
            Assert.Contains( lColorKey, ThemeService.HighContrastHighlightColorKeys );
        }
    }

    [StaFact]
    public void CreateHighContrastDictionary_OverridesEveryTimelineLaneBrushWithHighlight()
    {
        Color lHighlight = Colors.Cyan;

        var lDictionary = ThemeService.CreateHighContrastDictionary( Colors.Black, Colors.White, lHighlight, Colors.Navy, Colors.Yellow );

        Assert.All( ColorHelper.sAccentBrushKeys, pBrushKey =>
        {
            var lBrush = Assert.IsType<SolidColorBrush>( lDictionary[ pBrushKey ] );
            Assert.Equal( lHighlight, lBrush.Color );
        } );
    }

    private static void AssertRole( System.Windows.ResourceDictionary pDictionary, IEnumerable<string> pColorKeys, Color pExpected )
    {
        foreach ( string lColorKey in pColorKeys )
        {
            Assert.Equal( pExpected, Assert.IsType<Color>( pDictionary[ lColorKey ] ) );
            var lBrush = Assert.IsType<SolidColorBrush>( pDictionary[ ThemeService.GetBrushKeyForColorKey( lColorKey ) ] );
            Assert.Equal( pExpected, lBrush.Color );
            Assert.True( lBrush.IsFrozen );
        }
    }
}
