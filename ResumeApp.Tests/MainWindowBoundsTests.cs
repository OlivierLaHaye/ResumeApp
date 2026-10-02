using System.Windows;
using Xunit;

namespace ResumeApp.Tests;

public sealed class MainWindowBoundsTests
{
    [Theory]
    [InlineData( 1920, 1080, 960, 640 )]
    [InlineData( 1093, 580, 960, 580 )]
    [InlineData( 853, 520, 853, 520 )]
    public void ClampMinimumSizeToWorkArea_NeverExceedsWorkArea( double pWorkAreaWidth, double pWorkAreaHeight, double pExpectedWidth, double pExpectedHeight )
    {
        Size lResult = MainWindow.ClampMinimumSizeToWorkArea( 960, 640, pWorkAreaWidth, pWorkAreaHeight );

        Assert.Equal( pExpectedWidth, lResult.Width );
        Assert.Equal( pExpectedHeight, lResult.Height );
    }

    [Fact]
    public void ClampMinimumSizeToWorkArea_UnknownWorkArea_KeepsMinimum()
    {
        Size lResult = MainWindow.ClampMinimumSizeToWorkArea( 960, 640, 0, -1 );

        Assert.Equal( 960, lResult.Width );
        Assert.Equal( 640, lResult.Height );
    }

    [Theory]
    [InlineData( 1440, 960, 2560, 1400, 1440, 960 )]
    [InlineData( 1440, 960, 1366, 728, 1366, 728 )]
    [InlineData( 1440, 960, 0, 0, 1440, 960 )]
    [InlineData( -5, -5, 1366, 728, 0, 0 )]
    public void ClampMinimumTrackSize_ClampsBothDimensionsToWorkArea( int pRequestedWidth, int pRequestedHeight, int pWorkAreaWidth, int pWorkAreaHeight, int pExpectedWidth, int pExpectedHeight )
    {
        ( int lWidth, int lHeight ) = MainWindow.ClampMinimumTrackSize( pRequestedWidth, pRequestedHeight, pWorkAreaWidth, pWorkAreaHeight );

        Assert.Equal( pExpectedWidth, lWidth );
        Assert.Equal( pExpectedHeight, lHeight );
    }
}
