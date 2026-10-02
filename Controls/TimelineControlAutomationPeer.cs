// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace ResumeApp.Controls
{
	public sealed class TimelineControlAutomationPeer( TimelineControl pOwner ) : FrameworkElementAutomationPeer( pOwner ), IValueProvider
	{
		private const string AutomationNameResourceKey = "TimelineAutomationName";
		private const string HelpTextResourceKey = "TimelineControlInteractionsHelpText";

		private TimelineControl OwnerControl => ( TimelineControl )Owner;

		public string Value => OwnerControl.GetSelectedTimeFrameSummary();

		public bool IsReadOnly => true;

		public void SetValue( string pValue )
		{
			throw new InvalidOperationException();
		}

		public override object? GetPattern( PatternInterface pPatternInterface )
		{
			return pPatternInterface == PatternInterface.Value ? this : base.GetPattern( pPatternInterface );
		}

		internal void RaiseValueChanged( string pOldValue, string pNewValue )
		{
			if ( string.Equals( pOldValue, pNewValue, StringComparison.Ordinal ) || !ListenerExists( AutomationEvents.PropertyChanged ) )
			{
				return;
			}

			RaisePropertyChangedEvent( ValuePatternIdentifiers.ValueProperty, pOldValue, pNewValue );
		}

		protected override string GetClassNameCore() => nameof( TimelineControl );

		protected override string GetNameCore()
		{
			var lExplicitName = AutomationProperties.GetName( OwnerControl );
			if ( !string.IsNullOrWhiteSpace( lExplicitName ) )
			{
				return lExplicitName;
			}

			return TimelineResourceLookup.GetString( OwnerControl, AutomationNameResourceKey, string.Empty );
		}

		protected override string GetHelpTextCore()
		{
			var lExplicitHelpText = AutomationProperties.GetHelpText( OwnerControl );
			if ( !string.IsNullOrWhiteSpace( lExplicitHelpText ) )
			{
				return lExplicitHelpText;
			}

			return TimelineResourceLookup.GetString( OwnerControl, HelpTextResourceKey, string.Empty );
		}
	}
}
