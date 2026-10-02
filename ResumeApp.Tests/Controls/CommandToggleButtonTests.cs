using System.ComponentModel;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using ResumeApp.Controls;
using Xunit;

namespace ResumeApp.Tests.Controls;

public sealed class CommandToggleButtonTests
{
    [StaFact]
    public void AutomationToggle_ExecutesCommandOnceAndFollowsSourceState()
    {
        var lState = new ToggleState();
        var lCommand = new CountingCommand( () => lState.IsActive = !lState.IsActive );
        CommandToggleButton lButton = CreateBoundButton( lState, lCommand );

        InvokeToggle( lButton );

        Assert.Equal( 1, lCommand.ExecuteCount );
        Assert.True( lState.IsActive );
        Assert.True( lButton.IsChecked );
    }

    [StaFact]
    public void AutomationToggle_WhenCommandDoesNotChangeState_KeepsBindingAndCheckedState()
    {
        var lState = new ToggleState { IsActive = true };
        var lCommand = new CountingCommand( () => lState.IsActive = true );
        CommandToggleButton lButton = CreateBoundButton( lState, lCommand );

        InvokeToggle( lButton );

        Assert.Equal( 1, lCommand.ExecuteCount );
        Assert.True( lButton.IsChecked );
        Assert.NotNull( BindingOperations.GetBindingExpression( lButton, ToggleButton.IsCheckedProperty ) );
    }

    [StaFact]
    public void ProgrammaticSourceChange_UpdatesCheckedStateWithoutExecutingCommand()
    {
        var lState = new ToggleState();
        var lCommand = new CountingCommand( () => { } );
        CommandToggleButton lButton = CreateBoundButton( lState, lCommand );

        lState.IsActive = true;

        Assert.True( lButton.IsChecked );
        Assert.Equal( 0, lCommand.ExecuteCount );
    }

    [StaFact]
    public void AutomationToggle_WhenCommandCannotExecute_DoesNothing()
    {
        var lState = new ToggleState();
        var lCommand = new CountingCommand( () => lState.IsActive = true ) { CanExecuteValue = false };
        CommandToggleButton lButton = CreateBoundButton( lState, lCommand );

        InvokeToggle( lButton );

        Assert.Equal( 0, lCommand.ExecuteCount );
        Assert.False( lButton.IsChecked );
    }

    [StaFact]
    public void AutomationToggle_PassesCommandParameter()
    {
        object? lReceivedParameter = null;
        var lCommand = new CountingCommand( () => { } );
        lCommand.Executed += pParameter => lReceivedParameter = pParameter;
        var lButton = new CommandToggleButton { ToggleCommand = lCommand, ToggleCommandParameter = "fr" };

        InvokeToggle( lButton );

        Assert.Equal( "fr", lReceivedParameter );
    }

    private static CommandToggleButton CreateBoundButton( ToggleState pState, ICommand pCommand )
    {
        var lButton = new CommandToggleButton { ToggleCommand = pCommand };
        lButton.SetBinding( ToggleButton.IsCheckedProperty, new Binding( nameof( ToggleState.IsActive ) ) { Source = pState, Mode = BindingMode.OneWay } );
        return lButton;
    }

    private static void InvokeToggle( CommandToggleButton pButton )
    {
        var lPeer = new ToggleButtonAutomationPeer( pButton );
        ( ( IToggleProvider )lPeer ).Toggle();
    }

    private sealed class ToggleState : INotifyPropertyChanged
    {
        private bool mIsActive;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsActive
        {
            get => mIsActive;
            set
            {
                if ( mIsActive == value )
                {
                    return;
                }

                mIsActive = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( IsActive ) ) );
            }
        }
    }

    private sealed class CountingCommand( Action pAction ) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public event Action<object?>? Executed;

        public int ExecuteCount { get; private set; }

        public bool CanExecuteValue { get; init; } = true;

        public bool CanExecute( object? pParameter ) => CanExecuteValue;

        public void Execute( object? pParameter )
        {
            ExecuteCount++;
            Executed?.Invoke( pParameter );
            pAction();
        }
    }
}
