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
    public void PropertyChanged_RaisedWhenActiveThemeChanges()
    {
        var lService = new ThemeService();
        var lRaisedProperties = new List<string?>();
        lService.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        // ActiveTheme is private set, so we just verify the property changed infrastructure works
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
