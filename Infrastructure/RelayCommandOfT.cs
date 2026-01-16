// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class RelayCommand<T>( Action<T> pExecuteAction, Func<T, bool> pCanExecuteFunc = null )
		: ICommand
	{
		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		private readonly Action<T> mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );

		public bool CanExecute( object pParameter )
		{
			if ( pCanExecuteFunc == null )
			{
				return true;
			}

			switch ( pParameter )
			{
				case null:
					{
						return pCanExecuteFunc( default );
					}
				case T lTypedParameter:
					{
						return pCanExecuteFunc( lTypedParameter );
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
