// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
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
		[
			"jpg",
			"jpeg",
			"png",
			"bmp",
			"gif",
			"tif",
			"tiff"
		];

		private static readonly Lock sCacheLock = new();
		private static readonly Dictionary<string, IReadOnlyList<string>> sImageRelativePathsByFolderPath =
			new( StringComparer.OrdinalIgnoreCase );

		private static readonly Lock sRandomLock = new();
		private static readonly Random sRandom = new();

		private static readonly Lock sAllResourcePathsLock = new();
		private static bool sHasAttemptedAllResourcePathsBuild;
		private static IReadOnlyList<string>? sAllImageResourceRelativePaths;

		private readonly ResourcesService mResourcesService;

		private readonly string mAlbumImagesBasePath;

		private bool mHasInitializedImages;
		private bool mHasQueuedImageInitialization;
		private bool mIsInitializingImages;

		private readonly ObservableCollection<ImageSource> mImages;

		public string TitleText => mResourcesService[ field ];

		public string SubtitleText => mResourcesService[ field ];

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
			string? pTitleResourceKey,
			string? pSubtitleResourceKey,
			string pAlbumImagesBasePath )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );

			TitleText = pTitleResourceKey ?? string.Empty;
			SubtitleText = pSubtitleResourceKey ?? string.Empty;

			mAlbumImagesBasePath = NormalizeFolderPath( pAlbumImagesBasePath );
			mImages = [];

			mResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		[ExcludeFromCodeCoverage( Justification = "Async image loading pipeline using ImageSource and Task.Delay for incremental UI updates." )]
		private static async Task ReplaceObservableImagesIncrementallyAsync(
			ObservableCollection<ImageSource> pTarget,
			IReadOnlyList<ImageSource> pImages,
			int pBatchSize )
		{
			pTarget.Clear();

			if ( pImages.Count <= 0 )
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

					pTarget.Add( lImage );
				}

				await Task.Delay( 1 );
			}
		}

		internal static string NormalizeFolderPath( string? pFolderPath )
		{
			string lFolderPath = ( pFolderPath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lFolderPath ) )
			{
				return string.Empty;
			}

			lFolderPath = lFolderPath.Replace( "\\", "/" ).TrimStart( '/' );

			return lFolderPath.EndsWith( '/' ) ? lFolderPath : lFolderPath + "/";
		}

		internal static string NormalizeRelativePath( string? pRelativePath )
		{
			string lRelativePath = ( pRelativePath ?? string.Empty ).Trim().TrimStart( '/', '\\' ).Replace( "\\", "/" );
			return string.IsNullOrWhiteSpace( lRelativePath ) ? string.Empty : lRelativePath;
		}

		internal static string NormalizeRelativePathForPackUri( string pRelativePath )
		{
			string lNormalizedPath = NormalizeRelativePath( pRelativePath );

			if ( string.IsNullOrWhiteSpace( lNormalizedPath ) )
			{
				return string.Empty;
			}

			try
			{
				string lUnescapedPath = Uri.UnescapeDataString( lNormalizedPath );
				return NormalizeRelativePath( lUnescapedPath );
			}
			catch ( Exception )
			{
				return lNormalizedPath;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Uses Assembly.GetEntryAssembly which returns null or test runner assembly in unit tests." )]
		private static Assembly GetResourcesAssembly() => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

		[ExcludeFromCodeCoverage( Justification = "Delegates to GetResourcesAssembly which uses Assembly.GetEntryAssembly." )]
		private static string GetResourcesAssemblyName()
		{
			string? lAssemblyName = GetResourcesAssembly().GetName().Name;
			return string.IsNullOrWhiteSpace( lAssemblyName ) ? "ResumeApp" : lAssemblyName;
		}

		[ExcludeFromCodeCoverage( Justification = "Constructs pack:// URIs using assembly names requiring compiled assembly context." )]
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

		[ExcludeFromCodeCoverage( Justification = "Creates BitmapImage from Stream requiring WPF imaging subsystem." )]
		private static ImageSource? TryCreateBitmapImageFromStream( Stream? pStream )
		{
			if ( pStream == null )
			{
				return null;
			}

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

				return lBitmapImage;
			}
			catch ( Exception )
			{
				return null;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Creates BitmapImage from pack:// URIs requiring WPF resource loading." )]
		private static ImageSource? TryCreatePackImageSource( string pRelativePath )
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
				return null;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Creates BitmapImage from file stream requiring WPF imaging subsystem." )]
		private static ImageSource? TryCreateFileImageSource( string? pFilePath )
		{
			string lFilePath = ( pFilePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lFilePath ) || !File.Exists( lFilePath ) )
			{
				return null;
			}

			try
			{
				using var lFileStream = new FileStream( lFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
				return TryCreateBitmapImageFromStream( lFileStream );
			}
			catch ( Exception )
			{
				return null;
			}
		}

		internal static bool HasSupportedImageExtension( string? pPath )
		{
			string lExtension = Path.GetExtension( pPath ?? string.Empty ) ?? string.Empty;

			if ( string.IsNullOrWhiteSpace( lExtension ) )
			{
				return false;
			}

			string lExtensionWithoutDot = lExtension.TrimStart( '.' );

			return sSupportedImageExtensions.Contains( lExtensionWithoutDot, StringComparer.OrdinalIgnoreCase );
		}

		internal static string GetFileNameFromRelativePath( string? pRelativePath )
		{
			string lPath = ( pRelativePath ?? string.Empty ).Replace( "\\", "/" );

			if ( string.IsNullOrWhiteSpace( lPath ) )
			{
				return string.Empty;
			}

			int lLastSlashIndex = lPath.LastIndexOf( '/', lPath.Length - 1 );
			return lLastSlashIndex >= 0 ? lPath[ ( lLastSlashIndex + 1 ).. ] : lPath;
		}

		internal static IEnumerable<string> GetCandidateFolderRelativePaths( string pFolderRelativePath )
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

			string lSuffix = lFolderRelativePath[ lResourcesPhotographyPrefix.Length.. ];
			string lAlternatePath = NormalizeFolderPath( lResourcesProjectsPhotographyPrefix + lSuffix );

			if ( lYieldedPaths.Add( lAlternatePath ) )
			{
				yield return lAlternatePath;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Caching layer that delegates to ComputeImageRelativePathsWithFallbacks requiring assembly resources." )]
		private static IEnumerable<string> GetCachedImageRelativePaths( string pFolderRelativePath )
		{
			string lFolderRelativePath = NormalizeFolderPath( pFolderRelativePath );

			if ( string.IsNullOrWhiteSpace( lFolderRelativePath ) )
			{
				return [];
			}

			lock ( sCacheLock )
			{
				if ( sImageRelativePathsByFolderPath.TryGetValue( lFolderRelativePath, out IReadOnlyList<string>? lCachedPaths ) )
				{
					return lCachedPaths;
				}
			}

			IReadOnlyList<string> lComputedPaths = ComputeImageRelativePathsWithFallbacks( lFolderRelativePath );

			lock ( sCacheLock )
			{
				sImageRelativePathsByFolderPath[ lFolderRelativePath ] = lComputedPaths;
			}

			return lComputedPaths;
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to ComputeImageRelativePaths which requires assembly resources." )]
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

			return [];
		}

		[ExcludeFromCodeCoverage( Justification = "Uses Assembly.GetManifestResourceStream and ResourceReader requiring compiled assembly resources." )]
		private static IReadOnlyList<string>? GetAllImageResourceRelativePathsOrNull()
		{
			lock ( sAllResourcePathsLock )
			{
				if ( sHasAttemptedAllResourcePathsBuild )
				{
					return sAllImageResourceRelativePaths;
				}

				sHasAttemptedAllResourcePathsBuild = true;

				try
				{
					Assembly lAssembly = GetResourcesAssembly();
					string lAssemblyName = lAssembly.GetName().Name ?? "ResumeApp";
					string lGResourcesName = $"{lAssemblyName}.g.resources";

					using Stream? lGResourcesStream = lAssembly.GetManifestResourceStream( lGResourcesName );

					if ( lGResourcesStream == null )
					{
						sAllImageResourceRelativePaths = null;
						return null;
					}

					using var lResourceReader = new ResourceReader( lGResourcesStream );
					var lPaths = new List<string>();

					IDictionaryEnumerator lEnumerator = lResourceReader.GetEnumerator();
					using var lDisposable = lEnumerator as IDisposable;

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

					sAllImageResourceRelativePaths = lPaths;
					return sAllImageResourceRelativePaths;
				}
				catch ( Exception )
				{
					sAllImageResourceRelativePaths = null;
					return null;
				}
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to GetAllImageResourceRelativePathsOrNull and EnumerateFilePathsInFolderNearExecutableOrProject requiring assembly resources or file system." )]
		private static IReadOnlyList<string> ComputeImageRelativePaths( string pFolderRelativePath )
		{
			string lFolderRelativePath = NormalizeFolderPath( pFolderRelativePath );

			IReadOnlyList<string>? lAllResourcePaths = GetAllImageResourceRelativePathsOrNull();

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

			return EnumerateFilePathsInFolderNearExecutableOrProject( lFolderRelativePath )
				.Where( pFilePath => !string.IsNullOrWhiteSpace( pFilePath ) )
				.Distinct( StringComparer.OrdinalIgnoreCase )
				.OrderBy( Path.GetFileName, StringComparer.OrdinalIgnoreCase )
				.ThenBy( pFilePath => pFilePath, StringComparer.OrdinalIgnoreCase )
				.ToList();
		}

		[ExcludeFromCodeCoverage( Justification = "Uses Assembly.GetManifestResourceStream and ResourceReader requiring compiled assembly resources." )]
		private static IEnumerable<string> EnumerateResourceRelativePathsInFolderLegacy( string pFolderRelativePath )
		{
			string lFolderRelativePath = NormalizeFolderPath( pFolderRelativePath );

			if ( string.IsNullOrWhiteSpace( lFolderRelativePath ) )
			{
				yield break;
			}

			Assembly lAssembly = GetResourcesAssembly();
			string lAssemblyName = lAssembly.GetName().Name ?? "ResumeApp";
			string lGResourcesName = $"{lAssemblyName}.g.resources";

			Stream? lGResourcesStream;

			try
			{
				lGResourcesStream = lAssembly.GetManifestResourceStream( lGResourcesName );
			}
			catch ( Exception )
			{
				yield break;
			}

			if ( lGResourcesStream == null )
			{
				yield break;
			}

			using ( lGResourcesStream )
			using ( var lResourceReader = new ResourceReader( lGResourcesStream ) )
			{
				IDictionaryEnumerator lEnumerator = lResourceReader.GetEnumerator();
				using var lDisposable = lEnumerator as IDisposable;

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

		[ExcludeFromCodeCoverage( Justification = "File system directory traversal using Path.GetFullPath and DirectoryInfo.Parent." )]
		private static IEnumerable<string> EnumerateDirectoryAndParents( string? pDirectoryPath, int pMaxLevels )
		{
			string lCurrentDirectoryPath = ( pDirectoryPath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lCurrentDirectoryPath ) )
			{
				yield break;
			}

			string lFullPath;

			try
			{
				lFullPath = Path.GetFullPath( lCurrentDirectoryPath );
			}
			catch ( Exception )
			{
				yield break;
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

		[ExcludeFromCodeCoverage( Justification = "File system directory enumeration using AppDomain.BaseDirectory and Assembly.Location." )]
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

			string? lCurrentDirectoryPath = null;

			try
			{
				lCurrentDirectoryPath = Environment.CurrentDirectory;
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

			string? lExecutingAssemblyDirectoryPath = null;

			try
			{
				lExecutingAssemblyDirectoryPath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
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

		[ExcludeFromCodeCoverage( Justification = "File system directory enumeration near executable or project root." )]
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

				IEnumerable<string> lCandidateFiles;

				try
				{
					lCandidateFiles = Directory.EnumerateFiles( lCandidateDirectoryPath, "*.*", SearchOption.TopDirectoryOnly );
				}
				catch ( Exception )
				{
					continue;
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

		[ExcludeFromCodeCoverage( Justification = "File system path resolution using File.Exists and directory traversal." )]
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

		[ExcludeFromCodeCoverage( Justification = "Delegates to GetAllImageResourceRelativePathsOrNull which requires compiled assembly resources." )]
		private static bool HasResourceRelativePath( string pRelativePath )
		{
			IReadOnlyList<string>? lAllResourcePaths = GetAllImageResourceRelativePathsOrNull();

			if ( lAllResourcePaths == null )
			{
				return false;
			}

			string lNormalized = NormalizeRelativePath( pRelativePath );

			return !string.IsNullOrWhiteSpace( lNormalized )
				&& lAllResourcePaths.Contains( lNormalized, StringComparer.OrdinalIgnoreCase );
		}

		[ExcludeFromCodeCoverage( Justification = "Creates BitmapImage using pack:// URIs or file system requiring WPF imaging and assembly resources." )]
		private static ImageSource? TryCreateImageSource( string? pPath )
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
			ImageSource? lFileImageSource = TryCreateFileImageSource( lResolvedFilePath );

			return lFileImageSource ?? TryCreatePackImageSource( lPath );
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to TryCreateImageSource which creates BitmapImage requiring WPF imaging." )]
		private static IEnumerable<ImageSource> CreateImageSources( IEnumerable<string>? pPaths )
		{
			return ( pPaths ?? [] )
				.Where( pPath => !string.IsNullOrWhiteSpace( pPath ) )
				.Select( TryCreateImageSource )
				.OfType<ImageSource>();
		}

		internal static int GetRandomIndex( int pUpperBoundExclusive )
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

		[ExcludeFromCodeCoverage( Justification = "Operates on IList<ImageSource> which requires WPF ImageSource instances." )]
		private static void MoveRandomImageToFront( IList<ImageSource> pImages )
		{
			if ( pImages.Count <= 1 )
			{
				return;
			}

			int lRandomIndex = GetRandomIndex( pImages.Count );

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

		[ExcludeFromCodeCoverage( Justification = "Uses Application.Current.Dispatcher.BeginInvoke requiring a running WPF Dispatcher." )]
		private void EnsureImagesInitializationQueuedIfNeeded( DispatcherPriority pPriority )
		{
			if ( mHasInitializedImages || mHasQueuedImageInitialization )
			{
				return;
			}

			Dispatcher? lDispatcher = Application.Current?.Dispatcher;

			if ( lDispatcher == null )
			{
				return;
			}

			mHasQueuedImageInitialization = true;
			lDispatcher.BeginInvoke( new Action( InitializeImagesAsync ), pPriority );
		}

		[ExcludeFromCodeCoverage( Justification = "Async void image loading pipeline using Task.Run, BitmapImage, and Dispatcher integration." )]
		private async void InitializeImagesAsync()
		{
			try
			{
				if ( mHasInitializedImages || mIsInitializingImages )
				{
					return;
				}

				mIsInitializingImages = true;

				try
				{
					List<ImageSource> lImages = await LoadImagesAsync();
					await ReplaceObservableImagesIncrementallyAsync( mImages, lImages, pBatchSize: 4 );
					mHasInitializedImages = true;
				}
				catch ( Exception )
				{
					// ignored
				}
				finally
				{
					mIsInitializingImages = false;
				}
			}
			catch ( Exception )
			{
				// ignored
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to CreateImageSources and GetCachedImageRelativePaths which require assembly resources." )]
		private Task<List<ImageSource>> LoadImagesAsync()
		{
			return Task.Run( () =>
			{
				IEnumerable<string> lImagePaths = GetCachedImageRelativePaths( mAlbumImagesBasePath );

				List<ImageSource> lCreatedImages = CreateImageSources( lImagePaths )
					.ToList();

				MoveRandomImageToFront( lCreatedImages );

				return lCreatedImages;
			} );
		}

		private void OnResourcesServicePropertyChanged( object? pSender, PropertyChangedEventArgs pArgs )
		{
			if ( !string.Equals( pArgs.PropertyName, "Item[]", StringComparison.Ordinal ) )
			{
				return;
			}

			RaisePropertyChanged( nameof( TitleText ) );
			RaisePropertyChanged( nameof( SubtitleText ) );
		}
	}
}
