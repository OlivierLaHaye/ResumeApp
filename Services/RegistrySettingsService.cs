// Copyright (C) Olivier La Haye
// All rights reserved.

using Microsoft.Win32;
using System.Runtime.Versioning;

namespace ResumeApp.Services;

public static class RegistrySettingsService
{
	private const string CompanyKeyName = "ResumeApp";
	private const string ThemeValueName = "Theme";
	private const string LanguageValueName = "Language";

	public static bool TryLoadTheme( out AppTheme pTheme ) => TryLoadEnumValue( ThemeValueName, AppTheme.Light, out pTheme );

	public static bool TryLoadLanguage( out AppLanguage pLanguage ) => TryLoadEnumValue( LanguageValueName, AppLanguage.EnglishCanada, out pLanguage );

	public static void SaveTheme( AppTheme pTheme ) => SaveEnumValue( ThemeValueName, pTheme );

	public static void SaveLanguage( AppLanguage pLanguage ) => SaveEnumValue( LanguageValueName, pLanguage );

	private static bool TryLoadEnumValue<TEnum>( string pValueName, TEnum pDefaultValue, out TEnum pValue )
		where TEnum : struct, Enum
	{
		pValue = pDefaultValue;

		string lText = LoadStringValue( pValueName );
		if ( string.IsNullOrWhiteSpace( lText ) )
		{
			return false;
		}

		if ( !Enum.TryParse( lText, ignoreCase: true, out TEnum lParsedValue ) )
		{
			return false;
		}

		if ( !Enum.IsDefined( typeof( TEnum ), lParsedValue ) )
		{
			return false;
		}

		pValue = lParsedValue;
		return true;
	}

	private static void SaveEnumValue<TEnum>( string pValueName, TEnum pValue )
		where TEnum : struct, Enum
	{
		SaveStringValue( pValueName, pValue.ToString() );
	}

	private static string LoadStringValue( string pValueName )
	{
		if ( !OperatingSystem.IsWindows() )
		{
			return string.Empty;
		}

		try
		{
			return LoadStringValueOnWindows( pValueName );
		}
		catch ( Exception )
		{
			return string.Empty;
		}
	}

	private static void SaveStringValue( string pValueName, string pValue )
	{
		if ( !OperatingSystem.IsWindows() )
		{
			return;
		}

		try
		{
			SaveStringValueOnWindows( pValueName, pValue );
		}
		catch ( Exception )
		{
			// ignored
		}
	}

	[SupportedOSPlatform( "windows" )]
	private static string LoadStringValueOnWindows( string pValueName )
	{
		using RegistryKey? lKey = Registry.CurrentUser.CreateSubKey( GetRootKeyPath() );
		return lKey.GetValue( pValueName ) as string ?? string.Empty;
	}

	[SupportedOSPlatform( "windows" )]
	private static void SaveStringValueOnWindows( string pValueName, string pValue )
	{
		using RegistryKey? lKey = Registry.CurrentUser.CreateSubKey( GetRootKeyPath() );
		lKey.SetValue( pValueName, pValue, RegistryValueKind.String );
	}

	private static string GetRootKeyPath() => @"Software\" + CompanyKeyName;
}
