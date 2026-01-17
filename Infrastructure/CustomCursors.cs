// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows;
using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public static class CustomCursors
	{

		private static readonly Lazy<Cursor> sDragLeftRightCursor = new( CreateDragLeftRightCursor );
		public static Cursor DragLeftRightCursor => sDragLeftRightCursor.Value;

		private static Cursor CreateDragLeftRightCursor()
		{
			var lResourceInfo = Application.GetResourceStream( new Uri( "pack://application:,,,/Resources/DragCustom.cur", UriKind.Absolute ) );

			if ( lResourceInfo?.Stream is not { } lStream )
			{
				return Cursors.Arrow;
			}

			return new Cursor( lStream );
		}
	}
}
