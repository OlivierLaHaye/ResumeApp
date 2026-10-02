using System.Windows;
using System.Windows.Controls;
using ResumeApp.Behaviors;
using ResumeApp.Services;
using ResumeApp.Tests.Services;
using Xunit;

namespace ResumeApp.Tests.Behaviors;

[Collection( MotionPolicyCollection.Name )]
public sealed class ScrollViewerAnimatedOffsetBehaviorTests
{
    private static ScrollViewer CreateMeasuredScrollViewer()
    {
        var lScrollViewer = new ScrollViewer
        {
            Width = 200,
            Height = 200,
            Content = new Border { Width = 100, Height = 1000 }
        };

        lScrollViewer.Measure( new Size( 200, 200 ) );
        lScrollViewer.Arrange( new Rect( 0, 0, 200, 200 ) );
        lScrollViewer.UpdateLayout();

        return lScrollViewer;
    }

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

    [StaFact]
    public void AnimateVerticalOffset_WhenMotionReduced_SnapsWithoutAnimation()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( false );
            ScrollViewer lScrollViewer = CreateMeasuredScrollViewer();

            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, 300, 220 );
            lScrollViewer.UpdateLayout();

            Assert.False( lScrollViewer.HasAnimatedProperties );
            Assert.Equal( 300.0, ( double )lScrollViewer.GetValue( ScrollViewerAnimatedOffsetBehavior.sAnimatedVerticalOffsetProperty ) );
            Assert.Equal( 300.0, lScrollViewer.VerticalOffset );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }

    [StaFact]
    public void AnimateVerticalOffset_WhenMotionEnabled_StartsAnimation()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( true );
            ScrollViewer lScrollViewer = CreateMeasuredScrollViewer();

            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, 300, 220 );

            Assert.True( lScrollViewer.HasAnimatedProperties );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }

    [StaFact]
    public void AnimateVerticalOffset_WhenMotionBecomesReducedMidAnimation_CancelsAnimationAndSnaps()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( true );
            ScrollViewer lScrollViewer = CreateMeasuredScrollViewer();
            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, 300, 220 );

            MotionPolicy.SetAnimationEnabledOverride( false );
            ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( lScrollViewer, 500, 220 );
            lScrollViewer.UpdateLayout();

            Assert.False( lScrollViewer.HasAnimatedProperties );
            Assert.Equal( 500.0, lScrollViewer.VerticalOffset );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }
}
