// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using ResumeApp.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ResumeApp.Controls
{
	public partial class ProjectImageCarouselControl
	{
		private const double MinimumDragThresholdPixels = 60.0;
		private const double DragThresholdRatio = 0.085;
		private const double DragDeltaScale = 0.65;

		private const string HintProjectImageCarouselExpandAndDragResourceKey = "HintProjectImageCarouselExpandAndDrag";
		private const string HintProjectImageCarouselDragOnlyResourceKey = "HintProjectImageCarouselDragOnly";

		public static readonly DependencyProperty sImagesProperty =
			DependencyProperty.Register(
				nameof( Images ),
				typeof( IList ),
				typeof( ProjectImageCarouselControl ),
				new FrameworkPropertyMetadata( null, OnImagesChanged ) );

		public static readonly DependencyProperty sSelectedIndexProperty =
			DependencyProperty.Register(
				nameof( SelectedIndex ),
				typeof( int ),
				typeof( ProjectImageCarouselControl ),
				new FrameworkPropertyMetadata( 0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged ) );

		public static readonly DependencyProperty sPlaceholderTextProperty =
			DependencyProperty.Register(
				nameof( PlaceholderText ),
				typeof( string ),
				typeof( ProjectImageCarouselControl ),
				new FrameworkPropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty sIsFullscreenProperty =
			DependencyProperty.Register(
				nameof( IsFullscreen ),
				typeof( bool ),
				typeof( ProjectImageCarouselControl ),
				new FrameworkPropertyMetadata( false, OnIsFullscreenChanged ) );

		public static readonly DependencyProperty sIsOpenOnClickEnabledProperty =
			DependencyProperty.Register(
				nameof( IsOpenOnClickEnabled ),
				typeof( bool ),
				typeof( ProjectImageCarouselControl ),
				new FrameworkPropertyMetadata( false, OnIsOpenOnClickEnabledChanged ) );

		private static readonly Dictionary<string, ImageSource> sCachedImageSourcesByUri = new( StringComparer.OrdinalIgnoreCase );

		private static readonly TimeSpan sTransitionDuration = TimeSpan.FromMilliseconds( 240 );

		private static readonly ResourceManager sFallbackResourceManager = CreateFallbackResourceManager();

		private readonly IEasingFunction mCarouselEasingFunction;
		private readonly ProjectImageCarouselAdjacentImagesVisualService mAdjacentImagesVisualService;

		private bool mIsUpdatingSelectedIndexInternally;

		private bool mIsDragInProgress;
		private Point mDragLastPosition;
		private double mDragThresholdPixels;
		private double mDragAccumulatedHorizontalDelta;
		private bool mHasNavigatedDuringDrag;
		private bool mHasViewerOpenSuppressedDueToDragNavigation;

		private readonly Cursor mDefaultCursor;
		private Cursor mPreviousCursor;

		private INotifyCollectionChanged mImagesCollectionChangedNotifier;
		private bool mHasPendingImagesCollectionRefresh;

		private ResourcesService mResourcesServiceForHint;

		public IList Images
		{
			get => ( IList )GetValue( sImagesProperty );
			set => SetValue( sImagesProperty, value );
		}

		public int SelectedIndex
		{
			get => ( int )GetValue( sSelectedIndexProperty );
			set => SetValue( sSelectedIndexProperty, value );
		}

		public string PlaceholderText
		{
			get => ( string )GetValue( sPlaceholderTextProperty );
			set => SetValue( sPlaceholderTextProperty, value );
		}

		public bool IsFullscreen
		{
			get => ( bool )GetValue( sIsFullscreenProperty );
			set => SetValue( sIsFullscreenProperty, value );
		}

		public bool IsOpenOnClickEnabled
		{
			get => ( bool )GetValue( sIsOpenOnClickEnabledProperty );
			set => SetValue( sIsOpenOnClickEnabledProperty, value );
		}

		public ProjectImageCarouselControl()
		{
			mCarouselEasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
			mAdjacentImagesVisualService = new ProjectImageCarouselAdjacentImagesVisualService();

			InitializeComponent();

			mDefaultCursor = Cursor;

			Loaded += OnControlLoaded;
			Unloaded += OnControlUnloaded;
			DataContextChanged += OnControlDataContextChanged;
			MouseLeave += OnRootMouseLeave;
		}

		private static ResourceManager CreateFallbackResourceManager()
		{
			Assembly lAssembly = typeof( ProjectImageCarouselControl ).Assembly;
			string lAssemblyName = lAssembly.GetName().Name ?? "ResumeApp";

			string[] lCandidateBaseNames =
			{
				$"{lAssemblyName}.Properties.Resources",
				$"{lAssemblyName}.Resources",
				"ResumeApp.Properties.Resources",
				"ResumeApp.Resources"
			};

			foreach ( string lBaseName in lCandidateBaseNames )
			{
				ResourceManager lCandidate = CreateResourceManagerIfValid( lAssembly, lBaseName );
				if ( lCandidate != null )
				{
					return lCandidate;
				}
			}

			return new ResourceManager( $"{lAssemblyName}.Properties.Resources", lAssembly );
		}

		private static ResourceManager CreateResourceManagerIfValid( Assembly pAssembly, string pBaseName )
		{
			if ( string.IsNullOrWhiteSpace( pBaseName ) )
			{
				return null;
			}

			try
			{
				var lManager = new ResourceManager( pBaseName, pAssembly );
				lManager.GetResourceSet( CultureInfo.InvariantCulture, true, false );
				return lManager;
			}
			catch ( MissingManifestResourceException )
			{
				return null;
			}
		}

		private static void OnImagesChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not ProjectImageCarouselControl lControl )
			{
				return;
			}

			lControl.AttachToImagesCollectionChanged();

			lControl.EnsureSelectedIndexIsValid();
			lControl.UpdateAllVisuals( false );
			lControl.UpdateHoverCursorFromMouse();
		}

		private static void OnSelectedIndexChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not ProjectImageCarouselControl lControl || lControl.mIsUpdatingSelectedIndexInternally )
			{
				return;
			}

			lControl.EnsureSelectedIndexIsValid();
			lControl.UpdateAllVisuals( true );
		}

		private static void OnIsFullscreenChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not ProjectImageCarouselControl lControl )
			{
				return;
			}

			lControl.UpdateAllVisuals( false );
			lControl.UpdateHoverCursorFromMouse();
		}

		private static void OnIsOpenOnClickEnabledChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not ProjectImageCarouselControl lControl )
			{
				return;
			}

			lControl.UpdateHoverCursorFromMouse();
		}

		private static int WrapIndex( int pIndex, int pCount )
		{
			if ( pCount <= 0 )
			{
				return -1;
			}

			int lWrappedIndex = pIndex % pCount;
			if ( lWrappedIndex < 0 )
			{
				lWrappedIndex += pCount;
			}

			return lWrappedIndex;
		}

		private static ImageSource ConvertToImageSource( object pItem )
		{
			switch ( pItem )
			{
				case null:
					{
						return null;
					}
				case ImageSource lAlreadyImageSource:
					{
						return lAlreadyImageSource;
					}
			}

			if ( pItem is not string lUriText || string.IsNullOrWhiteSpace( lUriText ) )
			{
				return null;
			}

			if ( sCachedImageSourcesByUri.TryGetValue( lUriText, out ImageSource lCachedImageSource ) )
			{
				return lCachedImageSource;
			}

			ImageSource lCreatedImageSource = null;

			try
			{
				var lBitmapImage = new BitmapImage();
				lBitmapImage.BeginInit();
				lBitmapImage.UriSource = new Uri( lUriText, UriKind.RelativeOrAbsolute );
				lBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				lBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				lBitmapImage.EndInit();
				lBitmapImage.Freeze();

				lCreatedImageSource = lBitmapImage;
			}
			catch ( Exception )
			{
				return null;
			}

			sCachedImageSourcesByUri[ lUriText ] = lCreatedImageSource;
			return lCreatedImageSource;
		}

		private static bool IsDescendantOf( DependencyObject pElement, DependencyObject pPotentialAncestor )
		{
			if ( pElement == null || pPotentialAncestor == null )
			{
				return false;
			}

			DependencyObject lCurrentElement = pElement;
			while ( lCurrentElement != null )
			{
				if ( ReferenceEquals( lCurrentElement, pPotentialAncestor ) )
				{
					return true;
				}

				lCurrentElement = VisualTreeHelper.GetParent( lCurrentElement );
			}

			return false;
		}

		private static T FindAncestor<T>( DependencyObject pElement ) where T : DependencyObject
		{
			DependencyObject lCurrentElement = pElement;
			while ( lCurrentElement != null )
			{
				if ( lCurrentElement is T lTypedElement )
				{
					return lTypedElement;
				}

				lCurrentElement = VisualTreeHelper.GetParent( lCurrentElement );
			}

			return null;
		}

		private static void StopAndSet( DependencyObject pTarget, DependencyProperty pProperty, double pValue )
		{
			if ( pTarget is Animatable lAnimatable )
			{
				lAnimatable.BeginAnimation( pProperty, null );
			}

			pTarget.SetValue( pProperty, pValue );
		}

		private static DependencyObject GetParentDependencyObject( DependencyObject pElement )
		{
			if ( pElement == null )
			{
				return null;
			}

			DependencyObject lVisualParent = VisualTreeHelper.GetParent( pElement );
			return lVisualParent ?? LogicalTreeHelper.GetParent( pElement );
		}

		private static int GetMouseWheelNotchCount( int pWheelDelta )
		{
			double lNotchCount = Math.Abs( ( double )pWheelDelta ) / 120.0;
			int lRoundedNotchCount = ( int )Math.Round( lNotchCount, MidpointRounding.AwayFromZero );
			return Math.Max( 1, lRoundedNotchCount );
		}

		private static void ApplyMouseWheelPages( ScrollViewer pScrollViewer, int pNotchCount, bool pIsScrollingUp )
		{
			for ( int lNotchIndex = 0; lNotchIndex < pNotchCount; lNotchIndex++ )
			{
				if ( pIsScrollingUp )
				{
					pScrollViewer.PageUp();
					continue;
				}

				pScrollViewer.PageDown();
			}
		}

		private static void ApplyMouseWheelLines( ScrollViewer pScrollViewer, int pNotchCount, int pLinesPerNotch, bool pIsScrollingUp )
		{
			int lLineCount = Math.Max( 1, pLinesPerNotch );

			for ( int lNotchIndex = 0; lNotchIndex < pNotchCount; lNotchIndex++ )
			{
				for ( int lLineIndex = 0; lLineIndex < lLineCount; lLineIndex++ )
				{
					if ( pIsScrollingUp )
					{
						pScrollViewer.LineUp();
						continue;
					}

					pScrollViewer.LineDown();
				}
			}
		}

		private static void ApplyMouseWheelToScrollViewer( ScrollViewer pScrollViewer, int pWheelDelta )
		{
			if ( pScrollViewer == null || pWheelDelta == 0 )
			{
				return;
			}

			int lNotchCount = GetMouseWheelNotchCount( pWheelDelta );
			bool lIsScrollingUp = pWheelDelta > 0;

			int lWheelScrollLines = SystemParameters.WheelScrollLines;
			if ( lWheelScrollLines < 0 )
			{
				ApplyMouseWheelPages( pScrollViewer, lNotchCount, lIsScrollingUp );
				return;
			}

			if ( lWheelScrollLines == 0 )
			{
				return;
			}

			ApplyMouseWheelLines( pScrollViewer, lNotchCount, lWheelScrollLines, lIsScrollingUp );
		}

		private static Tuple<ScaleTransform, TranslateTransform> GetSlotTransforms( UIElement pImage )
		{
			if ( pImage.RenderTransform is not TransformGroup lTransformGroup )
			{
				return null;
			}

			ScaleTransform lScaleTransform = lTransformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
			TranslateTransform lTranslateTransform = lTransformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();

			if ( lScaleTransform == null || lTranslateTransform == null )
			{
				return null;
			}

			return Tuple.Create( lScaleTransform, lTranslateTransform );
		}

		private static void SetSlotCollapsed( Image pImage )
		{
			if ( pImage == null )
			{
				return;
			}

			pImage.Source = null;
			pImage.Visibility = Visibility.Collapsed;
			pImage.Opacity = 0;
			Panel.SetZIndex( pImage, 0 );

			Tuple<ScaleTransform, TranslateTransform> lTransforms = GetSlotTransforms( pImage );
			if ( lTransforms == null )
			{
				return;
			}

			StopAndSet( lTransforms.Item1, ScaleTransform.ScaleXProperty, 1 );
			StopAndSet( lTransforms.Item1, ScaleTransform.ScaleYProperty, 1 );
			StopAndSet( lTransforms.Item2, TranslateTransform.XProperty, 0 );
		}

		private void OnControlLoaded( object pSender, RoutedEventArgs pEventArgs )
		{
			AttachToImagesCollectionChanged();
			AttachToResourcesService();

			UpdateAllVisuals( false );
			UpdateHoverCursorFromMouse();
		}

		private void OnControlUnloaded( object pSender, RoutedEventArgs pEventArgs )
		{
			DetachFromImagesCollectionChanged();
			DetachFromResourcesService();
		}

		private void OnControlDataContextChanged( object pSender, DependencyPropertyChangedEventArgs pEventArgs )
		{
			AttachToResourcesService();
			UpdateHintText();
		}

		private void AttachToResourcesService()
		{
			DetachFromResourcesService();

			ResourcesService lResolvedResourcesService = TryResolveResourcesService();
			if ( lResolvedResourcesService == null )
			{
				return;
			}

			mResourcesServiceForHint = lResolvedResourcesService;
			mResourcesServiceForHint.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private void DetachFromResourcesService()
		{
			if ( mResourcesServiceForHint == null )
			{
				return;
			}

			mResourcesServiceForHint.PropertyChanged -= OnResourcesServicePropertyChanged;
			mResourcesServiceForHint = null;
		}

		private void OnResourcesServicePropertyChanged( object pSender, PropertyChangedEventArgs pEventArgs )
		{
			if ( pEventArgs == null )
			{
				return;
			}

			if ( !string.Equals( pEventArgs.PropertyName, "Item[]", StringComparison.Ordinal ) )
			{
				return;
			}

			UpdateHintText();
		}

		private ResourcesService TryResolveResourcesService()
		{
			ResourcesService lFromSelf = ExtractResourcesServiceFromDataContext( DataContext );
			if ( lFromSelf != null )
			{
				return lFromSelf;
			}

			DependencyObject lCurrentElement = GetParentDependencyObject( this );
			while ( lCurrentElement != null )
			{
				if ( lCurrentElement is FrameworkElement lFrameworkElement )
				{
					ResourcesService lFromAncestor = ExtractResourcesServiceFromDataContext( lFrameworkElement.DataContext );
					if ( lFromAncestor != null )
					{
						return lFromAncestor;
					}
				}

				lCurrentElement = GetParentDependencyObject( lCurrentElement );
			}

			return null;
		}

		private static ResourcesService ExtractResourcesServiceFromDataContext( object pDataContext )
		{
			if ( pDataContext == null )
			{
				return null;
			}

			if ( pDataContext is ResourcesService lResourcesService )
			{
				return lResourcesService;
			}

			PropertyInfo lResourcesServicePropertyInfo = pDataContext.GetType().GetProperty( "ResourcesService", BindingFlags.Instance | BindingFlags.Public );
			if ( lResourcesServicePropertyInfo == null )
			{
				return null;
			}

			if ( !typeof( ResourcesService ).IsAssignableFrom( lResourcesServicePropertyInfo.PropertyType ) )
			{
				return null;
			}

			try
			{
				object lValue = lResourcesServicePropertyInfo.GetValue( pDataContext, null );
				return lValue is ResourcesService lResolvedResourcesService ? lResolvedResourcesService : null;
			}
			catch ( Exception )
			{
				// ignored
			}

			return null;
		}

		private void AttachToImagesCollectionChanged()
		{
			DetachFromImagesCollectionChanged();

			if ( Images is not INotifyCollectionChanged lNotifyCollectionChanged )
			{
				return;
			}

			mImagesCollectionChangedNotifier = lNotifyCollectionChanged;
			mImagesCollectionChangedNotifier.CollectionChanged += OnImagesCollectionChanged;
		}

		private void DetachFromImagesCollectionChanged()
		{
			if ( mImagesCollectionChangedNotifier == null )
			{
				return;
			}

			mImagesCollectionChangedNotifier.CollectionChanged -= OnImagesCollectionChanged;
			mImagesCollectionChangedNotifier = null;
		}

		private void OnImagesCollectionChanged( object pSender, NotifyCollectionChangedEventArgs pEventArgs )
		{
			if ( mHasPendingImagesCollectionRefresh )
			{
				return;
			}

			mHasPendingImagesCollectionRefresh = true;

			Dispatcher.BeginInvoke(
				DispatcherPriority.Background,
				new Action( () =>
				{
					mHasPendingImagesCollectionRefresh = false;

					EnsureSelectedIndexIsValid();
					UpdateAllVisuals( false );
					UpdateHoverCursorFromMouse();
				} ) );
		}

		private ScrollViewer FindScrollableAncestorScrollViewer()
		{
			DependencyObject lCurrentElement = GetParentDependencyObject( this );
			while ( lCurrentElement != null )
			{
				if ( lCurrentElement is ScrollViewer { ScrollableHeight: > 0 } lScrollViewer )
				{
					return lScrollViewer;
				}

				lCurrentElement = GetParentDependencyObject( lCurrentElement );
			}

			return null;
		}

		private void OnRootMouseLeave( object pSender, MouseEventArgs pEventArgs )
		{
			if ( mIsDragInProgress )
			{
				return;
			}

			Cursor = mDefaultCursor;
		}

		private void EnsureSelectedIndexIsValid()
		{
			int lImageCount = GetImageCount();
			if ( lImageCount <= 0 )
			{
				return;
			}

			if ( SelectedIndex >= 0 && SelectedIndex < lImageCount )
			{
				return;
			}

			mIsUpdatingSelectedIndexInternally = true;

			try
			{
				SelectedIndex = 0;
			}
			finally
			{
				mIsUpdatingSelectedIndexInternally = false;
			}
		}

		private int GetImageCount()
		{
			IList lImages = Images;
			return lImages?.Count ?? 0;
		}

		private void UpdateAllVisuals( bool pIsAnimated )
		{
			int lImageCount = GetImageCount();
			bool lHasAnyImage = lImageCount > 0;
			bool lHasMultipleImages = lImageCount > 1;

			mPlaceholderGrid.Visibility = lHasAnyImage ? Visibility.Collapsed : Visibility.Visible;

			mNavigationOverlayGrid.Visibility = lHasMultipleImages ? Visibility.Visible : Visibility.Collapsed;
			mDotsBackgroundBorder.Visibility = lHasMultipleImages ? Visibility.Visible : Visibility.Collapsed;

			mPreviousButton.IsEnabled = lHasMultipleImages;
			mNextButton.IsEnabled = lHasMultipleImages;

			UpdateHintText();
			UpdateCarouselVisualState( pIsAnimated );
		}

		private void UpdateHintText()
		{
			if ( mHintTextBlock == null || mHintStackPanel == null )
			{
				return;
			}

			if ( GetImageCount() <= 0 )
			{
				mHintTextBlock.Text = string.Empty;
				mHintStackPanel.Visibility = Visibility.Collapsed;
				return;
			}

			string lResourceKey = IsFullscreen
				? HintProjectImageCarouselDragOnlyResourceKey
				: HintProjectImageCarouselExpandAndDragResourceKey;

			string lHintText = GetTextFromResources( lResourceKey );
			if ( string.IsNullOrWhiteSpace( lHintText ) )
			{
				mHintTextBlock.Text = string.Empty;
				mHintStackPanel.Visibility = Visibility.Collapsed;
				return;
			}

			mHintTextBlock.Text = lHintText;
			mHintStackPanel.Visibility = Visibility.Visible;
		}

		private string GetTextFromResources( string pResourceKey )
		{
			if ( string.IsNullOrWhiteSpace( pResourceKey ) )
			{
				return string.Empty;
			}

			ResourcesService lResourcesService = mResourcesServiceForHint;
			if ( lResourcesService != null )
			{
				return lResourcesService[ pResourceKey ] ?? string.Empty;
			}

			try
			{
				return sFallbackResourceManager.GetString( pResourceKey, CultureInfo.CurrentUICulture ) ?? string.Empty;
			}
			catch ( Exception )
			{
				// ignored
			}

			return string.Empty;
		}

		private void UpdateCarouselVisualState( bool pIsAnimated )
		{
			int lImageCount = GetImageCount();
			if ( lImageCount <= 0 )
			{
				SetSlotCollapsed( mCurrentImageImage );
				SetSlotCollapsed( mLeftStep1Image );
				SetSlotCollapsed( mRightStep1Image );
				SetSlotCollapsed( mLeftStep2Image );
				SetSlotCollapsed( mRightStep2Image );
				SetSlotCollapsed( mLeftStep3Image );
				SetSlotCollapsed( mRightStep3Image );
				return;
			}

			double lContainerWidth = mMediaRootGrid.ActualWidth;
			if ( lContainerWidth <= 1 )
			{
				lContainerWidth = mMediaCardBorder.ActualWidth;
			}

			if ( lContainerWidth <= 1 )
			{
				lContainerWidth = 1000;
			}

			int lMaxStep = Math.Min( 3, lImageCount - 1 );

			SetSlotState( mCurrentImageImage, GetImageSourceAtWrappedIndex( 0 ), 0, 0, lContainerWidth, pIsAnimated, 16 );

			SetSlotState( mLeftStep1Image, lMaxStep >= 1 ? GetImageSourceAtWrappedIndex( -1 ) : null, 1, -1, lContainerWidth, pIsAnimated, 15 );
			SetSlotState( mRightStep1Image, lMaxStep >= 1 ? GetImageSourceAtWrappedIndex( 1 ) : null, 1, 1, lContainerWidth, pIsAnimated, 15 );

			SetSlotState( mLeftStep2Image, lMaxStep >= 2 ? GetImageSourceAtWrappedIndex( -2 ) : null, 2, -1, lContainerWidth, pIsAnimated, 14 );
			SetSlotState( mRightStep2Image, lMaxStep >= 2 ? GetImageSourceAtWrappedIndex( 2 ) : null, 2, 1, lContainerWidth, pIsAnimated, 14 );

			SetSlotState( mLeftStep3Image, lMaxStep >= 3 ? GetImageSourceAtWrappedIndex( -3 ) : null, 3, -1, lContainerWidth, pIsAnimated, 13 );
			SetSlotState( mRightStep3Image, lMaxStep >= 3 ? GetImageSourceAtWrappedIndex( 3 ) : null, 3, 1, lContainerWidth, pIsAnimated, 13 );
		}

		private ImageSource GetImageSourceAtWrappedIndex( int pOffsetFromSelected )
		{
			int lImageCount = GetImageCount();
			if ( lImageCount <= 0 )
			{
				return null;
			}

			int lWrappedIndex = WrapIndex( SelectedIndex + pOffsetFromSelected, lImageCount );
			if ( lWrappedIndex < 0 )
			{
				return null;
			}

			object lItem = Images[ lWrappedIndex ];
			return ConvertToImageSource( lItem );
		}

		private void SetSlotState( Image pImage, ImageSource pImageSource, int pStep, int pDirection, double pContainerWidth, bool pIsAnimated, int pZIndex )
		{
			if ( pImage == null )
			{
				return;
			}

			if ( pImageSource == null )
			{
				SetSlotCollapsed( pImage );
				return;
			}

			pImage.Source = pImageSource;
			pImage.Visibility = Visibility.Visible;
			Panel.SetZIndex( pImage, pZIndex );

			ProjectImageCarouselSlotVisualTargets lTargets = ProjectImageCarouselAdjacentImagesVisualService.GetSlotVisualTargets( pStep, pDirection, pContainerWidth );

			Tuple<ScaleTransform, TranslateTransform> lTransforms = GetSlotTransforms( pImage );
			if ( lTransforms == null )
			{
				return;
			}

			ApplyDouble( lTransforms.Item1, ScaleTransform.ScaleXProperty, lTargets.Scale, pIsAnimated );
			ApplyDouble( lTransforms.Item1, ScaleTransform.ScaleYProperty, lTargets.Scale, pIsAnimated );
			ApplyDouble( lTransforms.Item2, TranslateTransform.XProperty, lTargets.TranslateX, pIsAnimated );
			ApplyDouble( pImage, OpacityProperty, lTargets.Opacity, pIsAnimated );
		}

		private void ApplyDouble( DependencyObject pTarget, DependencyProperty pProperty, double pToValue, bool pIsAnimated )
		{
			if ( pTarget == null )
			{
				return;
			}

			if ( !pIsAnimated )
			{
				StopAndSet( pTarget, pProperty, pToValue );
				return;
			}

			if ( pTarget is not Animatable lAnimatable )
			{
				return;
			}

			var lDoubleAnimation = new DoubleAnimation
			{
				To = pToValue,
				Duration = new Duration( sTransitionDuration ),
				EasingFunction = mCarouselEasingFunction
			};

			lAnimatable.BeginAnimation( pProperty, lDoubleAnimation );
		}

		private void NavigatePrevious()
		{
			int lImageCount = GetImageCount();
			if ( lImageCount <= 1 )
			{
				return;
			}

			SelectedIndex = WrapIndex( SelectedIndex - 1, lImageCount );
		}

		private void NavigateNext()
		{
			int lImageCount = GetImageCount();
			if ( lImageCount <= 1 )
			{
				return;
			}

			SelectedIndex = WrapIndex( SelectedIndex + 1, lImageCount );
		}

		private void OnPreviousButtonClick( object pSender, RoutedEventArgs pEventArgs )
		{
			NavigatePrevious();
		}

		private void OnNextButtonClick( object pSender, RoutedEventArgs pEventArgs )
		{
			NavigateNext();
		}

		private void OpenViewerWindow()
		{
			Window lOwnerWindow = Window.GetWindow( this );

			var lViewerWindow = new ProjectImageViewerWindow
			{
				Owner = lOwnerWindow,
				Images = Images as ObservableCollection<ImageSource>,
				SelectedIndex = SelectedIndex
			};

			lViewerWindow.Show();
		}

		private void OnMediaRootGridSizeChanged( object pSender, SizeChangedEventArgs pEventArgs )
		{
			UpdateCarouselVisualState( false );
			UpdateHoverCursorFromMouse();
		}

		private void OnRootPreviewMouseWheel( object pSender, MouseWheelEventArgs pMouseWheelEventArgs )
		{
			ScrollViewer lScrollViewer = FindScrollableAncestorScrollViewer();
			if ( lScrollViewer != null )
			{
				ApplyMouseWheelToScrollViewer( lScrollViewer, pMouseWheelEventArgs.Delta );
			}

			pMouseWheelEventArgs.Handled = true;
		}

		private bool TryOpenViewerOnDoubleClick( DependencyObject pOriginalSource, MouseButtonEventArgs pMouseButtonEventArgs )
		{
			if ( pMouseButtonEventArgs == null || pMouseButtonEventArgs.ChangedButton != MouseButton.Left )
			{
				return false;
			}

			if ( IsFullscreen )
			{
				return false;
			}

			if ( GetImageCount() <= 0 )
			{
				return false;
			}

			if ( mIsDragInProgress || mHasViewerOpenSuppressedDueToDragNavigation || mHasNavigatedDuringDrag )
			{
				return false;
			}

			if ( !IsValidDragStartSource( pOriginalSource ) )
			{
				return false;
			}

			OpenViewerWindow();
			return true;
		}

		private void OnRootPreviewMouseLeftButtonDown( object pSender, MouseButtonEventArgs pMouseButtonEventArgs )
		{
			Focus();

			DependencyObject lOriginalSource = pMouseButtonEventArgs.OriginalSource as DependencyObject;

			if ( pMouseButtonEventArgs is { ChangedButton: MouseButton.Left, ClickCount: 1 } )
			{
				mHasNavigatedDuringDrag = false;
				mHasViewerOpenSuppressedDueToDragNavigation = false;
			}

			if ( pMouseButtonEventArgs is { ChangedButton: MouseButton.Left, ClickCount: 2 } )
			{
				bool lHasOpenedViewer = TryOpenViewerOnDoubleClick( lOriginalSource, pMouseButtonEventArgs );

				mHasNavigatedDuringDrag = false;
				mHasViewerOpenSuppressedDueToDragNavigation = false;

				if ( lHasOpenedViewer )
				{
					pMouseButtonEventArgs.Handled = true;
				}

				return;
			}

			if ( pMouseButtonEventArgs.ChangedButton != MouseButton.Left || GetImageCount() <= 1 )
			{
				return;
			}

			if ( !IsValidDragStartSource( lOriginalSource ) )
			{
				return;
			}

			mIsDragInProgress = true;
			mHasNavigatedDuringDrag = false;
			mDragAccumulatedHorizontalDelta = 0.0;
			mDragThresholdPixels = GetDragThresholdPixels();

			Point lStartPosition = pMouseButtonEventArgs.GetPosition( mMediaRootGrid );
			mDragLastPosition = lStartPosition;

			bool lHasCapturedMouse = Mouse.Capture( this, CaptureMode.SubTree );
			if ( !lHasCapturedMouse )
			{
				mIsDragInProgress = false;
				return;
			}

			mPreviousCursor = Cursor;
			Cursor = Cursors.SizeWE;

			pMouseButtonEventArgs.Handled = true;
		}

		private void OnRootPreviewMouseMove( object pSender, MouseEventArgs pMouseEventArgs )
		{
			if ( !mIsDragInProgress )
			{
				UpdateHoverCursor( pMouseEventArgs.OriginalSource as DependencyObject );
				return;
			}

			if ( pMouseEventArgs.LeftButton != MouseButtonState.Pressed )
			{
				EndDrag();
				return;
			}

			Point lCurrentPosition = pMouseEventArgs.GetPosition( mMediaRootGrid );
			double lDeltaX = lCurrentPosition.X - mDragLastPosition.X;

			mDragLastPosition = lCurrentPosition;
			mDragAccumulatedHorizontalDelta += lDeltaX * DragDeltaScale;

			if ( Math.Abs( mDragAccumulatedHorizontalDelta ) < mDragThresholdPixels )
			{
				return;
			}

			int lNavigationCount = ( int )( Math.Abs( mDragAccumulatedHorizontalDelta ) / mDragThresholdPixels );
			if ( lNavigationCount <= 0 )
			{
				return;
			}

			mHasNavigatedDuringDrag = true;
			mHasViewerOpenSuppressedDueToDragNavigation = true;

			for ( int lNavigationIndex = 0; lNavigationIndex < lNavigationCount; lNavigationIndex++ )
			{
				if ( mDragAccumulatedHorizontalDelta < 0.0 )
				{
					NavigateNext();
					mDragAccumulatedHorizontalDelta += mDragThresholdPixels;
					continue;
				}

				NavigatePrevious();
				mDragAccumulatedHorizontalDelta -= mDragThresholdPixels;
			}

			pMouseEventArgs.Handled = true;
		}

		private void OnRootPreviewMouseLeftButtonUp( object pSender, MouseButtonEventArgs pMouseButtonEventArgs )
		{
			if ( mIsDragInProgress )
			{
				EndDrag();
				pMouseButtonEventArgs.Handled = true;
			}
		}

		private void OnRootLostMouseCapture( object pSender, MouseEventArgs pMouseEventArgs )
		{
			EndDrag();
		}

		private void EndDrag()
		{
			if ( !mIsDragInProgress )
			{
				return;
			}

			mIsDragInProgress = false;
			mDragAccumulatedHorizontalDelta = 0.0;

			if ( Mouse.Captured != null )
			{
				Mouse.Capture( null );
			}

			Cursor = mPreviousCursor;
			mPreviousCursor = null;

			UpdateHoverCursorFromMouse();
		}

		private void UpdateHoverCursorFromMouse()
		{
			DependencyObject lDirectlyOver = Mouse.DirectlyOver as DependencyObject;
			UpdateHoverCursor( lDirectlyOver );
		}

		private void UpdateHoverCursor( DependencyObject pOriginalSource )
		{
			if ( mIsDragInProgress )
			{
				return;
			}

			if ( Mouse.LeftButton == MouseButtonState.Pressed )
			{
				return;
			}

			bool lIsDragPossible = GetImageCount() > 1 && IsValidDragStartSource( pOriginalSource );

			Cursor lTargetCursor = lIsDragPossible ? Cursors.SizeWE : mDefaultCursor;
			if ( !Equals( Cursor, lTargetCursor ) )
			{
				Cursor = lTargetCursor;
			}
		}

		private double GetDragThresholdPixels()
		{
			double lWidth = ActualWidth;
			if ( lWidth <= 1 )
			{
				lWidth = mMediaCardBorder.ActualWidth;
			}

			if ( lWidth <= 1 )
			{
				lWidth = mMediaRootGrid.ActualWidth;
			}

			if ( lWidth <= 1 )
			{
				lWidth = 900;
			}

			double lRelativeThreshold = lWidth * DragThresholdRatio;
			return Math.Max( MinimumDragThresholdPixels, lRelativeThreshold );
		}

		private bool IsValidDragStartSource( DependencyObject pOriginalSource )
		{
			if ( pOriginalSource == null )
			{
				return false;
			}

			if ( FindAncestor<ButtonBase>( pOriginalSource ) != null )
			{
				return false;
			}

			if ( FindAncestor<ListBoxItem>( pOriginalSource ) != null )
			{
				return false;
			}

			if ( FindAncestor<TextBoxBase>( pOriginalSource ) != null )
			{
				return false;
			}

			if ( FindAncestor<ScrollBar>( pOriginalSource ) != null )
			{
				return false;
			}

			return !IsDescendantOf( pOriginalSource, mDotsBackgroundBorder );
		}

		private void OnDotPreviewMouseLeftButtonDown( object pSender, MouseButtonEventArgs pMouseButtonEventArgs )
		{
			if ( pSender is not ListBoxItem lListBoxItem )
			{
				return;
			}

			int lDotIndex = mDotsListBox.ItemContainerGenerator.IndexFromContainer( lListBoxItem );
			if ( lDotIndex < 0 )
			{
				return;
			}

			SelectedIndex = lDotIndex;
			pMouseButtonEventArgs.Handled = true;
		}
	}
}
