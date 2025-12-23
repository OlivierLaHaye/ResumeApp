// Copyright (C) Olivier La Haye
// All rights reserved.

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ResumeApp.Converters
{
	public abstract class BaseVisibilityConverter : IValueConverter
	{
		public Visibility TrueValue { get; set; }
		public Visibility FalseValue { get; set; }

		protected BaseVisibilityConverter()
		{
			TrueValue = Visibility.Visible;
			FalseValue = Visibility.Collapsed;
		}

		public abstract object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture );

		public virtual object ConvertBack( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( Equals( pValue, TrueValue ) )
			{
				return true;
			}
			return Equals( pValue, FalseValue ) ? false : DependencyProperty.UnsetValue;
		}
	}

	public abstract class BaseMultiVisibilityConverter : IMultiValueConverter
	{
		public Visibility TrueValue { get; set; }
		public Visibility FalseValue { get; set; }

		protected BaseMultiVisibilityConverter()
		{
			TrueValue = Visibility.Visible;
			FalseValue = Visibility.Collapsed;
		}

		public abstract object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture );

		public virtual object[] ConvertBack( object pValue, Type[] pTargetTypes, object pParameter, CultureInfo pCulture )
		{
			throw new NotImplementedException();
		}
	}

	[ValueConversion( typeof( bool ), typeof( Visibility ) )]
	public sealed class BoolToCollapseConverter : BaseVisibilityConverter
	{
		public override object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValue is bool lValue )
			{
				return lValue ? TrueValue : FalseValue;
			}
			return DependencyProperty.UnsetValue;
		}
	}

	[ValueConversion( typeof( Enum ), typeof( Visibility ) )]
	public sealed class EnumToVisibilityConverter : BaseVisibilityConverter
	{
		public override object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValue == null || pParameter == null )
			{
				return FalseValue;
			}

			string lEnumValue = pValue.ToString();
			var lTargetValues = pParameter.ToString().Split( '|' ).Select( pTargetValue => pTargetValue.Trim() );
			return lTargetValues.Contains( lEnumValue, StringComparer.InvariantCultureIgnoreCase ) ? TrueValue : FalseValue;
		}
	}

	[ValueConversion( typeof( object ), typeof( Visibility ) )]
	public sealed class IsNullToCollapseConverter : BaseVisibilityConverter
	{
		public override object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			return pValue != null ? TrueValue : FalseValue;
		}
	}

	[ValueConversion( typeof( string ), typeof( Visibility ) )]
	public sealed class StringNullOrEmptyToVisibilityConverter : BaseVisibilityConverter
	{
		public override object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			switch ( pValue )
			{
				case null:
					{
						return FalseValue;
					}
				case string lStrValue:
					{
						return string.IsNullOrEmpty( lStrValue ) ? FalseValue : TrueValue;
					}
				default:
					{
						return FalseValue;
					}
			}
		}
	}

	[ValueConversion( typeof( string ), typeof( Visibility ) )]
	public sealed class StringToVisibilityConverter : BaseVisibilityConverter
	{
		public override object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValue is string lKey && pParameter is string lExpectedKey )
			{
				return lKey.Equals( lExpectedKey, StringComparison.OrdinalIgnoreCase ) ? TrueValue : FalseValue;
			}
			return DependencyProperty.UnsetValue;
		}
	}

	[ValueConversion( typeof( ICollection ), typeof( Visibility ) )]
	public sealed class CollectionToVisibilityConverter : BaseVisibilityConverter
	{
		public override object Convert( object pValue, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValue is ICollection lCollection )
			{
				return lCollection.Count > 0 ? TrueValue : FalseValue;
			}
			return DependencyProperty.UnsetValue;
		}
	}

	[ValueConversion( typeof( object[] ), typeof( Visibility ) )]
	public sealed class AnyNotNullOrEmptyToVisibilityConverter : BaseMultiVisibilityConverter
	{
		public override object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			bool lAnyNotNullOrEmpty = pValues.Any( pValue => pValue != null && !string.IsNullOrEmpty( pValue.ToString() ) );
			return lAnyNotNullOrEmpty ? TrueValue : FalseValue;
		}
	}

	[ValueConversion( typeof( bool[] ), typeof( Visibility ) )]
	public sealed class BoolOrToVisibilityConverter : BaseMultiVisibilityConverter
	{
		public override object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValues == null || !pValues.All( pValue => pValue is bool ) )
			{
				return FalseValue;
			}

			bool lResult = pValues.OfType<bool>().Any( pBoolean => pBoolean );
			return lResult ? TrueValue : FalseValue;
		}
	}

	[ValueConversion( typeof( bool[] ), typeof( Visibility ) )]
	public sealed class BoolAndToVisibilityConverter : BaseMultiVisibilityConverter
	{
		public override object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValues == null || !pValues.All( pValue => pValue is bool ) )
			{
				return FalseValue;
			}

			bool lResult = pValues.OfType<bool>().All( pBoolean => pBoolean );
			return lResult ? TrueValue : FalseValue;
		}
	}
}
