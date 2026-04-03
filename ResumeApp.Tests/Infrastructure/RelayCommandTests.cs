using ResumeApp.Infrastructure;
using Xunit;

namespace ResumeApp.Tests.Infrastructure;

public sealed class RelayCommandTests
{
    [StaFact]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () => new RelayCommand( null! ) );
    }

    [StaFact]
    public void Execute_InvokesAction()
    {
        bool lWasExecuted = false;
        var lCommand = new RelayCommand( () => lWasExecuted = true );

        lCommand.Execute( null );

        Assert.True( lWasExecuted );
    }

    [StaFact]
    public void Execute_IgnoresParameter()
    {
        bool lWasExecuted = false;
        var lCommand = new RelayCommand( () => lWasExecuted = true );

        lCommand.Execute( "someParameter" );

        Assert.True( lWasExecuted );
    }

    [StaFact]
    public void CanExecute_WhenNoCanExecuteFunc_ReturnsTrue()
    {
        var lCommand = new RelayCommand( () => { } );

        Assert.True( lCommand.CanExecute( null ) );
    }

    [StaFact]
    public void CanExecute_WhenCanExecuteFuncReturnsTrue_ReturnsTrue()
    {
        var lCommand = new RelayCommand( () => { }, () => true );

        Assert.True( lCommand.CanExecute( null ) );
    }

    [StaFact]
    public void CanExecute_WhenCanExecuteFuncReturnsFalse_ReturnsFalse()
    {
        var lCommand = new RelayCommand( () => { }, () => false );

        Assert.False( lCommand.CanExecute( null ) );
    }

    [StaFact]
    public void CanExecuteChanged_CanSubscribeAndUnsubscribe()
    {
        var lCommand = new RelayCommand( () => { } );
        EventHandler lHandler = ( _, _ ) => { };

        var lException = Record.Exception( () =>
        {
            lCommand.CanExecuteChanged += lHandler;
            lCommand.CanExecuteChanged -= lHandler;
        } );

        Assert.Null( lException );
    }
}
