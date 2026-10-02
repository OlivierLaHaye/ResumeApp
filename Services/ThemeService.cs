// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using ResumeApp.Infrastructure;
using System.Runtime.Versioning;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace ResumeApp.Services
{
	public sealed class ThemeService : PropertyChangedNotifier
	{
		private const int DarkThemeRegistryValue = 0;

		public static ThemeService? Instance { get; private set; }

		private AppTheme mActiveTheme;
		public AppTheme ActiveTheme
		{
			get => mActiveTheme;
			internal set => SetProperty( ref mActiveTheme, value );
		}

		public bool IsDarkThemeActive => ActiveTheme == AppTheme.Dark;

		public bool IsHighContrastActive
		{
			get => mIsHighContrastActive;
			private set => SetProperty( ref mIsHighContrastActive, value );
		}

		internal static IReadOnlyList<string> HighContrastSurfaceColorKeys { get; } =
		[
			"CommonBlackColor",
			"CommonDarkerGrayColor",
			"CommonDarkGrayColor",
			"CommonGrayColor",
			"SurfaceHoverColor"
		];

		internal static IReadOnlyList<string> HighContrastTextColorKeys { get; } =
		[
			"CommonWhiteColor",
			"CommonLightGrayColor",
			"TextPrimaryColor",
			"TextSecondaryColor",
			"BorderSubtleColor",
			"CaptionCloseHoverGlyphColor"
		];

		internal static IReadOnlyList<string> HighContrastHighlightColorKeys { get; } =
		[
			"CommonColor",
			"CommonBlueColor",
			"AccentColor",
			"FocusRingColor",
			"SurfaceSelectedColor",
			"CaptionCloseHoverColor"
		];

		internal static IReadOnlyList<string> HighContrastHighlightTextColorKeys { get; } =
		[
			"TextOnSelectedColor"
		];

		internal static IReadOnlyList<string> HighContrastHotTrackColorKeys { get; } =
		[
			"AccentTextColor"
		];

		private Application? mApplication;
		private ResourceDictionary? mHighContrastDictionary;
		private bool mIsHighContrastActive;

		public ThemeService()
		{
			mActiveTheme = AppTheme.Light;
			Instance ??= this;
		}

		internal static ResourceDictionary CreateHighContrastDictionary(
			Color pWindowColor,
			Color pWindowTextColor,
			Color pHighlightColor,
			Color pHighlightTextColor,
			Color pHotTrackColor )
		{
			var lDictionary = new ResourceDictionary();

			AddColorAndBrush( lDictionary, HighContrastSurfaceColorKeys, pWindowColor );
			AddColorAndBrush( lDictionary, HighContrastTextColorKeys, pWindowTextColor );
			AddColorAndBrush( lDictionary, HighContrastHighlightColorKeys, pHighlightColor );
			AddColorAndBrush( lDictionary, HighContrastHighlightTextColorKeys, pHighlightTextColor );
			AddColorAndBrush( lDictionary, HighContrastHotTrackColorKeys, pHotTrackColor );

			return lDictionary;
		}

		internal static string GetBrushKeyForColorKey( string pColorKey )
		{
			ArgumentException.ThrowIfNullOrWhiteSpace( pColorKey );

			return pColorKey.EndsWith( "Color", StringComparison.Ordinal )
				? string.Concat( pColorKey.AsSpan( 0, pColorKey.Length - "Color".Length ), "Brush" )
				: pColorKey + "Brush";
		}

		private static void AddColorAndBrush( ResourceDictionary pDictionary, IEnumerable<string> pColorKeys, Color pColor )
		{
			foreach ( string lColorKey in pColorKeys )
			{
				var lBrush = new SolidColorBrush( pColor );
				lBrush.Freeze();

				pDictionary[ lColorKey ] = pColor;
				pDictionary[ GetBrushKeyForColorKey( lColorKey ) ] = lBrush;
			}
		}

		[ExcludeFromCodeCoverage( Justification = "Creates ResourceDictionary from XAML Source URI requiring compiled BAML resources." )]
		private static ResourceDictionary? LoadThemeDictionary( AppTheme pTheme )
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

		[ExcludeFromCodeCoverage( Justification = "Accesses Application.Resources.MergedDictionaries requiring a running WPF Application." )]
		private static void ReplaceMergedDictionary(
			Application pApplication,
			ResourceDictionary pNewDictionary,
			Func<ResourceDictionary, bool> pIsMatch )
		{
			ResourceDictionary? lExistingDictionary = pApplication.Resources.MergedDictionaries.FirstOrDefault( pIsMatch );

			if ( lExistingDictionary != null )
			{
				pApplication.Resources.MergedDictionaries.Remove( lExistingDictionary );
			}

			pApplication.Resources.MergedDictionaries.Add( pNewDictionary );
		}

		[ExcludeFromCodeCoverage( Justification = "Inspects ResourceDictionary.Source URIs for theme detection." )]
		private static bool IsThemeDictionary( ResourceDictionary? pDictionary )
		{
			Uri? lSource = pDictionary?.Source;

			if ( lSource == null )
			{
				return false;
			}

			string lOriginalString = lSource.OriginalString;
			return lOriginalString.IndexOf( "Theme.", StringComparison.OrdinalIgnoreCase ) >= 0;
		}

		[ExcludeFromCodeCoverage( Justification = "Windows-only code path using Registry.CurrentUser for theme detection; unreachable on non-Windows CI." )]
		[SupportedOSPlatform( "windows" )]
		private static AppTheme DetectWindowsAppThemeWindows()
		{
			try
			{
				using RegistryKey? lPersonalizeKey = Registry.CurrentUser.OpenSubKey(
					@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" );

				object? lValue = lPersonalizeKey?.GetValue( "AppsUseLightTheme" );

				return lValue switch
				{
					int lDword => lDword == DarkThemeRegistryValue ? AppTheme.Dark : AppTheme.Light,
					string lStringValue when int.TryParse( lStringValue, out int lParsedValue )
						=> lParsedValue == DarkThemeRegistryValue ? AppTheme.Dark : AppTheme.Light,
					_ => AppTheme.Light
				};
			}
			catch ( Exception )
			{
				// ignored
			}

			return AppTheme.Light;
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to Windows-only theme detection using Registry." )]
		private static AppTheme DetectWindowsAppTheme()
		{
			return OperatingSystem.IsWindows()
				? DetectWindowsAppThemeWindows()
				: AppTheme.Light;
		}

		[ExcludeFromCodeCoverage( Justification = "Applies theme to Application via ResourceDictionary replacement requiring a running WPF Application; null-guard tested separately." )]
		public void Initialize( Application pApplication )
		{
			ArgumentNullException.ThrowIfNull( pApplication );

			mApplication = pApplication;
			SystemParameters.StaticPropertyChanged -= OnSystemParametersStaticPropertyChanged;
			SystemParameters.StaticPropertyChanged += OnSystemParametersStaticPropertyChanged;

			if ( RegistrySettingsService.TryLoadTheme( out AppTheme lSavedTheme ) )
			{
				ApplyTheme( pApplication, lSavedTheme, true );
				return;
			}

			ApplyTheme( pApplication, DetectWindowsAppTheme(), true );
		}

		[ExcludeFromCodeCoverage( Justification = "Applies the requested theme to Application via ResourceDictionary replacement requiring a running WPF Application; null-guard tested separately." )]
		public void SetTheme( Application pApplication, AppTheme pTheme )
		{
			ArgumentNullException.ThrowIfNull( pApplication );

			ApplyTheme( pApplication, pTheme, false );
		}

		[ExcludeFromCodeCoverage( Justification = "Applies toggled theme to Application via ResourceDictionary replacement requiring a running WPF Application; null-guard tested separately." )]
		public void ToggleTheme( Application pApplication )
		{
			ArgumentNullException.ThrowIfNull( pApplication );

			AppTheme lNewTheme = ActiveTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
			ApplyTheme( pApplication, lNewTheme, false );
		}

		[ExcludeFromCodeCoverage( Justification = "Orchestrates ResourceDictionary replacement on Application requiring a running WPF Application." )]
		private void ApplyTheme( Application pApplication, AppTheme pTheme, bool pIsInitialization )
		{
			if ( ActiveTheme == pTheme && !pIsInitialization )
			{
				return;
			}

			ResourceDictionary? lThemeDictionary = LoadThemeDictionary( pTheme );
			if ( lThemeDictionary == null )
			{
				return;
			}

			ReplaceMergedDictionary( pApplication, lThemeDictionary, IsThemeDictionary );
			ApplyHighContrastOverrides( pApplication );

			ActiveTheme = pTheme;
			RaisePropertyChanged( nameof( IsDarkThemeActive ) );
			RegistrySettingsService.SaveTheme( pTheme );
		}

		[ExcludeFromCodeCoverage( Justification = "Reads SystemParameters.HighContrast and SystemColors and mutates Application.Resources requiring a running WPF Application." )]
		private void ApplyHighContrastOverrides( Application pApplication )
		{
			if ( mHighContrastDictionary != null )
			{
				pApplication.Resources.MergedDictionaries.Remove( mHighContrastDictionary );
				mHighContrastDictionary = null;
			}

			bool lIsHighContrast = SystemParameters.HighContrast;

			if ( lIsHighContrast )
			{
				mHighContrastDictionary = CreateHighContrastDictionary(
					SystemColors.WindowColor,
					SystemColors.WindowTextColor,
					SystemColors.HighlightColor,
					SystemColors.HighlightTextColor,
					SystemColors.HotTrackColor );

				pApplication.Resources.MergedDictionaries.Add( mHighContrastDictionary );
			}

			IsHighContrastActive = lIsHighContrast;
		}

		[ExcludeFromCodeCoverage( Justification = "Reacts to SystemParameters.StaticPropertyChanged raised by the WPF system-settings listener." )]
		private void OnSystemParametersStaticPropertyChanged( object? pSender, PropertyChangedEventArgs pEventArgs )
		{
			if ( pEventArgs.PropertyName != nameof( SystemParameters.HighContrast ) || mApplication is not Application lApplication )
			{
				return;
			}

			lApplication.Dispatcher.BeginInvoke( () => ApplyHighContrastOverrides( lApplication ) );
		}
	}
}
