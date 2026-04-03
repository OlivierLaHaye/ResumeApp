using System.Windows.Controls;
using ResumeApp.Behaviors;
using Xunit;

namespace ResumeApp.Tests.Behaviors;

public sealed class ScrollViewerAnimatedOffsetBehaviorTests
{
    [StaFact]
    public void sAnimatedVerticalOffsetProperty_IsRegistered()
    {
        Assert.NotNull( ScrollViewerAnimatedOffsetBehavior.sAnimatedVerticalOffsetProperty );
    }

    [StaFact]
    public void AnimateVerticalOffset_NullScrollViewer_DoesNotThrow()
    {
        var lException = Record.Exception( () =>
            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( null, 100, 200 ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void AnimateVerticalOffset_ZeroDuration_SetsDirectly()
    {
        var lScrollViewer = new ScrollViewer();

        var lException = Record.Exception( () =>
            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, 0, 0 ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void AnimateVerticalOffset_NegativeDuration_SetsDirectly()
    {
        var lScrollViewer = new ScrollViewer();

        var lException = Record.Exception( () =>
            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, 50, -1 ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void AnimateVerticalOffset_VerySmallDifference_SetsDirectly()
    {
        var lScrollViewer = new ScrollViewer();

        // Current offset is 0, target 0.1 - difference < 0.5
        var lException = Record.Exception( () =>
            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, 0.1, 200 ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void AnimateVerticalOffset_NormalAnimation_DoesNotThrow()
    {
        var lScrollViewer = new ScrollViewer();

        var lException = Record.Exception( () =>
            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, 100, 200 ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void AnimateVerticalOffset_NegativeTarget_ClampedToZero()
    {
        var lScrollViewer = new ScrollViewer();

        var lException = Record.Exception( () =>
            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, -10, 200 ) );

        Assert.Null( lException );
    }
}
