// Copyright (C) Olivier La Haye
// All rights reserved.

using Microsoft.Win32;
using System;

namespace ResumeApp.Services
{
	public sealed class RegistrySettingsService
	{
		private const string CompanyKeyName = "ResumeApp";
		private const string ThemeValueName = "Theme";
		private const string LanguageValueName = "Language";

		public static bool TryLoadTheme( out AppTheme pTheme )
		{
			pTheme = AppTheme.Light;

			string lThemeValue = LoadStringValue( ThemeValueName );

			if ( string.Equals( lThemeValue, AppTheme.Dark.ToString(), StringComparison.OrdinalIgnoreCase ) )
			{
				pTheme = AppTheme.Dark;
				return true;
			}

			if ( !string.Equals( lThemeValue, AppTheme.Light.ToString(), StringComparison.OrdinalIgnoreCase ) )
			{
				return false;
			}

			pTheme = AppTheme.Light;
			return true;

		}

		public static bool TryLoadLanguage( out AppLanguage pLanguage )
		{
			pLanguage = AppLanguage.EnglishCanada;

			string lLanguageValue = LoadStringValue( LanguageValueName );

			if ( string.Equals( lLanguageValue, AppLanguage.FrenchCanada.ToString(), StringComparison.OrdinalIgnoreCase ) )
			{
				pLanguage = AppLanguage.FrenchCanada;
				return true;
			}

			if ( !string.Equals( lLanguageValue, AppLanguage.EnglishCanada.ToString(),
				    StringComparison.OrdinalIgnoreCase ) )
			{
				return false;
			}

			pLanguage = AppLanguage.EnglishCanada;
			return true;

		}

		public static void SaveTheme( AppTheme pTheme )
		{
			SaveStringValue( ThemeValueName, pTheme.ToString() );
		}

		public static void SaveLanguage( AppLanguage pLanguage )
		{
			SaveStringValue( LanguageValueName, pLanguage.ToString() );
		}

		private static string LoadStringValue( string pValueName )
		{
			try
			{
				using ( RegistryKey lKey = Registry.CurrentUser.CreateSubKey( GetRootKeyPath() ) )
				{
					return lKey?.GetValue( pValueName ) as string;
				}
			}
			catch ( Exception )
			{
				// ignored
			}

			return null;
		}

		private static void SaveStringValue( string pValueName, string pValue )
		{
			try
			{
				using ( RegistryKey lKey = Registry.CurrentUser.CreateSubKey( GetRootKeyPath() ) )
				{
					lKey?.SetValue( pValueName, pValue, RegistryValueKind.String );
				}
			}
			catch ( Exception )
			{
				// ignored
			}
		}

		private static string GetRootKeyPath()
		{
			return @"Software\" + CompanyKeyName;
		}
	}
}
