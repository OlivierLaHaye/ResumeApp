// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ResumeApp.Converters
{
	public abstract class BaseBrushConverter : IValueConverter
	{
		protected static Color? GetColorFromBrush( Brush pBrush )
		{
			return pBrush switch
			{
				SolidColorBrush lSolidBrush => lSolidBrush.Color,
				LinearGradientBrush lGradientBrush => ColorHelper.CalculateAverageColor(
					lGradientBrush.GradientStops.Select( pStop => pStop.Color ).ToList() ),
				DrawingBrush lDrawingBrush => ColorHelper.GetAverageColorFromDrawingBrush( lDrawingBrush ),
				_ => null
			};
		}

		public object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			return pValue is Brush lBrush ? ConvertBrush( lBrush ) : DependencyProperty.UnsetValue;
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
			Color? lColor = GetColorFromBrush( pBrush );
			if ( !lColor.HasValue )
			{
				return false;
			}

			Color lOpaqueColor = Color.FromRgb( lColor.Value.R, lColor.Value.G, lColor.Value.B );
			return ColorHelper.IsWhiteForegroundPreferred( lOpaqueColor );
		}
	}

	[ValueConversion( typeof( int ), typeof( Brush ) )]
	public sealed class PaletteIndexToBrushConverter : IValueConverter
	{
		private static Brush? TryFindBrushOrNull( string pBrushKey )
		{
			if ( string.IsNullOrWhiteSpace( pBrushKey ) )
			{
				return null;
			}

			return Application.Current?.TryFindResource( pBrushKey ) as Brush;
		}

		public object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			int lPaletteIndex = pValue switch
			{
				int lIndex => lIndex,
				string lText when int.TryParse( lText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lParsedIndex ) => lParsedIndex,
				_ => 0
			};

			string[] lAccentBrushKeys = ColorHelper.sAccentBrushKeys;
			if ( lAccentBrushKeys.Length == 0 )
			{
				return TryFindBrushOrNull( "CommonBlueBrush" ) ?? Brushes.Transparent;
			}

			int lNormalizedIndex = lPaletteIndex < 0 ? 0 : lPaletteIndex;
			string lBrushKey = lAccentBrushKeys[ lNormalizedIndex % lAccentBrushKeys.Length ];

			return TryFindBrushOrNull( lBrushKey )
				?? TryFindBrushOrNull( "CommonBlueBrush" )
				?? Brushes.Transparent;
		}

		public object ConvertBack( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			return Binding.DoNothing;
		}
	}
}
