using System;
using System.Windows.Input;

namespace ResumeApp.Infrastructure
{
	public sealed class ParameterRelayCommand : ICommand
	{
		private readonly Action<object> mExecuteAction;
		private readonly Func<object, bool> mCanExecutePredicate;

		public event EventHandler CanExecuteChanged;

		public ParameterRelayCommand( Action<object> pExecuteAction )
			: this( pExecuteAction, null )
		{
		}

		public ParameterRelayCommand( Action<object> pExecuteAction, Func<object, bool> pCanExecutePredicate )
		{
			if ( pExecuteAction == null )
			{
				throw new ArgumentNullException( nameof( pExecuteAction ) );
			}

			mExecuteAction = pExecuteAction;
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
