using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ResumeApp.AttachedProperties;
using ResumeApp.Controls;
using ResumeApp.Converters;
using ResumeApp.Pages;
using ResumeApp.Services;
using ResumeApp.Tests.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.Pages;

[Collection( MotionPolicyCollection.Name )]
public sealed class ExperiencePageLayoutTests
{
    private const double WideContentWidthLimit = 1320.0;

    private static readonly string[] sResourceDictionaryNames = [ "Converters", "Tokens", "Controls", "Theme.Dark" ];

    private sealed class ResourceScope : IDisposable
    {
        private readonly List<ResourceDictionary> mAddedDictionaries = [ ];

        public ResourceScope()
        {
            if ( Application.Current == null )
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }

            foreach ( string lName in sResourceDictionaryNames )
            {
                var lUri = new Uri( $"pack://application:,,,/ResumeApp;component/Resources/{lName}.xaml", UriKind.Absolute );
                var lDictionary = new ResourceDictionary { Source = lUri };

                Application.Current!.Resources.MergedDictionaries.Add( lDictionary );
                mAddedDictionaries.Add( lDictionary );
            }
        }

        public void Dispose()
        {
            foreach ( ResourceDictionary lDictionary in mAddedDictionaries )
            {
                Application.Current!.Resources.MergedDictionaries.Remove( lDictionary );
            }

            mAddedDictionaries.Clear();
        }
    }

    private sealed class HostedPage( ResourceScope pScope, Border pHost, ExperiencePage pPage, ExperiencePageViewModel pViewModel ) : IDisposable
    {
        public Border Host { get; } = pHost;

        public ExperiencePage Page { get; } = pPage;

        public ExperiencePageViewModel ViewModel { get; } = pViewModel;

        public void Dispose() => pScope.Dispose();
    }

    private static void Layout( FrameworkElement pHost, double pWidth, double pHeight )
    {
        pHost.InvalidateMeasure();
        pHost.Measure( new Size( pWidth, pHeight ) );
        pHost.Arrange( new Rect( 0.0, 0.0, pWidth, pHeight ) );
        pHost.UpdateLayout();
    }

    private static HostedPage CreateHostedPage( LayoutSizeClass pSizeClass, double pWidth, double pHeight )
    {
        var lScope = new ResourceScope();

        var lViewModel = new ExperiencePageViewModel( new ResourcesService(), new ThemeService() );
        var lPage = new ExperiencePage { DataContext = lViewModel };
        var lHost = new Border { Width = pWidth, Height = pHeight, Child = lPage };
        AdaptiveLayout.SetSizeClass( lHost, pSizeClass );

        Layout( lHost, pWidth, pHeight );

        return new HostedPage( lScope, lHost, lPage, lViewModel );
    }

    private static IEnumerable<DependencyObject> VisualDescendants( DependencyObject pRoot )
    {
        int lCount = VisualTreeHelper.GetChildrenCount( pRoot );

        for ( int lIndex = 0; lIndex < lCount; lIndex++ )
        {
            DependencyObject lChild = VisualTreeHelper.GetChild( pRoot, lIndex );
            yield return lChild;

            foreach ( DependencyObject lDescendant in VisualDescendants( lChild ) )
            {
                yield return lDescendant;
            }
        }
    }

    private static List<T> FindAllByName<T>( DependencyObject pRoot, string pName ) where T : FrameworkElement =>
        VisualDescendants( pRoot ).OfType<T>().Where( pElement => pElement.Name == pName ).ToList();

    private static T FindFirstByName<T>( DependencyObject pRoot, string pName ) where T : FrameworkElement
    {
        List<T> lMatches = FindAllByName<T>( pRoot, pName );
        Assert.NotEmpty( lMatches );
        return lMatches[ 0 ];
    }

    private static Rect BoundsIn( FrameworkElement pElement, FrameworkElement pAncestor ) =>
        pElement.TransformToAncestor( pAncestor ).TransformBounds( new Rect( 0.0, 0.0, pElement.ActualWidth, pElement.ActualHeight ) );

    private static bool HasScrollBarAncestor( DependencyObject pElement )
    {
        for ( DependencyObject? lParent = VisualTreeHelper.GetParent( pElement ); lParent != null; lParent = VisualTreeHelper.GetParent( lParent ) )
        {
            if ( lParent is System.Windows.Controls.Primitives.ScrollBar )
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertNoHorizontalOverflow( HostedPage pHosted )
    {
        foreach ( FrameworkElement lElement in VisualDescendants( pHosted.Host ).OfType<FrameworkElement>() )
        {
            if ( lElement.Visibility != Visibility.Visible || lElement.ActualWidth <= 0.0 || HasScrollBarAncestor( lElement ) )
            {
                continue;
            }

            Rect lBounds = BoundsIn( lElement, pHosted.Host );

            Assert.True(
                lBounds.Left >= -0.5 && lBounds.Right <= pHosted.Host.Width + 0.5,
                $"{lElement.GetType().Name} '{lElement.Name}' spans {lBounds.Left:F1}..{lBounds.Right:F1} in a {pHosted.Host.Width:F0}-wide host." );
        }

        Assert.True( pHosted.Page.mExperienceScrollViewer.ExtentWidth <= pHosted.Page.mExperienceScrollViewer.ViewportWidth + 0.5 );
    }

    private static IEnumerable<object[]> SizeCases() =>
    [
        [ LayoutSizeClass.Compact, 960.0, 640.0 ],
        [ LayoutSizeClass.Regular, 1400.0, 900.0 ],
        [ LayoutSizeClass.Wide, 2560.0, 1300.0 ]
    ];

    [StaFact]
    public void HostedPage_HasNoHorizontalOverflowInEverySizeClass()
    {
        foreach ( object[] lCase in SizeCases() )
        {
            using HostedPage lHosted = CreateHostedPage( ( LayoutSizeClass )lCase[ 0 ], ( double )lCase[ 1 ], ( double )lCase[ 2 ] );

            AssertNoHorizontalOverflow( lHosted );
        }
    }

    [StaFact]
    public void PageColumn_UsesTheSizeClassSideMarginsAndCapsWideAtTheContentLimit()
    {
        ( LayoutSizeClass SizeClass, double Width, double Height, double ExpectedMargin )[] lExpectations =
        [
            ( LayoutSizeClass.Compact, 960.0, 640.0, 20.0 ),
            ( LayoutSizeClass.Regular, 1400.0, 900.0, 32.0 ),
            ( LayoutSizeClass.Wide, 2560.0, 1300.0, 40.0 )
        ];

        foreach ( var lExpectation in lExpectations )
        {
            using HostedPage lHosted = CreateHostedPage( lExpectation.SizeClass, lExpectation.Width, lExpectation.Height );
            FrameworkElement lHeader = lHosted.Page.mHeaderStackPanel;
            FrameworkElement lList = lHosted.Page.mExperienceItemsControl;

            double lAvailableWidth = lExpectation.Width - ( 2.0 * lExpectation.ExpectedMargin );
            double lExpectedWidth = lExpectation.SizeClass == LayoutSizeClass.Wide ? Math.Min( lAvailableWidth, WideContentWidthLimit ) : lAvailableWidth;
            Assert.Equal( lExpectedWidth, lHeader.ActualWidth, 0.5 );
            Assert.True( lList.ActualWidth <= lExpectedWidth + 0.5 );

            Rect lHeaderBounds = BoundsIn( lHeader, lHosted.Host );
            Assert.Equal( ( lExpectation.Width - lHeader.ActualWidth ) / 2.0, lHeaderBounds.Left, 0.5 );

            Rect lListBounds = BoundsIn( lList, lHosted.Host );
            Assert.Equal( lHosted.Page.mExperienceScrollViewer.ViewportWidth, lListBounds.Left + lListBounds.Width + lListBounds.Left, 0.5 );
        }
    }

    [StaFact]
    public void WideContent_NeverExceedsTheContentLimit()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Wide, 2560.0, 1300.0 );

        foreach ( FrameworkElement lElement in new FrameworkElement[]
                 {
                     lHosted.Page.mHeaderStackPanel,
                     lHosted.Page.mTimelineBorder,
                     lHosted.Page.mExperienceItemsControl
                 } )
        {
            Assert.True( lElement.ActualWidth <= WideContentWidthLimit + 0.5, $"{lElement.Name} is {lElement.ActualWidth:F1} wide." );
        }
    }

    [StaFact]
    public void TimelineFoot_FollowsTheSizeClass()
    {
        ( LayoutSizeClass SizeClass, double Width, double Height, double FootHeight, bool HasHint, bool HasSubtitle )[] lExpectations =
        [
            ( LayoutSizeClass.Compact, 960.0, 640.0, 28.0, false, false ),
            ( LayoutSizeClass.Regular, 1400.0, 900.0, 32.0, true, true ),
            ( LayoutSizeClass.Wide, 2560.0, 1300.0, 32.0, true, true )
        ];

        foreach ( var lExpectation in lExpectations )
        {
            using HostedPage lHosted = CreateHostedPage( lExpectation.SizeClass, lExpectation.Width, lExpectation.Height );

            Assert.Equal( lExpectation.FootHeight, lHosted.Page.mTimelineFootGrid.ActualHeight );
            Assert.Equal( lExpectation.HasHint, lHosted.Page.mTimelineHintTextBlock.Visibility == Visibility.Visible );
            Assert.Equal( lExpectation.HasSubtitle, lHosted.Page.mSectionSubtitleTextBlock.Visibility == Visibility.Visible );
        }
    }

    [StaFact]
    public void TimelineFoot_ShowsLocalizedZoomAutomationNamesAndShowAllText()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );

        Assert.Equal( "Zoom out", System.Windows.Automation.AutomationProperties.GetName( lHosted.Page.mTimelineZoomOutButton ) );
        Assert.Equal( "Zoom in", System.Windows.Automation.AutomationProperties.GetName( lHosted.Page.mTimelineZoomInButton ) );
        Assert.Equal( "Show all", lHosted.Page.mTimelineShowAllButton.Content );
        Assert.Equal( "Drag to pan · Wheel or +/− to zoom · Arrows to move", lHosted.Page.mTimelineHintTextBlock.Text );
    }

    [StaFact]
    public void TimelineFoot_HintIsSingleLineAndTrimmed()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1100.0, 800.0 );
        TextBlock lHint = lHosted.Page.mTimelineHintTextBlock;

        Assert.Equal( TextWrapping.NoWrap, lHint.TextWrapping );
        Assert.Equal( TextTrimming.CharacterEllipsis, lHint.TextTrimming );
        Assert.Equal( 12.0, lHint.FontSize );
        Assert.True( lHint.ActualHeight < 20.0 );
    }

    [StaFact]
    public void ShowAllButton_IsDisabledWhileTheTimelineShowsEverything()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );
        TimelineControl lTimeline = lHosted.Page.mExperienceTimelineControl;
        Button lShowAll = lHosted.Page.mTimelineShowAllButton;

        Assert.True( lTimeline.IsShowingAll );
        Assert.False( lShowAll.IsEnabled );

        lHosted.Page.mTimelineZoomInButton.RaiseEvent( new RoutedEventArgs( Button.ClickEvent ) );
        Layout( lHosted.Host, 1400.0, 900.0 );

        Assert.False( lTimeline.IsShowingAll );
        Assert.True( lShowAll.IsEnabled );

        lShowAll.RaiseEvent( new RoutedEventArgs( Button.ClickEvent ) );
        Layout( lHosted.Host, 1400.0, 900.0 );

        Assert.True( lTimeline.IsShowingAll );
        Assert.False( lShowAll.IsEnabled );
    }

    [StaFact]
    public void ZoomButtons_CallTheTimelineZoomMethods()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );
        TimelineControl lTimeline = lHosted.Page.mExperienceTimelineControl;

        lHosted.Page.mTimelineZoomInButton.RaiseEvent( new RoutedEventArgs( Button.ClickEvent ) );
        double lZoomedIn = lTimeline.ZoomLevel;
        lHosted.Page.mTimelineZoomInButton.RaiseEvent( new RoutedEventArgs( Button.ClickEvent ) );
        double lZoomedInTwice = lTimeline.ZoomLevel;
        lHosted.Page.mTimelineZoomOutButton.RaiseEvent( new RoutedEventArgs( Button.ClickEvent ) );

        Assert.True( lZoomedInTwice > lZoomedIn );
        Assert.True( lTimeline.ZoomLevel < lZoomedInTwice );
    }

    [StaFact]
    public void CardBody_UsesTwoColumnsWithMetaOnTheRightInRegularAndSingleColumnInCompact()
    {
        using HostedPage lRegular = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );
        using HostedPage lCompact = CreateHostedPage( LayoutSizeClass.Compact, 960.0, 640.0 );

        WrapPanel lRegularMeta = FindFirstByName<WrapPanel>( lRegular.Page.mExperienceItemsControl, "mMetaFactsWrapPanel" );
        Assert.Equal( 2, Grid.GetColumn( lRegularMeta ) );
        Assert.Equal( Orientation.Vertical, lRegularMeta.Orientation );
        Assert.Equal( Visibility.Visible, FindFirstByName<Border>( lRegular.Page.mExperienceItemsControl, "mMetaDividerBorder" ).Visibility );

        StackPanel lRegularScope = FindFirstByName<StackPanel>( lRegular.Page.mExperienceItemsControl, "mScopeBulletsStackPanel" );
        Assert.Equal( 0, Grid.GetColumn( lRegularScope ) );
        Assert.True( lRegularScope.ActualWidth <= 720.5 );

        Border lRegularCard = FindFirstByName<Border>( lRegular.Page.mExperienceItemsControl, "mCardBorder" );
        Rect lMetaBounds = BoundsIn( lRegularMeta, lRegular.Host );
        Rect lCardBounds = BoundsIn( lRegularCard, lRegular.Host );
        Assert.Equal( lCardBounds.Right - 28.0 - 1.0, lMetaBounds.Right, 1.0 );
        Assert.True( BoundsIn( lRegularScope, lRegular.Host ).Right < lMetaBounds.Left );

        WrapPanel lCompactMeta = FindFirstByName<WrapPanel>( lCompact.Page.mExperienceItemsControl, "mMetaFactsWrapPanel" );
        Assert.Equal( 0, Grid.GetColumn( lCompactMeta ) );
        Assert.Equal( 3, Grid.GetColumnSpan( lCompactMeta ) );
        Assert.Equal( Orientation.Horizontal, lCompactMeta.Orientation );
        Assert.NotEqual( Visibility.Visible, FindFirstByName<Border>( lCompact.Page.mExperienceItemsControl, "mMetaDividerBorder" ).Visibility );

        StackPanel lCompactScope = FindFirstByName<StackPanel>( lCompact.Page.mExperienceItemsControl, "mScopeBulletsStackPanel" );
        StackPanel lCompactTech = FindFirstByName<StackPanel>( lCompact.Page.mExperienceItemsControl, "mTechStackPanel" );
        double lMetaBottom = BoundsIn( lCompactMeta, lCompact.Host ).Bottom;
        double lScopeTop = BoundsIn( lCompactScope, lCompact.Host ).Top;
        double lScopeBottom = BoundsIn( lCompactScope, lCompact.Host ).Bottom;
        double lTechTop = BoundsIn( lCompactTech, lCompact.Host ).Top;
        Assert.True( lMetaBottom <= lScopeTop + 0.5 );
        Assert.True( lScopeBottom <= lTechTop + 0.5 );
    }

    [StaFact]
    public void CardBody_BulletsUseTheFullLeftColumnInsteadOfStoppingAt760()
    {
        using HostedPage lWide = CreateHostedPage( LayoutSizeClass.Wide, 2560.0, 1300.0 );

        StackPanel lScope = FindFirstByName<StackPanel>( lWide.Page.mExperienceItemsControl, "mScopeBulletsStackPanel" );

        Assert.Equal( 720.0, lScope.ActualWidth, 0.5 );
    }

    [StaFact]
    public void OnlyTheSelectedCardShowsTheOutlineAndTheMarkerRing()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );

        List<Border> lOutlines = FindAllByName<Border>( lHosted.Page.mExperienceItemsControl, "mCardSelectionOutlineBorder" );
        List<Border> lRings = FindAllByName<Border>( lHosted.Page.mExperienceItemsControl, "mMarkerSelectionRingBorder" );

        Assert.Equal( lHosted.ViewModel.TimelineEntries.Count, lOutlines.Count );
        Assert.Single( lOutlines, pOutline => pOutline.Visibility == Visibility.Visible );
        Assert.Single( lRings, pRing => pRing.Visibility == Visibility.Visible );
        Assert.All( lOutlines, pOutline => Assert.False( pOutline.IsHitTestVisible ) );
    }

    [StaFact]
    public void SelectedCardOutline_DoesNotChangeTheCardSize()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );
        List<Border> lCards = FindAllByName<Border>( lHosted.Page.mExperienceItemsControl, "mCardBorder" );
        double[] lHeightsBefore = lCards.Select( pCard => pCard.ActualHeight ).ToArray();

        lHosted.ViewModel.SelectedTimelineEntry = lHosted.ViewModel.TimelineEntries[ 2 ];
        Layout( lHosted.Host, 1400.0, 900.0 );

        Assert.Equal( lHeightsBefore, lCards.Select( pCard => pCard.ActualHeight ).ToArray() );
        Assert.All( lCards, pCard => Assert.Equal( new Thickness( 1.0 ), pCard.BorderThickness ) );
    }

    [StaFact]
    public void CardFocusVisual_UsesAnOutsetRingMatchingTheCardRadius()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );
        Button lButton = FindFirstByName<Button>( lHosted.Page.mExperienceItemsControl, "mExperienceCardButton" );
        Border lCard = FindFirstByName<Border>( lHosted.Page.mExperienceItemsControl, "mCardBorder" );

        var lStyle = Assert.IsType<Style>( lButton.FocusVisualStyle );
        var lTemplateSetter = Assert.IsType<Setter>( lStyle.Setters.Single( pSetter => pSetter is Setter lSetter && lSetter.Property == Control.TemplateProperty ) );
        var lTemplate = Assert.IsType<ControlTemplate>( lTemplateSetter.Value );
        var lRing = Assert.IsType<Border>( lTemplate.LoadContent() );

        Assert.Equal( lCard.CornerRadius.TopLeft + 3.0, lRing.CornerRadius.TopLeft );
        Assert.Equal( 2.0, lRing.BorderThickness.Left );
        Assert.True( lRing.Margin.Top < 0.0 && lRing.Margin.Right < 0.0 );
    }

    [StaFact]
    public void RailMarker_UsesTheRoleStrongFillAndEdgeStroke()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );
        List<Border> lMarkers = FindAllByName<Border>( lHosted.Page.mExperienceItemsControl, "mMarkerBorder" );

        Assert.Equal( lHosted.ViewModel.TimelineEntries.Count, lMarkers.Count );
        Assert.All( lMarkers, pMarker =>
        {
            Assert.Equal( new Thickness( 1.0 ), pMarker.BorderThickness );
            Assert.IsAssignableFrom<SolidColorBrush>( pMarker.Background );
            Assert.IsAssignableFrom<SolidColorBrush>( pMarker.BorderBrush );
            Assert.NotEqual( ( ( SolidColorBrush )pMarker.Background ).Color, ( ( SolidColorBrush )pMarker.BorderBrush ).Color );
        } );

        Assert.Equal( ( ( SolidColorBrush )Application.Current!.Resources[ "CommonBlueStrongBrush" ] ).Color, ( ( SolidColorBrush )lMarkers[ 0 ].Background ).Color );
        Assert.Equal( ( ( SolidColorBrush )Application.Current.Resources[ "CommonBlueEdgeBrush" ] ).Color, ( ( SolidColorBrush )lMarkers[ 0 ].BorderBrush ).Color );
    }

    [StaFact]
    public void PaletteIndexConverter_EdgeParameter_ReturnsTheEdgeBrushOfTheSameHue()
    {
        using var lScope = new ResourceScope();
        var lConverter = new PaletteIndexToBrushConverter();

        object lStrong = lConverter.Convert( 1, typeof( Brush ), null!, CultureInfo.InvariantCulture );
        object lEdge = lConverter.Convert( 1, typeof( Brush ), PaletteIndexToBrushConverter.EdgeParameter, CultureInfo.InvariantCulture );

        Assert.Same( Application.Current!.Resources[ "CommonGreenStrongBrush" ], lStrong );
        Assert.Same( Application.Current.Resources[ "CommonGreenEdgeBrush" ], lEdge );
    }

    [StaFact]
    public void SelectionActivated_FocusesTheSelectedRoleCard()
    {
        using HostedPage lHosted = CreateHostedPage( LayoutSizeClass.Regular, 1400.0, 900.0 );
        var lWindow = new Window
        {
            Width = 1400.0,
            Height = 900.0,
            Left = -32000.0,
            Top = -32000.0,
            ShowInTaskbar = false,
            ShowActivated = false,
            WindowStyle = WindowStyle.None
        };

        try
        {
            lHosted.Host.Child = null;
            lWindow.Content = lHosted.Page;
            lWindow.Show();
            lWindow.UpdateLayout();

            lHosted.ViewModel.SelectedTimelineEntry = lHosted.ViewModel.TimelineEntries[ 2 ];
            lHosted.Page.mExperienceTimelineControl.RaiseEvent( new RoutedEventArgs( TimelineControl.sSelectionActivatedEvent ) );

            var lFocused = Assert.IsType<Button>( FocusManager.GetFocusedElement( lWindow ) );
            Assert.Equal( lHosted.ViewModel.SelectedTimelineEntry, lFocused.DataContext );
        }
        finally
        {
            lWindow.Close();
        }
    }
}
