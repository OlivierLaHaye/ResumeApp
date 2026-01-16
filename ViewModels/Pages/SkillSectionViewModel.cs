// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class SkillSectionViewModel : PropertyChangedNotifier
	{
		private readonly ResourcesService mResourcesService;
		private readonly string mTitleResourceKey;

		public string TitleText => mResourcesService[ mTitleResourceKey ];

		public ObservableCollection<LocalizedResourceItemViewModel> Items { get; }

		public SkillSectionViewModel( ResourcesService pResourcesService, string pTitleResourceKey, string[] pItemResourceKeys )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );
			mTitleResourceKey = pTitleResourceKey ?? string.Empty;

			Items = new ObservableCollection<LocalizedResourceItemViewModel>( ( pItemResourceKeys ?? [] )
				.Where( pKey => !string.IsNullOrWhiteSpace( pKey ) )
				.Select( pKey => new LocalizedResourceItemViewModel( mResourcesService, pKey ) ) );

			mResourcesService.PropertyChanged += ( pSender, pArgs ) => RaisePropertyChanged( nameof( TitleText ) );
		}

		public void RefreshFromResources() => RaisePropertyChanged( nameof( TitleText ) );
	}
}
