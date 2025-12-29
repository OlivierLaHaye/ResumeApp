// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using ResumeApp.ViewModels;
using ResumeApp.ViewModels.Pages;
using System.Windows;

namespace ResumeApp
{
	public partial class App
	{
		private ThemeService mThemeService;
		private ResourcesService mResourcesService;

		protected override void OnStartup( StartupEventArgs pStartupEventArgs )
		{
			base.OnStartup( pStartupEventArgs );

			mThemeService = new ThemeService();
			mResourcesService = new ResourcesService();

			mResourcesService.Initialize();
			mThemeService.Initialize( this );

			OverviewPageViewModel lOverviewPageViewModel = new OverviewPageViewModel( mResourcesService, mThemeService );
			ExperiencePageViewModel lExperiencePageViewModel = new ExperiencePageViewModel( mResourcesService, mThemeService );
			SkillsPageViewModel lSkillsPageViewModel = new SkillsPageViewModel( mResourcesService, mThemeService );
			ProjectsPageViewModel lProjectsPageViewModel = new ProjectsPageViewModel( mResourcesService, mThemeService );
			PhotographyPageViewModel lPhotographyPageViewModel = new PhotographyPageViewModel( mResourcesService, mThemeService );
			EducationPageViewModel lEducationPageViewModel = new EducationPageViewModel( mResourcesService, mThemeService );

			MainViewModel lMainViewModel = new MainViewModel(
				mResourcesService,
				mThemeService,
				lOverviewPageViewModel,
				lExperiencePageViewModel,
				lSkillsPageViewModel,
				lProjectsPageViewModel,
				lPhotographyPageViewModel,
				lEducationPageViewModel );

			MainWindow lMainWindow = new MainWindow
			{
				DataContext = lMainViewModel
			};

			MainWindow = lMainWindow;
			lMainWindow.Show();
		}
	}
}
