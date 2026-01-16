// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class EducationPageViewModel : ViewModelBase
	{
		public ObservableCollection<EducationItemViewModel> Items { get; }

		public string PageTitleText => ResourcesService[ "TabEducationTitle" ];

		public string PageSubtitleText => ResourcesService[ "EducationPageSubtitle" ];

		private new ResourcesService ResourcesService => base.ResourcesService;

		public EducationPageViewModel( ResourcesService pResourcesService, ThemeService pThemeService )
			: base( pResourcesService, pThemeService )
		{
			Items = new ObservableCollection<EducationItemViewModel>( BuildKeys( "EducationItem", 2 )
				.Select( pPrefix => new EducationItemViewModel( ResourcesService, pPrefix ) ) );

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
			RaisePropertyChanged( nameof( PageTitleText ) );
			RaisePropertyChanged( nameof( PageSubtitleText ) );

			foreach ( EducationItemViewModel lItem in Items )
			{
				lItem.RefreshFromResources();
			}
		}
	}
}
