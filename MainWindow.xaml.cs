// Copyright (C) Olivier La Haye
// All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace ResumeApp
{
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

		private const double TitleBarHeight = 48.0;
		private const double NormalCornerRadius = 80.0;
		private const double InitialNormalSizeRatio = 0.95;
		private const double MinimumWindowWidth = 1400.0;

		private const int WmGetMinMaxInfo = 0x0024;
		private const int MonitorDefaultToNearest = 2;

		private WindowChrome mWindowChrome;
		private HwndSource mHwndSource;
		private bool mHasAppliedInitialNormalBounds;

		public MainWindow()
		{
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.Manual;

			MinWidth = MinimumWindowWidth;

			StateChanged += OnWindowStateChanged;
			Closed += OnWindowClosed;
			Loaded += OnMainWindowLoaded;
		}

		private static Matrix GetTransformFromDeviceOrIdentity( IntPtr pWindowHandle )
		{
			HwndSource lHwndSource = HwndSource.FromHwnd( pWindowHandle );

			CompositionTarget lCompositionTarget = lHwndSource?.CompositionTarget;
			return lCompositionTarget?.TransformFromDevice ?? Matrix.Identity;
		}

		private static Matrix GetTransformToDeviceOrIdentity( IntPtr pWindowHandle )
		{
			HwndSource lHwndSource = HwndSource.FromHwnd( pWindowHandle );

			CompositionTarget lCompositionTarget = lHwndSource?.CompositionTarget;
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
			bool lHasCursorPos = GetCursorPos( out var lCursorPoint );
			if ( !lHasCursorPos )
			{
				return MonitorFromWindow( pWindowHandle, MonitorDefaultToNearest );
			}

			IntPtr lMonitorFromCursor = MonitorFromPoint( lCursorPoint, MonitorDefaultToNearest );

			return lMonitorFromCursor != IntPtr.Zero ? lMonitorFromCursor : MonitorFromWindow( pWindowHandle, MonitorDefaultToNearest );
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

		private static bool TryGetMonitorWorkAreaWidthPixels( IntPtr pHwnd, out int pWorkAreaWidthPixels )
		{
			pWorkAreaWidthPixels = 0;

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
			return pWorkAreaWidthPixels > 0;
		}

		private static bool TryGetRequestedMinSizeDip( IntPtr pHwnd, out double pMinWidthDip, out double pMinHeightDip )
		{
			pMinWidthDip = 0.0;
			pMinHeightDip = 0.0;

			HwndSource lHwndSource = HwndSource.FromHwnd( pHwnd );
			if ( !( lHwndSource?.RootVisual is Window lWindow ) )
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
			MinMaxInfo lMinMaxInfo = ( MinMaxInfo )Marshal.PtrToStructure( pLParam, typeof( MinMaxInfo ) );

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
			MinMaxInfo lMinMaxInfo = ( MinMaxInfo )Marshal.PtrToStructure( pLParam, typeof( MinMaxInfo ) );

			bool lHasRequestedMinSize = TryGetRequestedMinSizeDip( pHwnd, out double lRequestedMinWidthDip, out double lRequestedMinHeightDip );
			if ( !lHasRequestedMinSize )
			{
				lRequestedMinWidthDip = MinimumWindowWidth;
				lRequestedMinHeightDip = 0.0;
			}

			lRequestedMinWidthDip = Math.Max( lRequestedMinWidthDip, MinimumWindowWidth );

			Matrix lTransformToDevice = GetTransformToDeviceOrIdentity( pHwnd );

			int lRequestedMinWidthPixels = ClampToInt32Ceiling( lRequestedMinWidthDip * lTransformToDevice.M11 );
			int lRequestedMinHeightPixels = ClampToInt32Ceiling( lRequestedMinHeightDip * lTransformToDevice.M22 );

			int lMinTrackWidthPixels = Math.Max( lMinMaxInfo.ptMinTrackSize.x, lRequestedMinWidthPixels );
			int lMinTrackHeightPixels = Math.Max( lMinMaxInfo.ptMinTrackSize.y, lRequestedMinHeightPixels );

			bool lHasWorkAreaWidthPixels = TryGetMonitorWorkAreaWidthPixels( pHwnd, out int lWorkAreaWidthPixels );
			if ( lHasWorkAreaWidthPixels && lWorkAreaWidthPixels > 0 )
			{
				lMinTrackWidthPixels = Math.Min( lMinTrackWidthPixels, lWorkAreaWidthPixels );
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
			UpdateWindowChromeForCurrentState();
		}

		private void InitializeWindowChrome()
		{
			mWindowChrome = new WindowChrome
			{
				CaptionHeight = TitleBarHeight,
				CornerRadius = new CornerRadius( NormalCornerRadius ),
				GlassFrameThickness = new Thickness( 0 ),
				ResizeBorderThickness = new Thickness( 6 ),
				UseAeroCaptionButtons = false
			};

			WindowChrome.SetWindowChrome( this, mWindowChrome );
		}

		private void InitializeWindowHooks()
		{
			IntPtr lWindowHandle = new WindowInteropHelper( this ).Handle;

			mHwndSource = HwndSource.FromHwnd( lWindowHandle );

			mHwndSource?.AddHook( WindowProc );
		}

		private void OnMainWindowLoaded( object pSender, RoutedEventArgs pEventArgs )
		{
			ApplyInitialNormalBoundsIfNeeded();
		}

		private void ApplyInitialNormalBoundsIfNeeded()
		{
			if ( mHasAppliedInitialNormalBounds )
			{
				return;
			}

			if ( WindowState != WindowState.Normal )
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

			bool lHasWorkArea = TryGetWorkAreaRectPixels( lMonitorHandle, out var lWorkAreaRectPixels );
			if ( !lHasWorkArea )
			{
				return;
			}

			System.Windows.Rect lWorkAreaRectDip = ConvertRectFromPixelsToDip( lWorkAreaRectPixels, lTransformFromDevice );
			if ( lWorkAreaRectDip.Width <= 0.0 || lWorkAreaRectDip.Height <= 0.0 )
			{
				return;
			}

			double lTargetWidth = Math.Floor( lWorkAreaRectDip.Width * InitialNormalSizeRatio );
			double lTargetHeight = Math.Floor( lWorkAreaRectDip.Height * InitialNormalSizeRatio );

			lTargetWidth = Math.Min( lTargetWidth, lWorkAreaRectDip.Width );
			lTargetHeight = Math.Min( lTargetHeight, lWorkAreaRectDip.Height );

			lTargetWidth = Math.Max( lTargetWidth, MinWidth );

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

		private void OnWindowClosed( object pSender, EventArgs pEventArgs )
		{
			if ( mHwndSource == null )
			{
				return;
			}

			mHwndSource.RemoveHook( WindowProc );
			mHwndSource = null;
		}

		private void OnWindowStateChanged( object pSender, EventArgs pEventArgs )
		{
			UpdateWindowChromeForCurrentState();
		}

		private void UpdateWindowChromeForCurrentState()
		{
			if ( mWindowChrome == null )
			{
				return;
			}

			mWindowChrome.CornerRadius = WindowState == WindowState.Maximized
				? new CornerRadius( 0 )
				: new CornerRadius( NormalCornerRadius );
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
}
