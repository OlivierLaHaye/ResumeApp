// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ResumeApp.Services
{
	public static class ScrollViewerScrollService
	{

		private static ICommand sScrollToTopCommand;
		public static ICommand ScrollToTopCommand =>
					sScrollToTopCommand ??
					( sScrollToTopCommand = new RelayCommand( ExecuteScrollToTop ) );

		private static ICommand sScrollToBottomCommand;
		public static ICommand ScrollToBottomCommand =>
					sScrollToBottomCommand ??
					( sScrollToBottomCommand = new RelayCommand( ExecuteScrollToBottom ) );

		private static void ExecuteScrollToTop()
		{
			ExecuteScrollToTop( GetDefaultScrollSource() );
		}

		private static void ExecuteScrollToBottom()
		{
			ExecuteScrollToBottom( GetDefaultScrollSource() );
		}

		private static object GetDefaultScrollSource()
		{
			var lFocusedElement = Keyboard.FocusedElement;
			return lFocusedElement ?? Mouse.DirectlyOver;
		}

		private static void ExecuteScrollToTop( object pSource )
		{
			var lScrollViewer = ExtractScrollViewer( pSource );
			lScrollViewer?.ScrollToTop();
		}

		private static void ExecuteScrollToBottom( object pSource )
		{
			var lScrollViewer = ExtractScrollViewer( pSource );
			lScrollViewer?.ScrollToBottom();
		}

		private static ScrollViewer ExtractScrollViewer( object pCandidate )
		{
			if ( pCandidate is ScrollViewer lViewer )
			{
				return lViewer;
			}

			if ( !( pCandidate is DependencyObject lCurrent ) )
			{
				return null;
			}

			while ( lCurrent != null && !( lCurrent is ScrollViewer ) )
			{
				lCurrent = VisualTreeHelper.GetParent( lCurrent );
			}

			return ( ScrollViewer )lCurrent;
		}
	}
}
