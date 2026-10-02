// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ResumeApp.Services
{
	public static class ImageDecodeService
	{
		public const int MinimumPreviewLongEdgePixels = 1280;
		public const int MaximumPreviewLongEdgePixels = 3840;
		public const double PreviewScreenFraction = 0.75;

		private const int ScreenWidthMetricIndex = 0;
		private const int ScreenHeightMetricIndex = 1;

		private static readonly Lazy<int> sPreviewLongEdgePixels = new( ComputePreviewLongEdgePixelsFromPrimaryScreen );

		public static int PreviewLongEdgePixels => sPreviewLongEdgePixels.Value;

		public static int ComputePreviewLongEdgePixels( int pScreenWidthPixels, int pScreenHeightPixels )
		{
			int lScreenLongEdgePixels = Math.Max( pScreenWidthPixels, pScreenHeightPixels );

			if ( lScreenLongEdgePixels <= 0 )
			{
				return MinimumPreviewLongEdgePixels;
			}

			double lPreviewLongEdgePixels = Math.Round( lScreenLongEdgePixels * PreviewScreenFraction );
			return ( int )Math.Clamp( lPreviewLongEdgePixels, MinimumPreviewLongEdgePixels, MaximumPreviewLongEdgePixels );
		}

		public static ImageDecodeSize ComputeDecodeSize( int pPixelWidth, int pPixelHeight, int pLongEdgeLimitPixels )
		{
			if ( pPixelWidth <= 0 || pPixelHeight <= 0 || pLongEdgeLimitPixels <= 0 )
			{
				return ImageDecodeSize.Native;
			}

			int lLongEdgePixels = Math.Max( pPixelWidth, pPixelHeight );

			if ( lLongEdgePixels <= pLongEdgeLimitPixels )
			{
				return ImageDecodeSize.Native;
			}

			double lScale = ( double )pLongEdgeLimitPixels / lLongEdgePixels;

			return pPixelWidth >= pPixelHeight
				? new ImageDecodeSize( pLongEdgeLimitPixels, 0 )
				: new ImageDecodeSize( 0, Math.Max( 1, ( int )Math.Round( pPixelHeight * lScale ) ) );
		}

		[ExcludeFromCodeCoverage( Justification = "Decodes pack or file URIs through WIC; requires compiled application resources." )]
		public static BitmapSource? TryCreatePreviewImage( Uri? pUri )
		{
			if ( pUri == null )
			{
				return null;
			}

			try
			{
				ImageDecodeSize lDecodeSize = TryReadPixelSize( pUri, out int lPixelWidth, out int lPixelHeight )
					? ComputeDecodeSize( lPixelWidth, lPixelHeight, PreviewLongEdgePixels )
					: new ImageDecodeSize( PreviewLongEdgePixels, 0 );

				return CreateFrozenBitmapImage( pUri, lDecodeSize );
			}
			catch ( Exception )
			{
				return null;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Decodes pack or file URIs through WIC; requires compiled application resources." )]
		public static BitmapSource? TryCreateFullResolutionImage( ImageSource? pImageSource )
		{
			if ( pImageSource is not BitmapImage { UriSource: Uri lUriSource } lBitmapImage )
			{
				return pImageSource as BitmapSource;
			}

			if ( lBitmapImage.DecodePixelWidth == 0 && lBitmapImage.DecodePixelHeight == 0 )
			{
				return lBitmapImage;
			}

			try
			{
				return CreateFrozenBitmapImage( lUriSource, ImageDecodeSize.Native );
			}
			catch ( Exception )
			{
				return lBitmapImage;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Reads image headers through WIC." )]
		private static bool TryReadPixelSize( Uri pUri, out int pPixelWidth, out int pPixelHeight )
		{
			pPixelWidth = 0;
			pPixelHeight = 0;

			try
			{
				BitmapDecoder lDecoder = BitmapDecoder.Create( pUri, BitmapCreateOptions.DelayCreation | BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None );

				if ( lDecoder.Frames.Count == 0 )
				{
					return false;
				}

				BitmapFrame lFrame = lDecoder.Frames[ 0 ];
				pPixelWidth = lFrame.PixelWidth;
				pPixelHeight = lFrame.PixelHeight;
				return pPixelWidth > 0 && pPixelHeight > 0;
			}
			catch ( Exception )
			{
				return false;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Creates BitmapImage through WIC." )]
		private static BitmapImage CreateFrozenBitmapImage( Uri pUri, ImageDecodeSize pDecodeSize )
		{
			var lBitmapImage = new BitmapImage();
			lBitmapImage.BeginInit();
			lBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			lBitmapImage.UriSource = pUri;

			if ( pDecodeSize.Width > 0 )
			{
				lBitmapImage.DecodePixelWidth = pDecodeSize.Width;
			}
			else if ( pDecodeSize.Height > 0 )
			{
				lBitmapImage.DecodePixelHeight = pDecodeSize.Height;
			}

			lBitmapImage.EndInit();
			lBitmapImage.Freeze();
			return lBitmapImage;
		}

		[ExcludeFromCodeCoverage( Justification = "Reads primary screen metrics through user32." )]
		private static int ComputePreviewLongEdgePixelsFromPrimaryScreen()
		{
			try
			{
				return ComputePreviewLongEdgePixels( GetSystemMetrics( ScreenWidthMetricIndex ), GetSystemMetrics( ScreenHeightMetricIndex ) );
			}
			catch ( Exception )
			{
				return MinimumPreviewLongEdgePixels;
			}
		}

		[DllImport( "user32.dll" )]
		private static extern int GetSystemMetrics( int pIndex );
	}

	public readonly record struct ImageDecodeSize( int Width, int Height )
	{
		public static ImageDecodeSize Native => new( 0, 0 );

		public bool IsNative => Width <= 0 && Height <= 0;
	}
}
