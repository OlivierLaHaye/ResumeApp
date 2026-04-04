using ResumeApp.Services;
using ResumeApp.ViewModels;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class ViewModelBaseTests
{
    private sealed class TestViewModel : ViewModelBase
    {
        public TestViewModel( ResourcesService pResourcesService, ThemeService pThemeService )
            : base( pResourcesService, pThemeService )
        {
        }
    }

    [Fact]
    public void Constructor_NullResourcesService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () => new TestViewModel( null!, new ThemeService() ) );
    }

    [Fact]
    public void Constructor_NullThemeService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () => new TestViewModel( new ResourcesService(), null! ) );
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var lResourcesService = new ResourcesService();
        var lThemeService = new ThemeService();

        var lViewModel = new TestViewModel( lResourcesService, lThemeService );

        Assert.Same( lResourcesService, lViewModel.ResourcesService );
        Assert.Same( lThemeService, lViewModel.ThemeService );
    }
}
