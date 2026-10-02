using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

public sealed class TestSettingsIsolationTests
{
    [Fact]
    public void TestAssembly_UsesInMemorySettingsStore()
    {
        Assert.True( RegistrySettingsService.IsUsingInMemoryStore );
    }

    [Fact]
    public void SaveAndLoadLanguage_RoundTripsThroughInMemoryStore()
    {
        RegistrySettingsService.SaveLanguage( AppLanguage.FrenchCanada );

        bool lHasValue = RegistrySettingsService.TryLoadLanguage( out AppLanguage lLanguage );

        Assert.True( lHasValue );
        Assert.Equal( AppLanguage.FrenchCanada, lLanguage );
    }
}
