// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using ResumeApp.Services;

namespace ResumeApp.Windows
{
	[ExcludeFromCodeCoverage( Justification = "Window code-behind with P/Invoke, HwndSource hooks, and window chrome management that requires a running Windows desktop and HWND handle." )]
	public partial class ProjectImageViewerWindow
	{
		[StructLayout( LayoutKind.Sequential )]
		private struct Point
		{
			public int x;
			public int y;
		}

		[StructLayout( LayoutKind.Sequential )]
		private struct MinMaxInfo
		{
			public readonly Point ptReserved;
			public Point ptMaxSize;
			public Point ptMaxPosition;
			public Point ptMinTrackSize;
			public Point ptMaxTrackSize;
		}

		[StructLayout( LayoutKind.Sequential, CharSet = CharSet.Auto )]
		private struct MonitorInfo
		{
			public int cbSize;
			public readonly Rect rcMonitor;
			public readonly Rect rcWork;
			public readonly int dwFlags;
		}

		[StructLayout( LayoutKind.Sequential )]
		private struct Rect
		{
			public readonly int left;
			public readonly int top;
			public readonly int right;
			public readonly int bottom;
		}

		private const double TitleBarHeight = 48.0;
		private const double FallbackFrameThickness = 1.0;
		private const double InitialNormalSizeRatio = 0.95;
		private const double MinimumWindowWidth = 960.0;
		private const double MinimumWindowHeight = 640.0;

		private const int WmGetMinMaxInfo = 0x0024;
		private const int MonitorDefaultToNearest = 2;
		private const int DwmWindowCornerPreferenceAttribute = 33;
		private const int DwmWindowCornerPreferenceRound = 2;

		public static readonly DependencyProperty sImagesProperty =
			DependencyProperty.Register(
				nameof( Images ),
				typeof( ObservableCollection<ImageSource> ),
				typeof( ProjectImageViewerWindow ),
				new PropertyMetadata( null, OnImagesChanged ) );

		public static readonly DependencyProperty sSelectedIndexProperty =
			DependencyProperty.Register(
				nameof( SelectedIndex ),
				typeof( int ),
				typeof( ProjectImageViewerWindow ),
				new FrameworkPropertyMetadata( -1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged ) );

		public static readonly DependencyProperty sResourcesServiceProperty =
			DependencyProperty.Register(
				nameof( ResourcesService ),
				typeof( ResourcesService ),
				typeof( ProjectImageViewerWindow ),
				new PropertyMetadata( null ) );

		private readonly Dictionary<int, ImageSource> mFullResolutionImagesByIndex = [];
		private readonly HashSet<int> mPendingDecodeIndexes = [];

		private HwndSource? mHwndSource;
		private CancellationTokenSource? mDecodeCancellationTokenSource = new();
		private bool mHasAppliedInitialNormalBounds;
		private bool mHasSystemRoundedCorners;
		private bool mIsClosed;

		public ObservableCollection<ImageSource> Images
		{
			get
			{
				if ( GetValue( sImagesProperty ) is ObservableCollection<ImageSource> lImages )
				{
					return lImages;
				}

				var lNewImages = new ObservableCollection<ImageSource>();
				SetValue( sImagesProperty, lNewImages );

				return lNewImages;
			}
			set => SetValue( sImagesProperty, value );
		}

		public int SelectedIndex
		{
			get => GetValue( sSelectedIndexProperty ) is int lSelectedIndex ? lSelectedIndex : -1;
			set => SetValue( sSelectedIndexProperty, value );
		}

		public ResourcesService? ResourcesService
		{
			get => GetValue( sResourcesServiceProperty ) as ResourcesService;
			set => SetValue( sResourcesServiceProperty, value );
		}

		public ProjectImageViewerWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.Manual;

			MinWidth = MinimumWindowWidth;
			MinHeight = MinimumWindowHeight;

			mFullscreenProjectImageCarouselControl.DisplayImageProvider = GetFullResolutionImage;

			StateChanged += OnWindowStateChanged;
			Closed += OnWindowClosed;
			Loaded += OnProjectImageViewerWindowLoaded;
		}

