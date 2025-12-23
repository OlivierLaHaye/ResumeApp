// Copyright (C) Olivier La Haye
// All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ResumeApp.Helpers
{
	public static class ColorHelper
	{
		public class CmykColor
		{
			public double C { get; set; }
			public double M { get; set; }
			public double Y { get; set; }
			public double K { get; set; }

			public CmykColor( double pC, double pM, double pY, double pK )
			{
				C = pC;
				M = pM;
				Y = pY;
				K = pK;
			}
		}

		public class HslColor
		{
			public double Hue { get; set; }

			public double Saturation { get; set; }

			public double Lightness { get; set; }

			public HslColor( double pHue, double pSaturation, double pLightness )
			{
				Hue = pHue;
				Saturation = pSaturation;
				Lightness = pLightness;
			}
		}

		public class LabColor
		{
			public double L { get; set; }
			public double A { get; set; }
			public double B { get; set; }

			public LabColor( double pLightness, double pGreenRed, double pBlueYellow )
			{
				L = pLightness;
				A = pGreenRed;
				B = pBlueYellow;
			}
		}

		private static readonly Dictionary<string, object> sPendingResourceUpdates = new Dictionary<string, object>();

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
			using ( var lContext = lVisual.RenderOpen() )
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

			if ( lLstar >= 75.0 )
			{
				return false;
			}

			if ( lLstar <= 45.0 )
			{
				return true;
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

			if ( lLightness >= 0.55 && lSaturation >= 0.25 )
			{
				return false;
			}

			if ( lLightness <= 0.45 )
			{
				return true;
			}

			return lContrastWithWhite >= lContrastWithBlack;
		}

		public static LabColor LabFromColor( Color pColor )
		{
			(double lXChannel, double lYChannel, double lZChannel) = RgbToXyz( pColor.R, pColor.G, pColor.B );

			const double lInverse100 = 0.01;
			const double lInverse95047 = 1.0 / 95.047;
			const double lInverse108883 = 1.0 / 108.883;

			double lFxyzX = Fxyz( lXChannel * lInverse95047 );
			double lFxyzY = Fxyz( lYChannel * lInverse100 );
			double lFxyzZ = Fxyz( lZChannel * lInverse108883 );

			double lLightness = 116 * lFxyzY - 16;
			double lGreenRed = 500 * ( lFxyzX - lFxyzY );
			double lBlueYellow = 200 * ( lFxyzY - lFxyzZ );

			return new LabColor( lLightness, lGreenRed, lBlueYellow );
		}

		public static Color ColorFromLab( LabColor pLabColor )
		{
			double lFxyzY = ( pLabColor.L + 16 ) / 116;
			double lFxyzX = pLabColor.A / 500 + lFxyzY;
			double lFxyzZ = lFxyzY - pLabColor.B / 200;

			double lXChannel = 95.047 * InverseFxyz( lFxyzX );
			double lYChannel = 100.000 * InverseFxyz( lFxyzY );
			double lZChannel = 108.883 * InverseFxyz( lFxyzZ );

			return XyzToRgb( lXChannel, lYChannel, lZChannel );
		}

		public static CmykColor CmykFromColor( Color pColor )
		{
			double lRed = pColor.R / 255.0;
			double lGreen = pColor.G / 255.0;
			double lBlue = pColor.B / 255.0;

			double lK = 1.0 - Math.Max( lRed, Math.Max( lGreen, lBlue ) );
			if ( Math.Abs( lK - 1.0 ) < 1e-10 )
			{
				return new CmykColor( 0.0, 0.0, 0.0, 1.0 );
			}

			double lC = ( 1.0 - lRed - lK ) / ( 1.0 - lK );
			double lM = ( 1.0 - lGreen - lK ) / ( 1.0 - lK );
			double lY = ( 1.0 - lBlue - lK ) / ( 1.0 - lK );

			if ( double.IsNaN( lC ) )
			{
				lC = 0.0;
			}

			if ( double.IsNaN( lM ) )
			{
				lM = 0.0;
			}

			if ( double.IsNaN( lY ) )
			{
				lY = 0.0;
			}

			return new CmykColor( lC, lM, lY, lK );
		}

		public static byte[] ApplyGradientLuminanceAdjustmentLab(
			byte[] pPixels, int pImageWidth, int pImageHeight, int pStride,
			SortedDictionary<double, double> pControlPoints,
			Point pStartPoint, Point pEndPoint )
		{
			double lDeltaX = pEndPoint.X - pStartPoint.X;
			double lDeltaY = pEndPoint.Y - pStartPoint.Y;
			double lLen2 = lDeltaX * lDeltaX + lDeltaY * lDeltaY;
			if ( lLen2 < 1e-12 )
			{
				lDeltaX = 0.0;
				lDeltaY = 0.0;
				lLen2 = 1.0;
			}

			double lInvLen2 = 1.0 / lLen2;

			double[] lColumnTerms = new double[ pImageWidth ];
			for ( int lColumnIndex = 0; lColumnIndex < pImageWidth; lColumnIndex++ )
			{
				double lU = ( double )lColumnIndex / pImageWidth;
				lColumnTerms[ lColumnIndex ] = ( lU - pStartPoint.X ) * lDeltaX * lInvLen2;
			}

			double[] lRowTerms = new double[ pImageHeight ];
			for ( int lRowIndex = 0; lRowIndex < pImageHeight; lRowIndex++ )
			{
				double lV = ( double )lRowIndex / pImageHeight;
				lRowTerms[ lRowIndex ] = ( lV - pStartPoint.Y ) * lDeltaY * lInvLen2;
			}

			double[] lLut = BuildMultiplierLut( pControlPoints, 1024 );

			try
			{
				Parallel.For( 0, pImageHeight, pRow =>
				{
					int lRowBaseIndex = pRow * pStride;
					double lRowTerm = lRowTerms[ pRow ];

					for ( int lColumn = 0; lColumn < pImageWidth; lColumn++ )
					{
						int lPixelIndex = lRowBaseIndex + lColumn * 4;
						if ( pPixels[ lPixelIndex + 3 ] == 0 )
						{
							continue;
						}

						double lT = lColumnTerms[ lColumn ] + lRowTerm;
						if ( lT < 0.0 )
						{
							lT = 0.0;
						}
						else if ( lT > 1.0 )
						{
							lT = 1.0;
						}

						int lLutIndex = ( int )Math.Round( lT * ( lLut.Length - 1 ) );
						if ( lLutIndex < 0 )
						{
							lLutIndex = 0;
						}
						else if ( lLutIndex >= lLut.Length )
						{
							lLutIndex = lLut.Length - 1;
						}

						double lMultiplier = lLut[ lLutIndex ];
						ApplyLuminanceToPixelLabFast( pPixels, lPixelIndex, lMultiplier );
					}
				} );
			}
			catch ( Exception )
			{
				// ignored
			}

			return pPixels;
		}

		public static BitmapSource ConvertPixelsToBitmapSource( byte[] pPixels, int pWidth, int pHeight )
		{
			if ( pPixels == null || pPixels.Length == 0 || pWidth <= 0 || pHeight <= 0 )
			{
				return null;
			}

			int lStride = pWidth * 4;
			var lBitmap = new WriteableBitmap( pWidth, pHeight, 96, 96, PixelFormats.Bgra32, null );
			lBitmap.WritePixels( new Int32Rect( 0, 0, pWidth, pHeight ), pPixels, lStride, 0 );
			lBitmap.Freeze();
			return lBitmap;
		}

		public static byte[] GetPixelsFromBitmapImage( BitmapSource pBitmapImage )
		{
			if ( pBitmapImage == null )
			{
				return Array.Empty<byte>();
			}

			int lStride = pBitmapImage.PixelWidth * 4;
			byte[] lPixels = new byte[ lStride * pBitmapImage.PixelHeight ];
			pBitmapImage.CopyPixels( lPixels, lStride, 0 );
			return lPixels;
		}

		public static BitmapImage ConvertBitmapSourceToBitmapImage( BitmapSource pSource )
		{
			if ( pSource == null )
			{
				return null;
			}

			var lBitmapImage = new BitmapImage();

			using ( var lStream = new MemoryStream() )
			{
				var lEncoder = new PngBitmapEncoder();
				lEncoder.Frames.Add( BitmapFrame.Create( pSource ) );
				lEncoder.Save( lStream );
				lStream.Position = 0;

				lBitmapImage.BeginInit();
				lBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				lBitmapImage.StreamSource = lStream;
				lBitmapImage.EndInit();
				lBitmapImage.Freeze();
			}

			return lBitmapImage;
		}

		public static Color ColorFromHex( string pHex )
		{
			if ( string.IsNullOrEmpty( pHex ) )
			{
				return Colors.Transparent;
			}

			pHex = pHex.TrimStart( '#' );

			switch ( pHex.Length )
			{
				case 3:
					{
						return Color.FromRgb(
							Convert.ToByte( new string( pHex[ 0 ], 2 ), 16 ),
							Convert.ToByte( new string( pHex[ 1 ], 2 ), 16 ),
							Convert.ToByte( new string( pHex[ 2 ], 2 ), 16 )
						);
					}
				case 6:
					{
						return Color.FromRgb(
							Convert.ToByte( pHex.Substring( 0, 2 ), 16 ),
							Convert.ToByte( pHex.Substring( 2, 2 ), 16 ),
							Convert.ToByte( pHex.Substring( 4, 2 ), 16 )
						);
					}
				case 8:
					{
						return Color.FromArgb(
							Convert.ToByte( pHex.Substring( 0, 2 ), 16 ),
							Convert.ToByte( pHex.Substring( 2, 2 ), 16 ),
							Convert.ToByte( pHex.Substring( 4, 2 ), 16 ),
							Convert.ToByte( pHex.Substring( 6, 2 ), 16 )
						);
					}
				default:
					{
						return Colors.Transparent;
					}
			}
		}

		public static HslColor HslFromColor( Color pColor )
		{
			double lNormalizedRed = pColor.R / 255.0;
			double lNormalizedGreen = pColor.G / 255.0;
			double lNormalizedBlue = pColor.B / 255.0;

			double lMinChannel = Math.Min( Math.Min( lNormalizedRed, lNormalizedGreen ), lNormalizedBlue );
			double lMaxChannel = Math.Max( Math.Max( lNormalizedRed, lNormalizedGreen ), lNormalizedBlue );
			double lChroma = lMaxChannel - lMinChannel;

			double lHue = 0.0;
			double lSaturation = 0.0;
			double lLightness = ( lMaxChannel + lMinChannel ) / 2.0;

			const double lEpsilon = 1e-10;

			if ( lChroma < lEpsilon )
			{
				return new HslColor( lHue, lSaturation, lLightness );
			}

			lSaturation = lLightness < 0.5 ? lChroma / ( lMaxChannel + lMinChannel ) : lChroma / ( 2.0 - lMaxChannel - lMinChannel );

			if ( Math.Abs( lNormalizedRed - lMaxChannel ) < lEpsilon )
			{
				lHue = ( lNormalizedGreen - lNormalizedBlue ) / lChroma;
			}
			else if ( Math.Abs( lNormalizedGreen - lMaxChannel ) < lEpsilon )
			{
				lHue = 2.0 + ( lNormalizedBlue - lNormalizedRed ) / lChroma;
			}
			else if ( Math.Abs( lNormalizedBlue - lMaxChannel ) < lEpsilon )
			{
				lHue = 4.0 + ( lNormalizedRed - lNormalizedGreen ) / lChroma;
			}

			lHue *= 60.0;
			if ( lHue < 0.0 )
			{
				lHue += 360.0;
			}

			return new HslColor( lHue, lSaturation, lLightness );
		}

		internal static Color MultiplyLightnessFromColor( Color pColor, double pFactor )
		{
			var lLabColor = LabFromColor( pColor );
			lLabColor.L = Math.Min( 100, lLabColor.L * pFactor );
			var lColorFromLab = ColorFromLab( lLabColor );
			return lColorFromLab;
		}

		internal static Color ColorFromHsl( HslColor pHslColor )
		{
			if ( !IsBetweenMaxExclude( pHslColor.Hue, 0.0, 360.0 )
				 || !IsBetweenInclusive( pHslColor.Saturation, 0.0, 1.0 )
				 || !IsBetweenInclusive( pHslColor.Lightness, 0.0, 1.0 ) )
			{
				return Colors.Transparent;
			}

			double lChroma = ( 1 - Math.Abs( 2.0 * pHslColor.Lightness - 1 ) ) * pHslColor.Saturation;
			double lSecondaryComponent = lChroma * ( 1 - Math.Abs( pHslColor.Hue / 60.0 % 2.0 - 1 ) );
			double lMatchLightness = pHslColor.Lightness - lChroma / 2.0;

			var (lTempRed, lTempGreen, lTempBlue) = GetTemporaryRgb( pHslColor.Hue, lChroma, lSecondaryComponent );

			double lScaledRed = ( lTempRed + lMatchLightness ) * 255.0;
			double lScaledGreen = ( lTempGreen + lMatchLightness ) * 255.0;
			double lScaledBlue = ( lTempBlue + lMatchLightness ) * 255.0;

			byte lRed = ( byte )Math.Min( 255, Math.Max( 0, Math.Round( lScaledRed ) ) );
			byte lGreen = ( byte )Math.Min( 255, Math.Max( 0, Math.Round( lScaledGreen ) ) );
			byte lBlue = ( byte )Math.Min( 255, Math.Max( 0, Math.Round( lScaledBlue ) ) );

			return Color.FromRgb( lRed, lGreen, lBlue );
		}

		internal static Color ColorFromCmyk( CmykColor pCmykColor )
		{
			double lC = pCmykColor.C;
			double lM = pCmykColor.M;
			double lY = pCmykColor.Y;
			double lK = pCmykColor.K;

			byte lRed = ( byte )Math.Round( 255.0 * ( 1.0 - lC ) * ( 1.0 - lK ) );
			byte lGreen = ( byte )Math.Round( 255.0 * ( 1.0 - lM ) * ( 1.0 - lK ) );
			byte lBlue = ( byte )Math.Round( 255.0 * ( 1.0 - lY ) * ( 1.0 - lK ) );

			return Color.FromRgb( lRed, lGreen, lBlue );
		}

		private static double[] BuildMultiplierLut( SortedDictionary<double, double> pControlPoints, int pSize )
		{
			int lSize = Math.Max( 2, pSize );
			double[] lLut = new double[ lSize ];
			for ( int lIndex = 0; lIndex < lSize; lIndex++ )
			{
				double lT = ( double )lIndex / ( lSize - 1 );
				lLut[ lIndex ] = InterpolateMultiplier( pControlPoints, lT );
			}

			return lLut;
		}

		private static void ApplyLuminanceToPixelLabFast( IList<byte> pPixels, int pPixelIndex, double pLuminanceMultiplier )
		{
			byte lBlue = pPixels[ pPixelIndex ];
			byte lGreen = pPixels[ pPixelIndex + 1 ];
			byte lRed = pPixels[ pPixelIndex + 2 ];

			double lR = lRed / 255.0;
			double lG = lGreen / 255.0;
			double lB = lBlue / 255.0;

			lR = lR <= 0.04045 ? lR / 12.92 : Math.Pow( ( lR + 0.055 ) / 1.055, 2.4 );
			lG = lG <= 0.04045 ? lG / 12.92 : Math.Pow( ( lG + 0.055 ) / 1.055, 2.4 );
			lB = lB <= 0.04045 ? lB / 12.92 : Math.Pow( ( lB + 0.055 ) / 1.055, 2.4 );

			double lX = ( lR * 0.4124 + lG * 0.3576 + lB * 0.1805 ) * 100.0;
			double lY = ( lR * 0.2126 + lG * 0.7152 + lB * 0.0722 ) * 100.0;
			double lZ = ( lR * 0.0193 + lG * 0.1192 + lB * 0.9505 ) * 100.0;

			const double lInverse95047 = 1.0 / 95.047;
			const double lInverse100 = 0.01;
			const double lInverse108883 = 1.0 / 108.883;

			double lFx = Fxyz( lX * lInverse95047 );
			double lFy = Fxyz( lY * lInverse100 );
			double lFz = Fxyz( lZ * lInverse108883 );

			double lL = 116.0 * lFy - 16.0;
			double lA = 500.0 * ( lFx - lFy );
			double lBb = 200.0 * ( lFy - lFz );

			lL *= pLuminanceMultiplier;
			if ( lL < 0.0 )
			{
				lL = 0.0;
			}
			else if ( lL > 100.0 )
			{
				lL = 100.0;
			}

			double lFy2 = ( lL + 16.0 ) / 116.0;
			double lFx2 = lA / 500.0 + lFy2;
			double lFz2 = lFy2 - lBb / 200.0;

			double lX2 = 95.047 * InverseFxyz( lFx2 );
			double lY2 = 100.0 * InverseFxyz( lFy2 );
			double lZ2 = 108.883 * InverseFxyz( lFz2 );

			lX2 /= 100.0;
			lY2 /= 100.0;
			lZ2 /= 100.0;

			double lRl = lX2 * 3.2406 + lY2 * -1.5372 + lZ2 * -0.4986;
			double lGl = lX2 * -0.9689 + lY2 * 1.8758 + lZ2 * 0.0415;
			double lBl = lX2 * 0.0557 + lY2 * -0.2040 + lZ2 * 1.0570;

			double lRs = lRl > 0.0031308 ? 1.055 * Math.Pow( lRl, 1.0 / 2.4 ) - 0.055 : 12.92 * lRl;
			double lGs = lGl > 0.0031308 ? 1.055 * Math.Pow( lGl, 1.0 / 2.4 ) - 0.055 : 12.92 * lGl;
			double lBs = lBl > 0.0031308 ? 1.055 * Math.Pow( lBl, 1.0 / 2.4 ) - 0.055 : 12.92 * lBl;

			byte lRByte = ( byte )Math.Min( 255, Math.Max( 0, Math.Round( lRs * 255.0 ) ) );
			byte lGByte = ( byte )Math.Min( 255, Math.Max( 0, Math.Round( lGs * 255.0 ) ) );
			byte lBByte = ( byte )Math.Min( 255, Math.Max( 0, Math.Round( lBs * 255.0 ) ) );

			pPixels[ pPixelIndex ] = lBByte;
			pPixels[ pPixelIndex + 1 ] = lGByte;
			pPixels[ pPixelIndex + 2 ] = lRByte;
		}

		private static void ApplyPendingResourceUpdates()
		{
			Application.Current.Resources.BeginInit();
			foreach ( var lPair in sPendingResourceUpdates )
			{
				Application.Current.Resources[ lPair.Key ] = lPair.Value;
			}

			Application.Current.Resources.EndInit();
			sPendingResourceUpdates.Clear();
		}

		private static double InterpolateMultiplier( SortedDictionary<double, double> pControlPoints, double pDistance )
		{
			double lPreviousKey = 0;
			double lPreviousValue = 1;
			foreach ( var lControlPoint in pControlPoints )
			{
				if ( pDistance < lControlPoint.Key )
				{
					double lRange = lControlPoint.Key - lPreviousKey;
					double lPosition = ( pDistance - lPreviousKey ) / lRange;
					return lPreviousValue + lPosition * ( lControlPoint.Value - lPreviousValue );
				}

				lPreviousKey = lControlPoint.Key;
				lPreviousValue = lControlPoint.Value;
			}

			return lPreviousValue;
		}

		private static double Fxyz( double pLinearValue )
		{
			const double lThreshold = 6.0 / 29.0;
			const double lThresholdCubed = lThreshold * lThreshold * lThreshold;

			return pLinearValue > lThresholdCubed
				? Math.Pow( pLinearValue, 1.0 / 3.0 )
				: pLinearValue / ( 3 * lThreshold * lThreshold ) + 4.0 / 29.0;
		}

		private static double InverseFxyz( double pNonLinearValue )
		{
			const double lThreshold = 6.0 / 29.0;

			return pNonLinearValue > lThreshold
				? pNonLinearValue * pNonLinearValue * pNonLinearValue
				: 3 * lThreshold * lThreshold * ( pNonLinearValue - 4.0 / 29.0 );
		}

		private static Color MultiplyLightnessFromColorLab( Color pColor, double pFactor )
		{
			var lLabColor = LabFromColor( pColor );
			lLabColor.L = Math.Min( 100, lLabColor.L * pFactor );
			lLabColor.L = Math.Max( 0, lLabColor.L );
			var lAdjustedColor = ColorFromLab( lLabColor );
			return lAdjustedColor;
		}

		private static void ApplyLuminanceToPixelLab( IList<byte> pPixels, int pPixelIndex, double pLuminanceMultiplier )
		{
			var lOriginalColor = Color.FromArgb(
				pPixels[ pPixelIndex + 3 ],
				pPixels[ pPixelIndex + 2 ],
				pPixels[ pPixelIndex + 1 ],
				pPixels[ pPixelIndex ] );

			var lAdjustedColor = MultiplyLightnessFromColorLab( lOriginalColor, pLuminanceMultiplier );

			pPixels[ pPixelIndex ] = lAdjustedColor.B;
			pPixels[ pPixelIndex + 1 ] = lAdjustedColor.G;
			pPixels[ pPixelIndex + 2 ] = lAdjustedColor.R;
		}

		private static (double lXChannel, double lYChannel, double lZChannel) RgbToXyz( double pRed, double pGreen, double pBlue )
		{
			pRed = pRed / 255.0 > 0.04045 ? Math.Pow( ( pRed / 255.0 + 0.055 ) / 1.055, 2.4 ) : pRed / 255.0 / 12.92;
			pGreen = pGreen / 255.0 > 0.04045 ? Math.Pow( ( pGreen / 255.0 + 0.055 ) / 1.055, 2.4 ) : pGreen / 255.0 / 12.92;
			pBlue = pBlue / 255.0 > 0.04045 ? Math.Pow( ( pBlue / 255.0 + 0.055 ) / 1.055, 2.4 ) : pBlue / 255.0 / 12.92;

			double lXChannel = ( pRed * 0.4124 + pGreen * 0.3576 + pBlue * 0.1805 ) * 100.0;
			double lYChannel = ( pRed * 0.2126 + pGreen * 0.7152 + pBlue * 0.0722 ) * 100.0;
			double lZChannel = ( pRed * 0.0193 + pGreen * 0.1192 + pBlue * 0.9505 ) * 100.0;

			return (lXChannel, lYChannel, lZChannel);
		}

		private static Color XyzToRgb( double pXChannel, double pYChannel, double pZChannel )
		{
			pXChannel /= 100.0;
			pYChannel /= 100.0;
			pZChannel /= 100.0;

			double lLinearRed = pXChannel * 3.2406 + pYChannel * -1.5372 + pZChannel * -0.4986;
			double lLinearGreen = pXChannel * -0.9689 + pYChannel * 1.8758 + pZChannel * 0.0415;
			double lLinearBlue = pXChannel * 0.0557 + pYChannel * -0.2040 + pZChannel * 1.0570;

			lLinearRed = lLinearRed > 0.0031308 ? 1.055 * Math.Pow( lLinearRed, 1 / 2.4 ) - 0.055 : 12.92 * lLinearRed;
			lLinearGreen = lLinearGreen > 0.0031308 ? 1.055 * Math.Pow( lLinearGreen, 1 / 2.4 ) - 0.055 : 12.92 * lLinearGreen;
			lLinearBlue = lLinearBlue > 0.0031308 ? 1.055 * Math.Pow( lLinearBlue, 1 / 2.4 ) - 0.055 : 12.92 * lLinearBlue;

			return Color.FromRgb(
				( byte )( Math.Max( 0, Math.Min( 1, lLinearRed ) ) * 255 ),
				( byte )( Math.Max( 0, Math.Min( 1, lLinearGreen ) ) * 255 ),
				( byte )( Math.Max( 0, Math.Min( 1, lLinearBlue ) ) * 255 )
			);
		}

		private static (double lRedComponent, double lGreenComponent, double lBlueComponent) GetTemporaryRgb(
			double pHue, double pChroma, double pSecondaryComponent )
		{
			double lRedComponent;
			double lGreenComponent;
			double lBlueComponent;

			if ( IsBetweenMaxExclude( pHue, 0.0, 60.0 ) )
			{
				lRedComponent = pChroma;
				lGreenComponent = pSecondaryComponent;
				lBlueComponent = 0.0;
			}
			else if ( IsBetweenMaxExclude( pHue, 60.0, 120.0 ) )
			{
				lRedComponent = pSecondaryComponent;
				lGreenComponent = pChroma;
				lBlueComponent = 0.0;
			}
			else if ( IsBetweenMaxExclude( pHue, 120.0, 180.0 ) )
			{
				lRedComponent = 0.0;
				lGreenComponent = pChroma;
				lBlueComponent = pSecondaryComponent;
			}
			else if ( IsBetweenMaxExclude( pHue, 180.0, 240.0 ) )
			{
				lRedComponent = 0.0;
				lGreenComponent = pSecondaryComponent;
				lBlueComponent = pChroma;
			}
			else if ( IsBetweenMaxExclude( pHue, 240.0, 300.0 ) )
			{
				lRedComponent = pSecondaryComponent;
				lGreenComponent = 0.0;
				lBlueComponent = pChroma;
			}
			else if ( IsBetweenMaxExclude( pHue, 300.0, 360.0 ) )
			{
				lRedComponent = pChroma;
				lGreenComponent = 0.0;
				lBlueComponent = pSecondaryComponent;
			}
			else
			{
				return (0, 0, 0);
			}

			return (lRedComponent, lGreenComponent, lBlueComponent);
		}

		private static bool IsBetweenMaxExclude( double pValue, double pMin, double pMax )
		{
			return pMin <= pValue && pValue < pMax;
		}

		private static bool IsBetweenInclusive( double pValue, double pMin, double pMax )
		{
			return pMin <= pValue && pValue <= pMax;
		}
	}
}
