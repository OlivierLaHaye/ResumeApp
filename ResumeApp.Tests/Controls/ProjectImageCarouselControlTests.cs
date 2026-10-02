using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ResumeApp.Controls;
using ResumeApp.Services;
using ResumeApp.Tests.Services;
using Xunit;

namespace ResumeApp.Tests.Controls;

[Collection( MotionPolicyCollection.Name )]
public sealed class ProjectImageCarouselControlTests
{
    private static List<ImageSource> CreateImages( int pCount )
    {
        var lImages = new List<ImageSource>();

        for ( int lIndex = 0; lIndex < pCount; lIndex++ )
        {
            BitmapSource lBitmap = BitmapSource.Create( 2, 2, 96, 96, PixelFormats.Bgra32, null, new byte[ 16 ], 8 );
            lBitmap.Freeze();
            lImages.Add( lBitmap );
        }

        return lImages;
    }

    private static ProjectImageCarouselControl CreateControl( int pImageCount )
    {
        EnsureTokensLoaded();

        var lControl = new ProjectImageCarouselControl();

        if ( pImageCount > 0 )
        {
            lControl.Images = CreateImages( pImageCount );
        }

        return lControl;
    }

    private static Image GetCurrentImageSlot( ProjectImageCarouselControl pControl ) =>
        ( Image )pControl.FindName( "mCurrentImageImage" );

