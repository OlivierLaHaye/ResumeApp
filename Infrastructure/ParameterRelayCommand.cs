// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class ParameterRelayCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;
		private readonly Action<object> mExecuteAction;
		private readonly Func<object, bool> mCanExecutePredicate;

		public ParameterRelayCommand( Action<object> pExecuteAction, Func<object, bool> pCanExecutePredicate = null )
		{
			mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );
			mCanExecutePredicate = pCanExecutePredicate;
		}

		public bool CanExecute( object pParameter )
		{
			return mCanExecutePredicate == null || mCanExecutePredicate( pParameter );
		}

		public void Execute( object pParameter )
		{
			mExecuteAction( pParameter );
		}

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke( this, EventArgs.Empty );
		}
	}
}
