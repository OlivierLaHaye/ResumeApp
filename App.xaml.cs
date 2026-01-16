// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using ResumeApp.ViewModels;
using ResumeApp.ViewModels.Pages;
using System.Windows;
using System.Windows.Threading;

namespace ResumeApp;

public partial class App
{
	private ThemeService? mThemeService;

	private bool mHasQueuedMainWindowInitialization;

	private static void QueueBackgroundImagePreload( ProjectsPageViewModel pProjectsPageViewModel, PhotographyPageViewModel pPhotographyPageViewModel )
	{
		if ( Current?.Dispatcher is not Dispatcher lDispatcher )
		{
			return;
		}

		lDispatcher.BeginInvoke( new Action( () =>
		{
			pProjectsPageViewModel.QueueImagesPreloadForAll();
			pPhotographyPageViewModel.QueueImagesPreloadForAll();
		} ), DispatcherPriority.ContextIdle );
	}

	private static async Task<ResourcesService> CreateInitializedResourcesServiceAsync()
	{
		var lResourcesService = new ResourcesService();

		if ( await TryRunAsync( () => Task.Run( () => lResourcesService.Initialize() ) ) )
		{
			return lResourcesService;
		}

		TryRun( () => lResourcesService.Initialize() );
		return lResourcesService;
	}

	private static bool TryCreatePageViewModels(
		ResourcesService pResourcesService,
		ThemeService pThemeService,
		out PageViewModels pPageViewModels )
	{
		return TryCreateValue(
			() => new PageViewModels(
				new OverviewPageViewModel( pResourcesService, pThemeService ),
				new ExperiencePageViewModel( pResourcesService, pThemeService ),
				new SkillsPageViewModel( pResourcesService, pThemeService ),
				new ProjectsPageViewModel( pResourcesService, pThemeService ),
				new PhotographyPageViewModel( pResourcesService, pThemeService ),
				new EducationPageViewModel( pResourcesService, pThemeService ) ),
			out pPageViewModels );
	}

	private static bool TryCreateMainViewModel(
		ResourcesService pResourcesService,
		ThemeService pThemeService,
		PageViewModels pPageViewModels,
		out MainViewModel pMainViewModel )
	{
		return TryCreateReference(
			() => new MainViewModel(
				pResourcesService,
				pThemeService,
				pPageViewModels.OverviewPageViewModel,
				pPageViewModels.ExperiencePageViewModel,
				pPageViewModels.SkillsPageViewModel,
				pPageViewModels.ProjectsPageViewModel,
				pPageViewModels.PhotographyPageViewModel,
				pPageViewModels.EducationPageViewModel ),
			out pMainViewModel );
	}

	private static bool TryRun( Action pAction )
	{
		try
		{
			pAction();
			return true;
		}
		catch ( Exception )
		{
			// ignored
		}

		return false;
	}

	private static async Task<bool> TryRunAsync( Func<Task> pActionAsync )
	{
		try
		{
			await pActionAsync();
			return true;
		}
		catch ( Exception )
		{
			// ignored
		}

		return false;
	}

	private static bool TryCreateValue<T>( Func<T> pFactory, out T pValue )
	{
		try
		{
			pValue = pFactory();
			return true;
		}
		catch ( Exception )
		{
			// ignored
		}

		pValue = default!;
		return false;
	}

	private static bool TryCreateReference<T>( Func<T> pFactory, out T pValue ) where T : class
	{
		try
		{
			pValue = pFactory();
			return true;
		}
		catch ( Exception )
		{
			// ignored
		}

		pValue = null!;
		return false;
	}

	protected override void OnStartup( StartupEventArgs pStartupEventArgs )
	{
		base.OnStartup( pStartupEventArgs );

		InitializeThemeService();

		var lMainWindow = new MainWindow();
		MainWindow = lMainWindow;

		lMainWindow.ContentRendered += OnMainWindowContentRenderedAsync;
		lMainWindow.Show();
	}

	private async void OnMainWindowContentRenderedAsync( object? pSender, EventArgs pArgs )
	{
		try
		{
			if ( mHasQueuedMainWindowInitialization || pSender is not MainWindow lMainWindow )
			{
				return;
			}

			mHasQueuedMainWindowInitialization = true;

			lMainWindow.ContentRendered -= OnMainWindowContentRenderedAsync;

			await InitializeMainWindowAsync( lMainWindow );
		}
		catch ( Exception )
		{
			// ignored
		}
	}

	private void InitializeThemeService()
	{
		mThemeService = new ThemeService();

		try
		{
			mThemeService.Initialize( this );
		}
		catch ( Exception )
		{
			// ignored
		}
	}

	private async Task InitializeMainWindowAsync( FrameworkElement pMainWindow )
	{
		if ( mThemeService is not ThemeService lThemeService )
		{
			return;
		}

		ResourcesService lResourcesService = await CreateInitializedResourcesServiceAsync();

		if ( !TryCreatePageViewModels( lResourcesService, lThemeService, out PageViewModels lPageViewModels )
		     || !TryCreateMainViewModel( lResourcesService, lThemeService, lPageViewModels, out MainViewModel lMainViewModel ) )
		{
			return;
		}

		pMainWindow.DataContext = lMainViewModel;

		QueueBackgroundImagePreload( lPageViewModels.ProjectsPageViewModel, lPageViewModels.PhotographyPageViewModel );
	}

	private readonly record struct PageViewModels(
		OverviewPageViewModel OverviewPageViewModel,
		ExperiencePageViewModel ExperiencePageViewModel,
		SkillsPageViewModel SkillsPageViewModel,
		ProjectsPageViewModel ProjectsPageViewModel,
		PhotographyPageViewModel PhotographyPageViewModel,
		EducationPageViewModel EducationPageViewModel );
}
