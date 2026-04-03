using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

public sealed class AppThemeTests
{
    [Fact]
    public void AppTheme_HasLightAndDark()
    {
        Assert.Equal( 0, (int)AppTheme.Light );
        Assert.Equal( 1, (int)AppTheme.Dark );
    }

    [Fact]
    public void AppTheme_ValuesAreDefined()
    {
        Assert.True( Enum.IsDefined( typeof( AppTheme ), AppTheme.Light ) );
        Assert.True( Enum.IsDefined( typeof( AppTheme ), AppTheme.Dark ) );
    }
}

public sealed class AppLanguageTests
{
    [Fact]
    public void AppLanguage_HasEnglishAndFrench()
    {
        Assert.Equal( 0, (int)AppLanguage.EnglishCanada );
        Assert.Equal( 1, (int)AppLanguage.FrenchCanada );
    }

    [Fact]
    public void AppLanguage_ValuesAreDefined()
    {
        Assert.True( Enum.IsDefined( typeof( AppLanguage ), AppLanguage.EnglishCanada ) );
        Assert.True( Enum.IsDefined( typeof( AppLanguage ), AppLanguage.FrenchCanada ) );
    }
}
