using ResumeApp.Infrastructure;
using Xunit;

namespace ResumeApp.Tests.Infrastructure;

public sealed class ParameterRelayCommandTests
{
    [StaFact]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () => new ParameterRelayCommand( null! ) );
    }

    [StaFact]
    public void Execute_InvokesActionWithParameter()
    {
        object? lReceivedValue = null;
        var lCommand = new ParameterRelayCommand( pValue => lReceivedValue = pValue );

        lCommand.Execute( "test" );

        Assert.Equal( "test", lReceivedValue );
    }

    [StaFact]
    public void Execute_WithNullParameter_InvokesActionWithNull()
    {
        object? lReceivedValue = "initial";
        var lCommand = new ParameterRelayCommand( pValue => lReceivedValue = pValue );

        lCommand.Execute( null );

        Assert.Null( lReceivedValue );
    }

    [StaFact]
    public void CanExecute_WhenNoCanExecuteFunc_ReturnsTrue()
    {
        var lCommand = new ParameterRelayCommand( _ => { } );

        Assert.True( lCommand.CanExecute( null ) );
        Assert.True( lCommand.CanExecute( "anything" ) );
    }

    [StaFact]
    public void CanExecute_DelegatesToPredicate()
    {
        var lCommand = new ParameterRelayCommand( _ => { }, pParam => pParam is string );

        Assert.True( lCommand.CanExecute( "string" ) );
        Assert.False( lCommand.CanExecute( 42 ) );
        Assert.False( lCommand.CanExecute( null ) );
    }

    [StaFact]
    public void CanExecuteChanged_CanSubscribeAndUnsubscribe()
    {
        var lCommand = new ParameterRelayCommand( _ => { } );
        EventHandler lHandler = ( _, _ ) => { };

        var lException = Record.Exception( () =>
        {
            lCommand.CanExecuteChanged += lHandler;
            lCommand.CanExecuteChanged -= lHandler;
        } );

        Assert.Null( lException );
    }
}