		internal static int ComputeMinTrackSizePixels( int pSystemMinPixels, int pRequestedMinPixels, int pWorkAreaPixels )
		{
			int lMinPixels = Math.Max( pSystemMinPixels, pRequestedMinPixels );

			return pWorkAreaPixels > 0
				? Math.Min( lMinPixels, pWorkAreaPixels )
				: lMinPixels;
		}

		internal static double ComputeInitialSizeDip( double pWorkAreaDip, double pRatio, double pMinimumDip )
		{
			double lTargetDip = Math.Max( Math.Floor( pWorkAreaDip * pRatio ), pMinimumDip );

			return Math.Min( lTargetDip, pWorkAreaDip );
		}

		internal static int WrapIndex( int pIndex, int pCount ) => ( ( pIndex % pCount ) + pCount ) % pCount;

		internal static int[] GetFullResolutionWindowIndexes( int pCurrentIndex, int pCount )
		{
			if ( pCount <= 0 || pCurrentIndex < 0 || pCurrentIndex >= pCount )
			{
				return [];
			}

			var lIndexes = new List<int>( 3 ) { pCurrentIndex };

			foreach ( int lOffset in new[] { 1, -1 } )
			{
				int lIndex = WrapIndex( pCurrentIndex + lOffset, pCount );
				if ( !lIndexes.Contains( lIndex ) )
				{
					lIndexes.Add( lIndex );
				}
			}

			return [.. lIndexes];
		}

