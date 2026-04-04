using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class PhotographyPageViewModelTests
{
    [Fact]
    public void Constructor_InitializesAlbums()
    {
        var lViewModel = new PhotographyPageViewModel( new ResourcesService(), new ThemeService() );

        Assert.NotNull( lViewModel.Albums );
        Assert.Equal( 3, lViewModel.Albums.Count );
    }

    [Fact]
    public void QueueImagesPreloadForAll_DoesNotThrow()
    {
        var lViewModel = new PhotographyPageViewModel( new ResourcesService(), new ThemeService() );

        var lException = Record.Exception( () => lViewModel.QueueImagesPreloadForAll() );

        Assert.Null( lException );
    }
}
