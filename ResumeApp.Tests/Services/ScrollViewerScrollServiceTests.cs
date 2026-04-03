using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

public sealed class ScrollViewerScrollServiceTests
{
    [StaFact]
    public void ScrollToTopCommand_IsNotNull()
    {
        Assert.NotNull( ScrollViewerScrollService.ScrollToTopCommand );
    }

    [StaFact]
    public void ScrollToBottomCommand_IsNotNull()
    {
        Assert.NotNull( ScrollViewerScrollService.ScrollToBottomCommand );
    }

    [StaFact]
    public void ScrollToTopCommand_IsSameInstanceOnSecondAccess()
    {
        var lCommand1 = ScrollViewerScrollService.ScrollToTopCommand;
        var lCommand2 = ScrollViewerScrollService.ScrollToTopCommand;

        Assert.Same( lCommand1, lCommand2 );
    }

    [StaFact]
    public void ScrollToBottomCommand_IsSameInstanceOnSecondAccess()
    {
        var lCommand1 = ScrollViewerScrollService.ScrollToBottomCommand;
        var lCommand2 = ScrollViewerScrollService.ScrollToBottomCommand;

        Assert.Same( lCommand1, lCommand2 );
    }

    [StaFact]
    public void ScrollToTopCommand_CanExecute_ReturnsTrue()
    {
        Assert.True( ScrollViewerScrollService.ScrollToTopCommand.CanExecute( null ) );
    }

    [StaFact]
    public void ScrollToBottomCommand_CanExecute_ReturnsTrue()
    {
        Assert.True( ScrollViewerScrollService.ScrollToBottomCommand.CanExecute( null ) );
    }

    [StaFact]
    public void ScrollToTopCommand_Execute_DoesNotThrow()
    {
        var lException = Record.Exception( () => ScrollViewerScrollService.ScrollToTopCommand.Execute( null ) );

        Assert.Null( lException );
    }

    [StaFact]
    public void ScrollToBottomCommand_Execute_DoesNotThrow()
    {
        var lException = Record.Exception( () => ScrollViewerScrollService.ScrollToBottomCommand.Execute( null ) );

        Assert.Null( lException );
    }
}
