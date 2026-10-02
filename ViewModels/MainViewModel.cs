// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using ResumeApp.Infrastructure;
using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using System.ComponentModel;
using System.Windows;

namespace ResumeApp.ViewModels
{
	public sealed class MainViewModel : ViewModelBase
	{
		public OverviewPageViewModel OverviewPageViewModel { get; }

		public ExperiencePageViewModel ExperiencePageViewModel { get; }

		public SkillsPageViewModel SkillsPageViewModel { get; }

		public ProjectsPageViewModel ProjectsPageViewModel { get; }

		public PhotographyPageViewModel PhotographyPageViewModel { get; }

		public EducationPageViewModel EducationPageViewModel { get; }

		public RelayCommand<AppLanguage> SetLanguageCommand { get; }

		public RelayCommand<AppTheme> SetThemeCommand { get; }

		public bool IsDarkThemeActive => ThemeService.IsDarkThemeActive;

		public bool IsLightThemeActive => !ThemeService.IsDarkThemeActive;

		public bool IsFrenchLanguageActive => SelectedLanguage == AppLanguage.FrenchCanada;

		public bool IsEnglishLanguageActive => SelectedLanguage == AppLanguage.EnglishCanada;

		public string ActiveLanguageDisplayName => ResourcesService.ActiveLanguageDisplayName;

		private AppLanguage mSelectedLanguage;
		public AppLanguage SelectedLanguage
		{
			get => mSelectedLanguage;
			set
			{
				if ( !SetSelectedLanguageState( value ) )
				{
					return;
				}

				ResourcesService.SetLanguage( value );
			}
		}

		public MainViewModel(
			ResourcesService pResourcesService,
			ThemeService pThemeService,
			OverviewPageViewModel pOverviewPageViewModel,
			ExperiencePageViewModel pExperiencePageViewModel,
			SkillsPageViewModel pSkillsPageViewModel,
			ProjectsPageViewModel pProjectsPageViewModel,
			PhotographyPageViewModel pPhotographyPageViewModel,
			EducationPageViewModel pEducationPageViewModel )
			: base( pResourcesService, pThemeService )
		{
			OverviewPageViewModel = pOverviewPageViewModel ?? throw new ArgumentNullException( nameof( pOverviewPageViewModel ) );
			ExperiencePageViewModel = pExperiencePageViewModel ?? throw new ArgumentNullException( nameof( pExperiencePageViewModel ) );
			SkillsPageViewModel = pSkillsPageViewModel ?? throw new ArgumentNullException( nameof( pSkillsPageViewModel ) );
			ProjectsPageViewModel = pProjectsPageViewModel ?? throw new ArgumentNullException( nameof( pProjectsPageViewModel ) );
			PhotographyPageViewModel = pPhotographyPageViewModel ?? throw new ArgumentNullException( nameof( pPhotographyPageViewModel ) );
			EducationPageViewModel = pEducationPageViewModel ?? throw new ArgumentNullException( nameof( pEducationPageViewModel ) );

			SetLanguageCommand = new RelayCommand<AppLanguage>( pLanguage => SelectedLanguage = pLanguage );
			SetThemeCommand = new RelayCommand<AppTheme>( SetTheme );

			mSelectedLanguage = GetLanguageForCulture( pResourcesService.ActiveCulture.Name );

			pResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
			pThemeService.PropertyChanged += OnThemeServicePropertyChanged;
		}

		private static AppLanguage GetLanguageForCulture( string? pCultureName )
		{
			return pCultureName?.StartsWith( "fr", StringComparison.OrdinalIgnoreCase ) == true
				? AppLanguage.FrenchCanada
				: AppLanguage.EnglishCanada;
		}

		private bool SetSelectedLanguageState( AppLanguage pLanguage )
		{
			if ( !SetProperty( ref mSelectedLanguage, pLanguage, nameof( SelectedLanguage ) ) )
			{
				return false;
			}

			RaisePropertyChanged( nameof( IsFrenchLanguageActive ) );
			RaisePropertyChanged( nameof( IsEnglishLanguageActive ) );
			return true;
		}

		private void OnResourcesServicePropertyChanged( object? pSender, PropertyChangedEventArgs pEventArgs )
		{
			if ( !string.Equals( pEventArgs.PropertyName, ResourcesService.IndexerPropertyName, StringComparison.Ordinal ) )
			{
				return;
			}

			RaisePropertyChanged( nameof( ActiveLanguageDisplayName ) );
			SetSelectedLanguageState( GetLanguageForCulture( ResourcesService.ActiveCulture.Name ) );
		}

		private void OnThemeServicePropertyChanged( object? pSender, PropertyChangedEventArgs pEventArgs )
		{
			RaisePropertyChanged( nameof( IsDarkThemeActive ) );
			RaisePropertyChanged( nameof( IsLightThemeActive ) );
		}

		[ExcludeFromCodeCoverage( Justification = "Delegates to ThemeService.SetTheme(Application.Current) which requires a running WPF Application." )]
		private void SetTheme( AppTheme pTheme )
		{
			if ( Application.Current is not Application lApplication )
			{
				return;
			}

			ThemeService.SetTheme( lApplication, pTheme );
		}
	}
}
