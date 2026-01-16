// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class ParameterRelayCommand(
		Action<object> pExecuteAction,
		Func<object, bool> pCanExecutePredicate = null )
		: ICommand
	{
		public event EventHandler CanExecuteChanged;
		private readonly Action<object> mExecuteAction = pExecuteAction ?? throw new ArgumentNullException( nameof( pExecuteAction ) );

		public bool CanExecute( object pParameter )
		{
			return pCanExecutePredicate == null || pCanExecutePredicate( pParameter );
		}

		public void Execute( object pParameter )
		{
			mExecuteAction( pParameter );
		}
	}
}
