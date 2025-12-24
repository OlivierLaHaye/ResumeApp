// Controls/ProjectImageCarouselControl.xaml.cs
using ResumeApp.Windows;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ResumeApp.Controls
{
	public sealed partial class ProjectImageCarouselControl
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

		public static readonly DependencyProperty sIsFullscreenProperty =
			DependencyProperty.Register(
				nameof( IsFullscreen ),
				typeof( bool ),
				typeof( ProjectImageCarouselControl ),
				new PropertyMetadata( false, OnIsFullscreenPropertyChanged ) );

		private static readonly Duration sFadeDuration = new Duration( TimeSpan.FromMilliseconds( 180 ) );

		private ObservableCollection<ImageSource> mObservedImages;
		private ProjectImageViewerWindow mProjectImageViewerWindow;

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

		public bool IsFullscreen
		{
			get => ( bool )GetValue( sIsFullscreenProperty );
			set => SetValue( sIsFullscreenProperty, value );
		}

		public ProjectImageCarouselControl()
		{
			InitializeComponent();

			Loaded += OnControlLoaded;
			Unloaded += OnControlUnloaded;
		}

		private static void OnImagesPropertyChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				ObservableCollection<ImageSource> lOldImages = pArgs.OldValue is ObservableCollection<ImageSource> lOldCollection ? lOldCollection : null;
				ObservableCollection<ImageSource> lNewImages = pArgs.NewValue is ObservableCollection<ImageSource> lNewCollection ? lNewCollection : null;

				lControl.OnImagesChanged( lOldImages, lNewImages );
			}
		}

		private static void OnSelectedIndexPropertyChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				lControl.OnSelectedIndexChanged();
			}
		}

		private static void OnPlaceholderTextPropertyChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				lControl.UpdatePlaceholderVisuals();
			}
		}

		private static void OnIsFullscreenPropertyChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is ProjectImageCarouselControl lControl )
			{
				lControl.OnIsFullscreenChanged();
			}
		}

		private static int GetWrappedIndex( int pTargetIndex, int pImagesCount )
		{
			if ( pImagesCount <= 0 )
			{
				return -1;
			}

			int lModulo = pTargetIndex % pImagesCount;

			if ( lModulo < 0 )
			{
				lModulo += pImagesCount;
			}

			return lModulo;
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

		private void OnImagesChanged( ObservableCollection<ImageSource> pOldImages, ObservableCollection<ImageSource> pNewImages )
		{
			AttachImages( pNewImages );
			EnsureSelectedIndexIsValid();
			UpdateVisuals( pShouldAnimate: false );
		}

		private void OnSelectedIndexChanged()
		{
			EnsureSelectedIndexIsValid();
			UpdateVisuals( pShouldAnimate: true );
		}

		private void OnIsFullscreenChanged()
		{
			UpdateVisuals( pShouldAnimate: false );
			UpdateMediaClip();
		}

		private void UpdatePlaceholderVisuals()
		{
			if ( mPlaceholderTextBlock == null )
			{
				return;
			}

			mPlaceholderTextBlock.Text = PlaceholderText ?? string.Empty;
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

			if ( mProjectImageViewerWindow == null )
			{
				return;
			}

			if ( HasImages() )
			{
				return;
			}

			if ( mProjectImageViewerWindow.IsVisible )
			{
				mProjectImageViewerWindow.Close();
			}
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
			UpdateExpandButtonVisibility( lHasImages );

			if ( !lHasImages )
			{
				mCurrentImageImage.Source = null;
				mCurrentImageImage.Opacity = 0;
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

		private void UpdateExpandButtonVisibility( bool pHasImages )
		{
			if ( mExpandButton == null )
			{
				return;
			}

			bool lIsExpandAllowed = pHasImages && !IsFullscreen;
			mExpandButton.Visibility = lIsExpandAllowed ? Visibility.Visible : Visibility.Collapsed;
		}

		private void UpdateNavigationEnabledState()
		{
			bool lHasMultipleImages = HasMultipleImages();

			if ( mPreviousButton != null )
			{
				mPreviousButton.IsEnabled = lHasMultipleImages;
			}

			if ( mNextButton != null )
			{
				mNextButton.IsEnabled = lHasMultipleImages;
			}

			if ( mDotsListBox != null )
			{
				mDotsListBox.IsEnabled = lHasMultipleImages;
			}
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

			int lWrappedIndex = GetWrappedIndex( pTargetIndex, lImagesCount );

			if ( lWrappedIndex == SelectedIndex )
			{
				return;
			}

			SelectedIndex = lWrappedIndex;
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

		private void OnExpandButtonClick( object pSender, RoutedEventArgs pArgs )
		{
			OpenOrActivateExpandedWindow();
		}

		private void OpenOrActivateExpandedWindow()
		{
			if ( IsFullscreen )
			{
				return;
			}

			if ( !HasImages() )
			{
				return;
			}

			if ( mProjectImageViewerWindow != null && mProjectImageViewerWindow.IsVisible )
			{
				mProjectImageViewerWindow.Activate();
				return;
			}

			var lViewerWindow = new ProjectImageViewerWindow();

			var lOwnerWindow = Window.GetWindow( this );
			if ( lOwnerWindow != null )
			{
				lViewerWindow.Owner = lOwnerWindow;
			}

			var lImagesBinding = new Binding
			{
				Source = this,
				Path = new PropertyPath( nameof( Images ) ),
				Mode = BindingMode.OneWay
			};

			var lSelectedIndexBinding = new Binding
			{
				Source = this,
				Path = new PropertyPath( nameof( SelectedIndex ) ),
				Mode = BindingMode.TwoWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};

			BindingOperations.SetBinding( lViewerWindow, ProjectImageViewerWindow.sImagesProperty, lImagesBinding );
			BindingOperations.SetBinding( lViewerWindow, ProjectImageViewerWindow.sSelectedIndexProperty, lSelectedIndexBinding );

			lViewerWindow.Closed += OnProjectImageViewerWindowClosed;

			mProjectImageViewerWindow = lViewerWindow;

			lViewerWindow.Show();
			lViewerWindow.Activate();
		}

		private void OnProjectImageViewerWindowClosed( object pSender, EventArgs pArgs )
		{
			if ( pSender is ProjectImageViewerWindow lViewerWindow && ReferenceEquals( mProjectImageViewerWindow, lViewerWindow ) )
			{
				mProjectImageViewerWindow.Closed -= OnProjectImageViewerWindowClosed;
				mProjectImageViewerWindow = null;
			}
		}

		private void OnRootPreviewKeyDown( object pSender, KeyEventArgs pArgs )
		{
			if ( !HasMultipleImages() )
			{
				return;
			}

			switch ( pArgs.Key )
			{
				case Key.Left:
					{
						NavigatePrevious();
						pArgs.Handled = true;
						return;
					}
				case Key.Right:
					{
						NavigateNext();
						pArgs.Handled = true;
						break;
					}
			}
		}

		private void OnRootPreviewMouseWheel( object pSender, MouseWheelEventArgs pArgs )
		{
			if ( IsFullscreen )
			{
				pArgs.Handled = true;
				return;
			}

			pArgs.Handled = false;
		}

		private void OnRootPreviewMouseLeftButtonDown( object pSender, MouseButtonEventArgs pArgs )
		{
			mProjectImageCarouselControlRoot.Focus();
		}

		private void OnDotPreviewMouseLeftButtonDown( object pSender, MouseButtonEventArgs pArgs )
		{
			if ( !HasMultipleImages() )
			{
				return;
			}

			if ( !( pSender is ListBoxItem lDotListBoxItem ) )
			{
				return;
			}

			int lClickedIndex = mDotsListBox.ItemContainerGenerator.IndexFromContainer( lDotListBoxItem );

			if ( lClickedIndex < 0 )
			{
				return;
			}

			mProjectImageCarouselControlRoot.Focus();
			TryNavigateToIndex( lClickedIndex );
			pArgs.Handled = true;
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
