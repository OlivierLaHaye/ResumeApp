using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

public sealed class RegistrySettingsServiceTests
{
    [Fact]
    public void TryLoadTheme_OnNonWindows_ReturnsFalse()
    {
        if ( OperatingSystem.IsWindows() )
        {
            return;
        }

        bool lResult = RegistrySettingsService.TryLoadTheme( out AppTheme lTheme );

        Assert.False( lResult );
        Assert.Equal( AppTheme.Light, lTheme );
    }

    [Fact]
    public void TryLoadLanguage_OnNonWindows_ReturnsFalse()
    {
        if ( OperatingSystem.IsWindows() )
        {
            return;
        }

        bool lResult = RegistrySettingsService.TryLoadLanguage( out AppLanguage lLanguage );

        Assert.False( lResult );
        Assert.Equal( AppLanguage.EnglishCanada, lLanguage );
    }

    [Fact]
    public void SaveTheme_OnNonWindows_DoesNotThrow()
    {
        if ( OperatingSystem.IsWindows() )
        {
            return;
        }

        var lException = Record.Exception( () => RegistrySettingsService.SaveTheme( AppTheme.Dark ) );

        Assert.Null( lException );
    }

    [Fact]
    public void SaveLanguage_OnNonWindows_DoesNotThrow()
    {
        if ( OperatingSystem.IsWindows() )
        {
            return;
        }

        var lException = Record.Exception( () => RegistrySettingsService.SaveLanguage( AppLanguage.FrenchCanada ) );

        Assert.Null( lException );
    }
}
