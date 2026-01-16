// Copyright (C) Olivier La Haye
// All rights reserved.

using Microsoft.Win32;
using ResumeApp.Infrastructure;
using System.Windows;

namespace ResumeApp.Services
{
	public sealed class ThemeService : PropertyChangedNotifier
	{
		private const int DarkThemeRegistryValue = 0;

		public static ThemeService Instance { get; private set; }

		private AppTheme mActiveTheme;
		public AppTheme ActiveTheme
		{
			get => mActiveTheme;
			private set => SetProperty( ref mActiveTheme, value );
		}

		public bool IsDarkThemeActive => ActiveTheme == AppTheme.Dark;

		public ThemeService()
		{
			mActiveTheme = AppTheme.Light;

			if ( Instance == null )
			{
				Instance = this;
			}
		}

		private static ResourceDictionary LoadThemeDictionary( AppTheme pTheme )
		{
			try
			{
				Uri lDictionaryUri = pTheme == AppTheme.Dark
					? new Uri( "Resources/Theme.Dark.xaml", UriKind.Relative )
					: new Uri( "Resources/Theme.Light.xaml", UriKind.Relative );

				return new ResourceDictionary { Source = lDictionaryUri };
			}
			catch ( Exception )
			{
				// ignored
			}

			return null;
		}

		private static void ReplaceMergedDictionary( Application pApplication, ResourceDictionary pNewDictionary, Func<ResourceDictionary, bool> pIsMatch )
		{
			ResourceDictionary lExistingDictionary = pApplication.Resources.MergedDictionaries.FirstOrDefault( pIsMatch );

			if ( lExistingDictionary != null )
			{
				pApplication.Resources.MergedDictionaries.Remove( lExistingDictionary );
			}

			pApplication.Resources.MergedDictionaries.Add( pNewDictionary );
		}

		private static bool IsThemeDictionary( ResourceDictionary pDictionary )
		{
			Uri lSource = pDictionary?.Source;

			if ( lSource == null )
			{
				return false;
			}

			string lOriginalString = lSource.OriginalString ?? string.Empty;
			return lOriginalString.IndexOf( "Theme.", StringComparison.OrdinalIgnoreCase ) >= 0;
		}

		private static AppTheme DetectWindowsAppTheme()
		{
			try
			{
				using ( RegistryKey lPersonalizeKey = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" ) )
				{
					object lValue = lPersonalizeKey?.GetValue( "AppsUseLightTheme" );

					switch ( lValue )
					{
						case int lDword:
							{
								return lDword == DarkThemeRegistryValue ? AppTheme.Dark : AppTheme.Light;
							}
						case string lStringValue when int.TryParse( lStringValue, out int lParsedValue ):
							{
								return lParsedValue == DarkThemeRegistryValue ? AppTheme.Dark : AppTheme.Light;
							}
					}
				}
			}
			catch ( Exception )
			{
				// ignored
			}

			return AppTheme.Light;
		}

		public void Initialize( Application pApplication )
		{
			if ( pApplication == null )
			{
				throw new ArgumentNullException( nameof( pApplication ) );
			}

			if ( RegistrySettingsService.TryLoadTheme( out AppTheme lSavedTheme ) )
			{
				ApplyTheme( pApplication, lSavedTheme, true );
				return;
			}

			AppTheme lDetectedTheme = DetectWindowsAppTheme();
			ApplyTheme( pApplication, lDetectedTheme, true );
		}

		public void ToggleTheme( Application pApplication )
		{
			if ( pApplication == null )
			{
				throw new ArgumentNullException( nameof( pApplication ) );
			}

			AppTheme lNewTheme = ActiveTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
			ApplyTheme( pApplication, lNewTheme, false );
		}

		private void ApplyTheme( Application pApplication, AppTheme pTheme, bool pIsInitialization )
		{
			if ( ActiveTheme == pTheme && !pIsInitialization )
			{
				return;
			}

			ResourceDictionary lThemeDictionary = LoadThemeDictionary( pTheme );

			if ( lThemeDictionary == null )
			{
				return;
			}

			ReplaceMergedDictionary( pApplication, lThemeDictionary, IsThemeDictionary );

			ActiveTheme = pTheme;
			RaisePropertyChanged( nameof( IsDarkThemeActive ) );
			RegistrySettingsService.SaveTheme( pTheme );
		}
	}
}
