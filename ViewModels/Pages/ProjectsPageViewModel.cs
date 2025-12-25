// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class ProjectsPageViewModel : ViewModelBase
	{
		public ObservableCollection<ProjectCardViewModel> Projects { get; }

		public ProjectsPageViewModel( ResourcesService pResourcesService, ThemeService pThemeService )
			: base( pResourcesService, pThemeService )
		{
			Projects = new ObservableCollection<ProjectCardViewModel>( new[]
			{
				new ProjectCardViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "Project3Title",
					pContextResourceKey: "Project3Context",
					pConstraintsResourceKey: "Project3Constraints",
					pWhatIBuiltItemResourceKeys: new[]
					{
						"Project3Bullet1",
						"Project3Bullet2"
					},
					pImpactResourceKey: "Project3Impact",
					pTechResourceKey: "Project3Tech",
					pImagesResourceKey: "Project3Images",
					pProjectLinkUriText: string.Empty ),

				new ProjectCardViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "Project1Title",
					pContextResourceKey: "Project1Context",
					pConstraintsResourceKey: "Project1Constraints",
					pWhatIBuiltItemResourceKeys: new[]
					{
						"Project1Bullet1",
						"Project1Bullet2"
					},
					pImpactResourceKey: "Project1Impact",
					pTechResourceKey: "Project1Tech",
					pImagesResourceKey: "Project1Images",
					pProjectLinkUriText: "https://www.creaform3d.com/en/products/software/creaform-integrity-suite" ),

				new ProjectCardViewModel(
					pResourcesService: ResourcesService,
					pTitleResourceKey: "Project2Title",
					pContextResourceKey: "Project2Context",
					pConstraintsResourceKey: "Project2Constraints",
					pWhatIBuiltItemResourceKeys: new[]
					{
						"Project2Bullet1"
					},
					pImpactResourceKey: "Project2Impact",
					pTechResourceKey: "Project2Tech",
					pImagesResourceKey: "Project2Images",
					pProjectLinkUriText: "https://store.peel-3d.com/software/peel-os/" )
			} );
		}
	}
}
