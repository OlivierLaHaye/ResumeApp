// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class ProjectCardViewModel : PropertyChangedNotifier
	{
		private const int MaximumIndexedImageProbeCount = 100;

		private static readonly string[] sSupportedImageExtensions = new[]
		{
			"png",
			"jpg",
			"jpeg",
			"bmp",
			"gif",
			"tif",
			"tiff"
		};

		private readonly ResourcesService mResourcesService;

		private readonly string mTitleResourceKey;
		private readonly string mContextResourceKey;
		private readonly string mConstraintsResourceKey;
		private readonly string mImpactResourceKey;
		private readonly string mTechResourceKey;
		private readonly string mImagesResourceKey;

		private readonly string mProjectLinkUriText;

		public string TitleText => mResourcesService[ mTitleResourceKey ];

		private string mContextValueText;
		public string ContextValueText
		{
			get => mContextValueText;
			private set => SetProperty( ref mContextValueText, value );
		}

		private string mConstraintsValueText;
		public string ConstraintsValueText
		{
			get => mConstraintsValueText;
			private set => SetProperty( ref mConstraintsValueText, value );
		}

		private string mImpactValueText;
		public string ImpactValueText
		{
			get => mImpactValueText;
			private set => SetProperty( ref mImpactValueText, value );
		}

		public ObservableCollection<LocalizedResourceItemViewModel> WhatIBuiltItems { get; }

		public ObservableCollection<string> TechItems { get; }

		public ObservableCollection<ImageSource> Images { get; }

		public bool IsProjectLinkButtonVisible => !string.IsNullOrWhiteSpace( mProjectLinkUriText );

		public ICommand OpenProjectLinkCommand { get; }

		public ProjectCardViewModel(
			ResourcesService pResourcesService,
			string pTitleResourceKey,
			string pContextResourceKey,
			string pConstraintsResourceKey,
			string[] pWhatIBuiltItemResourceKeys,
			string pImpactResourceKey,
			string pTechResourceKey,
			string pImagesResourceKey,
			string pProjectLinkUriText )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );

			mTitleResourceKey = pTitleResourceKey ?? string.Empty;
			mContextResourceKey = pContextResourceKey ?? string.Empty;
			mConstraintsResourceKey = pConstraintsResourceKey ?? string.Empty;
			mImpactResourceKey = pImpactResourceKey ?? string.Empty;
			mTechResourceKey = pTechResourceKey ?? string.Empty;
			mImagesResourceKey = pImagesResourceKey ?? string.Empty;

			mProjectLinkUriText = ( pProjectLinkUriText ?? string.Empty ).Trim();
			OpenProjectLinkCommand = new RelayCommand( ExecuteOpenProjectLink, () => IsProjectLinkButtonVisible );

			WhatIBuiltItems = new ObservableCollection<LocalizedResourceItemViewModel>( ( pWhatIBuiltItemResourceKeys ?? Array.Empty<string>() )
				.Where( pKey => !string.IsNullOrWhiteSpace( pKey ) )
				.Select( pKey => new LocalizedResourceItemViewModel( mResourcesService, pKey ) ) );

			TechItems = new ObservableCollection<string>();
			Images = new ObservableCollection<ImageSource>();

			RefreshFromResources();

			mResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private static string ExtractValueAfterFirstColon( string pText )
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

			return lColonIndex >= pText.Length - 1 ? string.Empty : pText.Substring( lColonIndex + 1 ).Trim();
		}

		private static IEnumerable<string> SplitSemicolonSeparatedItems( string pText )
		{
			if ( string.IsNullOrWhiteSpace( pText ) )
			{
				return Array.Empty<string>();
			}

			return pText
				.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
				.Select( pItem => pItem.Trim() )
				.Where( pItem => !string.IsNullOrWhiteSpace( pItem ) )
				.ToArray();
		}

		private static string[] SplitCommaSeparatedItems( string pText )
		{
			if ( string.IsNullOrWhiteSpace( pText ) )
			{
				return Array.Empty<string>();
			}

			return pText
				.Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
				.Select( pItem => pItem.Trim() )
				.Where( pItem => !string.IsNullOrWhiteSpace( pItem ) )
				.ToArray();
		}

		private static void ReplaceObservableItems( ICollection<string> pTarget, string[] pItems )
		{
			if ( pTarget == null )
			{
				return;
			}

			pTarget.Clear();

			foreach ( string lItem in pItems ?? Array.Empty<string>() )
			{
				pTarget.Add( lItem );
			}
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

		private static string BuildImageUriText( string pPackOrRelativePath )
		{
			string lValue = ( pPackOrRelativePath ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lValue ) )
			{
				return string.Empty;
			}

			return lValue.StartsWith( "pack://", StringComparison.OrdinalIgnoreCase ) ? lValue : BuildPackUriText( lValue );
		}

		private static ImageSource TryCreateImageSource( string pPackOrRelativePath )
		{
			string lUriText = BuildImageUriText( pPackOrRelativePath );

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

		private static bool IsKnownImageExtension( string pPath )
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

			string lExtension = lPath.Substring( lLastDotIndex + 1 ).Trim();

			return sSupportedImageExtensions.Any( pSupportedExtension =>
				string.Equals( pSupportedExtension, lExtension, StringComparison.OrdinalIgnoreCase ) );
		}

		private static IEnumerable<ImageSource> BuildImageSourcesFromResourceValue( string pImagesValueText )
		{
			string lValueText = ( pImagesValueText ?? string.Empty ).Trim();

			if ( string.IsNullOrWhiteSpace( lValueText ) )
			{
				return Enumerable.Empty<ImageSource>();
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

			if ( lIsSingleImagePath )
			{
				ImageSource lSingleImage = TryCreateImageSource( lValueText );

				return lSingleImage == null ? Enumerable.Empty<ImageSource>() : new[] { lSingleImage };
			}

			return EnumerateIndexedImagesFromBasePath( lValueText ).ToArray();
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

			if ( string.IsNullOrWhiteSpace( lIndexedPrefix ) )
			{
				return null;
			}

			foreach ( string lExtension in sSupportedImageExtensions )
			{
				string lCandidatePath = $"{lIndexedPrefix}{pImageIndex}.{lExtension}";
				ImageSource lImageSource = TryCreateImageSource( lCandidatePath );

				if ( lImageSource != null )
				{
					return lImageSource;
				}
			}

			return null;
		}

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

			string lImagesValueText = ExtractValueAfterFirstColon( mResourcesService[ mImagesResourceKey ] );

			List<ImageSource> lImages = BuildImageSourcesFromResourceValue( lImagesValueText )
				.Where( pImageSource => pImageSource != null )
				.ToList();

			ReplaceObservableImages( Images, lImages );

			RaisePropertyChanged( nameof( TitleText ) );
		}

		private void OnResourcesServicePropertyChanged( object pSender, PropertyChangedEventArgs pArgs )
		{
			RefreshFromResources();
		}
	}
}
