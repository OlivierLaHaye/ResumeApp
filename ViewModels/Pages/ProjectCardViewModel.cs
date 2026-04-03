// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class ProjectCardViewModel : PropertyChangedNotifier
	{
		private const int MaximumIndexedImageProbeCount = 100;

		private static readonly string[] sSupportedImageExtensions =
		[
			"png",
			"jpg",
			"jpeg",
			"bmp",
			"gif",
			"tif",
			"tiff"
		];

		private static readonly Lock sResourceIndexLock = new();
		private static bool sHasAttemptedBuildResourceIndex;
		private static HashSet<string>? sAvailableResourceRelativePaths;

		private readonly ResourcesService mResourcesService;

		private readonly string mContextResourceKey;
		private readonly string mConstraintsResourceKey;
		private readonly string mImpactResourceKey;
		private readonly string mTechResourceKey;

		private readonly string mProjectImagesBaseName;
		private readonly string mProjectLinkUriText;

		private bool mHasInitializedImages;
		private bool mHasQueuedImageInitialization;
		private bool mIsInitializingImages;

		public string TitleText => mResourcesService[ field ];

		[field: AllowNull, MaybeNull]
		public string ContextValueText
		{
			get;
			private set => SetProperty( ref field, value );
		}

		[field: AllowNull, MaybeNull]
		public string ConstraintsValueText
		{
			get;
			private set => SetProperty( ref field, value );
		}

		[field: AllowNull, MaybeNull]
		public string ImpactValueText
		{
			get;
			private set => SetProperty( ref field, value );
		}

		public ObservableCollection<LocalizedResourceItemViewModel> WhatIBuiltItems { get; }

		public ObservableCollection<string> TechItems { get; }

		private readonly ObservableCollection<ImageSource> mImages;
		public ObservableCollection<ImageSource> Images
		{
			get
			{
				EnsureImagesInitializationQueuedIfNeeded( DispatcherPriority.Background );
				return mImages;
			}
		}

		public bool IsProjectLinkButtonVisible => !string.IsNullOrWhiteSpace( mProjectLinkUriText );

		public ICommand OpenProjectLinkCommand { get; }

		public ProjectCardViewModel(
			ResourcesService pResourcesService,
			string? pTitleResourceKey,
			string? pContextResourceKey,
			string? pConstraintsResourceKey,
			string[]? pWhatIBuiltItemResourceKeys,
			string? pImpactResourceKey,
			string? pTechResourceKey,
			string? pProjectImagesBaseName,
			string? pProjectLinkUriText )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );

			TitleText = pTitleResourceKey ?? string.Empty;
			mContextResourceKey = pContextResourceKey ?? string.Empty;
			mConstraintsResourceKey = pConstraintsResourceKey ?? string.Empty;
			mImpactResourceKey = pImpactResourceKey ?? string.Empty;
			mTechResourceKey = pTechResourceKey ?? string.Empty;

			mProjectImagesBaseName = ( pProjectImagesBaseName ?? string.Empty ).Trim();
			mProjectLinkUriText = ( pProjectLinkUriText ?? string.Empty ).Trim();

			OpenProjectLinkCommand = new RelayCommand( ExecuteOpenProjectLink, () => IsProjectLinkButtonVisible );

			WhatIBuiltItems = new ObservableCollection<LocalizedResourceItemViewModel>( ( pWhatIBuiltItemResourceKeys ?? [] )
				.Where( pKey => !string.IsNullOrWhiteSpace( pKey ) )
				.Select( pKey => new LocalizedResourceItemViewModel( mResourcesService, pKey ) ) );

			TechItems = [];
			mImages = [];

			RefreshFromResources();

			mResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		[ExcludeFromCodeCoverage( Justification = "Async image loading pipeline using ImageSource and Task.Delay for incremental UI updates." )]
		private static async Task ReplaceObservableImagesIncrementallyAsync( ICollection<ImageSource>? pTarget, List<ImageSource?> pImages, int pBatchSize )
		{
			if ( pTarget == null )
			{
				return;
			}

			pTarget.Clear();

			if ( pImages is not { Count: > 0 } )
			{
				return;
			}

			int lBatchSize = pBatchSize <= 0 ? 4 : pBatchSize;

			for ( int lStartIndex = 0; lStartIndex < pImages.Count; lStartIndex += lBatchSize )
			{
				int lEndIndexExclusive = Math.Min( lStartIndex + lBatchSize, pImages.Count );

				for ( int lCurrentIndex = lStartIndex; lCurrentIndex < lEndIndexExclusive; lCurrentIndex++ )
				{
					ImageSource? lImage = pImages[ lCurrentIndex ];

					if ( lImage == null )
					{
						continue;
					}

					pTarget.Add( lImage );
				}

				await Task.Delay( 1 );
			}
		}

		internal static string ExtractValueAfterFirstColon( string pText )
		{
			if ( string.IsNullOrWhiteSpace( pText ) )
			{
				return string.Empty;
			}

			int lColonIndex = pText.IndexOf( ':' );

			if ( lColonIndex < 0 )
			{
				return pText.Trim();
			}

			return lColonIndex >= pText.Length - 1 ? string.Empty : pText[ ( lColonIndex + 1 ).. ].Trim();
		}

		private static IEnumerable<string> SplitSemicolonSeparatedItems( string pText )
		{
			if ( string.IsNullOrWhiteSpace( pText ) )
			{
				return [];
			}

			return pText
				.Split( [ ';' ], StringSplitOptions.RemoveEmptyEntries )
				.Select( pItem => pItem.Trim() )
				.Where( pItem => !string.IsNullOrWhiteSpace( pItem ) )
				.ToArray();
		}

		private static string[] SplitCommaSeparatedItems( string pText )
		{
			if ( string.IsNullOrWhiteSpace( pText ) )
			{
				return [];
			}

			return pText
				.Split( [ ',' ], StringSplitOptions.RemoveEmptyEntries )
				.Select( pItem => pItem.Trim() )
				.Where( pItem => !string.IsNullOrWhiteSpace( pItem ) )
				.ToArray();
		}

		private static void ReplaceObservableItems( ICollection<string>? pTarget, string[]? pItems )
		{
			if ( pTarget == null )
			{
				return;
			}

			pTarget.Clear();

			foreach ( string lItem in pItems ?? [] )
			{
				pTarget.Add( lItem );
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Uses Assembly.GetEntryAssembly which returns null or test runner assembly in unit tests." )]
		private static string GetEntryAssemblyName()
		{
			Assembly? lEntryAssembly = Assembly.GetEntryAssembly();
			string? lAssemblyName = lEntryAssembly?.GetName().Name;

			return string.IsNullOrWhiteSpace( lAssemblyName ) ? "ResumeApp" : lAssemblyName;
		}

		internal static string NormalizeRelativePath( string? pRelativePath )
		{
			string lRelativePath = ( pRelativePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lRelativePath ) )
			{
				return string.Empty;
			}

			lRelativePath = lRelativePath.TrimStart( '/', '\\' ).Replace( "\\", "/" );

			if ( string.IsNullOrWhiteSpace( lRelativePath ) )
			{
				return string.Empty;
			}

			try
			{
				lRelativePath = Uri.UnescapeDataString( lRelativePath );
			}
			catch ( Exception )
			{
				// ignored
			}

			return ( lRelativePath ?? string.Empty ).Trim().TrimStart( '/', '\\' ).Replace( "\\", "/" );
		}

		[ExcludeFromCodeCoverage( Justification = "Uses Assembly.GetEntryAssembly/GetManifestResourceStream and ResourceReader requiring compiled assembly resources." )]
		private static HashSet<string>? GetAvailableResourceRelativePathsOrNull()
		{
			lock ( sResourceIndexLock )
			{
				if ( sHasAttemptedBuildResourceIndex )
				{
					return sAvailableResourceRelativePaths;
				}

				sHasAttemptedBuildResourceIndex = true;

				HashSet<string>? lPaths = null;

				try
				{
					Assembly lAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
					string lAssemblyName = lAssembly?.GetName()?.Name ?? "ResumeApp";
					string lGResourcesName = $"{lAssemblyName}.g.resources";

					using Stream? lGResourcesStream = lAssembly?.GetManifestResourceStream( lGResourcesName );
					if ( lGResourcesStream == null )
					{
						sAvailableResourceRelativePaths = null;
						return null;
					}

					using var lResourceReader = new ResourceReader( lGResourcesStream );
					lPaths = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

					IDictionaryEnumerator lEnumerator = lResourceReader.GetEnumerator();
					using var lDisposable = lEnumerator as IDisposable;

					while ( lEnumerator.MoveNext() )
					{
						if ( lEnumerator.Key is not string lResourceKey )
						{
							continue;
						}

						if ( !IsKnownImageExtension( lResourceKey ) )
						{
							continue;
						}

						string lNormalized = NormalizeRelativePath( lResourceKey );

						if ( string.IsNullOrWhiteSpace( lNormalized ) )
						{
							continue;
						}

						lPaths.Add( lNormalized );
					}
				}
				catch ( Exception )
				{
					// ignored
				}

				sAvailableResourceRelativePaths = lPaths;
				return sAvailableResourceRelativePaths;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to GetAvailableResourceRelativePathsOrNull which requires compiled assembly resources." )]
		private static bool HasResourceRelativePath( string pRelativePath )
		{
			HashSet<string>? lAvailablePaths = GetAvailableResourceRelativePathsOrNull();

			if ( lAvailablePaths == null )
			{
				return true;
			}

			string lNormalized = NormalizeRelativePath( pRelativePath );

			return !string.IsNullOrWhiteSpace( lNormalized ) && lAvailablePaths.Contains( lNormalized );
		}

		[ExcludeFromCodeCoverage( Justification = "Constructs pack:// URIs using Assembly names requiring compiled assembly context." )]
		private static string BuildPackUriText( string pRelativePath )
		{
			string lNormalizedPath = NormalizeRelativePath( pRelativePath );

			if ( string.IsNullOrWhiteSpace( lNormalizedPath ) )
			{
				return string.Empty;
			}

			string lAssemblyName = GetEntryAssemblyName();

			return $"pack://application:,,,/{lAssemblyName};component/{lNormalizedPath}";
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to BuildPackUriText which constructs pack:// URIs requiring assembly context." )]
		private static string BuildImageUriText( string? pPackOrRelativePath )
		{
			string lValue = ( pPackOrRelativePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lValue ) )
			{
				return string.Empty;
			}

			return lValue.StartsWith( "pack://", StringComparison.OrdinalIgnoreCase ) ? lValue : BuildPackUriText( lValue );
		}

		[ExcludeFromCodeCoverage( Justification = "Creates BitmapImage from pack:// URIs requiring WPF resource loading." )]
		private static ImageSource? TryCreateImageSource( string? pPackOrRelativePath )
		{
			string lValue = ( pPackOrRelativePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lValue ) )
			{
				return null;
			}

			bool lIsPackUri = lValue.StartsWith( "pack://", StringComparison.OrdinalIgnoreCase );

			if ( !lIsPackUri && !HasResourceRelativePath( lValue ) )
			{
				return null;
			}

			string lUriText = BuildImageUriText( lValue );

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
				// ignored
			}

			return null;
		}

		internal static bool IsKnownImageExtension( string? pPath )
		{
			string lPath = ( pPath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lPath ) )
			{
				return false;
			}

			int lLastDotIndex = lPath.LastIndexOf( '.' );

			if ( lLastDotIndex < 0 || lLastDotIndex >= lPath.Length - 1 )
			{
				return false;
			}

			string lExtension = lPath[ ( lLastDotIndex + 1 ).. ].Trim();

			return sSupportedImageExtensions.Any( pSupportedExtension =>
				string.Equals( pSupportedExtension, lExtension, StringComparison.OrdinalIgnoreCase ) );
		}

		internal static string BuildProjectImagesBasePath( string? pProjectImagesBaseName )
		{
			string lProjectImagesBaseName = ( pProjectImagesBaseName ?? string.Empty ).Trim().TrimStart( '/' );

			if ( string.IsNullOrWhiteSpace( lProjectImagesBaseName ) )
			{
				return string.Empty;
			}

			bool lLooksLikeAPath = lProjectImagesBaseName.Contains( "/" ) || lProjectImagesBaseName.Contains( "\\" );

			return lLooksLikeAPath ? lProjectImagesBaseName.Replace( "\\", "/" ).TrimStart( '/' ) : $"Resources/Projects/{lProjectImagesBaseName}/{lProjectImagesBaseName}";
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to TryCreateImageSource which creates BitmapImage from pack:// URIs." )]
		private static ImageSource?[] BuildImageSourcesFromBasePathOrDescriptor( string? pImagesDescriptorOrBasePath )
		{
			string lValueText = ( pImagesDescriptorOrBasePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lValueText ) )
			{
				return [];
			}

			bool lHasExplicitList = lValueText.Contains( ";" );

			if ( lHasExplicitList )
			{
				return SplitSemicolonSeparatedItems( lValueText )
					.Select( TryCreateImageSource )
					.Where( pImageSource => pImageSource != null )
					.ToArray();
			}

			bool lIsSingleImagePath = IsKnownImageExtension( lValueText );

			if ( !lIsSingleImagePath )
			{
				return EnumerateIndexedImagesFromBasePath( lValueText ).ToArray();
			}

			ImageSource? lSingleImage = TryCreateImageSource( lValueText );

			return lSingleImage == null ? [] : [ lSingleImage ];
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to TryCreateImageSource which creates BitmapImage from pack:// URIs." )]
		private static IEnumerable<ImageSource> EnumerateIndexedImagesFromBasePath( string? pImagesBasePath )
		{
			string lBasePath = ( pImagesBasePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lBasePath ) )
			{
				yield break;
			}

			string lIndexSeparator = lBasePath.EndsWith( '_' ) ? string.Empty : "_";
			string lIndexedPrefix = lBasePath + lIndexSeparator;

			for ( int lImageIndex = 1; lImageIndex <= MaximumIndexedImageProbeCount; lImageIndex++ )
			{
				ImageSource? lImageSource = TryCreateFirstExistingIndexedImage( lIndexedPrefix, lImageIndex );

				if ( lImageSource == null )
				{
					yield break;
				}

				yield return lImageSource;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to TryCreateImageSource which creates BitmapImage from pack:// URIs." )]
		private static ImageSource? TryCreateFirstExistingIndexedImage( string? pIndexedPrefix, int pImageIndex )
		{
			string lIndexedPrefix = ( pIndexedPrefix ?? string.Empty ).Trim();

			return string.IsNullOrWhiteSpace( lIndexedPrefix ) ? null : sSupportedImageExtensions.Select( pExtension => $"{lIndexedPrefix}{pImageIndex}.{pExtension}" ).Select( pCandidateRelativePath => TryCreateImageSource( pCandidateRelativePath ) ).FirstOrDefault( pImageSource => pImageSource != null );
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
			if ( mHasInitializedImages || mIsInitializingImages )
			{
				return;
			}

			mIsInitializingImages = true;

			try
			{
				List<ImageSource?>? lImages = null;

				try
				{
					lImages = await Task.Run( () =>
					{
						string lImagesBasePath = BuildProjectImagesBasePath( mProjectImagesBaseName );

						return BuildImageSourcesFromBasePathOrDescriptor( lImagesBasePath )
							.Where( pImageSource => pImageSource != null )
							.ToList();
					} );
				}
				catch ( Exception )
				{
					// ignored
				}

				await ReplaceObservableImagesIncrementallyAsync( mImages, lImages ?? [], pBatchSize: 4 );

				mHasInitializedImages = true;
			}
			finally
			{
				mIsInitializingImages = false;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Calls Process.Start with UseShellExecute requiring a running desktop shell." )]
		private void ExecuteOpenProjectLink()
		{
			if ( !IsProjectLinkButtonVisible )
			{
				return;
			}

			try
			{
				var lProcessStartInfo = new ProcessStartInfo
				{
					FileName = mProjectLinkUriText,
					UseShellExecute = true
				};

				Process.Start( lProcessStartInfo );
			}
			catch ( Exception )
			{
				// ignored
			}
		}

		private void RefreshFromResources()
		{
			ContextValueText = ExtractValueAfterFirstColon( mResourcesService[ mContextResourceKey ] );
			ConstraintsValueText = ExtractValueAfterFirstColon( mResourcesService[ mConstraintsResourceKey ] );
			ImpactValueText = ExtractValueAfterFirstColon( mResourcesService[ mImpactResourceKey ] );

			string lTechValueText = ExtractValueAfterFirstColon( mResourcesService[ mTechResourceKey ] );
			ReplaceObservableItems( TechItems, SplitCommaSeparatedItems( lTechValueText ) );

			RaisePropertyChanged( nameof( TitleText ) );
		}

		private void OnResourcesServicePropertyChanged( object? pSender, PropertyChangedEventArgs? pArgs )
		{
			string lPropertyName = pArgs?.PropertyName ?? string.Empty;

			if ( !string.Equals( lPropertyName, "Item[]", StringComparison.Ordinal ) )
			{
				return;
			}

			RefreshFromResources();
		}
	}
}
