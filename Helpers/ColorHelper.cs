// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ResumeApp.Helpers
{
	public static class ColorHelper
	{
		public static readonly string[] sAccentBrushKeys =
		[
			"CommonBlueStrongBrush",
			"CommonGreenStrongBrush",
			"CommonYellowStrongBrush",
			"CommonRedStrongBrush",
			"CommonPurpleStrongBrush",
			"CommonOrangeStrongBrush",
			"CommonCyanStrongBrush",
			"CommonPinkStrongBrush"
		];

		public static Color CalculateAverageColor( List<Color> pColors )
		{
			return Color.FromArgb(
				( byte )pColors.Average( pColor => pColor.A ),
				( byte )pColors.Average( pColor => pColor.R ),
				( byte )pColors.Average( pColor => pColor.G ),
				( byte )pColors.Average( pColor => pColor.B )
			);
		}

		public static Color GetAverageColorFromDrawingBrush( DrawingBrush pBrush )
		{
			const int lWidth = 10;
			const int lHeight = 10;
			const double lDpiX = 96.0;
			const double lDpiY = 96.0;
			var lPixelFormat = PixelFormats.Pbgra32;
			var lRenderTarget = new RenderTargetBitmap( lWidth, lHeight, lDpiX, lDpiY, lPixelFormat );

			var lVisual = new DrawingVisual();
			using ( DrawingContext lContext = lVisual.RenderOpen() )
			{
				lContext.DrawRectangle( pBrush, null, new Rect( 0, 0, lWidth, lHeight ) );
			}

			lRenderTarget.Render( lVisual );

			byte[] lPixels = new byte[ lWidth * lHeight * 4 ];
			lRenderTarget.CopyPixels( lPixels, lWidth * 4, 0 );

			long lAlpha = 0;
			long lRed = 0;
			long lGreen = 0;
			long lBlue = 0;

			for ( int lIndex = 0; lIndex < lPixels.Length; lIndex += 4 )
			{
				lBlue += lPixels[ lIndex ];
				lGreen += lPixels[ lIndex + 1 ];
				lRed += lPixels[ lIndex + 2 ];
				lAlpha += lPixels[ lIndex + 3 ];
			}

			const int lPixelCount = lWidth * lHeight;
			byte lAvgA = ( byte )( lAlpha / lPixelCount );
			byte lAvgR = ( byte )( lRed / lPixelCount );
			byte lAvgG = ( byte )( lGreen / lPixelCount );
			byte lAvgB = ( byte )( lBlue / lPixelCount );

			return Color.FromArgb( lAvgA, lAvgR, lAvgG, lAvgB );
		}

		public static bool IsWhiteForegroundPreferred( Color pColor )
		{
			double lRed = pColor.R / 255.0;
			double lGreen = pColor.G / 255.0;
			double lBlue = pColor.B / 255.0;

			lRed = lRed <= 0.04045 ? lRed / 12.92 : Math.Pow( ( lRed + 0.055 ) / 1.055, 2.4 );
			lGreen = lGreen <= 0.04045 ? lGreen / 12.92 : Math.Pow( ( lGreen + 0.055 ) / 1.055, 2.4 );
			lBlue = lBlue <= 0.04045 ? lBlue / 12.92 : Math.Pow( ( lBlue + 0.055 ) / 1.055, 2.4 );

			double lLuminance = 0.2126 * lRed + 0.7152 * lGreen + 0.0722 * lBlue;

			double lContrastWithWhite = ( 1.0 + 0.05 ) / ( lLuminance + 0.05 );
			double lContrastWithBlack = ( lLuminance + 0.05 ) / 0.05;

			if ( lContrastWithWhite >= 4.5 && lContrastWithBlack < 4.5 )
			{
				return true;
			}

			if ( lContrastWithBlack >= 4.5 && lContrastWithWhite < 4.5 )
			{
				return false;
			}

			double lY16 = lLuminance > 0.008856 ? Math.Pow( lLuminance, 1.0 / 3.0 ) : 7.787 * lLuminance + 16.0 / 116.0;
			double lLstar = 116.0 * lY16 - 16.0;

			switch ( lLstar )
			{
				case >= 75.0:
					{
						return false;
					}
				case <= 45.0:
					{
						return true;
					}
			}

			double lMax = Math.Max( pColor.R / 255.0, Math.Max( pColor.G / 255.0, pColor.B / 255.0 ) );
			double lMin = Math.Min( pColor.R / 255.0, Math.Min( pColor.G / 255.0, pColor.B / 255.0 ) );
			double lLightness = ( lMax + lMin ) * 0.5;
			double lDelta = lMax - lMin;
			double lSaturation = lDelta == 0.0 ? 0.0 : lLightness > 0.5 ? lDelta / ( 2.0 - lMax - lMin ) : lDelta / ( lMax + lMin );

			double lDiff = Math.Abs( lContrastWithWhite - lContrastWithBlack );
			if ( !( lDiff <= 0.5 ) )
			{
				return lContrastWithWhite >= lContrastWithBlack;
			}

			return lLightness switch
			{
				>= 0.55 when lSaturation >= 0.25 => false,
				<= 0.45 => true,
				_ => lContrastWithWhite >= lContrastWithBlack
			};
		}
	}
}
