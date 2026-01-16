// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

		private static readonly object sCacheLock = new();
		private static readonly Dictionary<string, IReadOnlyList<string>> sImageRelativePathsByFolderPath = new( StringComparer.OrdinalIgnoreCase );

		private static readonly object sRandomLock = new();
		private static readonly Random sRandom = new();

		private static readonly object sAllResourcePathsLock = new();
		private static bool sHasAttemptedAllResourcePathsBuild;
		private static IReadOnlyList<string> sAllImageResourceRelativePaths;

		private readonly ResourcesService mResourcesService;

		private readonly string mTitleResourceKey;
		private readonly string mSubtitleResourceKey;
		private readonly string mAlbumImagesBasePath;

		private bool mHasInitializedImages;
		private bool mHasQueuedImageInitialization;
		private bool mIsInitializingImages;

		public string TitleText => mResourcesService[ mTitleResourceKey ];

		public string SubtitleText => mResourcesService[ mSubtitleResourceKey ];

		private readonly ObservableCollection<ImageSource> mImages;
		public ObservableCollection<ImageSource> Images
		{
			get
			{
				EnsureImagesInitializationQueuedIfNeeded( DispatcherPriority.Background );
				return mImages;
			}
		}

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

			mImages = new ObservableCollection<ImageSource>();

			mResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private static async Task ReplaceObservableImagesIncrementallyAsync( ObservableCollection<ImageSource> pTarget, IList<ImageSource> pImages, int pBatchSize )
		{
			if ( pTarget == null )
			{
				return;
			}

			pTarget.Clear();

			if ( pImages == null || pImages.Count <= 0 )
			{
				return;
			}

			int lBatchSize = pBatchSize <= 0 ? 4 : pBatchSize;

			for ( int lStartIndex = 0; lStartIndex < pImages.Count; lStartIndex += lBatchSize )
			{
				int lEndIndexExclusive = Math.Min( lStartIndex + lBatchSize, pImages.Count );

				for ( int lCurrentIndex = lStartIndex; lCurrentIndex < lEndIndexExclusive; lCurrentIndex++ )
				{
					ImageSource lImage = pImages[ lCurrentIndex ];

					if ( lImage == null )
					{
						continue;
					}

					pTarget.Add( lImage );
				}

				await Task.Delay( 1 );
			}
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

			try
			{
				var lBitmapImage = new BitmapImage();
				lBitmapImage.BeginInit();
				lBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				lBitmapImage.UriSource = new Uri( lUriText, UriKind.Absolute );
				lBitmapImage.EndInit();
				lBitmapImage.Freeze();

				return lBitmapImage;
			}
			catch ( Exception )
			{
				// ignored
			}

			return null;
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

				if ( lPaths is { Count: > 0 } )
				{
					return lPaths;
				}
			}

			return Array.Empty<string>();
		}

		private static IReadOnlyList<string> GetAllImageResourceRelativePathsOrNull()
		{
			lock ( sAllResourcePathsLock )
			{
				if ( sHasAttemptedAllResourcePathsBuild )
				{
					return sAllImageResourceRelativePaths;
				}

				sHasAttemptedAllResourcePathsBuild = true;

				List<string> lPaths = null;

				try
				{
					Assembly lAssembly = GetResourcesAssembly();
					string lAssemblyName = lAssembly?.GetName()?.Name ?? "ResumeApp";
					string lGResourcesName = $"{lAssemblyName}.g.resources";

					using ( Stream lGResourcesStream = lAssembly?.GetManifestResourceStream( lGResourcesName ) )
					{
						if ( lGResourcesStream == null )
						{
							sAllImageResourceRelativePaths = null;
							return null;
						}

						using ( var lResourceReader = new ResourceReader( lGResourcesStream ) )
						{
							lPaths = new List<string>();

							IDictionaryEnumerator lEnumerator = lResourceReader.GetEnumerator();

							while ( lEnumerator.MoveNext() )
							{
								if ( lEnumerator.Key is not string lResourceKey )
								{
									continue;
								}

								if ( !HasSupportedImageExtension( lResourceKey ) )
								{
									continue;
								}

								string lNormalized = NormalizeRelativePath( lResourceKey );

								if ( string.IsNullOrWhiteSpace( lNormalized ) )
								{
									continue;
								}

								lPaths.Add( lNormalized.Replace( "\\", "/" ) );
							}
						}
					}
				}
				catch ( Exception )
				{
					// ignored
				}

				sAllImageResourceRelativePaths = lPaths;
				return sAllImageResourceRelativePaths;
			}
		}

		private static IReadOnlyList<string> ComputeImageRelativePaths( string pFolderRelativePath )
		{
			string lFolderRelativePath = NormalizeFolderPath( pFolderRelativePath );

			IReadOnlyList<string> lAllResourcePaths = GetAllImageResourceRelativePathsOrNull();

			List<string> lResourcePaths = ( lAllResourcePaths ?? EnumerateResourceRelativePathsInFolderLegacy( lFolderRelativePath ) )
				.Where( pRelativePath => !string.IsNullOrWhiteSpace( pRelativePath ) )
				.Where( pRelativePath => pRelativePath.StartsWith( lFolderRelativePath, StringComparison.OrdinalIgnoreCase ) )
				.Distinct( StringComparer.OrdinalIgnoreCase )
				.OrderBy( GetFileNameFromRelativePath, StringComparer.OrdinalIgnoreCase )
				.ThenBy( pRelativePath => pRelativePath, StringComparer.OrdinalIgnoreCase )
				.ToList();

			if ( lResourcePaths.Count > 0 )
			{
				return lResourcePaths;
			}

			List<string> lFilePaths = EnumerateFilePathsInFolderNearExecutableOrProject( lFolderRelativePath )
				.Where( pFilePath => !string.IsNullOrWhiteSpace( pFilePath ) )
				.Distinct( StringComparer.OrdinalIgnoreCase )
				.OrderBy( Path.GetFileName, StringComparer.OrdinalIgnoreCase )
				.ThenBy( pFilePath => pFilePath, StringComparer.OrdinalIgnoreCase )
				.ToList();

			return lFilePaths;
		}

		private static IEnumerable<string> EnumerateResourceRelativePathsInFolderLegacy( string pFolderRelativePath )
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
						if ( lEnumerator.Key is not string lResourceKey )
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

		private static bool HasResourceRelativePath( string pRelativePath )
		{
			IReadOnlyList<string> lAllResourcePaths = GetAllImageResourceRelativePathsOrNull();

			if ( lAllResourcePaths == null )
			{
				return false;
			}

			string lNormalized = NormalizeRelativePath( pRelativePath );

			return !string.IsNullOrWhiteSpace( lNormalized )
				&& lAllResourcePaths.Contains( lNormalized, StringComparer.OrdinalIgnoreCase );
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

			if ( HasResourceRelativePath( lPath ) )
			{
				return TryCreatePackImageSource( lPath );
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

		private static int GetRandomIndex( int pUpperBoundExclusive )
		{
			if ( pUpperBoundExclusive <= 1 )
			{
				return 0;
			}

			lock ( sRandomLock )
			{
				return sRandom.Next( pUpperBoundExclusive );
			}
		}

		private static void MoveRandomImageToFront( IList<ImageSource> pImages )
		{
			if ( pImages == null )
			{
				return;
			}

			int lImageCount = pImages.Count;

			if ( lImageCount <= 1 )
			{
				return;
			}

			int lRandomIndex = GetRandomIndex( lImageCount );

			if ( lRandomIndex <= 0 )
			{
				return;
			}

			ImageSource lRandomImage = pImages[ lRandomIndex ];
			pImages.RemoveAt( lRandomIndex );
			pImages.Insert( 0, lRandomImage );
		}

		internal void QueueImagesPreload()
		{
			EnsureImagesInitializationQueuedIfNeeded( DispatcherPriority.ContextIdle );
		}

		private void EnsureImagesInitializationQueuedIfNeeded( DispatcherPriority pPriority )
		{
			if ( mHasInitializedImages || mHasQueuedImageInitialization )
			{
				return;
			}

			Dispatcher lDispatcher = Application.Current?.Dispatcher;

			if ( lDispatcher == null )
			{
				return;
			}

			mHasQueuedImageInitialization = true;

			lDispatcher.BeginInvoke( new Action( InitializeImagesAsync ), pPriority );
		}

		private async void InitializeImagesAsync()
		{
			if ( mHasInitializedImages || mIsInitializingImages )
			{
				return;
			}

			mIsInitializingImages = true;

			try
			{
				List<ImageSource> lImages = null;

				try
				{
					lImages = await Task.Run( () =>
					{
						IEnumerable<string> lImagePaths = GetCachedImageRelativePaths( mAlbumImagesBasePath );

						List<ImageSource> lCreatedImages = CreateImageSources( lImagePaths )
							.ToList();

						MoveRandomImageToFront( lCreatedImages );

						return lCreatedImages;
					} );
				}
				catch ( Exception )
				{
					// ignored
				}

				await ReplaceObservableImagesIncrementallyAsync( mImages, lImages ?? new List<ImageSource>(), pBatchSize: 4 );

				mHasInitializedImages = true;
			}
			finally
			{
				mIsInitializingImages = false;
			}
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
