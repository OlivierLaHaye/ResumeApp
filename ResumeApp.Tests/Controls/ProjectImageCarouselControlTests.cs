using System.Windows;
using ResumeApp.Controls;
using Xunit;

namespace ResumeApp.Tests.Controls;

public sealed class ProjectImageCarouselControlTests
{
    private static void EnsureTokensLoaded()
    {
        if ( Application.Current == null )
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        var lTokensUri = new Uri( "pack://application:,,,/ResumeApp;component/Resources/Tokens.xaml", UriKind.Absolute );

        if ( !Application.Current!.Resources.MergedDictionaries.Any( pDictionary =>
                pDictionary.Source == lTokensUri ) )
        {
            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary { Source = lTokensUri } );
        }
    }

    [StaFact]
    public void DependencyProperties_AreRegistered()
    {
        Assert.NotNull( ProjectImageCarouselControl.sImagesProperty );
        Assert.NotNull( ProjectImageCarouselControl.sSelectedIndexProperty );
        Assert.NotNull( ProjectImageCarouselControl.sPlaceholderTextProperty );
        Assert.NotNull( ProjectImageCarouselControl.sIsFullscreenProperty );
        Assert.NotNull( ProjectImageCarouselControl.sIsOpenOnClickEnabledProperty );
    }

    [StaFact]
    public void Constructor_DoesNotThrow()
    {
        EnsureTokensLoaded();

        var lException = Record.Exception( () => new ProjectImageCarouselControl() );

        Assert.Null( lException );
    }

    [StaFact]
    public void Images_DefaultIsNull()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.Null( lControl.Images );
    }

    [StaFact]
    public void SelectedIndex_DefaultIsZero()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.Equal( 0, lControl.SelectedIndex );
    }

    [StaFact]
    public void IsFullscreen_DefaultIsFalse()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.False( lControl.IsFullscreen );
    }

    [StaFact]
    public void IsOpenOnClickEnabled_DefaultIsFalse()
    {
        EnsureTokensLoaded();
        var lControl = new ProjectImageCarouselControl();

        Assert.False( lControl.IsOpenOnClickEnabled );
    }
}
