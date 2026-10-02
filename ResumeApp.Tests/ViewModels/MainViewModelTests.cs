using ResumeApp.Services;
using ResumeApp.ViewModels;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class MainViewModelTests
{
    private static MainViewModel Create( ResourcesService? pResourcesService = null, ThemeService? pThemeService = null )
    {
        var lResourcesService = pResourcesService ?? new ResourcesService();
        var lThemeService = pThemeService ?? new ThemeService();

        return new MainViewModel(
            lResourcesService,
            lThemeService,
            new OverviewPageViewModel( lResourcesService, lThemeService ),
            new ExperiencePageViewModel( lResourcesService, lThemeService ),
            new SkillsPageViewModel( lResourcesService, lThemeService ),
            new ProjectsPageViewModel( lResourcesService, lThemeService ),
            new PhotographyPageViewModel( lResourcesService, lThemeService ),
            new EducationPageViewModel( lResourcesService, lThemeService ) );
    }

    [Fact]
    public void Constructor_NullParameters_ThrowsArgumentNullException()
    {
        var lResources = new ResourcesService();
        var lTheme = new ThemeService();
        var lOverview = new OverviewPageViewModel( lResources, lTheme );
        var lExperience = new ExperiencePageViewModel( lResources, lTheme );
        var lSkills = new SkillsPageViewModel( lResources, lTheme );
        var lProjects = new ProjectsPageViewModel( lResources, lTheme );
        var lPhotography = new PhotographyPageViewModel( lResources, lTheme );
        var lEducation = new EducationPageViewModel( lResources, lTheme );

        Assert.Throws<ArgumentNullException>( () => new MainViewModel( lResources, lTheme, null!, lExperience, lSkills, lProjects, lPhotography, lEducation ) );
        Assert.Throws<ArgumentNullException>( () => new MainViewModel( lResources, lTheme, lOverview, null!, lSkills, lProjects, lPhotography, lEducation ) );
        Assert.Throws<ArgumentNullException>( () => new MainViewModel( lResources, lTheme, lOverview, lExperience, null!, lProjects, lPhotography, lEducation ) );
        Assert.Throws<ArgumentNullException>( () => new MainViewModel( lResources, lTheme, lOverview, lExperience, lSkills, null!, lPhotography, lEducation ) );
        Assert.Throws<ArgumentNullException>( () => new MainViewModel( lResources, lTheme, lOverview, lExperience, lSkills, lProjects, null!, lEducation ) );
        Assert.Throws<ArgumentNullException>( () => new MainViewModel( lResources, lTheme, lOverview, lExperience, lSkills, lProjects, lPhotography, null! ) );
    }

    [Fact]
    public void Constructor_SetsChildViewModels()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.OverviewPageViewModel );
        Assert.NotNull( lViewModel.ExperiencePageViewModel );
        Assert.NotNull( lViewModel.SkillsPageViewModel );
        Assert.NotNull( lViewModel.ProjectsPageViewModel );
        Assert.NotNull( lViewModel.PhotographyPageViewModel );
        Assert.NotNull( lViewModel.EducationPageViewModel );
    }

    [Fact]
    public void IsDarkThemeActive_DefaultIsFalse()
    {
        var lViewModel = Create();

        Assert.False( lViewModel.IsDarkThemeActive );
    }

    [Fact]
    public void IsFrenchLanguageActive_DefaultIsFalse()
    {
        var lViewModel = Create();

        Assert.False( lViewModel.IsFrenchLanguageActive );
    }

    [Fact]
    public void ActiveLanguageDisplayName_IsNotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.ActiveLanguageDisplayName );
    }

    [Fact]
    public void SelectedLanguage_Default_IsEnglish()
    {
        var lViewModel = Create();

        Assert.Equal( AppLanguage.EnglishCanada, lViewModel.SelectedLanguage );
    }

    [Fact]
    public void SelectedLanguage_SetFrench_RaisesPropertyChanged()
    {
        var lViewModel = Create();
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lViewModel.SelectedLanguage = AppLanguage.FrenchCanada;

        Assert.Contains( "SelectedLanguage", lRaisedProperties );
        Assert.Contains( "IsFrenchLanguageActive", lRaisedProperties );
        Assert.True( lViewModel.IsFrenchLanguageActive );
    }

    [Fact]
    public void SelectedLanguage_SameValue_DoesNotChange()
    {
        var lViewModel = Create();
        bool lChanged = false;
        lViewModel.PropertyChanged += ( _, _ ) => lChanged = true;

        lViewModel.SelectedLanguage = AppLanguage.EnglishCanada;

        Assert.False( lChanged );
    }

    [StaFact]
    public void SetLanguageCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.SetLanguageCommand );
    }

    [StaFact]
    public void SetLanguageCommand_SetsFrench()
    {
        var lViewModel = Create();

        lViewModel.SetLanguageCommand.Execute( AppLanguage.FrenchCanada );

        Assert.Equal( "fr-CA", lViewModel.ResourcesService.ActiveCulture.Name );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_ItemArray_RaisesActiveLanguageDisplayName()
    {
        var lResourcesService = new ResourcesService();
        var lViewModel = Create( pResourcesService: lResourcesService );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "ActiveLanguageDisplayName", lRaisedProperties );
        Assert.Contains( "IsFrenchLanguageActive", lRaisedProperties );
    }

    [Fact]
    public void ThemeServicePropertyChanged_RaisesIsDarkThemeActive()
    {
        var lResourcesService = new ResourcesService();
        var lThemeService = new ThemeService();
        var lViewModel = Create( pResourcesService: lResourcesService, pThemeService: lThemeService );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lThemeService.ActiveTheme = AppTheme.Dark;

        Assert.Contains( "IsDarkThemeActive", lRaisedProperties );
        Assert.True( lViewModel.IsDarkThemeActive );
    }

    [Fact]
    public void ThemeServicePropertyChanged_BackToLight_RaisesIsDarkThemeActive()
    {
        var lResourcesService = new ResourcesService();
        var lThemeService = new ThemeService();
        var lViewModel = Create( pResourcesService: lResourcesService, pThemeService: lThemeService );
        lThemeService.ActiveTheme = AppTheme.Dark;
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lThemeService.ActiveTheme = AppTheme.Light;

        Assert.Contains( "IsDarkThemeActive", lRaisedProperties );
        Assert.False( lViewModel.IsDarkThemeActive );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_NonItemArray_DoesNotRaiseActiveLanguageDisplayName()
    {
        var lResourcesService = new ResourcesService();
        var lThemeService = new ThemeService();
        var lViewModel = Create( pResourcesService: lResourcesService, pThemeService: lThemeService );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        int lActiveLanguageCount = lRaisedProperties.Count( pName => pName == "ActiveLanguageDisplayName" );
        Assert.Equal( 1, lActiveLanguageCount );
    }

    [Fact]
    public void Constructor_FrenchCulture_DefaultsToFrench()
    {
        var lResourcesService = new ResourcesService();
        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );
        var lThemeService = new ThemeService();

        var lViewModel = new MainViewModel(
            lResourcesService,
            lThemeService,
            new OverviewPageViewModel( lResourcesService, lThemeService ),
            new ExperiencePageViewModel( lResourcesService, lThemeService ),
            new SkillsPageViewModel( lResourcesService, lThemeService ),
            new ProjectsPageViewModel( lResourcesService, lThemeService ),
            new PhotographyPageViewModel( lResourcesService, lThemeService ),
            new EducationPageViewModel( lResourcesService, lThemeService ) );

        Assert.Equal( AppLanguage.FrenchCanada, lViewModel.SelectedLanguage );
        Assert.True( lViewModel.IsFrenchLanguageActive );
    }

    [StaFact]
    public void SetLanguageCommand_SwitchesLanguageStateOnce()
    {
        var lViewModel = Create();
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lViewModel.SetLanguageCommand.Execute( AppLanguage.FrenchCanada );

        Assert.Equal( AppLanguage.FrenchCanada, lViewModel.SelectedLanguage );
        Assert.True( lViewModel.IsFrenchLanguageActive );
        Assert.False( lViewModel.IsEnglishLanguageActive );
        Assert.Equal( 1, lRaisedProperties.Count( pName => pName == nameof( MainViewModel.IsFrenchLanguageActive ) ) );
        Assert.Equal( 1, lRaisedProperties.Count( pName => pName == nameof( MainViewModel.IsEnglishLanguageActive ) ) );
    }

    [StaFact]
    public void SetLanguageCommand_SameLanguage_RaisesNothing()
    {
        var lViewModel = Create();
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lViewModel.SetLanguageCommand.Execute( AppLanguage.EnglishCanada );

        Assert.Empty( lRaisedProperties );
        Assert.True( lViewModel.IsEnglishLanguageActive );
    }

    [StaFact]
    public void SetLanguageCommand_RoundTrip_RestoresEnglish()
    {
        var lViewModel = Create();

        lViewModel.SetLanguageCommand.Execute( AppLanguage.FrenchCanada );
        lViewModel.SetLanguageCommand.Execute( AppLanguage.EnglishCanada );

        Assert.Equal( "en-CA", lViewModel.ResourcesService.ActiveCulture.Name );
        Assert.True( lViewModel.IsEnglishLanguageActive );
        Assert.False( lViewModel.IsFrenchLanguageActive );
    }

    [Fact]
    public void ExternalLanguageChange_SynchronizesSelectedLanguage()
    {
        var lResourcesService = new ResourcesService();
        var lViewModel = Create( pResourcesService: lResourcesService );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Equal( AppLanguage.FrenchCanada, lViewModel.SelectedLanguage );
        Assert.True( lViewModel.IsFrenchLanguageActive );
        Assert.False( lViewModel.IsEnglishLanguageActive );
    }

    [StaFact]
    public void SetThemeCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.SetThemeCommand );
    }

    [Fact]
    public void IsLightThemeActive_DefaultIsTrue()
    {
        var lViewModel = Create();

        Assert.True( lViewModel.IsLightThemeActive );
    }

    [Fact]
    public void ThemeServicePropertyChanged_RaisesBothThemeStates()
    {
        var lThemeService = new ThemeService();
        var lViewModel = Create( pThemeService: lThemeService );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lThemeService.ActiveTheme = AppTheme.Dark;

        Assert.Contains( nameof( MainViewModel.IsDarkThemeActive ), lRaisedProperties );
        Assert.Contains( nameof( MainViewModel.IsLightThemeActive ), lRaisedProperties );
        Assert.False( lViewModel.IsLightThemeActive );
    }
}
