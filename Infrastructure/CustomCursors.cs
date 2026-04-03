// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	[ExcludeFromCodeCoverage( Justification = "Loads cursors from pack:// URIs via Application.GetResourceStream requiring a running WPF Application with compiled BAML resources." )]
	public static class CustomCursors
	{

		private static readonly Lazy<Cursor> sDragLeftRightCursor = new( CreateDragLeftRightCursor );
		public static Cursor DragLeftRightCursor => sDragLeftRightCursor.Value;

		private static readonly Lazy<Cursor> sDraggingCursor = new( CreateDraggingCursor );
		public static Cursor DraggingCursor => sDraggingCursor.Value;

		private static Cursor CreateDragLeftRightCursor() => CreateCursorFromPackUri( "pack://application:,,,/Resources/DragCustom.cur" );

		private static Cursor CreateDraggingCursor() => CreateCursorFromPackUri( "pack://application:,,,/Resources/DraggingCustom.cur" );

		private static Cursor CreateCursorFromPackUri( string pPackUri )
		{
			if ( string.IsNullOrWhiteSpace( pPackUri ) )
			{
				return Cursors.Arrow;
			}

			var lResourceInfo = Application.GetResourceStream( new Uri( pPackUri, UriKind.Absolute ) );

			if ( lResourceInfo?.Stream is not { } lStream )
			{
				return Cursors.Arrow;
			}

			return new Cursor( lStream );
		}
	}
}
