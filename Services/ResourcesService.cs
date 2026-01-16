// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace ResumeApp.Services
{
	public sealed class ResourcesService : PropertyChangedNotifier
	{
		private static readonly ResourceManager sResourceManager = CreateResourceManager();

		private CultureInfo mActiveCulture;
		public CultureInfo ActiveCulture
		{
			get => mActiveCulture;
			private set => SetProperty( ref mActiveCulture, value );
		}

		public string ActiveLanguageDisplayName
		{
			get
			{
				TextInfo lTextInfo = ActiveCulture.TextInfo;
				string lNativeName = ActiveCulture.NativeName ?? string.Empty;
				return lTextInfo.ToTitleCase( lNativeName );
			}
		}

		public string this[ string pResourceKey ]
		{
			get
			{
				if ( string.IsNullOrWhiteSpace( pResourceKey ) )
				{
					return string.Empty;
				}

				return sResourceManager.GetString( pResourceKey, ActiveCulture ) ?? string.Empty;
			}
		}

		public ResourcesService()
		{
			ActiveCulture = CultureInfo.GetCultureInfo( "en-CA" );
		}

		private static ResourceManager CreateResourceManager()
		{
			Assembly lAssembly = typeof( ResourcesService ).Assembly;
			string lAssemblyName = lAssembly.GetName().Name ?? "ResumeApp";

			string[] lCandidateBaseNames =
			[
				$"{lAssemblyName}.Properties.Resources",
				$"{lAssemblyName}.Resources",
				"ResumeApp.Properties.Resources",
				"ResumeApp.Resources"
			];

			foreach ( string lBaseName in lCandidateBaseNames )
			{
				ResourceManager lCandidate = CreateIfValid( lAssembly, lBaseName );

				if ( lCandidate != null )
				{
					return lCandidate;
				}
			}

			string lManifestBaseName = lAssembly
				.GetManifestResourceNames()
				.Where( pItem => pItem.EndsWith( ".resources", StringComparison.OrdinalIgnoreCase ) )
				.Where( pItem => !pItem.EndsWith( ".g.resources", StringComparison.OrdinalIgnoreCase ) )
				.OrderByDescending( pItem => pItem.EndsWith( ".Properties.Resources.resources", StringComparison.OrdinalIgnoreCase ) )
				.FirstOrDefault();

			if ( string.IsNullOrWhiteSpace( lManifestBaseName ) )
			{
				return new ResourceManager( $"{lAssemblyName}.Properties.Resources", lAssembly );
			}

			{
				string lBaseName = lManifestBaseName.Substring( 0, lManifestBaseName.Length - ".resources".Length );
				ResourceManager lFromManifest = CreateIfValid( lAssembly, lBaseName );

				if ( lFromManifest != null )
				{
					return lFromManifest;
				}
			}

			return new ResourceManager( $"{lAssemblyName}.Properties.Resources", lAssembly );
		}

		private static ResourceManager CreateIfValid( Assembly pAssembly, string pBaseName )
		{
			if ( string.IsNullOrWhiteSpace( pBaseName ) )
			{
				return null;
			}

			try
			{
				ResourceManager lManager = new ResourceManager( pBaseName, pAssembly );
				lManager.GetResourceSet( CultureInfo.InvariantCulture, true, false );
				return lManager;
			}
			catch ( MissingManifestResourceException )
			{
				return null;
			}
		}

		private static CultureInfo GetCultureForLanguage( AppLanguage pLanguage )
		{
			return pLanguage == AppLanguage.FrenchCanada
				? CultureInfo.GetCultureInfo( "fr-CA" )
				: CultureInfo.GetCultureInfo( "en-CA" );
		}

		private static AppLanguage DetectWindowsLanguage()
		{
			try
			{
				CultureInfo lCulture = CultureInfo.CurrentUICulture;
				string lName = lCulture.Name;

				if ( lName.StartsWith( "fr", StringComparison.OrdinalIgnoreCase ) )
				{
					return AppLanguage.FrenchCanada;
				}
			}
			catch ( Exception )
			{
				// ignored
			}

			return AppLanguage.EnglishCanada;
		}

		public void Initialize()
		{
			if ( RegistrySettingsService.TryLoadLanguage( out AppLanguage lSavedLanguage ) )
			{
				SetLanguageInternal( lSavedLanguage, true );
				return;
			}

			AppLanguage lDetectedLanguage = DetectWindowsLanguage();
			SetLanguageInternal( lDetectedLanguage, true );
		}

		public void SetLanguage( AppLanguage pLanguage )
		{
			SetLanguageInternal( pLanguage, false );
		}

		private void SetLanguageInternal( AppLanguage pLanguage, bool pIsInitialization )
		{
			CultureInfo lCulture = GetCultureForLanguage( pLanguage );

			if ( Equals( ActiveCulture, lCulture ) && !pIsInitialization )
			{
				return;
			}

			ActiveCulture = lCulture;

			CultureInfo.DefaultThreadCurrentCulture = lCulture;
			CultureInfo.DefaultThreadCurrentUICulture = lCulture;
			Thread.CurrentThread.CurrentCulture = lCulture;
			Thread.CurrentThread.CurrentUICulture = lCulture;

			RegistrySettingsService.SaveLanguage( pLanguage );

			RaisePropertyChanged( nameof( ActiveLanguageDisplayName ) );
			RaisePropertyChanged( "Item[]" );
		}
	}
}
