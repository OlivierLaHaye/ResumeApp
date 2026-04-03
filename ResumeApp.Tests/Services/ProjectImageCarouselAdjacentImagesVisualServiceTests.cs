using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

public sealed class ProjectImageCarouselAdjacentImagesVisualServiceTests
{
    [Theory]
    [InlineData( 0, 0, 800.0 )]
    [InlineData( 0, 1, 800.0 )]
    [InlineData( 0, -1, 800.0 )]
    public void GetSlotVisualTargets_Step0_Returns1Scale1Opacity0Translate( int pStep, int pDirection, double pContainerWidth )
    {
        var lResult = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( pStep, pDirection, pContainerWidth );

        Assert.Equal( 1.0, lResult.Scale );
        Assert.Equal( 1.0, lResult.Opacity );
        Assert.Equal( 0.0, lResult.TranslateX );
    }

    [Fact]
    public void GetSlotVisualTargets_Step1_RightDirection_PositiveTranslate()
    {
        var lResult = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 1, 1, 1000.0 );

        Assert.True( lResult.Scale < 1.0 );
        Assert.True( lResult.Scale > 0.0 );
        Assert.True( lResult.Opacity < 1.0 );
        Assert.True( lResult.Opacity > 0.0 );
        Assert.True( lResult.TranslateX > 0.0 );
    }

    [Fact]
    public void GetSlotVisualTargets_Step1_LeftDirection_NegativeTranslate()
    {
        var lResult = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 1, -1, 1000.0 );

        Assert.True( lResult.TranslateX < 0.0 );
    }

    [Fact]
    public void GetSlotVisualTargets_HigherStep_SmallerScale()
    {
        var lResult1 = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 1, 1, 1000.0 );
        var lResult2 = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 2, 1, 1000.0 );
        var lResult3 = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 3, 1, 1000.0 );

        Assert.True( lResult2.Scale < lResult1.Scale );
        Assert.True( lResult3.Scale < lResult2.Scale );
    }

    [Fact]
    public void GetSlotVisualTargets_HigherStep_SmallerOpacity()
    {
        var lResult1 = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 1, 1, 1000.0 );
        var lResult2 = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 2, 1, 1000.0 );

        Assert.True( lResult2.Opacity < lResult1.Opacity );
    }

    [Fact]
    public void GetSlotVisualTargets_HigherStep_LargerAbsTranslate()
    {
        var lResult1 = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 1, 1, 1000.0 );
        var lResult2 = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 2, 1, 1000.0 );

        Assert.True( Math.Abs( lResult2.TranslateX ) > Math.Abs( lResult1.TranslateX ) );
    }

    [Fact]
    public void GetSlotVisualTargets_NegativeStep_TreatedAsZero()
    {
        var lResult = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( -1, 1, 1000.0 );

        Assert.Equal( 1.0, lResult.Scale );
        Assert.Equal( 1.0, lResult.Opacity );
        Assert.Equal( 0.0, lResult.TranslateX );
    }

    [Fact]
    public void GetSlotVisualTargets_ZeroContainerWidth_TranslateIsZero()
    {
        var lResult = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 1, 1, 0.0 );

        Assert.Equal( 0.0, lResult.TranslateX );
    }

    [Fact]
    public void GetSlotVisualTargets_Step1_Direction0_TranslateIsZero()
    {
        var lResult = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( 1, 0, 1000.0 );

        Assert.Equal( 0.0, lResult.TranslateX );
        Assert.True( lResult.Scale < 1.0 );
        Assert.True( lResult.Opacity < 1.0 );
    }

    [Fact]
    public void ProjectImageCarouselSlotVisualTargets_Struct_StoresValues()
    {
        var lTargets = new ProjectImageCarouselSlotVisualTargets( 0.5, 0.7, 100.0 );

        Assert.Equal( 0.5, lTargets.Scale );
        Assert.Equal( 0.7, lTargets.Opacity );
        Assert.Equal( 100.0, lTargets.TranslateX );
    }
}
