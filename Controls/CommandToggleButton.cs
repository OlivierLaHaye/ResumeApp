// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ResumeApp.Controls;

public class CommandToggleButton : ToggleButton
{
	public static readonly DependencyProperty sToggleCommandProperty = DependencyProperty.Register(
		nameof( ToggleCommand ),
		typeof( ICommand ),
		typeof( CommandToggleButton ),
		new PropertyMetadata( null ) );

	public static readonly DependencyProperty sToggleCommandParameterProperty = DependencyProperty.Register(
		nameof( ToggleCommandParameter ),
		typeof( object ),
		typeof( CommandToggleButton ),
		new PropertyMetadata( null ) );

	public ICommand? ToggleCommand
	{
		get => ( ICommand? )GetValue( sToggleCommandProperty );
		set => SetValue( sToggleCommandProperty, value );
	}

	public object? ToggleCommandParameter
	{
		get => GetValue( sToggleCommandParameterProperty );
		set => SetValue( sToggleCommandParameterProperty, value );
	}

	protected override void OnToggle()
	{
		ICommand? lCommand = ToggleCommand;
		object? lParameter = ToggleCommandParameter;

		if ( lCommand?.CanExecute( lParameter ) != true )
		{
			return;
		}

		lCommand.Execute( lParameter );
	}
}
