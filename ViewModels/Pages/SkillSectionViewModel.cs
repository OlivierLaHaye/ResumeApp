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

		public string TitleText => mResourcesService[ field ];

		public ObservableCollection<LocalizedResourceItemViewModel> Items { get; }

		public SkillSectionViewModel( ResourcesService pResourcesService, string? pTitleResourceKey, string[]? pItemResourceKeys )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );
			TitleText = pTitleResourceKey ?? string.Empty;

			Items = new ObservableCollection<LocalizedResourceItemViewModel>( ( pItemResourceKeys ?? [] )
				.Where( pKey => !string.IsNullOrWhiteSpace( pKey ) )
				.Select( pKey => new LocalizedResourceItemViewModel( mResourcesService, pKey ) ) );

			mResourcesService.PropertyChanged += ( _, _ ) => RaisePropertyChanged( nameof( TitleText ) );
		}

		public void RefreshFromResources() => RaisePropertyChanged( nameof( TitleText ) );
	}
}
