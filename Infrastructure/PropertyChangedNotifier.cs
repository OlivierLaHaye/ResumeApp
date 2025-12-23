// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ResumeApp.Infrastructure
{
	public abstract class PropertyChangedNotifier : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged( [CallerMemberName] string pPropertyName = null )
		{
			PropertyChangedEventHandler lHandler = PropertyChanged;

			lHandler?.Invoke( this, new PropertyChangedEventArgs( pPropertyName ) );
		}

		protected void RaiseAllPropertiesChanged()
		{
			PropertyChangedEventHandler lHandler = PropertyChanged;

			lHandler?.Invoke( this, new PropertyChangedEventArgs( null ) );
		}

		protected bool SetProperty<T>( ref T pStorage, T pValue, [CallerMemberName] string pPropertyName = null )
		{
			if ( EqualityComparer<T>.Default.Equals( pStorage, pValue ) )
			{
				return false;
			}

			pStorage = pValue;
			RaisePropertyChanged( pPropertyName );
			return true;
		}
	}
}
