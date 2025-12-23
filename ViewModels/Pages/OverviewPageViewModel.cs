// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class OverviewPageViewModel : ViewModelBase
	{
		public ObservableCollection<LocalizedResourceItemViewModel> Highlights { get; }

		public ObservableCollection<LocalizedResourceItemViewModel> DesignSystemPoints { get; }

		public ObservableCollection<LocalizedResourceItemViewModel> CoreSkillsLines { get; }

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
				return Array.Empty<string>();
			}

			return pCount <= 0 ? Array.Empty<string>() : Enumerable.Range( 1, pCount ).Select( pIndex => pPrefix + pIndex ).ToArray();
		}

		private void OnResourcesServicePropertyChanged( object pSender, PropertyChangedEventArgs pArgs )
		{
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