    private static void EnsureTokensLoaded()
    {
        if ( Application.Current == null )
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        var lTokensUri = new Uri( "pack://application:,,,/ResumeApp;component/Resources/Tokens.xaml", UriKind.Absolute );

        if ( !Application.Current!.Resources.MergedDictionaries.Any( pDictionary =>
                pDictionary.Source == lTokensUri ) )
        {
            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary { Source = lTokensUri } );
        }
    }

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
        EnsureTokensLoaded();

        var lException = Record.Exception( () => new ProjectImageCarouselControl() );

        Assert.Null( lException );
    }

    [StaFact]
    public void Images_DefaultIsNull()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.Null( lControl.Images );
    }

    [StaFact]
    public void SelectedIndex_DefaultIsZero()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.Equal( 0, lControl.SelectedIndex );
    }

    [StaFact]
    public void IsFullscreen_DefaultIsFalse()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.False( lControl.IsFullscreen );
    }

    [StaFact]
    public void IsOpenOnClickEnabled_DefaultIsFalse()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.False( lControl.IsOpenOnClickEnabled );
    }

    [StaFact]
    public void ResourcesService_DefaultIsNull_AndDependencyPropertyIsRegistered()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.NotNull( ProjectImageCarouselControl.sResourcesServiceProperty );
        Assert.Null( lControl.ResourcesService );
    }

    [StaFact]
    public void TryHandleKey_Right_AdvancesAndWraps()
    {
        ProjectImageCarouselControl lControl = CreateControl( 3 );

        Assert.True( lControl.TryHandleKey( Key.Right, ModifierKeys.None, null ) );
        Assert.Equal( 1, lControl.SelectedIndex );

        Assert.True( lControl.TryHandleKey( Key.Right, ModifierKeys.None, null ) );
        Assert.True( lControl.TryHandleKey( Key.Right, ModifierKeys.None, null ) );
        Assert.Equal( 0, lControl.SelectedIndex );
    }

    [StaFact]
    public void TryHandleKey_Left_GoesBackAndWraps()
    {
        ProjectImageCarouselControl lControl = CreateControl( 3 );

        Assert.True( lControl.TryHandleKey( Key.Left, ModifierKeys.None, null ) );
        Assert.Equal( 2, lControl.SelectedIndex );

        Assert.True( lControl.TryHandleKey( Key.Left, ModifierKeys.None, null ) );
        Assert.Equal( 1, lControl.SelectedIndex );
    }

    [StaFact]
    public void TryHandleKey_HomeAndEnd_JumpToFirstAndLast()
    {
        ProjectImageCarouselControl lControl = CreateControl( 5 );
        lControl.SelectedIndex = 2;

        Assert.True( lControl.TryHandleKey( Key.End, ModifierKeys.None, null ) );
        Assert.Equal( 4, lControl.SelectedIndex );

        Assert.True( lControl.TryHandleKey( Key.Home, ModifierKeys.None, null ) );
        Assert.Equal( 0, lControl.SelectedIndex );
    }

    [StaFact]
    public void TryHandleKey_UnrelatedKeys_StayUnhandled()
    {
        ProjectImageCarouselControl lControl = CreateControl( 3 );
        lControl.SelectedIndex = 1;

        Assert.False( lControl.TryHandleKey( Key.A, ModifierKeys.None, null ) );
        Assert.False( lControl.TryHandleKey( Key.Up, ModifierKeys.None, null ) );
        Assert.False( lControl.TryHandleKey( Key.Down, ModifierKeys.None, null ) );
        Assert.False( lControl.TryHandleKey( Key.PageDown, ModifierKeys.None, null ) );
        Assert.Equal( 1, lControl.SelectedIndex );
    }

    [StaFact]
    public void TryHandleKey_WithControlModifier_StaysUnhandled()
    {
        ProjectImageCarouselControl lControl = CreateControl( 3 );

        Assert.False( lControl.TryHandleKey( Key.Right, ModifierKeys.Control, null ) );
        Assert.Equal( 0, lControl.SelectedIndex );
    }

    [StaFact]
    public void TryHandleKey_WithSingleOrNoImage_StaysUnhandled()
    {
        ProjectImageCarouselControl lSingleControl = CreateControl( 1 );
        ProjectImageCarouselControl lEmptyControl = CreateControl( 0 );

        foreach ( Key lKey in new[] { Key.Left, Key.Right, Key.Home, Key.End, Key.Enter } )
        {
            Assert.False( lEmptyControl.TryHandleKey( lKey, ModifierKeys.None, null ) );
        }

        foreach ( Key lKey in new[] { Key.Left, Key.Right, Key.Home, Key.End } )
        {
            Assert.False( lSingleControl.TryHandleKey( lKey, ModifierKeys.None, null ) );
        }

        Assert.Equal( 0, lSingleControl.SelectedIndex );
    }

    [StaFact]
    public void TryHandleKey_EnterWhenFullscreen_StaysUnhandled()
    {
        ProjectImageCarouselControl lControl = CreateControl( 3 );
        lControl.IsFullscreen = true;

        Assert.False( lControl.TryHandleKey( Key.Enter, ModifierKeys.None, null ) );
    }

    [StaFact]
    public void TryHandleKey_EnterFromNavigationButton_StaysUnhandled()
    {
        ProjectImageCarouselControl lControl = CreateControl( 3 );
        var lButton = ( Button )lControl.FindName( "mNextButton" );

        Assert.False( lControl.TryHandleKey( Key.Enter, ModifierKeys.None, lButton ) );
    }

    [StaFact]
    public void AutomationName_FormatsImagePositionFromResourcesService()
    {
        ProjectImageCarouselControl lControl = CreateControl( 0 );
        var lResourcesService = new ResourcesService();
        lControl.ResourcesService = lResourcesService;

        lControl.Images = CreateImages( 3 );
        lControl.SelectedIndex = 1;

        Assert.Equal( "Image 2 of 3", AutomationProperties.GetName( lControl ) );

        lControl.SelectedIndex = 2;

        Assert.Equal( "Image 3 of 3", AutomationProperties.GetName( lControl ) );
    }

    [StaFact]
    public void AutomationName_UpdatesOnLanguageChange()
    {
        ProjectImageCarouselControl lControl = CreateControl( 0 );
        var lResourcesService = new ResourcesService();
        lControl.ResourcesService = lResourcesService;
        lControl.Images = CreateImages( 4 );
        lControl.SelectedIndex = 1;

        try
        {
            lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

            Assert.Equal( "Image 2 sur 4", AutomationProperties.GetName( lControl ) );
        }
        finally
        {
            lResourcesService.SetLanguage( AppLanguage.EnglishCanada );
        }

        Assert.Equal( "Image 2 of 4", AutomationProperties.GetName( lControl ) );
    }

    [StaFact]
    public void AutomationHelpText_UsesExpandAndDragHint_AndDragOnlyWhenFullscreen()
    {
        ProjectImageCarouselControl lControl = CreateControl( 0 );
        var lResourcesService = new ResourcesService();
        lControl.ResourcesService = lResourcesService;
        lControl.Images = CreateImages( 2 );

        Assert.Equal( lResourcesService[ "HintProjectImageCarouselExpandAndDrag" ], AutomationProperties.GetHelpText( lControl ) );
        Assert.False( string.IsNullOrWhiteSpace( AutomationProperties.GetHelpText( lControl ) ) );

        lControl.IsFullscreen = true;

        Assert.Equal( lResourcesService[ "HintProjectImageCarouselDragOnly" ], AutomationProperties.GetHelpText( lControl ) );
    }

    [StaFact]
    public void SelectedIndexChange_WhenMotionReduced_AppliesSlotStateWithoutAnimationClocks()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( false );

            ProjectImageCarouselControl lControl = CreateControl( 4 );
            lControl.SelectedIndex = 1;

            Image lCurrentSlot = GetCurrentImageSlot( lControl );
            var lTransforms = ( TransformGroup )lCurrentSlot.RenderTransform;

            Assert.False( lCurrentSlot.HasAnimatedProperties );
            Assert.All( lTransforms.Children, pTransform => Assert.False( pTransform.HasAnimatedProperties ) );
            Assert.Equal( Visibility.Visible, lCurrentSlot.Visibility );
            Assert.True( lCurrentSlot.Opacity > 0.0 );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }

    [StaFact]
    public void SelectedIndexChange_WhenMotionEnabled_AnimatesSlotTransforms()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( true );

            ProjectImageCarouselControl lControl = CreateControl( 4 );
            lControl.SelectedIndex = 1;

            Image lCurrentSlot = GetCurrentImageSlot( lControl );
            var lTransforms = ( TransformGroup )lCurrentSlot.RenderTransform;

            Assert.Contains( lTransforms.Children, pTransform => pTransform.HasAnimatedProperties );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }

    [StaFact]
    public void SelectedIndexChange_WhenMotionDisabledAfterAnimationStarted_CancelsRunningAnimations()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( true );

            ProjectImageCarouselControl lControl = CreateControl( 4 );
            lControl.SelectedIndex = 1;

            MotionPolicy.SetAnimationEnabledOverride( false );
            lControl.SelectedIndex = 2;

            var lTransforms = ( TransformGroup )GetCurrentImageSlot( lControl ).RenderTransform;

            Assert.All( lTransforms.Children, pTransform => Assert.False( pTransform.HasAnimatedProperties ) );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }

    [StaFact]
    public void DisplayImageProvider_OverridesSlotSource()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( false );

            ProjectImageCarouselControl lControl = CreateControl( 3 );
            BitmapSource lReplacement = BitmapSource.Create( 4, 4, 96, 96, PixelFormats.Bgra32, null, new byte[ 64 ], 16 );
            lReplacement.Freeze();

            lControl.DisplayImageProvider = pIndex => pIndex == 0 ? lReplacement : null;
            lControl.RefreshDisplayedImages();

            Assert.Same( lReplacement, GetCurrentImageSlot( lControl ).Source );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }
}
