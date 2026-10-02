// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using ResumeApp.AttachedProperties;
using ResumeApp.Services;

namespace ResumeApp;

[ExcludeFromCodeCoverage( Justification = "Window code-behind with P/Invoke, HwndSource hooks, and window chrome management that requires a running Windows desktop and HWND handle." )]
public partial class MainWindow
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

	private const double TitleBarHeight = 56.0;
	private const double InitialNormalSizeRatio = 0.95;
	private const double MinimumWindowWidth = 960.0;
	private const double MinimumWindowHeight = 640.0;
	private const double FallbackFrameThickness = 1.0;
	private const string SelectedContentHostPartName = "PART_SelectedContentHost";

	private const int WmGetMinMaxInfo = 0x0024;
	private const int MonitorDefaultToNearest = 2;
	private const int DwmWindowCornerPreferenceAttribute = 33;
	private const int DwmWindowCornerPreferenceRound = 2;

	private static readonly TimeSpan sSectionFadeDuration = TimeSpan.FromMilliseconds( 160 );

	private HwndSource? mHwndSource;
	private bool mHasAppliedInitialNormalBounds;
	private bool mHasSystemRoundedCorners;

	public MainWindow()
	{
		InitializeComponent();
		WindowStartupLocation = WindowStartupLocation.Manual;

		MinWidth = MinimumWindowWidth;
		MinHeight = MinimumWindowHeight;

		StateChanged += OnWindowStateChanged;
		Closed += OnWindowClosed;
		Loaded += OnMainWindowLoaded;
		SizeChanged += OnMainWindowSizeChanged;
		DataContextChanged += OnMainWindowDataContextChanged;
		mMainTabControl.SelectionChanged += OnMainTabControlSelectionChanged;

		UpdateShellReadyState();
	}

	internal static ( int Width, int Height ) ClampMinimumTrackSize( int pRequestedWidthPixels, int pRequestedHeightPixels, int pWorkAreaWidthPixels, int pWorkAreaHeightPixels )
	{
		int lWidth = Math.Max( 0, pRequestedWidthPixels );
		int lHeight = Math.Max( 0, pRequestedHeightPixels );

		if ( pWorkAreaWidthPixels > 0 )
		{
			lWidth = Math.Min( lWidth, pWorkAreaWidthPixels );
		}

		if ( pWorkAreaHeightPixels > 0 )
		{
			lHeight = Math.Min( lHeight, pWorkAreaHeightPixels );
		}

		return ( lWidth, lHeight );
	}

	public void ShowStartupStatus( string? pStatusText )
	{
		mStartupStatusTextBlock.Text = pStatusText ?? string.Empty;
		mStartupStatusGrid.Visibility = Visibility.Visible;
	}

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
		System.Windows.Point lTopLeftDip = pTransformFromDevice.Transform( new System.Windows.Point( pRectPixels.left, pRectPixels.top ) );
		System.Windows.Point lBottomRightDip = pTransformFromDevice.Transform( new System.Windows.Point( pRectPixels.right, pRectPixels.bottom ) );

		return new System.Windows.Rect( lTopLeftDip, lBottomRightDip );
	}

	private static IntPtr GetTargetMonitorHandle( IntPtr pWindowHandle )
	{
		if ( GetCursorPos( out Point lCursorPoint ) )
		{
			IntPtr lMonitorFromCursor = MonitorFromPoint( lCursorPoint, MonitorDefaultToNearest );
			if ( lMonitorFromCursor != IntPtr.Zero )
			{
				return lMonitorFromCursor;
			}
		}

		return MonitorFromWindow( pWindowHandle, MonitorDefaultToNearest );
	}

	private static bool TryGetMonitorInfo( IntPtr pMonitorHandle, out MonitorInfo pMonitorInfo )
	{
		pMonitorInfo = new MonitorInfo
		{
			cbSize = Marshal.SizeOf( typeof( MonitorInfo ) )
		};

		return pMonitorHandle != IntPtr.Zero && GetMonitorInfo( pMonitorHandle, ref pMonitorInfo );
	}

	private static bool TryGetWorkAreaRectPixels( IntPtr pMonitorHandle, out Rect pWorkAreaRectPixels )
	{
		pWorkAreaRectPixels = default;

		if ( !TryGetMonitorInfo( pMonitorHandle, out MonitorInfo lMonitorInfo ) )
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
		if ( !TryGetMonitorInfo( lMonitorHandle, out MonitorInfo lMonitorInfo ) )
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
		MinMaxInfo lMinMaxInfo = Marshal.PtrToStructure<MinMaxInfo>( pLParam );

		IntPtr lMonitorHandle = MonitorFromWindow( pHwnd, MonitorDefaultToNearest );
		if ( !TryGetMonitorInfo( lMonitorHandle, out MonitorInfo lMonitorInfo ) )
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
		MinMaxInfo lMinMaxInfo = Marshal.PtrToStructure<MinMaxInfo>( pLParam );

		double lRequestedMinWidthDip = MinimumWindowWidth;
		double lRequestedMinHeightDip = MinimumWindowHeight;

		if ( TryGetRequestedMinSizeDip( pHwnd, out double lWindowMinWidthDip, out double lWindowMinHeightDip ) )
		{
			lRequestedMinWidthDip = lWindowMinWidthDip;
			lRequestedMinHeightDip = lWindowMinHeightDip;
		}

		lRequestedMinWidthDip = Math.Max( lRequestedMinWidthDip, MinimumWindowWidth );
		lRequestedMinHeightDip = Math.Max( lRequestedMinHeightDip, MinimumWindowHeight );

		Matrix lTransformToDevice = GetTransformToDeviceOrIdentity( pHwnd );

		int lRequestedMinWidthPixels = ClampToInt32Ceiling( lRequestedMinWidthDip * lTransformToDevice.M11 );
		int lRequestedMinHeightPixels = ClampToInt32Ceiling( lRequestedMinHeightDip * lTransformToDevice.M22 );

		int lMinTrackWidthPixels = Math.Max( lMinMaxInfo.ptMinTrackSize.x, lRequestedMinWidthPixels );
		int lMinTrackHeightPixels = Math.Max( lMinMaxInfo.ptMinTrackSize.y, lRequestedMinHeightPixels );

		if ( TryGetMonitorWorkAreaSizePixels( pHwnd, out int lWorkAreaWidthPixels, out int lWorkAreaHeightPixels ) )
		{
			( lMinTrackWidthPixels, lMinTrackHeightPixels ) = ClampMinimumTrackSize( lMinTrackWidthPixels, lMinTrackHeightPixels, lWorkAreaWidthPixels, lWorkAreaHeightPixels );
		}

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
		mHwndSource = lWindowHandle != IntPtr.Zero ? HwndSource.FromHwnd( lWindowHandle ) : null;
		mHwndSource?.AddHook( WindowProc );
	}

	private void OnMainWindowLoaded( object? pSender, RoutedEventArgs pEventArgs )
	{
		ApplyInitialNormalBoundsIfNeeded();
		UpdateSizeClass();
	}

	private void OnMainWindowSizeChanged( object? pSender, SizeChangedEventArgs pEventArgs )
	{
		UpdateSizeClass();
	}

	private void UpdateSizeClass()
	{
		LayoutSizeClass lSizeClass = AdaptiveLayout.GetSizeClassForWidth( ActualWidth );

		if ( AdaptiveLayout.GetSizeClass( this ) == lSizeClass )
		{
			return;
		}

		AdaptiveLayout.SetSizeClass( this, lSizeClass );
	}

	private void OnMainWindowDataContextChanged( object? pSender, DependencyPropertyChangedEventArgs pEventArgs )
	{
		UpdateShellReadyState();
	}

	private void UpdateShellReadyState()
	{
		bool lIsReady = DataContext != null;
		Visibility lShellVisibility = lIsReady ? Visibility.Visible : Visibility.Hidden;

		mLanguageSegmentBorder.Visibility = lShellVisibility;
		mThemeSegmentBorder.Visibility = lShellVisibility;
		mMainTabControl.Visibility = lShellVisibility;

		if ( lIsReady )
		{
			mStartupStatusGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void OnMainTabControlSelectionChanged( object? pSender, SelectionChangedEventArgs pEventArgs )
	{
		if ( !ReferenceEquals( pEventArgs.OriginalSource, mMainTabControl ) )
		{
			return;
		}

		if ( mMainTabControl.Template?.FindName( SelectedContentHostPartName, mMainTabControl ) is not UIElement lContentHost )
		{
			return;
		}

		if ( !MotionPolicy.IsAnimationEnabled )
		{
			lContentHost.BeginAnimation( OpacityProperty, null );
			return;
		}

		var lFadeAnimation = new DoubleAnimation( 0.0, 1.0, MotionPolicy.GetDuration( sSectionFadeDuration ) )
		{
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
		};

		lContentHost.BeginAnimation( OpacityProperty, lFadeAnimation );
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

		IntPtr lMonitorHandle = GetTargetMonitorHandle( lWindowHandle );
		if ( lMonitorHandle == IntPtr.Zero || !TryGetWorkAreaRectPixels( lMonitorHandle, out Rect lWorkAreaRectPixels ) )
		{
			return;
		}

		Matrix lTransformFromDevice = GetTransformFromDeviceOrIdentity( lWindowHandle );

		System.Windows.Rect lWorkAreaRectDip = ConvertRectFromPixelsToDip( lWorkAreaRectPixels, lTransformFromDevice );
		if ( lWorkAreaRectDip.Width <= 0.0 || lWorkAreaRectDip.Height <= 0.0 )
		{
			return;
		}

		double lTargetWidth = Math.Floor( lWorkAreaRectDip.Width * InitialNormalSizeRatio );
		double lTargetHeight = Math.Floor( lWorkAreaRectDip.Height * InitialNormalSizeRatio );

		lTargetWidth = Math.Min( lTargetWidth, lWorkAreaRectDip.Width );
		lTargetHeight = Math.Min( lTargetHeight, lWorkAreaRectDip.Height );

		lTargetWidth = Math.Min( Math.Max( lTargetWidth, MinWidth ), lWorkAreaRectDip.Width );
		lTargetHeight = Math.Min( Math.Max( lTargetHeight, MinHeight ), lWorkAreaRectDip.Height );

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
		if ( mHwndSource is not HwndSource lHwndSource )
		{
			return;
		}

		lHwndSource.RemoveHook( WindowProc );
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
}
