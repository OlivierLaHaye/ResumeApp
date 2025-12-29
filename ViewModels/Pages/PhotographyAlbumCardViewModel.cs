// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class PhotographyAlbumCardViewModel : PropertyChangedNotifier
	{
		private const int MaximumIndexedImageProbeCount = 100;

		private static readonly string[] sSupportedImageExtensions = {
			"jpg",
			"jpeg",
			"png",
			"bmp",
			"gif",
			"tif",
			"tiff"
		};

		private readonly ResourcesService mResourcesService;

		private readonly string mTitleResourceKey;
		private readonly string mSubtitleResourceKey;
		private readonly string mAlbumImagesBasePath;

		private bool mHasInitializedImages;

		public string TitleText => mResourcesService[ mTitleResourceKey ];

		public string SubtitleText => mResourcesService[ mSubtitleResourceKey ];

		public ObservableCollection<ImageSource> Images { get; }

		public PhotographyAlbumCardViewModel(
			ResourcesService pResourcesService,
			string pTitleResourceKey,
			string pSubtitleResourceKey,
			string pAlbumImagesBasePath )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );

			mTitleResourceKey = pTitleResourceKey ?? string.Empty;
			mSubtitleResourceKey = pSubtitleResourceKey ?? string.Empty;

			mAlbumImagesBasePath = NormalizeBasePath( pAlbumImagesBasePath );

			Images = new ObservableCollection<ImageSource>();

			InitializeImagesIfNeeded();

			mResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private static string NormalizeBasePath( string pBasePath )
		{
			string lBasePath = ( pBasePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lBasePath ) )
			{
				return string.Empty;
			}

			return lBasePath.Replace( "\\", "/" ).TrimStart( '/' );
		}

		private static void ReplaceObservableImages( ICollection<ImageSource> pTarget, IEnumerable<ImageSource> pImages )
		{
			if ( pTarget == null )
			{
				return;
			}

			pTarget.Clear();

			foreach ( ImageSource lImage in pImages ?? Enumerable.Empty<ImageSource>() )
			{
				if ( lImage == null )
				{
					continue;
				}

				pTarget.Add( lImage );
			}
		}

		private static string GetEntryAssemblyName()
		{
			Assembly lEntryAssembly = Assembly.GetEntryAssembly();
			string lAssemblyName = lEntryAssembly?.GetName()?.Name;

			return string.IsNullOrWhiteSpace( lAssemblyName ) ? "ResumeApp" : lAssemblyName;
		}

		private static string BuildPackUriText( string pRelativePath )
		{
			string lNormalizedPath = ( pRelativePath ?? string.Empty ).Trim().TrimStart( '/' );

			if ( string.IsNullOrWhiteSpace( lNormalizedPath ) )
			{
				return string.Empty;
			}

			string lAssemblyName = GetEntryAssemblyName();

			return $"pack://application:,,,/{lAssemblyName};component/{lNormalizedPath}";
		}

		private static ImageSource TryCreateImageSource( string pRelativePath )
		{
			string lUriText = BuildPackUriText( pRelativePath );

			if ( string.IsNullOrWhiteSpace( lUriText ) )
			{
				return null;
			}

			try
			{
				var lBitmapImage = new BitmapImage();
				lBitmapImage.BeginInit();
				lBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				lBitmapImage.UriSource = new Uri( lUriText, UriKind.RelativeOrAbsolute );
				lBitmapImage.EndInit();
				lBitmapImage.Freeze();

				return lBitmapImage;
			}
			catch ( Exception )
			{
				return null;
			}
		}

		private static IEnumerable<ImageSource> EnumerateIndexedImagesFromBasePath( string pImagesBasePath )
		{
			string lBasePath = ( pImagesBasePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lBasePath ) )
			{
				yield break;
			}

			string lIndexSeparator = lBasePath.EndsWith( "_", StringComparison.Ordinal ) ? string.Empty : "_";
			string lIndexedPrefix = lBasePath + lIndexSeparator;

			for ( int lImageIndex = 1; lImageIndex <= MaximumIndexedImageProbeCount; lImageIndex++ )
			{
				ImageSource lImageSource = TryCreateFirstExistingIndexedImage( lIndexedPrefix, lImageIndex );

				if ( lImageSource == null )
				{
					yield break;
				}

				yield return lImageSource;
			}
		}

		private static ImageSource TryCreateFirstExistingIndexedImage( string pIndexedPrefix, int pImageIndex )
		{
			string lIndexedPrefix = ( pIndexedPrefix ?? string.Empty ).Trim();

			return string.IsNullOrWhiteSpace( lIndexedPrefix )
				? null
				: sSupportedImageExtensions
					.Select( pExtension => $"{lIndexedPrefix}{pImageIndex}.{pExtension}" )
					.Select( TryCreateImageSource )
					.FirstOrDefault( pImageSource => pImageSource != null );
		}

		private void InitializeImagesIfNeeded()
		{
			if ( mHasInitializedImages )
			{
				return;
			}

			List<ImageSource> lImages = EnumerateIndexedImagesFromBasePath( mAlbumImagesBasePath )
				.Where( pImageSource => pImageSource != null )
				.ToList();

			ReplaceObservableImages( Images, lImages );

			mHasInitializedImages = true;
		}

		private void OnResourcesServicePropertyChanged( object pSender, PropertyChangedEventArgs pArgs )
		{
			string lPropertyName = pArgs?.PropertyName ?? string.Empty;

			if ( !string.Equals( lPropertyName, "Item[]", StringComparison.Ordinal ) )
			{
				return;
			}

			RaisePropertyChanged( nameof( TitleText ) );
			RaisePropertyChanged( nameof( SubtitleText ) );
		}
	}
}
