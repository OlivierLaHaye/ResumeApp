using ResumeApp.Services;
using ResumeApp.ViewModels.Pages;
using Xunit;

namespace ResumeApp.Tests.ViewModels;

public sealed class OverviewPageViewModelTests
{
    private static OverviewPageViewModel Create()
    {
        return new OverviewPageViewModel( new ResourcesService(), new ThemeService() );
    }

    [Fact]
    public void Constructor_InitializesHighlights()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.Highlights );
        Assert.Equal( 5, lViewModel.Highlights.Count );
    }

    [Fact]
    public void Constructor_InitializesDesignSystemPoints()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.DesignSystemPoints );
        Assert.Equal( 4, lViewModel.DesignSystemPoints.Count );
    }

    [Fact]
    public void Constructor_InitializesCoreSkillsLines()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.CoreSkillsLines );
        Assert.Equal( 6, lViewModel.CoreSkillsLines.Count );
    }

    [Fact]
    public void ComposeEmailCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.ComposeEmailCommand );
    }

    [Fact]
    public void OpenUrlCommand_NotNull()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.OpenUrlCommand );
    }

    [StaFact]
    public void ComposeEmailCommand_CanExecute_NonEmptyString_ReturnsTrue()
    {
        var lViewModel = Create();

        Assert.True( lViewModel.ComposeEmailCommand.CanExecute( "test@test.com" ) );
    }

    [StaFact]
    public void ComposeEmailCommand_CanExecute_EmptyString_ReturnsFalse()
    {
        var lViewModel = Create();

        Assert.False( lViewModel.ComposeEmailCommand.CanExecute( "" ) );
        Assert.False( lViewModel.ComposeEmailCommand.CanExecute( "  " ) );
        Assert.False( lViewModel.ComposeEmailCommand.CanExecute( null ) );
    }

    [StaFact]
    public void ComposeEmailCommand_CanExecute_NonString_ReturnsFalse()
    {
        var lViewModel = Create();

        Assert.False( lViewModel.ComposeEmailCommand.CanExecute( 42 ) );
    }

    [StaFact]
    public void ComposeEmailCommand_Execute_NonStringParam_DoesNotThrow()
    {
        var lViewModel = Create();

        var lException = Record.Exception( () => lViewModel.ComposeEmailCommand.Execute( 42 ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void ComposeEmailCommand_Execute_NullParam_DoesNotThrow()
    {
        var lViewModel = Create();

        var lException = Record.Exception( () => lViewModel.ComposeEmailCommand.Execute( null ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void OpenUrlCommand_Execute_NonStringParam_DoesNotThrow()
    {
        var lViewModel = Create();

        var lException = Record.Exception( () => lViewModel.OpenUrlCommand.Execute( null ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void OpenUrlCommand_Execute_InvalidUrl_DoesNotThrow()
    {
        var lViewModel = Create();

        var lException = Record.Exception( () => lViewModel.OpenUrlCommand.Execute( "" ) );

        Assert.Null( lException );
    }

    [Fact]
    public void Properties_ReturnResourceStrings()
    {
        var lViewModel = Create();

        Assert.NotNull( lViewModel.ComposeEmailButtonText );
        Assert.NotNull( lViewModel.OpenUrlButtonText );
        Assert.NotNull( lViewModel.FullNameText );
        Assert.NotNull( lViewModel.TargetTitlesText );
        Assert.NotNull( lViewModel.LocationText );
        Assert.NotNull( lViewModel.WorkPreferenceText );
        Assert.NotNull( lViewModel.EmailText );
        Assert.NotNull( lViewModel.LinkedInText );
        Assert.NotNull( lViewModel.GitHubText );
        Assert.NotNull( lViewModel.PortfolioText );
        Assert.NotNull( lViewModel.SummaryText );
    }

    [Fact]
    public void ResourcesServicePropertyChanged_RaisesPropertyChanged()
    {
        var lResourcesService = new ResourcesService();
        var lViewModel = new OverviewPageViewModel( lResourcesService, new ThemeService() );
        var lRaisedProperties = new List<string?>();
        lViewModel.PropertyChanged += ( _, pArgs ) => lRaisedProperties.Add( pArgs.PropertyName );

        lResourcesService.SetLanguage( AppLanguage.FrenchCanada );

        Assert.Contains( "FullNameText", lRaisedProperties );
        Assert.Contains( "ComposeEmailButtonText", lRaisedProperties );
    }
}
