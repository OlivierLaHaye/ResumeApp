// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class SkillsPageViewModel : ViewModelBase
	{
		public ObservableCollection<SkillSectionViewModel> Sections { get; }

		public string PageTitleText => ResourcesService[ "TabSkillsTitle" ];

		public string PageSubtitleText => ResourcesService[ "SkillsPageSubtitle" ];

		public SkillsPageViewModel( ResourcesService pResourcesService, ThemeService pThemeService )
			: base( pResourcesService, pThemeService )
		{
			Sections = new ObservableCollection<SkillSectionViewModel>( new[]
			{
				new SkillSectionViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "SkillsSectionDesignSystemsTitle",
					pItemResourceKeys: BuildKeys( "SkillsSectionDesignSystemsItem", 6 ) ),

				new SkillSectionViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "SkillsSectionWpfTitle",
					pItemResourceKeys: BuildKeys( "SkillsSectionWpfItem", 6 ) ),

				new SkillSectionViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "SkillsSectionPrototypingTitle",
					pItemResourceKeys: BuildKeys( "SkillsSectionPrototypingItem", 6 ) ),

				new SkillSectionViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "SkillsSectionCollaborationTitle",
					pItemResourceKeys: BuildKeys( "SkillsSectionCollaborationItem", 6 ) )
			} );

			ResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private static string[] BuildKeys( string pPrefix, int pCount )
		{
			if ( string.IsNullOrWhiteSpace( pPrefix ) )
			{
				return [];
			}

			return pCount <= 0 ? [] : Enumerable.Range( 1, pCount ).Select( pIndex => pPrefix + pIndex ).ToArray();
		}

		private void OnResourcesServicePropertyChanged( object? pSender, PropertyChangedEventArgs pArgs )
		{
			RaisePropertyChanged( nameof( PageTitleText ) );
			RaisePropertyChanged( nameof( PageSubtitleText ) );

			foreach ( SkillSectionViewModel lSection in Sections )
			{
				lSection.RefreshFromResources();
			}
		}
	}
}
