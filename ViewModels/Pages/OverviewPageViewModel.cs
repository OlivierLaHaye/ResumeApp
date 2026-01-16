// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class OverviewPageViewModel : ViewModelBase
	{
		private sealed class DelegateCommand( Action<object?>? pExecuteAction, Func<object?, bool>? pCanExecuteFunc )
			: ICommand
		{
			public event EventHandler? CanExecuteChanged
			{
				add => CommandManager.RequerySuggested += value;
				remove => CommandManager.RequerySuggested -= value;
			}

			public bool CanExecute( object? pParameter )
			{
				return pCanExecuteFunc == null || pCanExecuteFunc( pParameter );
			}

			public void Execute( object? pParameter )
			{
				pExecuteAction?.Invoke( pParameter );
			}
		}

		private const string MailtoSchemePrefix = "mailto:";
		private const string HttpsSchemePrefix = "https://";

		public ObservableCollection<LocalizedResourceItemViewModel> Highlights { get; }

		public ObservableCollection<LocalizedResourceItemViewModel> DesignSystemPoints { get; }

		public ObservableCollection<LocalizedResourceItemViewModel> CoreSkillsLines { get; }

		public ICommand ComposeEmailCommand { get; }

		public ICommand OpenUrlCommand { get; }

		public string ComposeEmailButtonText => ResourcesService[ "OverviewContactComposeEmailButtonText" ];

		public string OpenUrlButtonText => ResourcesService[ "OverviewContactOpenUrlButtonText" ];

		public string FullNameText => ResourcesService[ "HeaderFullNameValue" ];

		public string TargetTitlesText => ResourcesService[ "HeaderTargetTitlesValue" ];

		public string LocationText => ResourcesService[ "HeaderLocationValue" ];

		public string WorkPreferenceText => ResourcesService[ "HeaderWorkPreferenceValue" ];

		public string EmailText => ResourcesService[ "HeaderEmailValue" ];

		public string LinkedInText => ResourcesService[ "HeaderLinkedInValue" ];

		public string GitHubText => ResourcesService[ "HeaderGitHubValue" ];

		public string PortfolioText => ResourcesService[ "HeaderPortfolioValue" ];

		public string SummaryText => ResourcesService[ "SummaryText" ];

		public OverviewPageViewModel( ResourcesService pResourcesService, ThemeService pThemeService )
			: base( pResourcesService, pThemeService )
		{
			ComposeEmailCommand = new DelegateCommand( ExecuteComposeEmail, CanExecuteNonEmptyString );
			OpenUrlCommand = new DelegateCommand( ExecuteOpenUrl, CanExecuteNonEmptyString );

			Highlights = new ObservableCollection<LocalizedResourceItemViewModel>(
				BuildKeys( "HighlightBullet", 5 )
					.Select( pKey => new LocalizedResourceItemViewModel( ResourcesService, pKey ) ) );

			DesignSystemPoints = new ObservableCollection<LocalizedResourceItemViewModel>(
				BuildKeys( "DesignSystemBullet", 4 )
					.Select( pKey => new LocalizedResourceItemViewModel( ResourcesService, pKey ) ) );

			CoreSkillsLines = new ObservableCollection<LocalizedResourceItemViewModel>(
				BuildKeys( "CoreSkillsLine", 6 )
					.Select( pKey => new LocalizedResourceItemViewModel( ResourcesService, pKey ) ) );

			ResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private static IEnumerable<string> BuildKeys( string pPrefix, int pCount )
		{
			if ( string.IsNullOrWhiteSpace( pPrefix ) )
			{
				return [];
			}

			return pCount <= 0 ? [] : Enumerable.Range( 1, pCount ).Select( pIndex => pPrefix + pIndex ).ToArray();
		}

		private static bool CanExecuteNonEmptyString( object? pParameter )
		{
			return pParameter is string lText && !string.IsNullOrWhiteSpace( lText );
		}

		private static void ExecuteComposeEmail( object? pParameter )
		{
			if ( pParameter is not string lEmailAddress )
			{
				return;
			}

			string lMailtoUri = BuildMailtoUri( lEmailAddress );
			OpenUri( lMailtoUri );
		}

		private static void ExecuteOpenUrl( object? pParameter )
		{
			if ( pParameter is not string lUrl )
			{
				return;
			}

			string lNormalizedUrl = NormalizeUrl( lUrl );
			OpenUri( lNormalizedUrl );
		}

		private static string BuildMailtoUri( string pEmailAddress )
		{
			if ( string.IsNullOrWhiteSpace( pEmailAddress ) )
			{
				return string.Empty;
			}

			return pEmailAddress.StartsWith( MailtoSchemePrefix, StringComparison.OrdinalIgnoreCase )
				? pEmailAddress
				: MailtoSchemePrefix + pEmailAddress;
		}

		private static string NormalizeUrl( string pUrl )
		{
			if ( string.IsNullOrWhiteSpace( pUrl ) )
			{
				return string.Empty;
			}

			if ( Uri.TryCreate( pUrl, UriKind.Absolute, out Uri? lAbsoluteUri ) )
			{
				return lAbsoluteUri.AbsoluteUri;
			}

			string lPrefixedUrl = HttpsSchemePrefix + pUrl.Trim();
			return Uri.TryCreate( lPrefixedUrl, UriKind.Absolute, out Uri? lPrefixedUri ) ? lPrefixedUri.AbsoluteUri : pUrl;
		}

		private static void OpenUri( string pUri )
		{
			if ( string.IsNullOrWhiteSpace( pUri ) )
			{
				return;
			}

			try
			{
				var lProcessStartInfo = new ProcessStartInfo
				{
					FileName = pUri,
					UseShellExecute = true
				};

				Process.Start( lProcessStartInfo );
			}
			catch ( Exception )
			{
				// ignored
			}
		}

		private void OnResourcesServicePropertyChanged( object? pSender, PropertyChangedEventArgs pArgs )
		{
			RaisePropertyChanged( nameof( ComposeEmailButtonText ) );
			RaisePropertyChanged( nameof( OpenUrlButtonText ) );
			RaisePropertyChanged( nameof( FullNameText ) );
			RaisePropertyChanged( nameof( TargetTitlesText ) );
			RaisePropertyChanged( nameof( LocationText ) );
			RaisePropertyChanged( nameof( WorkPreferenceText ) );
			RaisePropertyChanged( nameof( EmailText ) );
			RaisePropertyChanged( nameof( LinkedInText ) );
			RaisePropertyChanged( nameof( GitHubText ) );
			RaisePropertyChanged( nameof( PortfolioText ) );
			RaisePropertyChanged( nameof( SummaryText ) );
		}
	}
}
