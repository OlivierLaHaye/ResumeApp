// ResumeApp.MainWindow.xaml.cs
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;

namespace ResumeApp
{
	public partial class MainWindow
	{
		private const double TitleBarHeight = 48.0;
		private const double NormalCornerRadius = 80.0;

		private const int WmGetMinMaxInfo = 0x0024;
		private const int MonitorDefaultToNearest = 2;

		private WindowChrome mWindowChrome;
		private HwndSource mHwndSource;

		public MainWindow()
		{
			InitializeComponent();
			StateChanged += OnWindowStateChanged;
			Closed += OnWindowClosed;
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
			if ( mHwndSource == null )
			{
				return;
			}

			mHwndSource.AddHook( WindowProc );
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

		private IntPtr WindowProc( IntPtr pHwnd, int pMessage, IntPtr pWParam, IntPtr pLParam, ref bool pIsHandled )
		{
			if ( pMessage == WmGetMinMaxInfo )
			{
				ApplyWorkingAreaMaximizeBounds( pHwnd, pLParam );
				pIsHandled = true;
			}

			return IntPtr.Zero;
		}

		private static void ApplyWorkingAreaMaximizeBounds( IntPtr pHwnd, IntPtr pLParam )
		{
			MINMAXINFO lMinMaxInfo = ( MINMAXINFO )Marshal.PtrToStructure( pLParam, typeof( MINMAXINFO ) );

			IntPtr lMonitorHandle = MonitorFromWindow( pHwnd, MonitorDefaultToNearest );
			if ( lMonitorHandle == IntPtr.Zero )
			{
				return;
			}

			MONITORINFO lMonitorInfo = new MONITORINFO
			{
				cbSize = Marshal.SizeOf( typeof( MONITORINFO ) )
			};

			bool lHasMonitorInfo = GetMonitorInfo( lMonitorHandle, ref lMonitorInfo );
			if ( !lHasMonitorInfo )
			{
				return;
			}

			RECT lWorkAreaRect = lMonitorInfo.rcWork;
			RECT lMonitorRect = lMonitorInfo.rcMonitor;

			lMinMaxInfo.ptMaxPosition.x = lWorkAreaRect.left - lMonitorRect.left;
			lMinMaxInfo.ptMaxPosition.y = lWorkAreaRect.top - lMonitorRect.top;

			lMinMaxInfo.ptMaxSize.x = lWorkAreaRect.right - lWorkAreaRect.left;
			lMinMaxInfo.ptMaxSize.y = lWorkAreaRect.bottom - lWorkAreaRect.top;

			Marshal.StructureToPtr( lMinMaxInfo, pLParam, true );
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

		[DllImport( "user32.dll" )]
		private static extern IntPtr MonitorFromWindow( IntPtr pHwnd, int pFlags );

		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		private static extern bool GetMonitorInfo( IntPtr pMonitorHandle, ref MONITORINFO pMonitorInfo );

		[StructLayout( LayoutKind.Sequential )]
		private struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout( LayoutKind.Sequential )]
		private struct MINMAXINFO
		{
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		}

		[StructLayout( LayoutKind.Sequential, CharSet = CharSet.Auto )]
		private struct MONITORINFO
		{
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public int dwFlags;
		}

		[StructLayout( LayoutKind.Sequential )]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}
	}
}
