// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class ParameterRelayCommand(
		Action<object?> pExecuteAction,
		Func<object?, bool>? pCanExecutePredicate = null )
		: ICommand
	{
		private readonly Action<object?> mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );

		public event EventHandler? CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public bool CanExecute( object? pParameter ) => pCanExecutePredicate?.Invoke( pParameter ) ?? true;

		public void Execute( object? pParameter ) => mExecuteAction( pParameter );
	}
}
