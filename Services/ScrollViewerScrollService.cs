// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ResumeApp.Services;

public static class ScrollViewerScrollService
{
	private static ICommand? sScrollToTopCommand;
	public static ICommand ScrollToTopCommand => sScrollToTopCommand ??= new RelayCommand( ExecuteScrollToTop );

	private static ICommand? sScrollToBottomCommand;
	public static ICommand ScrollToBottomCommand => sScrollToBottomCommand ??= new RelayCommand( ExecuteScrollToBottom );

	private static void ExecuteScrollToTop()
	{
		ExecuteScrollToTop( GetDefaultScrollSource() );
	}

	private static void ExecuteScrollToBottom()
	{
		ExecuteScrollToBottom( GetDefaultScrollSource() );
	}

	private static object? GetDefaultScrollSource() => Keyboard.FocusedElement ?? Mouse.DirectlyOver;

	private static void ExecuteScrollToTop( object? pSource ) => ExtractScrollViewer( pSource )?.ScrollToTop();

	private static void ExecuteScrollToBottom( object? pSource ) => ExtractScrollViewer( pSource )?.ScrollToBottom();

	private static ScrollViewer? ExtractScrollViewer( object? pCandidate )
	{
		if ( pCandidate is ScrollViewer lViewer )
		{
			return lViewer;
		}

		if ( pCandidate is not DependencyObject lDependencyObject )
		{
			return null;
		}

		DependencyObject? lCurrent = lDependencyObject;
		while ( lCurrent is not null and not ScrollViewer )
		{
			lCurrent = VisualTreeHelper.GetParent( lCurrent );
		}

		return lCurrent as ScrollViewer;
	}
}
