// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows;

namespace ResumeApp.AttachedProperties
{
	public static class CornerRadiusAssist
	{
		public static readonly DependencyProperty sCornerRadiusProperty =
			DependencyProperty.RegisterAttached(
				"sCornerRadius",
				typeof( CornerRadius ),
				typeof( CornerRadiusAssist ),
				new FrameworkPropertyMetadata( default( CornerRadius ), FrameworkPropertyMetadataOptions.Inherits ) );

		public static void SetsCornerRadius( DependencyObject pTarget, CornerRadius pCornerRadius )
		{
			pTarget.SetValue( sCornerRadiusProperty, pCornerRadius );
		}

		public static CornerRadius GetsCornerRadius( DependencyObject pTarget )
		{
			return ( CornerRadius )pTarget.GetValue( sCornerRadiusProperty );
		}
	}
}
