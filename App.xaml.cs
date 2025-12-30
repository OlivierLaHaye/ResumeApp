// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using ResumeApp.ViewModels;
using ResumeApp.ViewModels.Pages;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ResumeApp
{
	public partial class App
	{
		private ThemeService mThemeService;
		private ResourcesService mResourcesService;

		private bool mHasQueuedMainWindowInitialization;

		private static void QueueBackgroundImagePreload( ProjectsPageViewModel pProjectsPageViewModel, PhotographyPageViewModel pPhotographyPageViewModel )
		{
			Dispatcher lDispatcher = Current?.Dispatcher;

			lDispatcher?.BeginInvoke( new Action( () =>
			{
				pProjectsPageViewModel?.QueueImagesPreloadForAll();
				pPhotographyPageViewModel?.QueueImagesPreloadForAll();
			} ), DispatcherPriority.ContextIdle );
		}

		protected override void OnStartup( StartupEventArgs pStartupEventArgs )
		{
			base.OnStartup( pStartupEventArgs );

			mThemeService = new ThemeService();

			try
			{
				mThemeService.Initialize( this );
			}
			catch ( Exception )
			{
				// ignored
			}

			var lMainWindow = new MainWindow();
			MainWindow = lMainWindow;

			lMainWindow.ContentRendered += OnMainWindowContentRendered;
			lMainWindow.Show();
		}

		private async void OnMainWindowContentRendered( object pSender, EventArgs pArgs )
		{
			if ( mHasQueuedMainWindowInitialization )
			{
				return;
			}

			mHasQueuedMainWindowInitialization = true;

			if ( !( pSender is MainWindow lMainWindow ) )
			{
				return;
			}

			lMainWindow.ContentRendered -= OnMainWindowContentRendered;

			await InitializeMainWindowAsync( lMainWindow );
		}

		private async Task InitializeMainWindowAsync( FrameworkElement pMainWindow )
		{
			if ( pMainWindow == null )
			{
				return;
			}

			var lResourcesService = new ResourcesService();

			bool lHasInitializedResourcesInBackground = false;

			try
			{
				await Task.Run( () => lResourcesService.Initialize() );
				lHasInitializedResourcesInBackground = true;
			}
			catch ( Exception )
			{
				// ignored
			}

			if ( !lHasInitializedResourcesInBackground )
			{
				try
				{
					lResourcesService.Initialize();
				}
				catch ( Exception )
				{
					// ignored
				}
			}

			mResourcesService = lResourcesService;

			OverviewPageViewModel lOverviewPageViewModel = null;
			ExperiencePageViewModel lExperiencePageViewModel = null;
			SkillsPageViewModel lSkillsPageViewModel = null;
			ProjectsPageViewModel lProjectsPageViewModel = null;
			PhotographyPageViewModel lPhotographyPageViewModel = null;
			EducationPageViewModel lEducationPageViewModel = null;

			try
			{
				lOverviewPageViewModel = new OverviewPageViewModel( mResourcesService, mThemeService );
				lExperiencePageViewModel = new ExperiencePageViewModel( mResourcesService, mThemeService );
				lSkillsPageViewModel = new SkillsPageViewModel( mResourcesService, mThemeService );
				lProjectsPageViewModel = new ProjectsPageViewModel( mResourcesService, mThemeService );
				lPhotographyPageViewModel = new PhotographyPageViewModel( mResourcesService, mThemeService );
				lEducationPageViewModel = new EducationPageViewModel( mResourcesService, mThemeService );
			}
			catch ( Exception )
			{
				// ignored
			}

			if ( lOverviewPageViewModel == null
				|| lExperiencePageViewModel == null
				|| lSkillsPageViewModel == null
				|| lProjectsPageViewModel == null
				|| lPhotographyPageViewModel == null
				|| lEducationPageViewModel == null )
			{
				return;
			}

			MainViewModel lMainViewModel = null;

			try
			{
				lMainViewModel = new MainViewModel(
					mResourcesService,
					mThemeService,
					lOverviewPageViewModel,
					lExperiencePageViewModel,
					lSkillsPageViewModel,
					lProjectsPageViewModel,
					lPhotographyPageViewModel,
					lEducationPageViewModel );
			}
			catch ( Exception )
			{
				// ignored
			}

			if ( lMainViewModel == null )
			{
				return;
			}

			pMainWindow.DataContext = lMainViewModel;

			QueueBackgroundImagePreload( lProjectsPageViewModel, lPhotographyPageViewModel );
		}
	}
}
