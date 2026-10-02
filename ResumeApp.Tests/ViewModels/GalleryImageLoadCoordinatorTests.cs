using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using System.Windows.Media;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class GalleryImageLoadCoordinatorTests
{
    [Fact]
    public void MaximumConcurrentDecodes_IsTwo()
    {
        Assert.Equal( 2, GalleryImageLoadCoordinator.MaximumConcurrentDecodes );
    }

    [Theory]
    [InlineData( 0, 0, false, "Empty" )]
    [InlineData( 3, 0, false, "Failed" )]
    [InlineData( 0, 0, true, "Failed" )]
    [InlineData( 3, 2, false, "HasImages" )]
    [InlineData( 2, 1, true, "HasImages" )]
    public void Result_State_ReflectsCounts( int pAttempted, int pPublished, bool pHasFailed, string pExpectedStateName )
    {
        var lResult = new GalleryImageLoadResult( pAttempted, pPublished, pHasFailed );

        Assert.Equal( pExpectedStateName, lResult.State.ToString() );
    }

    [Fact]
    public void ResolveStatusText_MapsStatesToResourceKeys()
    {
        var lService = new ResourcesService();

        Assert.Equal( lService[ "LabelImagesLoading" ], GalleryImageLoadCoordinator.ResolveStatusText( lService, GalleryImageLoadState.Loading ) );
        Assert.Equal( lService[ "LabelProjectNoImagesYet" ], GalleryImageLoadCoordinator.ResolveStatusText( lService, GalleryImageLoadState.Empty ) );
        Assert.Equal( lService[ "LabelImagesLoadFailed" ], GalleryImageLoadCoordinator.ResolveStatusText( lService, GalleryImageLoadState.Failed ) );
        Assert.Equal( string.Empty, GalleryImageLoadCoordinator.ResolveStatusText( lService, GalleryImageLoadState.HasImages ) );
        Assert.NotEqual( string.Empty, lService[ "LabelImagesLoading" ] );
        Assert.NotEqual( string.Empty, lService[ "LabelProjectNoImagesYet" ] );
        Assert.NotEqual( string.Empty, lService[ "LabelImagesLoadFailed" ] );
    }

    [Fact]
    public async Task LoadAsync_PublishesNonNullImagesInOrderAndCountsAttempts()
    {
        ImageSource lFirst = GalleryImageTestHelper.CreateFrozenImage();
        ImageSource lSecond = GalleryImageTestHelper.CreateFrozenImage();
        var lPublished = new List<ImageSource>();

        GalleryImageLoadResult lResult = await GalleryImageLoadCoordinator.LoadAsync(
            () => new ImageSource?[] { lFirst, null, lSecond },
            lPublished.Add );

        Assert.Equal( [ lFirst, lSecond ], lPublished );
        Assert.Equal( 3, lResult.AttemptedCount );
        Assert.Equal( 2, lResult.PublishedCount );
        Assert.False( lResult.HasFailed );
    }

    [Fact]
    public async Task LoadAsync_FactoryThrows_ReportsFailure()
    {
        GalleryImageLoadResult lResult = await GalleryImageLoadCoordinator.LoadAsync(
            () => throw new InvalidOperationException( "boom" ),
            _ => { } );

        Assert.True( lResult.HasFailed );
        Assert.Equal( GalleryImageLoadState.Failed, lResult.State );
    }

    [Fact]
    public async Task LoadAsync_EnumerationThrowsAfterFirstImage_KeepsPublishedImage()
    {
        ImageSource lFirst = GalleryImageTestHelper.CreateFrozenImage();
        var lPublished = new List<ImageSource>();

        static IEnumerable<ImageSource?> Sequence( ImageSource pImage )
        {
            yield return pImage;
            throw new InvalidOperationException( "boom" );
        }

        GalleryImageLoadResult lResult = await GalleryImageLoadCoordinator.LoadAsync( () => Sequence( lFirst ), lPublished.Add );

        Assert.Single( lPublished );
        Assert.True( lResult.HasFailed );
        Assert.Equal( GalleryImageLoadState.HasImages, lResult.State );
    }

    [Fact]
    public async Task LoadAsync_FirstImageIsPublishedBeforeNextIsDecoded()
    {
        ImageSource lFirst = GalleryImageTestHelper.CreateFrozenImage();
        ImageSource lSecond = GalleryImageTestHelper.CreateFrozenImage();
        using var lReleaseSecond = new ManualResetEventSlim( false );
        var lPublished = new List<ImageSource>();

        IEnumerable<ImageSource?> Sequence()
        {
            yield return lFirst;
            lReleaseSecond.Wait( TimeSpan.FromSeconds( 10 ) );
            yield return lSecond;
        }

        Task<GalleryImageLoadResult> lTask = GalleryImageLoadCoordinator.LoadAsync( Sequence, pImage =>
        {
            lock ( lPublished )
            {
                lPublished.Add( pImage );
            }
        } );

        await GalleryImageTestHelper.WaitUntilAsync( () =>
        {
            lock ( lPublished )
            {
                return lPublished.Count == 1;
            }
        } );

        Assert.False( lTask.IsCompleted );
        Assert.Same( lFirst, lPublished[ 0 ] );

        lReleaseSecond.Set();
        GalleryImageLoadResult lResult = await lTask;

        Assert.Equal( 2, lResult.PublishedCount );
    }

    [Fact]
    public async Task RunLimitedAsync_NeverExceedsMaximumConcurrentDecodes()
    {
        int lCurrent = 0;
        int lMaximum = 0;
        using var lRelease = new ManualResetEventSlim( false );

        int Work()
        {
            int lNow = Interlocked.Increment( ref lCurrent );

            int lPrevious;
            do
            {
                lPrevious = Volatile.Read( ref lMaximum );
            }
            while ( lNow > lPrevious && Interlocked.CompareExchange( ref lMaximum, lNow, lPrevious ) != lPrevious );

            lRelease.Wait( TimeSpan.FromSeconds( 10 ) );
            Interlocked.Decrement( ref lCurrent );

            return lNow;
        }

        Task[] lTasks = Enumerable.Range( 0, 6 )
            .Select( _ => GalleryImageLoadCoordinator.RunLimitedAsync( Work ) )
            .ToArray();

        await GalleryImageTestHelper.WaitUntilAsync( () => Volatile.Read( ref lCurrent ) == GalleryImageLoadCoordinator.MaximumConcurrentDecodes );
        await Task.Delay( 150 );

        Assert.Equal( GalleryImageLoadCoordinator.MaximumConcurrentDecodes, Volatile.Read( ref lCurrent ) );
        Assert.Equal( GalleryImageLoadCoordinator.MaximumConcurrentDecodes, Volatile.Read( ref lMaximum ) );

        lRelease.Set();
        await Task.WhenAll( lTasks );

        Assert.Equal( GalleryImageLoadCoordinator.MaximumConcurrentDecodes, Volatile.Read( ref lMaximum ) );
    }

    [Fact]
    public async Task RunLimitedAsync_WorkThrows_ReleasesSlot()
    {
        for ( int lIndex = 0; lIndex < GalleryImageLoadCoordinator.MaximumConcurrentDecodes + 2; lIndex++ )
        {
            await Assert.ThrowsAsync<InvalidOperationException>( () =>
                GalleryImageLoadCoordinator.RunLimitedAsync<int>( () => throw new InvalidOperationException( "boom" ) ) );
        }

        int lResult = await GalleryImageLoadCoordinator.RunLimitedAsync( () => 7 ).WaitAsync( TimeSpan.FromSeconds( 10 ) );

        Assert.Equal( 7, lResult );
    }
}
