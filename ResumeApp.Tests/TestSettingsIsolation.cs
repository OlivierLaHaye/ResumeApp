using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ResumeApp.Services;

namespace ResumeApp.Tests;

internal static class TestSettingsIsolation
{
    [ModuleInitializer]
    [SuppressMessage( "Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "Test assembly: theme and language settings must never be persisted to the developer's HKCU registry." )]
    internal static void Initialize()
    {
        RegistrySettingsService.UseInMemoryStore();
    }
}
