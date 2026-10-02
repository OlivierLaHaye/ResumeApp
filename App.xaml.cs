// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using ResumeApp.Services;
using ResumeApp.ViewModels;
using ResumeApp.ViewModels.Pages;
using System.Windows;
using System.Windows.Threading;

namespace ResumeApp;

[ExcludeFromCodeCoverage( Justification = "WPF Application entry point: OnStartup, Dispatcher, and async initialization require a running WPF Application instance with message loop." )]
public partial class App
{
	private const string StartupLoadingTextResourceKey = "StartupLoadingText";
	private const string StartupFailureTextResourceKey = "StartupFailureText";

	private ThemeService? mThemeService;

	private ResourcesService? mResourcesService;

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

	private static ResourcesService CreateInitializedResourcesService()
	{
		var lResourcesService = new ResourcesService();
		TryRun( lResourcesService.Initialize );
		return lResourcesService;
	}

	private static string GetResourceTextOrEmpty( ResourcesService? pResourcesService, string pResourceKey )
	{
		if ( pResourcesService is null )
		{
			return string.Empty;
		}

		try
		{
			return pResourcesService[ pResourceKey ];
		}
		catch ( Exception )
		{
			return string.Empty;
		}
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
		mResourcesService = CreateInitializedResourcesService();

		var lMainWindow = new MainWindow();
		MainWindow = lMainWindow;

		lMainWindow.ShowStartupStatus( GetResourceTextOrEmpty( mResourcesService, StartupLoadingTextResourceKey ) );
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

			if ( !await InitializeMainWindowAsync( lMainWindow ) )
			{
				ShowStartupFailure( lMainWindow );
			}
		}
		catch ( Exception )
		{
			ShowStartupFailure( pSender as MainWindow );
		}
	}

	private void ShowStartupFailure( MainWindow? pMainWindow )
	{
		if ( pMainWindow is null || pMainWindow.DataContext != null )
		{
			return;
		}

		pMainWindow.ShowStartupStatus( GetResourceTextOrEmpty( mResourcesService, StartupFailureTextResourceKey ) );
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

	private async Task<bool> InitializeMainWindowAsync( FrameworkElement pMainWindow )
	{
		if ( mThemeService is not ThemeService lThemeService || mResourcesService is not ResourcesService lResourcesService )
		{
			return false;
		}

		await Dispatcher.Yield( DispatcherPriority.Background );

		if ( !TryCreatePageViewModels( lResourcesService, lThemeService, out PageViewModels lPageViewModels )
		     || !TryCreateMainViewModel( lResourcesService, lThemeService, lPageViewModels, out MainViewModel lMainViewModel ) )
		{
			return false;
		}

		pMainWindow.DataContext = lMainViewModel;

		QueueBackgroundImagePreload( lPageViewModels.ProjectsPageViewModel, lPageViewModels.PhotographyPageViewModel );
		return true;
	}

	private readonly record struct PageViewModels(
		OverviewPageViewModel OverviewPageViewModel,
		ExperiencePageViewModel ExperiencePageViewModel,
		SkillsPageViewModel SkillsPageViewModel,
		ProjectsPageViewModel ProjectsPageViewModel,
		PhotographyPageViewModel PhotographyPageViewModel,
		EducationPageViewModel EducationPageViewModel );
}
