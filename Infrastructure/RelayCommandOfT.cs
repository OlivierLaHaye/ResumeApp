// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure;

public sealed class RelayCommand<T>( Action<T?> pExecuteAction, Func<T?, bool>? pCanExecuteFunc = null )
	: ICommand
{
	private readonly Action<T?> mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );

	public event EventHandler? CanExecuteChanged
	{
		add => CommandManager.RequerySuggested += value;
		remove => CommandManager.RequerySuggested -= value;
	}

	public bool CanExecute( object? pParameter )
	{
		if ( pCanExecuteFunc is null )
		{
			return true;
		}

		return pParameter switch
		{
			null => pCanExecuteFunc( default ),
			T lTypedParameter => pCanExecuteFunc( lTypedParameter ),
			_ => false
		};
	}

	public void Execute( object? pParameter )
	{
		switch ( pParameter )
		{
			case null:
				{
					mExecuteAction( default );
					return;
				}
			case T lTypedParameter:
				{
					mExecuteAction( lTypedParameter );
					return;
				}
		}
	}
}
