// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows;

namespace ResumeApp.AttachedProperties;

public enum LayoutSizeClass
{
	Compact,
	Regular,
	Wide
}

public static class AdaptiveLayout
{
	public const double RegularMinimumWidth = 1100.0;
	public const double WideMinimumWidth = 1600.0;

	public static readonly DependencyProperty sSizeClassProperty = DependencyProperty.RegisterAttached(
		"SizeClass",
		typeof( LayoutSizeClass ),
		typeof( AdaptiveLayout ),
		new FrameworkPropertyMetadata( LayoutSizeClass.Regular, FrameworkPropertyMetadataOptions.Inherits ) );

	public static LayoutSizeClass GetSizeClass( DependencyObject pElement )
	{
		ArgumentNullException.ThrowIfNull( pElement );
		return ( LayoutSizeClass )pElement.GetValue( sSizeClassProperty );
	}

	public static void SetSizeClass( DependencyObject pElement, LayoutSizeClass pValue )
	{
		ArgumentNullException.ThrowIfNull( pElement );
		pElement.SetValue( sSizeClassProperty, pValue );
	}

	public static LayoutSizeClass GetSizeClassForWidth( double pWidthDip )
	{
		if ( double.IsNaN( pWidthDip ) || pWidthDip < RegularMinimumWidth )
		{
			return LayoutSizeClass.Compact;
		}

		return pWidthDip < WideMinimumWidth ? LayoutSizeClass.Regular : LayoutSizeClass.Wide;
	}
}