		private static void OnImagesChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is ProjectImageViewerWindow lWindow )
			{
				lWindow.ResetFullResolutionState();
			}
		}

		private static void OnSelectedIndexChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is ProjectImageViewerWindow lWindow )
			{
				lWindow.QueueFullResolutionDecode();
			}
		}

		private static bool CanDecodeOffUiThread( ImageSource? pImageSource ) =>
			pImageSource is not Freezable lFreezable || lFreezable.IsFrozen;

		private static Matrix GetTransformFromDeviceOrIdentity( IntPtr pWindowHandle )
		{
			HwndSource? lHwndSource = HwndSource.FromHwnd( pWindowHandle );

			CompositionTarget? lCompositionTarget = lHwndSource?.CompositionTarget;
			return lCompositionTarget?.TransformFromDevice ?? Matrix.Identity;
		}

		private static Matrix GetTransformToDeviceOrIdentity( IntPtr pWindowHandle )
		{
			HwndSource? lHwndSource = HwndSource.FromHwnd( pWindowHandle );

			CompositionTarget? lCompositionTarget = lHwndSource?.CompositionTarget;
			return lCompositionTarget?.TransformToDevice ?? Matrix.Identity;
		}

		private static System.Windows.Rect ConvertRectFromPixelsToDip( Rect pRectPixels, Matrix pTransformFromDevice )
		{
			System.Windows.Point lTopLeftDip =
				pTransformFromDevice.Transform( new System.Windows.Point( pRectPixels.left, pRectPixels.top ) );

			System.Windows.Point lBottomRightDip =
				pTransformFromDevice.Transform( new System.Windows.Point( pRectPixels.right, pRectPixels.bottom ) );

			return new System.Windows.Rect( lTopLeftDip, lBottomRightDip );
		}

		private static IntPtr GetTargetMonitorHandle( IntPtr pWindowHandle )
		{
			if ( !GetCursorPos( out Point lCursorPoint ) )
			{
				return MonitorFromWindow( pWindowHandle, MonitorDefaultToNearest );
			}

			IntPtr lMonitorFromCursor = MonitorFromPoint( lCursorPoint, MonitorDefaultToNearest );

			return lMonitorFromCursor != IntPtr.Zero
				? lMonitorFromCursor
				: MonitorFromWindow( pWindowHandle, MonitorDefaultToNearest );
		}

		private static bool TryGetWorkAreaRectPixels( IntPtr pMonitorHandle, out Rect pWorkAreaRectPixels )
		{
			pWorkAreaRectPixels = default;

			MonitorInfo lMonitorInfo = new MonitorInfo
			{
				cbSize = Marshal.SizeOf( typeof( MonitorInfo ) )
			};

			bool lHasMonitorInfo = GetMonitorInfo( pMonitorHandle, ref lMonitorInfo );
			if ( !lHasMonitorInfo )
			{
				return false;
			}

			pWorkAreaRectPixels = lMonitorInfo.rcWork;
			return true;
		}

		private static bool TryGetMonitorWorkAreaSizePixels( IntPtr pHwnd, out int pWorkAreaWidthPixels, out int pWorkAreaHeightPixels )
		{
			pWorkAreaWidthPixels = 0;
			pWorkAreaHeightPixels = 0;

			IntPtr lMonitorHandle = MonitorFromWindow( pHwnd, MonitorDefaultToNearest );
			if ( lMonitorHandle == IntPtr.Zero )
			{
				return false;
			}

			MonitorInfo lMonitorInfo = new MonitorInfo
			{
				cbSize = Marshal.SizeOf( typeof( MonitorInfo ) )
			};

			bool lHasMonitorInfo = GetMonitorInfo( lMonitorHandle, ref lMonitorInfo );
			if ( !lHasMonitorInfo )
			{
				return false;
			}

			pWorkAreaWidthPixels = Math.Max( 0, lMonitorInfo.rcWork.right - lMonitorInfo.rcWork.left );
			pWorkAreaHeightPixels = Math.Max( 0, lMonitorInfo.rcWork.bottom - lMonitorInfo.rcWork.top );
			return pWorkAreaWidthPixels > 0 && pWorkAreaHeightPixels > 0;
		}

		private static bool TryGetRequestedMinSizeDip( IntPtr pHwnd, out double pMinWidthDip, out double pMinHeightDip )
		{
			pMinWidthDip = 0.0;
			pMinHeightDip = 0.0;

			HwndSource? lHwndSource = HwndSource.FromHwnd( pHwnd );
			if ( lHwndSource?.RootVisual is not Window lWindow )
			{
				return false;
			}

			pMinWidthDip = lWindow.MinWidth;
			pMinHeightDip = lWindow.MinHeight;

			return true;
		}

		private static int ClampToInt32Ceiling( double pValue )
		{
			if ( double.IsNaN( pValue ) || double.IsInfinity( pValue ) || pValue <= 0.0 )
			{
				return 0;
			}

			double lCeiling = Math.Ceiling( pValue );
			if ( lCeiling >= int.MaxValue )
			{
				return int.MaxValue;
			}

			return ( int )lCeiling;
		}

		private static void ApplyWorkingAreaMaximizeBounds( IntPtr pHwnd, IntPtr pLParam )
		{
			if ( pLParam == IntPtr.Zero )
			{
				return;
			}

			MinMaxInfo lMinMaxInfo = Marshal.PtrToStructure<MinMaxInfo>( pLParam );

			IntPtr lMonitorHandle = MonitorFromWindow( pHwnd, MonitorDefaultToNearest );
			if ( lMonitorHandle == IntPtr.Zero )
			{
				return;
			}

			MonitorInfo lMonitorInfo = new MonitorInfo
			{
				cbSize = Marshal.SizeOf( typeof( MonitorInfo ) )
			};

			bool lHasMonitorInfo = GetMonitorInfo( lMonitorHandle, ref lMonitorInfo );
			if ( !lHasMonitorInfo )
			{
				return;
			}

			Rect lWorkAreaRect = lMonitorInfo.rcWork;
			Rect lMonitorRect = lMonitorInfo.rcMonitor;

			lMinMaxInfo.ptMaxPosition.x = lWorkAreaRect.left - lMonitorRect.left;
			lMinMaxInfo.ptMaxPosition.y = lWorkAreaRect.top - lMonitorRect.top;

			lMinMaxInfo.ptMaxSize.x = lWorkAreaRect.right - lWorkAreaRect.left;
			lMinMaxInfo.ptMaxSize.y = lWorkAreaRect.bottom - lWorkAreaRect.top;

			Marshal.StructureToPtr( lMinMaxInfo, pLParam, true );
		}

		private static void ApplyMinimumTrackSizeBounds( IntPtr pHwnd, IntPtr pLParam )
		{
			if ( pLParam == IntPtr.Zero )
			{
				return;
			}

			MinMaxInfo lMinMaxInfo = Marshal.PtrToStructure<MinMaxInfo>( pLParam );

			bool lHasRequestedMinSize =
				TryGetRequestedMinSizeDip( pHwnd, out double lRequestedMinWidthDip, out double lRequestedMinHeightDip );

			if ( !lHasRequestedMinSize )
			{
				lRequestedMinWidthDip = MinimumWindowWidth;
				lRequestedMinHeightDip = MinimumWindowHeight;
			}

			lRequestedMinWidthDip = Math.Max( lRequestedMinWidthDip, MinimumWindowWidth );
			lRequestedMinHeightDip = Math.Max( lRequestedMinHeightDip, MinimumWindowHeight );

			Matrix lTransformToDevice = GetTransformToDeviceOrIdentity( pHwnd );

			int lRequestedMinWidthPixels = ClampToInt32Ceiling( lRequestedMinWidthDip * lTransformToDevice.M11 );
			int lRequestedMinHeightPixels = ClampToInt32Ceiling( lRequestedMinHeightDip * lTransformToDevice.M22 );

			bool lHasWorkAreaSizePixels = TryGetMonitorWorkAreaSizePixels( pHwnd, out int lWorkAreaWidthPixels, out int lWorkAreaHeightPixels );
			if ( !lHasWorkAreaSizePixels )
			{
				lWorkAreaWidthPixels = 0;
				lWorkAreaHeightPixels = 0;
			}

			int lMinTrackWidthPixels = ComputeMinTrackSizePixels( lMinMaxInfo.ptMinTrackSize.x, lRequestedMinWidthPixels, lWorkAreaWidthPixels );
			int lMinTrackHeightPixels = ComputeMinTrackSizePixels( lMinMaxInfo.ptMinTrackSize.y, lRequestedMinHeightPixels, lWorkAreaHeightPixels );

			if ( lMinTrackWidthPixels > 0 )
			{
				lMinMaxInfo.ptMinTrackSize.x = lMinTrackWidthPixels;
			}

			if ( lMinTrackHeightPixels > 0 )
			{
				lMinMaxInfo.ptMinTrackSize.y = lMinTrackHeightPixels;
			}

			Marshal.StructureToPtr( lMinMaxInfo, pLParam, true );
		}

		[DllImport( "user32.dll" )]
		private static extern IntPtr MonitorFromWindow( IntPtr pHwnd, int pFlags );

		[DllImport( "user32.dll" )]
		private static extern IntPtr MonitorFromPoint( Point pPoint, int pFlags );

		[DllImport( "user32.dll" )]
		private static extern bool GetCursorPos( out Point pPoint );

		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		private static extern bool GetMonitorInfo( IntPtr pMonitorHandle, ref MonitorInfo pMonitorInfo );

		[DllImport( "dwmapi.dll" )]
		private static extern int DwmSetWindowAttribute( IntPtr pHwnd, int pAttribute, ref int pValue, int pValueSize );

		private static bool TryApplySystemRoundedCorners( IntPtr pHwnd )
		{
			if ( pHwnd == IntPtr.Zero )
			{
				return false;
			}

			try
			{
				int lPreference = DwmWindowCornerPreferenceRound;
				return DwmSetWindowAttribute( pHwnd, DwmWindowCornerPreferenceAttribute, ref lPreference, sizeof( int ) ) == 0;
			}
			catch ( Exception )
			{
				return false;
			}
		}

		private static IntPtr WindowProc( IntPtr pHwnd, int pMessage, IntPtr pWParam, IntPtr pLParam, ref bool pIsHandled )
		{
			if ( pMessage != WmGetMinMaxInfo )
			{
				return IntPtr.Zero;
			}

			ApplyWorkingAreaMaximizeBounds( pHwnd, pLParam );
			ApplyMinimumTrackSizeBounds( pHwnd, pLParam );

			pIsHandled = true;

			return IntPtr.Zero;
		}

		private static double Clamp( double pValue, double pMin, double pMax )
		{
			if ( pValue < pMin )
			{
				return pMin;
			}

			return pValue > pMax ? pMax : pValue;
		}

		protected override void OnSourceInitialized( EventArgs pEventArgs )
		{
			base.OnSourceInitialized( pEventArgs );

			InitializeWindowChrome();
			InitializeWindowHooks();
			mHasSystemRoundedCorners = TryApplySystemRoundedCorners( new WindowInteropHelper( this ).Handle );
			UpdateWindowChromeForCurrentState();
		}

		private void InitializeWindowChrome()
		{
			var lWindowChrome = new WindowChrome
			{
				CaptionHeight = TitleBarHeight,
				CornerRadius = new CornerRadius( 0 ),
				GlassFrameThickness = new Thickness( 0 ),
				ResizeBorderThickness = new Thickness( 6 ),
				UseAeroCaptionButtons = false
			};

			WindowChrome.SetWindowChrome( this, lWindowChrome );
		}

		private void InitializeWindowHooks()
		{
			IntPtr lWindowHandle = new WindowInteropHelper( this ).Handle;

			mHwndSource = HwndSource.FromHwnd( lWindowHandle );

			mHwndSource?.AddHook( WindowProc );
		}

		private void OnProjectImageViewerWindowLoaded( object pSender, RoutedEventArgs pEventArgs )
		{
			ApplyInitialNormalBoundsIfNeeded();

			Activate();
			mFullscreenProjectImageCarouselControl.Focus();

			QueueFullResolutionDecode();
		}

		protected override void OnKeyDown( KeyEventArgs pEventArgs )
		{
			base.OnKeyDown( pEventArgs );

			if ( pEventArgs.Handled || ( Keyboard.Modifiers & ( ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows ) ) != ModifierKeys.None )
			{
				return;
			}

			int lImageCount = Images.Count;

			switch ( pEventArgs.Key )
			{
				case Key.Escape:
					{
						pEventArgs.Handled = true;
						Close();
						return;
					}
				case Key.Left when lImageCount > 1:
					{
						SelectedIndex = WrapIndex( SelectedIndex - 1, lImageCount );
						pEventArgs.Handled = true;
						return;
					}
				case Key.Right when lImageCount > 1:
					{
						SelectedIndex = WrapIndex( SelectedIndex + 1, lImageCount );
						pEventArgs.Handled = true;
						return;
					}
				case Key.Home when lImageCount > 1:
					{
						SelectedIndex = 0;
						pEventArgs.Handled = true;
						return;
					}
				case Key.End when lImageCount > 1:
					{
						SelectedIndex = lImageCount - 1;
						pEventArgs.Handled = true;
						return;
					}
			}
		}

		private ImageSource? GetFullResolutionImage( int pIndex ) =>
			mFullResolutionImagesByIndex.GetValueOrDefault( pIndex );

		private void ResetFullResolutionState()
		{
			if ( mIsClosed )
			{
				return;
			}

			mDecodeCancellationTokenSource?.Cancel();
			mDecodeCancellationTokenSource?.Dispose();
			mDecodeCancellationTokenSource = new CancellationTokenSource();

			mFullResolutionImagesByIndex.Clear();
			mPendingDecodeIndexes.Clear();

			QueueFullResolutionDecode();
		}

		private void QueueFullResolutionDecode()
		{
			if ( mIsClosed || !IsLoaded || mDecodeCancellationTokenSource is null )
			{
				return;
			}

			ObservableCollection<ImageSource> lImages = Images;
			int[] lWantedIndexes = GetFullResolutionWindowIndexes( SelectedIndex, lImages.Count );

			ReleaseFullResolutionImagesOutside( lWantedIndexes );

			foreach ( int lIndex in lWantedIndexes )
			{
				if ( mFullResolutionImagesByIndex.ContainsKey( lIndex ) || !mPendingDecodeIndexes.Add( lIndex ) )
				{
					continue;
				}

				_ = DecodeFullResolutionAsync( lIndex, lImages[ lIndex ], mDecodeCancellationTokenSource.Token );
			}
		}

		private void ReleaseFullResolutionImagesOutside( int[] pWantedIndexes )
		{
			int lReleasedCount = 0;

			foreach ( int lIndex in mFullResolutionImagesByIndex.Keys.Where( pIndex => !pWantedIndexes.Contains( pIndex ) ).ToList() )
			{
				mFullResolutionImagesByIndex.Remove( lIndex );
				lReleasedCount++;
			}

			if ( lReleasedCount > 0 )
			{
				mFullscreenProjectImageCarouselControl.RefreshDisplayedImages();
			}
		}

		private async Task DecodeFullResolutionAsync( int pIndex, ImageSource? pPreview, CancellationToken pCancellationToken )
		{
			BitmapSource? lFullResolution;

			try
			{
				lFullResolution = CanDecodeOffUiThread( pPreview )
					? await Task.Run( () => ImageDecodeService.TryCreateFullResolutionImage( pPreview ), pCancellationToken )
					: ImageDecodeService.TryCreateFullResolutionImage( pPreview );
			}
			catch ( Exception )
			{
				lFullResolution = null;
			}

			if ( pCancellationToken.IsCancellationRequested || mIsClosed )
			{
				return;
			}

			mPendingDecodeIndexes.Remove( pIndex );
			ApplyFullResolutionImage( pIndex, pPreview, lFullResolution );
		}

		private void ApplyFullResolutionImage( int pIndex, ImageSource? pPreview, BitmapSource? pFullResolution )
		{
			ObservableCollection<ImageSource> lImages = Images;

			if ( pFullResolution is null
				|| ReferenceEquals( pFullResolution, pPreview )
				|| pIndex < 0
				|| pIndex >= lImages.Count
				|| !ReferenceEquals( lImages[ pIndex ], pPreview )
				|| !GetFullResolutionWindowIndexes( SelectedIndex, lImages.Count ).Contains( pIndex ) )
			{
				return;
			}

			mFullResolutionImagesByIndex[ pIndex ] = pFullResolution;
			mFullscreenProjectImageCarouselControl.RefreshDisplayedImages();
		}

		private void ReleaseFullResolutionResources()
		{
			mIsClosed = true;

			mDecodeCancellationTokenSource?.Cancel();
			mDecodeCancellationTokenSource?.Dispose();
			mDecodeCancellationTokenSource = null;

			mFullResolutionImagesByIndex.Clear();
			mPendingDecodeIndexes.Clear();

			mFullscreenProjectImageCarouselControl.DisplayImageProvider = null;
		}

		private void ApplyInitialNormalBoundsIfNeeded()
		{
			if ( mHasAppliedInitialNormalBounds || WindowState != WindowState.Normal )
			{
				return;
			}

			IntPtr lWindowHandle = new WindowInteropHelper( this ).Handle;
			if ( lWindowHandle == IntPtr.Zero )
			{
				return;
			}

			Matrix lTransformFromDevice = GetTransformFromDeviceOrIdentity( lWindowHandle );

			IntPtr lMonitorHandle = GetTargetMonitorHandle( lWindowHandle );
			if ( lMonitorHandle == IntPtr.Zero )
			{
				return;
			}

			bool lHasWorkArea = TryGetWorkAreaRectPixels( lMonitorHandle, out Rect lWorkAreaRectPixels );
			if ( !lHasWorkArea )
			{
				return;
			}

			System.Windows.Rect lWorkAreaRectDip = ConvertRectFromPixelsToDip( lWorkAreaRectPixels, lTransformFromDevice );
			if ( lWorkAreaRectDip.Width <= 0.0 || lWorkAreaRectDip.Height <= 0.0 )
			{
				return;
			}

			MinWidth = Math.Min( MinimumWindowWidth, lWorkAreaRectDip.Width );
			MinHeight = Math.Min( MinimumWindowHeight, lWorkAreaRectDip.Height );

			double lTargetWidth = ComputeInitialSizeDip( lWorkAreaRectDip.Width, InitialNormalSizeRatio, MinWidth );
			double lTargetHeight = ComputeInitialSizeDip( lWorkAreaRectDip.Height, InitialNormalSizeRatio, MinHeight );

			if ( lTargetWidth <= 0.0 || lTargetHeight <= 0.0 )
			{
				return;
			}

			Width = lTargetWidth;
			Height = lTargetHeight;

			Left = lWorkAreaRectDip.Left + ( lWorkAreaRectDip.Width - lTargetWidth ) / 2.0;
			Top = lWorkAreaRectDip.Top + ( lWorkAreaRectDip.Height - lTargetHeight ) / 2.0;

			mHasAppliedInitialNormalBounds = true;
		}

		private void OnWindowClosed( object? pSender, EventArgs pEventArgs )
		{
			ReleaseFullResolutionResources();

			if ( mHwndSource == null )
			{
				return;
			}

			mHwndSource.RemoveHook( WindowProc );
			mHwndSource = null;
		}

		private void OnWindowStateChanged( object? pSender, EventArgs pEventArgs )
		{
			UpdateWindowChromeForCurrentState();
		}

		private void UpdateWindowChromeForCurrentState()
		{
			bool lIsFrameVisible = WindowState != WindowState.Maximized && !mHasSystemRoundedCorners;
			mWindowFrameBorder.BorderThickness = new Thickness( lIsFrameVisible ? FallbackFrameThickness : 0.0 );
		}

		private void OnMinimizeWindowButtonClick( object pSender, RoutedEventArgs pEventArgs )
		{
			WindowState = WindowState.Minimized;
		}

		private void OnMaximizeRestoreWindowButtonClick( object pSender, RoutedEventArgs pEventArgs )
		{
			WindowState = WindowState == WindowState.Maximized
				? WindowState.Normal
				: WindowState.Maximized;
		}

		private void OnCloseWindowButtonClick( object pSender, RoutedEventArgs pEventArgs )
		{
			Close();
		}

		private void OnCustomTitleBarDragSurfaceBorderMouseLeftButtonDown( object pSender, MouseButtonEventArgs? pMouseButtonEventArgs )
		{
			if ( pMouseButtonEventArgs is not { ButtonState: MouseButtonState.Pressed, ClickCount: 1 } )
			{
				return;
			}

			RestoreFromMaximizedForDragIfNeeded( pMouseButtonEventArgs );

			try
			{
				DragMove();
			}
			catch ( Exception )
			{
				// ignored
			}
		}

		private void RestoreFromMaximizedForDragIfNeeded( MouseEventArgs pMouseButtonEventArgs )
		{
			if ( WindowState != WindowState.Maximized )
			{
				return;
			}

			System.Windows.Point lCursorPositionInWindowDip = pMouseButtonEventArgs.GetPosition( this );
			System.Windows.Rect lRestoreBounds = RestoreBounds;

			double lHorizontalRatio = ActualWidth > 0.0 ? lCursorPositionInWindowDip.X / ActualWidth : 0.5;
			lHorizontalRatio = Clamp( lHorizontalRatio, 0.0, 1.0 );

			double lCursorOffsetXInRestoredWindow = lRestoreBounds.Width * lHorizontalRatio;
			double lCursorOffsetYInRestoredWindow =
				Math.Min( lCursorPositionInWindowDip.Y, Math.Max( 0.0, TitleBarHeight - 1.0 ) );

			System.Windows.Point lCursorPositionOnScreenDip = GetCursorScreenPositionDip( lCursorPositionInWindowDip );

			WindowState = WindowState.Normal;

			Left = lCursorPositionOnScreenDip.X - lCursorOffsetXInRestoredWindow;
			Top = lCursorPositionOnScreenDip.Y - lCursorOffsetYInRestoredWindow;
		}

		private System.Windows.Point GetCursorScreenPositionDip( System.Windows.Point pCursorPositionInWindowDip )
		{
			System.Windows.Point lCursorPositionOnScreenPixels = PointToScreen( pCursorPositionInWindowDip );

			IntPtr lWindowHandle = new WindowInteropHelper( this ).Handle;
			Matrix lTransformFromDevice = GetTransformFromDeviceOrIdentity( lWindowHandle );

			return lTransformFromDevice.Transform( lCursorPositionOnScreenPixels );
		}
	}
}
