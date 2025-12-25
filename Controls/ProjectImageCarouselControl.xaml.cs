// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ResumeApp.Controls
{
	public partial class ProjectImageCarouselControl : UserControl
	{
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

		private static readonly Dictionary<string, ImageSource> sCachedImageSourcesByUri = new Dictionary<string, ImageSource>( StringComparer.OrdinalIgnoreCase );

		private static readonly TimeSpan sTransitionDuration = TimeSpan.FromMilliseconds( 240 );

		private readonly IEasingFunction mCarouselEasingFunction;

		private bool mIsUpdatingSelectedIndexInternally;

		private bool mIsDragInProgress;
		private bool mHasNavigatedDuringDrag;
		private Point mDragStartPosition;
		private double mDragThresholdPixels;

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

		public ProjectImageCarouselControl()
		{
			mCarouselEasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
			InitializeComponent();
			Loaded += OnControlLoaded;
		}

		private static void OnImagesChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				lControl.EnsureSelectedIndexIsValid();
				lControl.UpdateAllVisuals( false );
			}
		}

		private static void OnSelectedIndexChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				if ( lControl.mIsUpdatingSelectedIndexInternally )
				{
					return;
				}

				lControl.EnsureSelectedIndexIsValid();
				lControl.UpdateAllVisuals( true );
			}
		}

		private static void OnIsFullscreenChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				lControl.UpdateAllVisuals( false );
			}
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

			var lUriText = pItem as string;
			if ( string.IsNullOrWhiteSpace( lUriText ) )
			{
				return null;
			}

			if ( sCachedImageSourcesByUri.TryGetValue( lUriText, out ImageSource lCachedImageSource ) )
			{
				return lCachedImageSource;
			}

			try
			{
				var lBitmapImage = new BitmapImage();
				lBitmapImage.BeginInit();
				lBitmapImage.UriSource = new Uri( lUriText, UriKind.RelativeOrAbsolute );
				lBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				lBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				lBitmapImage.EndInit();
				lBitmapImage.Freeze();

				sCachedImageSourcesByUri[ lUriText ] = lBitmapImage;
				return lBitmapImage;
			}
			catch ( Exception )
			{
				// ignored
			}

			return null;
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

		private void OnControlLoaded( object pSender, RoutedEventArgs pEventArgs )
		{
			UpdateAllVisuals( false );
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
			var lImages = Images;
			return lImages?.Count ?? 0;
		}

		private void UpdateAllVisuals( bool pIsAnimated )
		{
			int lImageCount = GetImageCount();
			bool lHasAnyImage = lImageCount > 0;
			bool lHasMultipleImages = lImageCount > 1;

			mPlaceholderGrid.Visibility = lHasAnyImage ? Visibility.Collapsed : Visibility.Visible;

			mNavigationOverlayGrid.Visibility = lHasMultipleImages ? Visibility.Visible : Visibility.Collapsed;

			mPreviousButton.IsEnabled = lHasMultipleImages;
			mNextButton.IsEnabled = lHasMultipleImages;

			mExpandButton.Visibility = ( lHasAnyImage && !IsFullscreen ) ? Visibility.Visible : Visibility.Collapsed;

			UpdateCarouselVisualState( pIsAnimated );
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

		private void SetSlotCollapsed( Image pImage )
		{
			if ( pImage == null )
			{
				return;
			}

			pImage.Source = null;
			pImage.Visibility = Visibility.Collapsed;
			pImage.Opacity = 0;
			Panel.SetZIndex( pImage, 0 );

			var lTransforms = GetSlotTransforms( pImage );
			if ( lTransforms == null )
			{
				return;
			}

			StopAndSet( lTransforms.Item1, ScaleTransform.ScaleXProperty, 1 );
			StopAndSet( lTransforms.Item1, ScaleTransform.ScaleYProperty, 1 );
			StopAndSet( lTransforms.Item2, TranslateTransform.XProperty, 0 );
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

			double lTargetScale = pStep <= 0 ? 1.0 : Math.Exp( -pStep * 0.32 );
			double lTargetOpacity = pStep <= 0 ? 1.0 : Math.Exp( -pStep * 0.55 );

			double lOffsetX = pStep <= 0
				? 0.0
				: pContainerWidth * 0.18 * ( Math.Exp( pStep * 0.50 ) - 1.0 );

			double lTargetTranslateX = pDirection < 0 ? -lOffsetX : ( pDirection > 0 ? lOffsetX : 0.0 );

			var lTransforms = GetSlotTransforms( pImage );
			if ( lTransforms == null )
			{
				return;
			}

			ApplyDouble( lTransforms.Item1, ScaleTransform.ScaleXProperty, lTargetScale, pIsAnimated );
			ApplyDouble( lTransforms.Item1, ScaleTransform.ScaleYProperty, lTargetScale, pIsAnimated );
			ApplyDouble( lTransforms.Item2, TranslateTransform.XProperty, lTargetTranslateX, pIsAnimated );
			ApplyDouble( pImage, UIElement.OpacityProperty, lTargetOpacity, pIsAnimated );
		}

		private Tuple<ScaleTransform, TranslateTransform> GetSlotTransforms( Image pImage )
		{
			if ( !( pImage.RenderTransform is TransformGroup lTransformGroup ) )
			{
				return null;
			}

			var lScaleTransform = lTransformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
			var lTranslateTransform = lTransformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();

			if ( lScaleTransform == null || lTranslateTransform == null )
			{
				return null;
			}

			return Tuple.Create( lScaleTransform, lTranslateTransform );
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

			var lAnimatable = pTarget as Animatable;
			if ( lAnimatable == null )
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

		private void StopAndSet( DependencyObject pTarget, DependencyProperty pProperty, double pValue )
		{
			if ( pTarget is Animatable lAnimatable )
			{
				lAnimatable.BeginAnimation( pProperty, null );
			}

			pTarget.SetValue( pProperty, pValue );
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

		private void OnExpandButtonClick( object pSender, RoutedEventArgs pEventArgs )
		{
			if ( IsFullscreen )
			{
				return;
			}

			int lImageCount = GetImageCount();
			if ( lImageCount <= 0 )
			{
				return;
			}

			var lOwnerWindow = Window.GetWindow( this );
			var lViewerWindow = new ProjectImageViewerWindow
			{
				Owner = lOwnerWindow,
				Images = ( System.Collections.ObjectModel.ObservableCollection<ImageSource> )Images,
				SelectedIndex = SelectedIndex
			};

			lViewerWindow.Show();
		}

		private void OnMediaRootGridSizeChanged( object pSender, SizeChangedEventArgs pEventArgs )
		{
			UpdateCarouselVisualState( false );
		}

		private void OnRootPreviewMouseWheel( object pSender, MouseWheelEventArgs pMouseWheelEventArgs )
		{
			pMouseWheelEventArgs.Handled = true;
		}

		private void OnRootPreviewMouseLeftButtonDown( object pSender, MouseButtonEventArgs pMouseButtonEventArgs )
		{
			Focus();

			if ( pMouseButtonEventArgs.ChangedButton != MouseButton.Left )
			{
				return;
			}

			var lOriginalSource = pMouseButtonEventArgs.OriginalSource as DependencyObject;
			if ( !IsValidDragStartSource( lOriginalSource ) )
			{
				return;
			}

			mIsDragInProgress = true;
			mHasNavigatedDuringDrag = false;
			mDragStartPosition = pMouseButtonEventArgs.GetPosition( mMediaRootGrid );
			mDragThresholdPixels = GetDragThresholdPixels();

			CaptureMouse();
			pMouseButtonEventArgs.Handled = true;
		}

		private void OnRootPreviewMouseMove( object pSender, MouseEventArgs pMouseEventArgs )
		{
			if ( !mIsDragInProgress )
			{
				return;
			}

			if ( pMouseEventArgs.LeftButton != MouseButtonState.Pressed )
			{
				EndDrag();
				return;
			}

			if ( mHasNavigatedDuringDrag )
			{
				return;
			}

			Point lCurrentPosition = pMouseEventArgs.GetPosition( mMediaRootGrid );
			double lDeltaX = lCurrentPosition.X - mDragStartPosition.X;

			if ( Math.Abs( lDeltaX ) < mDragThresholdPixels )
			{
				return;
			}

			if ( lDeltaX < 0 )
			{
				NavigateNext();
			}
			else
			{
				NavigatePrevious();
			}

			mHasNavigatedDuringDrag = true;
			pMouseEventArgs.Handled = true;
		}

		private void OnRootPreviewMouseLeftButtonUp( object pSender, MouseButtonEventArgs pMouseButtonEventArgs )
		{
			if ( !mIsDragInProgress )
			{
				return;
			}

			EndDrag();
			pMouseButtonEventArgs.Handled = true;
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
			mHasNavigatedDuringDrag = false;

			if ( IsMouseCaptured )
			{
				ReleaseMouseCapture();
			}
		}

		private double GetDragThresholdPixels()
		{
			double lWidth = mMediaRootGrid.ActualWidth;
			if ( lWidth <= 1 )
			{
				lWidth = ActualWidth;
			}

			if ( lWidth <= 1 )
			{
				lWidth = 900;
			}

			double lRelativeThreshold = lWidth * 0.06;
			return Math.Max( 42, lRelativeThreshold );
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

			return IsDescendantOf( pOriginalSource, mMediaRootGrid );
		}

		private void OnRootPreviewKeyDown( object pSender, KeyEventArgs pKeyEventArgs )
		{
			switch ( pKeyEventArgs.Key )
			{
				case Key.Left:
					{
						NavigatePrevious();
						pKeyEventArgs.Handled = true;
						return;
					}
				case Key.Right:
					{
						NavigateNext();
						pKeyEventArgs.Handled = true;
						return;
					}
				case Key.Home:
					{
						if ( GetImageCount() > 0 )
						{
							SelectedIndex = 0;
							pKeyEventArgs.Handled = true;
						}

						return;
					}
				case Key.End:
					{
						int lImageCount = GetImageCount();
						if ( lImageCount > 0 )
						{
							SelectedIndex = lImageCount - 1;
							pKeyEventArgs.Handled = true;
						}

						break;
					}
			}
		}

		private void OnDotPreviewMouseLeftButtonDown( object pSender, MouseButtonEventArgs pMouseButtonEventArgs )
		{
			if ( !( pSender is ListBoxItem lListBoxItem ) )
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
