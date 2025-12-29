// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class PhotographyPageViewModel : ViewModelBase
	{
		public ObservableCollection<PhotographyAlbumCardViewModel> Albums { get; }

		public PhotographyPageViewModel( ResourcesService pResourcesService, ThemeService pThemeService )
			: base( pResourcesService, pThemeService )
		{
			Albums = new ObservableCollection<PhotographyAlbumCardViewModel>( new[]
			{
				new PhotographyAlbumCardViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "AlbumVolleyballTitle",
					pSubtitleResourceKey: "AlbumVolleyballSubtitle",
					pAlbumImagesBasePath: "Resources/Photography/Volleyball/" ),

				new PhotographyAlbumCardViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "AlbumPortraitsTitle",
					pSubtitleResourceKey: "AlbumPortraitsSubtitle",
					pAlbumImagesBasePath: "Resources/Photography/Portraits/" ),

				new PhotographyAlbumCardViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "AlbumEventsTitle",
					pSubtitleResourceKey: "AlbumEventsSubtitle",
					pAlbumImagesBasePath: "Resources/Photography/Events/" )
			} );
		}
	}
}
