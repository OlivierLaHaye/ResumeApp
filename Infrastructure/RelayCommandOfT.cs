// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class RelayCommand<T> : ICommand
	{
		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		private readonly Action<T> mExecuteAction;
		private readonly Func<T, bool> mCanExecuteFunc;

		public RelayCommand( Action<T> pExecuteAction, Func<T, bool> pCanExecuteFunc = null )
		{
			mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );
			mCanExecuteFunc = pCanExecuteFunc;
		}

		public bool CanExecute( object pParameter )
		{
			if ( mCanExecuteFunc == null )
			{
				return true;
			}

			switch ( pParameter )
			{
				case null:
					{
						return mCanExecuteFunc( default );
					}
				case T lTypedParameter:
					{
						return mCanExecuteFunc( lTypedParameter );
					}
				default:
					{
						return false;
					}
			}
		}

		public void Execute( object pParameter )
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
						break;
					}
			}
		}

		public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
	}
}
