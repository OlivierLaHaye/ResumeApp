using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using System.Windows.Media;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class ProjectCardViewModelImageLoadingTests
{
    private static ProjectCardViewModel Create( ResourcesService pResourcesService ) =>
        new( pResourcesService, "title", "ctx", "con", null, "imp", "tech", "base", "" );

    [Fact]
    public void ImagesStatusText_Initially_IsLoadingText()
    {
        var lResourcesService = new ResourcesService();
        ProjectCardViewModel lViewModel = Create( lResourcesService );

        Assert.Equal( lResourcesService[ "LabelImagesLoading" ], lViewModel.ImagesStatusText );
        Assert.True( lViewModel.IsImagesLoading );
    }

    [Fact]
    public async Task LoadImagesAsync_NoImages_ShowsNoImagesYetText()
    {
        var lResourcesService = new ResourcesService();
        ProjectCardViewModel lViewModel = Create( lResourcesService );

        await lViewModel.LoadImagesAsync( () => [] );

        Assert.Equal( lResourcesService[ "LabelProjectNoImagesYet" ], lViewModel.ImagesStatusText );
        Assert.False( lViewModel.IsImagesLoading );
        Assert.Empty( lViewModel.Images );
    }

    [Fact]
    public async Task LoadImagesAsync_AllDecodesFail_ShowsFailedText()
    {
        var lResourcesService = new ResourcesService();
        ProjectCardViewModel lViewModel = Create( lResourcesService );

        await lViewModel.LoadImagesAsync( () => new ImageSource?[] { null, null } );

        Assert.Equal( lResourcesService[ "LabelImagesLoadFailed" ], lViewModel.ImagesStatusText );
        Assert.False( lViewModel.IsImagesLoading );
    }

    [Fact]
    public async Task LoadImagesAsync_EnumerationThrows_ShowsFailedText()
    {
        var lResourcesService = new ResourcesService();
        ProjectCardViewModel lViewModel = Create( lResourcesService );

        await lViewModel.LoadImagesAsync( () => throw new InvalidOperationException( "boom" ) );

        Assert.Equal( lResourcesService[ "LabelImagesLoadFailed" ], lViewModel.ImagesStatusText );
    }

    [Fact]
    public async Task LoadImagesAsync_WithImages_PublishesAllAndClearsStatusText()
    {
        var lResourcesService = new ResourcesService();
        ProjectCardViewModel lViewModel = Create( lResourcesService );
        ImageSource lFirst = GalleryImageTestHelper.CreateFrozenImage();
        ImageSource lSecond = GalleryImageTestHelper.CreateFrozenImage();
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        await lViewModel.LoadImagesAsync( () => new ImageSource?[] { lFirst, lSecond } );

        Assert.Equal( [ lFirst, lSecond ], lViewModel.Images );
        Assert.Equal( string.Empty, lViewModel.ImagesStatusText );
        Assert.False( lViewModel.IsImagesLoading );
        Assert.Contains( "ImagesStatusText", lRaisedProperties );
        Assert.Contains( "IsImagesLoading", lRaisedProperties );
    }

    [Fact]
    public async Task LoadImagesAsync_FirstImageIsPublishedBeforeTheRestAreDecoded()
    {
        var lResourcesService = new ResourcesService();
        ProjectCardViewModel lViewModel = Create( lResourcesService );
        ImageSource lFirst = GalleryImageTestHelper.CreateFrozenImage();
        ImageSource lSecond = GalleryImageTestHelper.CreateFrozenImage();
        using var lReleaseSecond = new ManualResetEventSlim( false );

        IEnumerable<ImageSource?> Sequence()
        {
            yield return lFirst;
            lReleaseSecond.Wait( TimeSpan.FromSeconds( 10 ) );
            yield return lSecond;
        }

        Task lTask = lViewModel.LoadImagesAsync( Sequence );

        await GalleryImageTestHelper.WaitUntilAsync( () => lViewModel.Images.Count == 1 );

        Assert.False( lTask.IsCompleted );
        Assert.Same( lFirst, lViewModel.Images[ 0 ] );
        Assert.Equal( string.Empty, lViewModel.ImagesStatusText );

        lReleaseSecond.Set();
        await lTask;

        Assert.Equal( 2, lViewModel.Images.Count );
    }

    [Fact]
    public async Task LoadImagesAsync_CalledTwice_LoadsOnlyOnce()
    {
        var lResourcesService = new ResourcesService();
        ProjectCardViewModel lViewModel = Create( lResourcesService );
        ImageSource lImage = GalleryImageTestHelper.CreateFrozenImage();
        int lFactoryCallCount = 0;

        IEnumerable<ImageSource?> Factory()
        {
            Interlocked.Increment( ref lFactoryCallCount );
            yield return lImage;
        }

        await lViewModel.LoadImagesAsync( Factory );
        await lViewModel.LoadImagesAsync( Factory );

        Assert.Equal( 1, lFactoryCallCount );
        Assert.Single( lViewModel.Images );
    }

    [Fact]
    public async Task LoadImagesAsync_CalledWhileLoading_DoesNotDuplicateImages()
    {
        var lResourcesService = new ResourcesService();
        ProjectCardViewModel lViewModel = Create( lResourcesService );
        ImageSource lImage = GalleryImageTestHelper.CreateFrozenImage();
        using var lRelease = new ManualResetEventSlim( false );

        IEnumerable<ImageSource?> Blocking()
        {
            lRelease.Wait( TimeSpan.FromSeconds( 10 ) );
            yield return lImage;
        }

        Task lFirstTask = lViewModel.LoadImagesAsync( Blocking );
        Task lSecondTask = lViewModel.LoadImagesAsync( Blocking );

        await lSecondTask;
        lRelease.Set();
        await lFirstTask;

        Assert.Single( lViewModel.Images );
    }

    [Fact]
    public async Task ImagesStatusText_LanguageChange_RaisesAndReturnsLocalizedText()
    {
        var lResourcesService = new ResourcesService();
        lResourcesService.SetLanguage( AppLanguage.EnglishCanada );
        ProjectCardViewModel lViewModel = Create( lResourcesService );
        await lViewModel.LoadImagesAsync( () => [] );
        string lEnglishText = lViewModel.ImagesStatusText;
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "ImagesStatusText", lRaisedProperties );
        Assert.Equal( lResourcesService[ "LabelProjectNoImagesYet" ], lViewModel.ImagesStatusText );
        Assert.NotEqual( lEnglishText, lViewModel.ImagesStatusText );
    }
}
