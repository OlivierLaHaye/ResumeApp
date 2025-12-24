using ResumeApp.Infrastructure;
using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using System;
using System.Windows;

namespace ResumeApp.ViewModels
{
	public sealed class MainViewModel : ViewModelBase
	{
		public OverviewPageViewModel OverviewPageViewModel { get; }

		public ExperiencePageViewModel ExperiencePageViewModel { get; }

		public SkillsPageViewModel SkillsPageViewModel { get; }

		public ProjectsPageViewModel ProjectsPageViewModel { get; }

		public EducationPageViewModel EducationPageViewModel { get; }

		public RelayCommand CheckBoxThemeCommand { get; }

		public RelayCommand CheckBoxLanguageCommand { get; }

		public RelayCommand<AppLanguage> SetLanguageCommand { get; }

		public bool IsDarkThemeActive => ThemeService.IsDarkThemeActive;

		public bool IsFrenchLanguageActive => SelectedLanguage == AppLanguage.FrenchCanada;

		public string ActiveLanguageDisplayName => ResourcesService.ActiveLanguageDisplayName;

		private bool mIsTopBarCollapsed;
		public bool IsTopBarCollapsed
		{
			get => mIsTopBarCollapsed;
			set => SetProperty( ref mIsTopBarCollapsed, value );
		}

		private AppLanguage mSelectedLanguage;
		public AppLanguage SelectedLanguage
		{
			get => mSelectedLanguage;
			set
			{
				if ( !SetProperty( ref mSelectedLanguage, value ) )
				{
					return;
				}

				RaisePropertyChanged( nameof( IsFrenchLanguageActive ) );
				SetLanguage( value );
			}
		}

		public MainViewModel(
			ResourcesService pResourcesService,
			ThemeService pThemeService,
			OverviewPageViewModel pOverviewPageViewModel,
			ExperiencePageViewModel pExperiencePageViewModel,
			SkillsPageViewModel pSkillsPageViewModel,
			ProjectsPageViewModel pProjectsPageViewModel,
			EducationPageViewModel pEducationPageViewModel )
			: base( pResourcesService, pThemeService )
		{
			OverviewPageViewModel = pOverviewPageViewModel ?? throw new ArgumentNullException( nameof( pOverviewPageViewModel ) );
			ExperiencePageViewModel = pExperiencePageViewModel ?? throw new ArgumentNullException( nameof( pExperiencePageViewModel ) );
			SkillsPageViewModel = pSkillsPageViewModel ?? throw new ArgumentNullException( nameof( pSkillsPageViewModel ) );
			ProjectsPageViewModel = pProjectsPageViewModel ?? throw new ArgumentNullException( nameof( pProjectsPageViewModel ) );
			EducationPageViewModel = pEducationPageViewModel ?? throw new ArgumentNullException( nameof( pEducationPageViewModel ) );

			CheckBoxThemeCommand = new RelayCommand( ToggleTheme );
			CheckBoxLanguageCommand = new RelayCommand( ToggleLanguage );
			SetLanguageCommand = new RelayCommand<AppLanguage>( SetLanguage );

			mSelectedLanguage = pResourcesService.ActiveCulture.Name.StartsWith( "fr", StringComparison.OrdinalIgnoreCase )
				? AppLanguage.FrenchCanada
				: AppLanguage.EnglishCanada;

			mIsTopBarCollapsed = false;

			pResourcesService.PropertyChanged += ( pSender, pEventArgs ) =>
			{
				RaisePropertyChanged( nameof( ActiveLanguageDisplayName ) );
				RaisePropertyChanged( nameof( IsFrenchLanguageActive ) );
			};

			pThemeService.PropertyChanged += ( pSender, pEventArgs ) => RaisePropertyChanged( nameof( IsDarkThemeActive ) );
		}

		private void ToggleTheme()
		{
			ThemeService.ToggleTheme( Application.Current );
		}

		private void ToggleLanguage()
		{
			SelectedLanguage = SelectedLanguage == AppLanguage.FrenchCanada
				? AppLanguage.EnglishCanada
				: AppLanguage.FrenchCanada;
		}

		private void SetLanguage( AppLanguage pLanguage )
		{
			ResourcesService.SetLanguage( pLanguage );
		}
	}
}
