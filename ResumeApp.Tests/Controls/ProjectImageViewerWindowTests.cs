using ResumeApp.Windows;
using Xunit;

namespace ResumeApp.Tests.Controls;

public sealed class ProjectImageViewerWindowTests
{
    [Theory]
    [InlineData( 0, 1440, 1920, 1440 )]
    [InlineData( 0, 1440, 1280, 1280 )]
    [InlineData( 0, 960, 1020, 960 )]
    [InlineData( 0, 1280, 1020, 1020 )]
    [InlineData( 200, 1440, 0, 1440 )]
    [InlineData( 1600, 1440, 1920, 1600 )]
    [InlineData( 1600, 1440, 1500, 1500 )]
    public void ComputeMinTrackSizePixels_ClampsToWorkArea( int pSystemMin, int pRequestedMin, int pWorkArea, int pExpected )
    {
        Assert.Equal( pExpected, ProjectImageViewerWindow.ComputeMinTrackSizePixels( pSystemMin, pRequestedMin, pWorkArea ) );
    }

    [Theory]
    [InlineData( 1280.0, 0.95, 960.0, 1216.0 )]
    [InlineData( 1000.0, 0.95, 960.0, 960.0 )]
    [InlineData( 800.0, 0.95, 960.0, 800.0 )]
    [InlineData( 680.0, 0.95, 640.0, 646.0 )]
    public void ComputeInitialSizeDip_RespectsMinimumAndWorkArea( double pWorkArea, double pRatio, double pMinimum, double pExpected )
    {
        Assert.Equal( pExpected, ProjectImageViewerWindow.ComputeInitialSizeDip( pWorkArea, pRatio, pMinimum ) );
    }

    [Fact]
    public void GetFullResolutionWindowIndexes_ReturnsCurrentThenNeighbors()
    {
        Assert.Equal( [ 2, 3, 1 ], ProjectImageViewerWindow.GetFullResolutionWindowIndexes( 2, 5 ) );
    }

    [Fact]
    public void GetFullResolutionWindowIndexes_WrapsAtBothEnds()
    {
        Assert.Equal( [ 0, 1, 4 ], ProjectImageViewerWindow.GetFullResolutionWindowIndexes( 0, 5 ) );
        Assert.Equal( [ 4, 0, 3 ], ProjectImageViewerWindow.GetFullResolutionWindowIndexes( 4, 5 ) );
    }

    [Fact]
    public void GetFullResolutionWindowIndexes_DeduplicatesSmallCollections()
    {
        Assert.Equal( [ 0 ], ProjectImageViewerWindow.GetFullResolutionWindowIndexes( 0, 1 ) );
        Assert.Equal( [ 0, 1 ], ProjectImageViewerWindow.GetFullResolutionWindowIndexes( 0, 2 ) );
    }

    [Theory]
    [InlineData( -1, 3 )]
    [InlineData( 3, 3 )]
    [InlineData( 0, 0 )]
    public void GetFullResolutionWindowIndexes_InvalidInput_ReturnsEmpty( int pCurrentIndex, int pCount )
    {
        Assert.Empty( ProjectImageViewerWindow.GetFullResolutionWindowIndexes( pCurrentIndex, pCount ) );
    }

    [Theory]
    [InlineData( -1, 4, 3 )]
    [InlineData( 4, 4, 0 )]
    [InlineData( 2, 4, 2 )]
    public void WrapIndex_WrapsIntoRange( int pIndex, int pCount, int pExpected )
    {
        Assert.Equal( pExpected, ProjectImageViewerWindow.WrapIndex( pIndex, pCount ) );
    }
}
