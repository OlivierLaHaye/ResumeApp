// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class RelayCommand( Action pExecuteAction, Func<bool>? pCanExecuteFunc = null ) : ICommand
	{
		private readonly Action mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );

		public event EventHandler? CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public bool CanExecute( object? pParameter )
		{
			return pCanExecuteFunc?.Invoke() ?? true;
		}

		public void Execute( object? pParameter )
		{
			mExecuteAction();
		}
	}
}
