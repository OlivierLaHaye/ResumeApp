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
    public void IsTopBarCollapsed_DefaultIsFalse()
    {
        var lViewModel = Create();

        Assert.False( lViewModel.IsTopBarCollapsed );
    }

    [Fact]
    public void IsTopBarCollapsed_SetTrue_RaisesPropertyChanged()
    {
        var lViewModel = Create();
        string? lRaisedPropertyName = null;
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedPropertyName = pArgs.PropertyName;

        lViewModel.IsTopBarCollapsed = true;

        Assert.True( lViewModel.IsTopBarCollapsed );
        Assert.Equal( "IsTopBarCollapsed", lRaisedPropertyName );
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
    public void CheckBoxThemeCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.CheckBoxThemeCommand );
    }

    [StaFact]
    public void CheckBoxLanguageCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.CheckBoxLanguageCommand );
    }

    [StaFact]
    public void SetLanguageCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.SetLanguageCommand );
    }

    [StaFact]
    public void CheckBoxLanguageCommand_TogglesLanguage()
    {
        var lViewModel = Create();
        Assert.Equal( AppLanguage.EnglishCanada, lViewModel.SelectedLanguage );

        lViewModel.CheckBoxLanguageCommand.Execute( null );

        Assert.Equal( AppLanguage.FrenchCanada, lViewModel.SelectedLanguage );
    }

    [StaFact]
    public void CheckBoxLanguageCommand_TogglesBack()
    {
        var lViewModel = Create();
        lViewModel.CheckBoxLanguageCommand.Execute( null );
        Assert.Equal( AppLanguage.FrenchCanada, lViewModel.SelectedLanguage );

        lViewModel.CheckBoxLanguageCommand.Execute( null );

        Assert.Equal( AppLanguage.EnglishCanada, lViewModel.SelectedLanguage );
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

        // Setting language raises multiple PropertyChanged events including "ActiveCulture"
        // which should be filtered out by the OnResourcesServicePropertyChanged guard
        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        // "ActiveLanguageDisplayName" should appear (from Item[] event) but not duplicated for non-Item[] events
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
}
