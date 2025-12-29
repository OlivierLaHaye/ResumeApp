// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class PhotographyAlbumCardViewModel : PropertyChangedNotifier
	{
		private static readonly string[] sSupportedImageExtensions =
		{
			"jpg",
			"jpeg",
			"png",
			"bmp",
			"gif",
			"tif",
			"tiff"
		};

		private static readonly object sCacheLock = new object();
		private static readonly Dictionary<string, IReadOnlyList<string>> sImageRelativePathsByFolderPath = new Dictionary<string, IReadOnlyList<string>>( StringComparer.OrdinalIgnoreCase );

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

			mAlbumImagesBasePath = NormalizeFolderPath( pAlbumImagesBasePath );

			Images = new ObservableCollection<ImageSource>();

			InitializeImagesIfNeeded();

			mResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private static string NormalizeFolderPath( string pFolderPath )
		{
			string lFolderPath = ( pFolderPath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lFolderPath ) )
			{
				return string.Empty;
			}

			lFolderPath = lFolderPath.Replace( "\\", "/" ).TrimStart( '/' );

			return lFolderPath.EndsWith( "/", StringComparison.Ordinal ) ? lFolderPath : ( lFolderPath + "/" );
		}

		private static string NormalizeRelativePath( string pRelativePath )
		{
			string lRelativePath = ( pRelativePath ?? string.Empty ).Trim().TrimStart( '/', '\\' ).Replace( "\\", "/" );

			return string.IsNullOrWhiteSpace( lRelativePath ) ? string.Empty : lRelativePath;
		}

		private static string NormalizeRelativePathForPackUri( string pRelativePath )
		{
			string lNormalizedPath = NormalizeRelativePath( pRelativePath );

			if ( string.IsNullOrWhiteSpace( lNormalizedPath ) )
			{
				return string.Empty;
			}

			string lUnescapedPath = lNormalizedPath;

			try
			{
				lUnescapedPath = Uri.UnescapeDataString( lNormalizedPath );
			}
			catch ( Exception )
			{
				// ignored
			}

			return NormalizeRelativePath( lUnescapedPath );
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

		private static Assembly GetResourcesAssembly() => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

		private static string GetResourcesAssemblyName()
		{
			Assembly lAssembly = GetResourcesAssembly();
			string lAssemblyName = lAssembly?.GetName()?.Name;

			return string.IsNullOrWhiteSpace( lAssemblyName ) ? "ResumeApp" : lAssemblyName;
		}

		private static string BuildPackUriText( string pRelativePath )
		{
			string lNormalizedPathForPackUri = NormalizeRelativePathForPackUri( pRelativePath );

			if ( string.IsNullOrWhiteSpace( lNormalizedPathForPackUri ) )
			{
				return string.Empty;
			}

			string lAssemblyName = GetResourcesAssemblyName();

			return $"pack://application:,,,/{lAssemblyName};component/{lNormalizedPathForPackUri}";
		}

		private static ImageSource TryCreateBitmapImageFromStream( Stream pStream )
		{
			if ( pStream == null )
			{
				return null;
			}

			ImageSource lImageSource = null;

			try
			{
				if ( pStream.CanSeek )
				{
					pStream.Position = 0;
				}

				var lBitmapImage = new BitmapImage();
				lBitmapImage.BeginInit();
				lBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				lBitmapImage.StreamSource = pStream;
				lBitmapImage.EndInit();
				lBitmapImage.Freeze();

				lImageSource = lBitmapImage;
			}
			catch ( Exception )
			{
				// ignored
			}

			return lImageSource;
		}

		private static ImageSource TryCreatePackImageSource( string pRelativePath )
		{
			string lUriText = BuildPackUriText( pRelativePath );

			if ( string.IsNullOrWhiteSpace( lUriText ) )
			{
				return null;
			}

			ImageSource lImageSource = null;

			try
			{
				var lPackUri = new Uri( lUriText, UriKind.Absolute );
				StreamResourceInfo lResourceInfo = Application.GetResourceStream( lPackUri );

				if ( lResourceInfo == null )
				{
					return null;
				}

				using ( Stream lStream = lResourceInfo.Stream )
				{
					lImageSource = TryCreateBitmapImageFromStream( lStream );
				}
			}
			catch ( Exception )
			{
				// ignored
			}

			return lImageSource;
		}

		private static ImageSource TryCreateFileImageSource( string pFilePath )
		{
			string lFilePath = ( pFilePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lFilePath ) )
			{
				return null;
			}

			if ( !File.Exists( lFilePath ) )
			{
				return null;
			}

			ImageSource lImageSource = null;

			try
			{
				using ( var lFileStream = new FileStream( lFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
				{
					lImageSource = TryCreateBitmapImageFromStream( lFileStream );
				}
			}
			catch ( Exception )
			{
				// ignored
			}

			return lImageSource;
		}

		private static bool HasSupportedImageExtension( string pPath )
		{
			string lExtension = Path.GetExtension( pPath ?? string.Empty ) ?? string.Empty;

			if ( string.IsNullOrWhiteSpace( lExtension ) )
			{
				return false;
			}

			string lExtensionWithoutDot = lExtension.TrimStart( '.' );

			return sSupportedImageExtensions.Contains( lExtensionWithoutDot, StringComparer.OrdinalIgnoreCase );
		}

		private static string GetFileNameFromRelativePath( string pRelativePath )
		{
			string lPath = ( pRelativePath ?? string.Empty ).Replace( "\\", "/" );

			if ( string.IsNullOrWhiteSpace( lPath ) )
			{
				return string.Empty;
			}

			int lLastSlashIndex = lPath.LastIndexOf( '/', lPath.Length - 1 );

			return lLastSlashIndex >= 0 ? lPath.Substring( lLastSlashIndex + 1 ) : lPath;
		}

		private static IEnumerable<string> GetCandidateFolderRelativePaths( string pFolderRelativePath )
		{
			string lFolderRelativePath = NormalizeFolderPath( pFolderRelativePath );

			if ( string.IsNullOrWhiteSpace( lFolderRelativePath ) )
			{
				yield break;
			}

			var lYieldedPaths = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

			if ( lYieldedPaths.Add( lFolderRelativePath ) )
			{
				yield return lFolderRelativePath;
			}

			const string lResourcesPhotographyPrefix = "Resources/Photography/";
			const string lResourcesProjectsPhotographyPrefix = "Resources/Projects/Photography/";

			if ( !lFolderRelativePath.StartsWith( lResourcesPhotographyPrefix, StringComparison.OrdinalIgnoreCase ) )
			{
				yield break;
			}

			string lSuffix = lFolderRelativePath.Substring( lResourcesPhotographyPrefix.Length );
			string lAlternatePath = NormalizeFolderPath( lResourcesProjectsPhotographyPrefix + lSuffix );

			if ( lYieldedPaths.Add( lAlternatePath ) )
			{
				yield return lAlternatePath;
			}
		}

		private static IEnumerable<string> GetCachedImageRelativePaths( string pFolderRelativePath )
		{
			string lFolderRelativePath = NormalizeFolderPath( pFolderRelativePath );

			if ( string.IsNullOrWhiteSpace( lFolderRelativePath ) )
			{
				return Array.Empty<string>();
			}

			lock ( sCacheLock )
			{
				if ( sImageRelativePathsByFolderPath.TryGetValue( lFolderRelativePath, out IReadOnlyList<string> lCachedPaths ) )
				{
					return lCachedPaths ?? Array.Empty<string>();
				}
			}

			IReadOnlyList<string> lComputedPaths = ComputeImageRelativePathsWithFallbacks( lFolderRelativePath );

			lock ( sCacheLock )
			{
				sImageRelativePathsByFolderPath[ lFolderRelativePath ] = lComputedPaths ?? Array.Empty<string>();
			}

			return lComputedPaths ?? Array.Empty<string>();
		}

		private static IReadOnlyList<string> ComputeImageRelativePathsWithFallbacks( string pFolderRelativePath )
		{
			foreach ( string lCandidateFolderRelativePath in GetCandidateFolderRelativePaths( pFolderRelativePath ) )
			{
				IReadOnlyList<string> lPaths = ComputeImageRelativePaths( lCandidateFolderRelativePath );

				if ( lPaths != null && lPaths.Count > 0 )
				{
					return lPaths;
				}
			}

			return Array.Empty<string>();
		}

		private static IReadOnlyList<string> ComputeImageRelativePaths( string pFolderRelativePath )
		{
			List<string> lResourcePaths = EnumerateResourceRelativePathsInFolder( pFolderRelativePath )
				.Where( pRelativePath => !string.IsNullOrWhiteSpace( pRelativePath ) )
				.Distinct( StringComparer.OrdinalIgnoreCase )
				.OrderBy( GetFileNameFromRelativePath, StringComparer.OrdinalIgnoreCase )
				.ThenBy( pRelativePath => pRelativePath, StringComparer.OrdinalIgnoreCase )
				.ToList();

			if ( lResourcePaths.Count > 0 )
			{
				return lResourcePaths;
			}

			List<string> lFilePaths = EnumerateFilePathsInFolderNearExecutableOrProject( pFolderRelativePath )
				.Where( pFilePath => !string.IsNullOrWhiteSpace( pFilePath ) )
				.Distinct( StringComparer.OrdinalIgnoreCase )
				.OrderBy( Path.GetFileName, StringComparer.OrdinalIgnoreCase )
				.ThenBy( pFilePath => pFilePath, StringComparer.OrdinalIgnoreCase )
				.ToList();

			return lFilePaths;
		}

		private static IEnumerable<string> EnumerateResourceRelativePathsInFolder( string pFolderRelativePath )
		{
			string lFolderRelativePath = NormalizeFolderPath( pFolderRelativePath );

			if ( string.IsNullOrWhiteSpace( lFolderRelativePath ) )
			{
				yield break;
			}

			Assembly lAssembly = GetResourcesAssembly();
			string lAssemblyName = lAssembly.GetName()?.Name ?? "ResumeApp";
			string lGResourcesName = $"{lAssemblyName}.g.resources";

			Stream lGResourcesStream = null;

			try
			{
				lGResourcesStream = lAssembly.GetManifestResourceStream( lGResourcesName );
			}
			catch ( Exception )
			{
				// ignored
			}

			if ( lGResourcesStream == null )
			{
				yield break;
			}

			using ( lGResourcesStream )
			{
				ResourceReader lResourceReader = null;

				try
				{
					lResourceReader = new ResourceReader( lGResourcesStream );
				}
				catch ( Exception )
				{
					// ignored
				}

				if ( lResourceReader == null )
				{
					yield break;
				}

				using ( lResourceReader )
				{
					IDictionaryEnumerator lEnumerator = lResourceReader.GetEnumerator();

					while ( lEnumerator.MoveNext() )
					{
						if ( !( lEnumerator.Key is string lResourceKey ) )
						{
							continue;
						}

						if ( !lResourceKey.StartsWith( lFolderRelativePath, StringComparison.OrdinalIgnoreCase ) )
						{
							continue;
						}

						if ( !HasSupportedImageExtension( lResourceKey ) )
						{
							continue;
						}

						yield return lResourceKey.Replace( "\\", "/" );
					}
				}
			}
		}

		private static IEnumerable<string> EnumerateDirectoryAndParents( string pDirectoryPath, int pMaxLevels )
		{
			string lCurrentDirectoryPath = ( pDirectoryPath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lCurrentDirectoryPath ) )
			{
				yield break;
			}

			string lFullPath = string.Empty;

			try
			{
				lFullPath = Path.GetFullPath( lCurrentDirectoryPath );
			}
			catch ( Exception )
			{
				// ignored
			}

			if ( string.IsNullOrWhiteSpace( lFullPath ) )
			{
				yield break;
			}

			var lDirectoryInfo = new DirectoryInfo( lFullPath );

			for ( int lLevelIndex = 0; lLevelIndex < pMaxLevels && lDirectoryInfo != null; lLevelIndex++ )
			{
				yield return lDirectoryInfo.FullName;
				lDirectoryInfo = lDirectoryInfo.Parent;
			}
		}

		private static IEnumerable<string> EnumerateBaseDirectoriesForDiskLookup()
		{
			var lUniqueBaseDirectories = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

			string lAppBaseDirectoryPath = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;

			foreach ( string lDirectoryPath in EnumerateDirectoryAndParents( lAppBaseDirectoryPath, pMaxLevels: 20 ) )
			{
				if ( lUniqueBaseDirectories.Add( lDirectoryPath ) )
				{
					yield return lDirectoryPath;
				}
			}

			string lCurrentDirectoryPath = string.Empty;

			try
			{
				lCurrentDirectoryPath = Environment.CurrentDirectory ?? string.Empty;
			}
			catch ( Exception )
			{
				// ignored
			}

			foreach ( string lDirectoryPath in EnumerateDirectoryAndParents( lCurrentDirectoryPath, pMaxLevels: 20 ) )
			{
				if ( lUniqueBaseDirectories.Add( lDirectoryPath ) )
				{
					yield return lDirectoryPath;
				}
			}

			string lExecutingAssemblyDirectoryPath = string.Empty;

			try
			{
				lExecutingAssemblyDirectoryPath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ?? string.Empty;
			}
			catch ( Exception )
			{
				// ignored
			}

			foreach ( string lDirectoryPath in EnumerateDirectoryAndParents( lExecutingAssemblyDirectoryPath, pMaxLevels: 20 ) )
			{
				if ( lUniqueBaseDirectories.Add( lDirectoryPath ) )
				{
					yield return lDirectoryPath;
				}
			}
		}

		private static IEnumerable<string> EnumerateFilePathsInFolderNearExecutableOrProject( string pFolderRelativePath )
		{
			string lFolderRelativePath = NormalizeFolderPath( pFolderRelativePath );

			if ( string.IsNullOrWhiteSpace( lFolderRelativePath ) )
			{
				yield break;
			}

			string lRelativeFolderForDisk = lFolderRelativePath.Replace( '/', Path.DirectorySeparatorChar );

			foreach ( string lBaseDirectoryPath in EnumerateBaseDirectoriesForDiskLookup() )
			{
				if ( string.IsNullOrWhiteSpace( lBaseDirectoryPath ) )
				{
					continue;
				}

				string lCandidateDirectoryPath = Path.Combine( lBaseDirectoryPath, lRelativeFolderForDisk );

				if ( !Directory.Exists( lCandidateDirectoryPath ) )
				{
					continue;
				}

				IEnumerable<string> lCandidateFiles = Enumerable.Empty<string>();

				try
				{
					lCandidateFiles = Directory.EnumerateFiles( lCandidateDirectoryPath, "*.*", SearchOption.TopDirectoryOnly );
				}
				catch ( Exception )
				{
					// ignored
				}

				List<string> lSupportedFiles = lCandidateFiles
					.Where( HasSupportedImageExtension )
					.ToList();

				if ( lSupportedFiles.Count <= 0 )
				{
					continue;
				}

				foreach ( string lFilePath in lSupportedFiles )
				{
					yield return lFilePath;
				}

				yield break;
			}
		}

		private static string TryResolveFilePathFromRelativePath( string pRelativePath )
		{
			string lRelativePath = NormalizeRelativePath( pRelativePath );

			if ( string.IsNullOrWhiteSpace( lRelativePath ) )
			{
				return string.Empty;
			}

			string lRelativePathForDisk = lRelativePath
				.Replace( '/', Path.DirectorySeparatorChar )
				.Replace( '\\', Path.DirectorySeparatorChar );

			foreach ( string lBaseDirectoryPath in EnumerateBaseDirectoriesForDiskLookup() )
			{
				if ( string.IsNullOrWhiteSpace( lBaseDirectoryPath ) )
				{
					continue;
				}

				string lCandidateFilePath = Path.Combine( lBaseDirectoryPath, lRelativePathForDisk );

				if ( File.Exists( lCandidateFilePath ) )
				{
					return lCandidateFilePath;
				}
			}

			return string.Empty;
		}

		private static ImageSource TryCreateImageSource( string pPath )
		{
			string lPath = ( pPath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lPath ) )
			{
				return null;
			}

			if ( Path.IsPathRooted( lPath ) )
			{
				return TryCreateFileImageSource( lPath );
			}

			string lResolvedFilePath = TryResolveFilePathFromRelativePath( lPath );
			ImageSource lFileImageSource = TryCreateFileImageSource( lResolvedFilePath );

			return lFileImageSource ?? TryCreatePackImageSource( lPath );
		}

		private static IEnumerable<ImageSource> CreateImageSources( IEnumerable<string> pPaths )
		{
			return ( pPaths ?? Enumerable.Empty<string>() )
				.Where( pPath => !string.IsNullOrWhiteSpace( pPath ) )
				.Select( TryCreateImageSource )
				.Where( pImageSource => pImageSource != null );
		}

		private void InitializeImagesIfNeeded()
		{
			if ( mHasInitializedImages )
			{
				return;
			}

			IEnumerable<string> lImagePaths = GetCachedImageRelativePaths( mAlbumImagesBasePath );

			List<ImageSource> lImages = CreateImageSources( lImagePaths )
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
