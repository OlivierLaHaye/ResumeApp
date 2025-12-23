// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Helpers;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ResumeApp.Converters
{
	public abstract class BaseBrushConverter : IValueConverter
	{
		protected static Color? GetColorFromBrush( Brush pBrush )
		{
			switch ( pBrush )
			{
				case SolidColorBrush lSolidBrush:
					{
						return lSolidBrush.Color;
					}

				case LinearGradientBrush lGradientBrush:
					{
						return ColorHelper.CalculateAverageColor( lGradientBrush.GradientStops.Select( pStop => pStop.Color ).ToList() );
					}

				case DrawingBrush lDrawingBrush:
					{
						return ColorHelper.GetAverageColorFromDrawingBrush( lDrawingBrush );
					}

				default:
					{
						return null;
					}
			}
		}

		public object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValue is Brush lBrush )
			{
				return ConvertBrush( lBrush );
			}

			return DependencyProperty.UnsetValue;
		}

		public object ConvertBack( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			throw new NotSupportedException();
		}

		protected abstract object ConvertBrush( Brush pBrush );
	}

	[ValueConversion( typeof( Brush ), typeof( bool ) )]
	public class IsDarkBrushConverter : BaseBrushConverter
	{
		protected override object ConvertBrush( Brush pBrush )
		{
			var lColor = GetColorFromBrush( pBrush );
			if ( !lColor.HasValue )
			{
				return false;
			}

			var lOpaqueColor = Color.FromRgb( lColor.Value.R, lColor.Value.G, lColor.Value.B );

			return ColorHelper.IsWhiteForegroundPreferred( lOpaqueColor );
		}
	}

	[ValueConversion( typeof( int ), typeof( Brush ) )]
	public sealed class PaletteIndexToBrushConverter : IValueConverter
	{
		private static readonly string[] sBrushKeys =
		{
			"CommonBlueBrush",
			"CommonGreenBrush",
			"CommonYellowBrush",
			"CommonRedBrush",
			"CommonPurpleBrush",
			"CommonOrangeBrush",
			"CommonCyanBrush",
			"CommonPinkBrush"
		};

		public object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			int lPaletteIndex = 0;

			switch ( pValue )
			{
				case int lIndex:
					{
						lPaletteIndex = lIndex;
						break;
					}
				case string lText when int.TryParse( lText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lParsedIndex ):
					{
						lPaletteIndex = lParsedIndex;
						break;
					}
			}

			if ( sBrushKeys == null || sBrushKeys.Length == 0 )
			{
				return TryFindBrushOrNull( "CommonBlueBrush" ) ?? Brushes.Transparent;
			}

			int lNormalizedIndex = lPaletteIndex < 0 ? 0 : lPaletteIndex;
			string lBrushKey = sBrushKeys[ lNormalizedIndex % sBrushKeys.Length ];

			return TryFindBrushOrNull( lBrushKey )
			       ?? TryFindBrushOrNull( "CommonBlueBrush" )
			       ?? Brushes.Transparent;
		}

		public object ConvertBack( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			return Binding.DoNothing;
		}

		private static Brush TryFindBrushOrNull( string pBrushKey )
		{
			if ( string.IsNullOrWhiteSpace( pBrushKey ) )
			{
				return null;
			}

			return Application.Current?.TryFindResource( pBrushKey ) as Brush;
		}
	}
}
