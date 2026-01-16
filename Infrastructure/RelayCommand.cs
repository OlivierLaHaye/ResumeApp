// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class RelayCommand : ICommand
	{
		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		private readonly Action mExecuteAction;
		private readonly Func<bool> mCanExecuteFunc;

		public RelayCommand( Action pExecuteAction, Func<bool> pCanExecuteFunc = null )
		{
			mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );
			mCanExecuteFunc = pCanExecuteFunc;
		}

		public bool CanExecute( object pParameter )
		{
			return mCanExecuteFunc == null || mCanExecuteFunc();
		}

		public void Execute( object pParameter )
		{
			mExecuteAction();
		}
	}
}
