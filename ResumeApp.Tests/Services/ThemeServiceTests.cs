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
}
