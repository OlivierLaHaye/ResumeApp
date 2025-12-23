// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
					pTitleResourceKey: "Project1Title",
					pContextResourceKey: "Project1Context",
					pConstraintsResourceKey: "Project1Constraints",
					pWhatIBuiltItemResourceKeys: new[]
					{
						"Project1Bullet1",
						"Project1Bullet2"
					},
					pImpactResourceKey: "Project1Impact",
					pTechResourceKey: "Project1Tech" ),

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
					pTechResourceKey: "Project2Tech" ),

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
					pTechResourceKey: "Project3Tech" )
			} );

			ResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private void OnResourcesServicePropertyChanged( object pSender, PropertyChangedEventArgs pArgs )
		{
			foreach ( ProjectCardViewModel lProject in Projects )
			{
				lProject.RefreshFromResources();
			}
		}
	}
}
