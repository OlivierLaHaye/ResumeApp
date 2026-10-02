using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

public sealed class ImageDecodeServiceTests
{
    [Theory]
    [InlineData( 1920, 1080, 1440 )]
    [InlineData( 2560, 1440, 1920 )]
    [InlineData( 3840, 2160, 2880 )]
    [InlineData( 1080, 1920, 1440 )]
    [InlineData( 1280, 720, 1280 )]
    [InlineData( 7680, 4320, 3840 )]
    [InlineData( 0, 0, 1280 )]
    [InlineData( -5, -5, 1280 )]
    public void ComputePreviewLongEdgePixels_ClampsScreenFraction( int pScreenWidth, int pScreenHeight, int pExpected )
    {
        Assert.Equal( pExpected, ImageDecodeService.ComputePreviewLongEdgePixels( pScreenWidth, pScreenHeight ) );
    }

    [Fact]
    public void ComputeDecodeSize_LandscapeLargerThanLimit_DecodesWidthOnly()
    {
        ImageDecodeSize lSize = ImageDecodeService.ComputeDecodeSize( 6000, 4000, 1440 );

        Assert.Equal( new ImageDecodeSize( 1440, 0 ), lSize );
        Assert.False( lSize.IsNative );
    }

    [Fact]
    public void ComputeDecodeSize_PortraitLargerThanLimit_DecodesHeightOnly()
    {
        ImageDecodeSize lSize = ImageDecodeService.ComputeDecodeSize( 4000, 6000, 1440 );

        Assert.Equal( new ImageDecodeSize( 0, 1440 ), lSize );
    }

    [Fact]
    public void ComputeDecodeSize_SquareLargerThanLimit_DecodesWidthOnly()
    {
        Assert.Equal( new ImageDecodeSize( 2000, 0 ), ImageDecodeService.ComputeDecodeSize( 5000, 5000, 2000 ) );
    }

    [Theory]
    [InlineData( 1200, 800, 1440 )]
    [InlineData( 1440, 900, 1440 )]
    [InlineData( 0, 900, 1440 )]
    [InlineData( 1200, 0, 1440 )]
    [InlineData( 6000, 4000, 0 )]
    public void ComputeDecodeSize_SmallOrInvalidInput_ReturnsNative( int pWidth, int pHeight, int pLimit )
    {
        Assert.True( ImageDecodeService.ComputeDecodeSize( pWidth, pHeight, pLimit ).IsNative );
    }

    [Fact]
    public void PreviewLongEdgePixels_IsWithinBounds()
    {
        int lValue = ImageDecodeService.PreviewLongEdgePixels;

        Assert.InRange( lValue, ImageDecodeService.MinimumPreviewLongEdgePixels, ImageDecodeService.MaximumPreviewLongEdgePixels );
    }

    [Fact]
    public void TryCreatePreviewImage_NullUri_ReturnsNull()
    {
        Assert.Null( ImageDecodeService.TryCreatePreviewImage( null ) );
    }

    [Fact]
    public void TryCreateFullResolutionImage_Null_ReturnsNull()
    {
        Assert.Null( ImageDecodeService.TryCreateFullResolutionImage( null ) );
    }
}
