// Controls/ProjectImageCarouselControl.xaml.cs

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ResumeApp.Controls
{
	public sealed partial class ProjectImageCarouselControl : UserControl
	{
		public static readonly DependencyProperty sImagesProperty =
			DependencyProperty.Register(
				nameof( Images ),
				typeof( ObservableCollection<ImageSource> ),
				typeof( ProjectImageCarouselControl ),
				new PropertyMetadata( null, OnImagesPropertyChanged ) );

		public static readonly DependencyProperty sSelectedIndexProperty =
			DependencyProperty.Register(
				nameof( SelectedIndex ),
				typeof( int ),
				typeof( ProjectImageCarouselControl ),
				new FrameworkPropertyMetadata( -1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexPropertyChanged ) );

		public static readonly DependencyProperty sPlaceholderTextProperty =
			DependencyProperty.Register(
				nameof( PlaceholderText ),
				typeof( string ),
				typeof( ProjectImageCarouselControl ),
				new PropertyMetadata( string.Empty, OnPlaceholderTextPropertyChanged ) );

		private static readonly Duration sFadeDuration = new Duration( TimeSpan.FromMilliseconds( 180 ) );

		private ObservableCollection<ImageSource> mObservedImages;

		public ObservableCollection<ImageSource> Images
		{
			get => ( ObservableCollection<ImageSource> )GetValue( sImagesProperty );
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

		public ProjectImageCarouselControl()
		{
			InitializeComponent();

			Loaded += OnControlLoaded;
			Unloaded += OnControlUnloaded;
		}

		private void OnControlLoaded( object pSender, RoutedEventArgs pArgs )
		{
			AttachImages( Images );
			EnsureSelectedIndexIsValid();
			UpdateVisuals( pShouldAnimate: false );
			UpdateMediaClip();
		}

		private void OnControlUnloaded( object pSender, RoutedEventArgs pArgs )
		{
			AttachImages( null );
		}

		private static void OnImagesPropertyChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				lControl.OnImagesChanged( pArgs.OldValue as ObservableCollection<ImageSource>, pArgs.NewValue as ObservableCollection<ImageSource> );
			}
		}

		private void OnImagesChanged( ObservableCollection<ImageSource> pOldImages, ObservableCollection<ImageSource> pNewImages )
		{
			AttachImages( pNewImages );
			EnsureSelectedIndexIsValid();
			UpdateVisuals( pShouldAnimate: false );
		}

		private static void OnSelectedIndexPropertyChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				lControl.OnSelectedIndexChanged();
			}
		}

		private void OnSelectedIndexChanged()
		{
			EnsureSelectedIndexIsValid();
			UpdateVisuals( pShouldAnimate: true );
		}

		private static void OnPlaceholderTextPropertyChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				lControl.UpdatePlaceholderVisuals();
			}
		}

		private void UpdatePlaceholderVisuals()
		{
			if ( mPlaceholderTextBlock != null )
			{
				mPlaceholderTextBlock.Text = PlaceholderText ?? string.Empty;
			}
		}

		private void AttachImages( ObservableCollection<ImageSource> pImages )
		{
			if ( mObservedImages != null )
			{
				mObservedImages.CollectionChanged -= OnImagesCollectionChanged;
			}

			mObservedImages = pImages;

			if ( mObservedImages != null )
			{
				mObservedImages.CollectionChanged += OnImagesCollectionChanged;
			}
		}

		private void OnImagesCollectionChanged( object pSender, NotifyCollectionChangedEventArgs pArgs )
		{
			EnsureSelectedIndexIsValid();
			UpdateVisuals( pShouldAnimate: false );
		}

		private int GetImagesCount() => Images?.Count ?? 0;

		private bool HasImages() => GetImagesCount() > 0;

		private bool HasMultipleImages() => GetImagesCount() > 1;

		private void EnsureSelectedIndexIsValid()
		{
			int lImagesCount = GetImagesCount();

			if ( lImagesCount <= 0 )
			{
				if ( SelectedIndex != -1 )
				{
					SelectedIndex = -1;
				}

				return;
			}

			if ( SelectedIndex < 0 || SelectedIndex >= lImagesCount )
			{
				SelectedIndex = 0;
			}
		}

		private ImageSource TryGetSelectedImageSource()
		{
			if ( Images == null )
			{
				return null;
			}

			int lImagesCount = Images.Count;

			if ( lImagesCount <= 0 )
			{
				return null;
			}

			if ( SelectedIndex < 0 || SelectedIndex >= lImagesCount )
			{
				return null;
			}

			return Images[ SelectedIndex ];
		}

		private void UpdateVisuals( bool pShouldAnimate )
		{
			ImageSource lSelectedImageSource = TryGetSelectedImageSource();

			bool lHasImages = lSelectedImageSource != null && HasImages();
			bool lHasMultipleImages = lHasImages && HasMultipleImages();

			mPlaceholderGrid.Visibility = lHasImages ? Visibility.Collapsed : Visibility.Visible;
			mCurrentImageImage.Visibility = lHasImages ? Visibility.Visible : Visibility.Collapsed;
			mNavigationOverlayGrid.Visibility = lHasMultipleImages ? Visibility.Visible : Visibility.Collapsed;

			UpdateNavigationEnabledState();

			if ( !lHasImages )
			{
				mCurrentImageImage.Source = null;
				return;
			}

			bool lIsSameSource = ReferenceEquals( mCurrentImageImage.Source, lSelectedImageSource );

			mCurrentImageImage.Source = lSelectedImageSource;

			if ( !pShouldAnimate || lIsSameSource )
			{
				mCurrentImageImage.Opacity = 1;
				return;
			}

			AnimateImageFadeIn();
		}

		private void UpdateNavigationEnabledState()
		{
			int lImagesCount = GetImagesCount();

			bool lCanGoPrevious = lImagesCount > 1 && SelectedIndex > 0;
			bool lCanGoNext = lImagesCount > 1 && SelectedIndex >= 0 && SelectedIndex < lImagesCount - 1;

			mPreviousButton.IsEnabled = lCanGoPrevious;
			mNextButton.IsEnabled = lCanGoNext;
			mDotsListBox.IsEnabled = lImagesCount > 1;
		}

		private void AnimateImageFadeIn()
		{
			mCurrentImageImage.BeginAnimation( OpacityProperty, null );
			mCurrentImageImage.Opacity = 0;

			var lFadeAnimation = new DoubleAnimation
			{
				From = 0,
				To = 1,
				Duration = sFadeDuration,
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			};

			mCurrentImageImage.BeginAnimation( OpacityProperty, lFadeAnimation );
		}

		private void TryNavigateToIndex( int pTargetIndex )
		{
			int lImagesCount = GetImagesCount();

			if ( lImagesCount <= 0 )
			{
				return;
			}

			int lClampedIndex = Math.Max( 0, Math.Min( pTargetIndex, lImagesCount - 1 ) );

			if ( lClampedIndex == SelectedIndex )
			{
				return;
			}

			SelectedIndex = lClampedIndex;
		}

		private void NavigatePrevious() => TryNavigateToIndex( SelectedIndex - 1 );

		private void NavigateNext() => TryNavigateToIndex( SelectedIndex + 1 );

		private void OnPreviousButtonClick( object pSender, RoutedEventArgs pArgs )
		{
			mProjectImageCarouselControlRoot.Focus();
			NavigatePrevious();
		}

		private void OnNextButtonClick( object pSender, RoutedEventArgs pArgs )
		{
			mProjectImageCarouselControlRoot.Focus();
			NavigateNext();
		}

		private void OnRootPreviewKeyDown( object pSender, KeyEventArgs pArgs )
		{
			if ( !HasMultipleImages() )
			{
				return;
			}

			if ( pArgs.Key == Key.Left )
			{
				NavigatePrevious();
				pArgs.Handled = true;
				return;
			}

			if ( pArgs.Key == Key.Right )
			{
				NavigateNext();
				pArgs.Handled = true;
			}
		}

		private void OnRootPreviewMouseWheel( object pSender, MouseWheelEventArgs pArgs )
		{
			if ( !HasMultipleImages() )
			{
				return;
			}

			if ( pArgs.Delta > 0 )
			{
				NavigatePrevious();
				pArgs.Handled = true;
				return;
			}

			if ( pArgs.Delta < 0 )
			{
				NavigateNext();
				pArgs.Handled = true;
			}
		}

		private void OnRootPreviewMouseLeftButtonDown( object pSender, MouseButtonEventArgs pArgs )
		{
			mProjectImageCarouselControlRoot.Focus();
		}

		private void OnMediaRootGridSizeChanged( object pSender, SizeChangedEventArgs pArgs )
		{
			UpdateMediaClip();
		}

		private void UpdateMediaClip()
		{
			if ( mMediaRootGrid == null || mMediaCardBorder == null )
			{
				return;
			}

			double lWidth = Math.Max( 0, mMediaRootGrid.ActualWidth );
			double lHeight = Math.Max( 0, mMediaRootGrid.ActualHeight );

			if ( lWidth <= 0 || lHeight <= 0 )
			{
				return;
			}

			CornerRadius lCornerRadius = mMediaCardBorder.CornerRadius;
			double lRadius = Math.Max( 0, lCornerRadius.TopLeft );
			double lMaxRadius = Math.Min( lWidth / 2, lHeight / 2 );
			double lClampedRadius = Math.Min( lRadius, lMaxRadius );

			mMediaRootGrid.Clip = new RectangleGeometry( new Rect( 0, 0, lWidth, lHeight ), lClampedRadius, lClampedRadius );
		}
	}
}
