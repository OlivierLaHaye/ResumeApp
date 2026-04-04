using ResumeApp.Controls;
using Xunit;

namespace ResumeApp.Tests.Controls;

public sealed class ProjectImageCarouselControlTests
{
    [StaFact]
    public void DependencyProperties_AreRegistered()
    {
        Assert.NotNull( ProjectImageCarouselControl.sImagesProperty );
        Assert.NotNull( ProjectImageCarouselControl.sSelectedIndexProperty );
        Assert.NotNull( ProjectImageCarouselControl.sPlaceholderTextProperty );
        Assert.NotNull( ProjectImageCarouselControl.sIsFullscreenProperty );
        Assert.NotNull( ProjectImageCarouselControl.sIsOpenOnClickEnabledProperty );
    }

    [StaFact]
    public void Constructor_DoesNotThrow()
    {
        var lException = Record.Exception( () => new ProjectImageCarouselControl() );

        Assert.Null( lException );
    }

    [StaFact]
    public void Images_DefaultIsNull()
    {
        var lControl = new ProjectImageCarouselControl();

        Assert.Null( lControl.Images );
    }

    [StaFact]
    public void SelectedIndex_DefaultIsZero()
    {
        var lControl = new ProjectImageCarouselControl();

        Assert.Equal( 0, lControl.SelectedIndex );
    }

    [StaFact]
    public void IsFullscreen_DefaultIsFalse()
    {
        var lControl = new ProjectImageCarouselControl();

        Assert.False( lControl.IsFullscreen );
    }

    [StaFact]
    public void IsOpenOnClickEnabled_DefaultIsFalse()
    {
        var lControl = new ProjectImageCarouselControl();

        Assert.False( lControl.IsOpenOnClickEnabled );
    }
}
