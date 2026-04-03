using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class ProjectsPageViewModelTests
{
    [Fact]
    public void Constructor_InitializesProjects()
    {
        var lViewModel = new ProjectsPageViewModel( new ResourcesService(), new ThemeService() );

        Assert.NotNull( lViewModel.Projects );
        Assert.Equal( 4, lViewModel.Projects.Count );
    }

    [Fact]
    public void QueueImagesPreloadForAll_DoesNotThrow()
    {
        var lViewModel = new ProjectsPageViewModel( new ResourcesService(), new ThemeService() );

        var lException = Record.Exception( () => lViewModel.QueueImagesPreloadForAll() );

        Assert.Null( lException );
    }
}
