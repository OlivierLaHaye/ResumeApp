// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class RelayCommand( Action pExecuteAction, Func<bool> pCanExecuteFunc = null ) : ICommand
	{
		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		private readonly Action mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );

		public bool CanExecute( object pParameter )
		{
			return pCanExecuteFunc == null || pCanExecuteFunc();
		}

		public void Execute( object pParameter )
		{
			mExecuteAction();
		}
	}
}
