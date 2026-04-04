using ResumeApp.Infrastructure;
using Xunit;

namespace ResumeApp.Tests.Infrastructure;

public sealed class RelayCommandOfTTests
{
    [StaFact]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>( () => new RelayCommand<string>( null! ) );
    }

    [StaFact]
    public void Execute_WithTypedParameter_InvokesAction()
    {
        string? lReceivedValue = null;
        var lCommand = new RelayCommand<string>( pValue => lReceivedValue = pValue );

        lCommand.Execute( "hello" );

        Assert.Equal( "hello", lReceivedValue );
    }

    [StaFact]
    public void Execute_WithNullParameter_InvokesActionWithDefault()
    {
        string? lReceivedValue = "initial";
        var lCommand = new RelayCommand<string>( pValue => lReceivedValue = pValue );

        lCommand.Execute( null );

        Assert.Null( lReceivedValue );
    }

    [StaFact]
    public void Execute_WithWrongType_DoesNothing()
    {
        bool lWasExecuted = false;
        var lCommand = new RelayCommand<string>( _ => lWasExecuted = true );

        lCommand.Execute( 42 );

        Assert.False( lWasExecuted );
    }

    [StaFact]
    public void CanExecute_WhenNoCanExecuteFunc_ReturnsTrue()
    {
        var lCommand = new RelayCommand<string>( _ => { } );

        Assert.True( lCommand.CanExecute( "test" ) );
    }

    [StaFact]
    public void CanExecute_WithTypedParameter_DelegatesToFunc()
    {
        var lCommand = new RelayCommand<string>( _ => { }, pValue => pValue == "yes" );

        Assert.True( lCommand.CanExecute( "yes" ) );
        Assert.False( lCommand.CanExecute( "no" ) );
    }

    [StaFact]
    public void CanExecute_WithNullParameter_PassesDefaultToFunc()
    {
        bool lReceivedNull = false;
        var lCommand = new RelayCommand<string>( _ => { }, pValue =>
        {
            lReceivedNull = pValue == null;
            return true;
        } );

        lCommand.CanExecute( null );

        Assert.True( lReceivedNull );
    }

    [StaFact]
    public void CanExecute_WithWrongType_ReturnsFalse()
    {
        var lCommand = new RelayCommand<string>( _ => { }, _ => true );

        Assert.False( lCommand.CanExecute( 42 ) );
    }

    [StaFact]
    public void CanExecuteChanged_CanSubscribeAndUnsubscribe()
    {
        var lCommand = new RelayCommand<string>( _ => { } );
        EventHandler lHandler = ( _, _ ) => { };

        var lException = Record.Exception( () =>
        {
            lCommand.CanExecuteChanged += lHandler;
            lCommand.CanExecuteChanged -= lHandler;
        } );

        Assert.Null( lException );
    }

    [StaFact]
    public void Execute_WithIntType_InvokesActionCorrectly()
    {
        int lReceivedValue = 0;
        var lCommand = new RelayCommand<int>( pValue => lReceivedValue = pValue );

        lCommand.Execute( 42 );

        Assert.Equal( 42, lReceivedValue );
    }

    [StaFact]
    public void CanExecute_NullParam_NoFunc_ReturnsTrue()
    {
        var lCommand = new RelayCommand<int>( _ => { } );

        Assert.True( lCommand.CanExecute( null ) );
    }
}
