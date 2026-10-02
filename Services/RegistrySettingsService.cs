// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace ResumeApp.Services;

public static class RegistrySettingsService
{
	private const string CompanyKeyName = "ResumeApp";
	private const string ThemeValueName = "Theme";
	private const string LanguageValueName = "Language";

	private static readonly Lock sInMemoryValuesLock = new();
	private static Dictionary<string, string>? sInMemoryValues;

	internal static bool IsUsingInMemoryStore
	{
		get
		{
			lock ( sInMemoryValuesLock )
			{
				return sInMemoryValues != null;
			}
		}
	}

	internal static void UseInMemoryStore()
	{
		lock ( sInMemoryValuesLock )
		{
			sInMemoryValues ??= new Dictionary<string, string>( StringComparer.Ordinal );
		}
	}

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

	private static bool TryLoadInMemoryValue( string pValueName, out string pValue )
	{
		lock ( sInMemoryValuesLock )
		{
			if ( sInMemoryValues == null )
			{
				pValue = string.Empty;
				return false;
			}

			pValue = sInMemoryValues.GetValueOrDefault( pValueName, string.Empty );
			return true;
		}
	}

	private static bool TrySaveInMemoryValue( string pValueName, string pValue )
	{
		lock ( sInMemoryValuesLock )
		{
			if ( sInMemoryValues == null )
			{
				return false;
			}

			sInMemoryValues[ pValueName ] = pValue;
			return true;
		}
	}

	private static string LoadStringValue( string pValueName )
	{
		if ( TryLoadInMemoryValue( pValueName, out string lInMemoryValue ) )
		{
			return lInMemoryValue;
		}

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
		if ( TrySaveInMemoryValue( pValueName, pValue ) || !OperatingSystem.IsWindows() )
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

	[ExcludeFromCodeCoverage( Justification = "Windows-only code path using Registry.CurrentUser; unreachable on non-Windows CI." )]
	[SupportedOSPlatform( "windows" )]
	private static string LoadStringValueOnWindows( string pValueName )
	{
		using RegistryKey? lKey = Registry.CurrentUser.CreateSubKey( GetRootKeyPath() );
		return lKey.GetValue( pValueName ) as string ?? string.Empty;
	}

	[ExcludeFromCodeCoverage( Justification = "Windows-only code path using Registry.CurrentUser; unreachable on non-Windows CI." )]
	[SupportedOSPlatform( "windows" )]
	private static void SaveStringValueOnWindows( string pValueName, string pValue )
	{
		using RegistryKey? lKey = Registry.CurrentUser.CreateSubKey( GetRootKeyPath() );
		lKey.SetValue( pValueName, pValue, RegistryValueKind.String );
	}

	private static string GetRootKeyPath() => @"Software\" + CompanyKeyName;
}
