// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ResumeApp.Controls
{
	public partial class ThemeCheckBox
	{
		public static readonly DependencyProperty sIsDarkThemeActiveProperty = DependencyProperty.Register(
			nameof( IsDarkThemeActive ),
			typeof( bool ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( false ) );

		public static readonly DependencyProperty sCheckBoxThemeCommandProperty = DependencyProperty.Register(
			nameof( CheckBoxThemeCommand ),
			typeof( ICommand ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( null ) );

		public static readonly DependencyProperty sResourcesServiceProperty = DependencyProperty.Register(
			nameof( ResourcesService ),
			typeof( ResourcesService ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( null, OnResourcesChanged ) );

		public static readonly DependencyProperty sLabelResourceKeyProperty = DependencyProperty.Register(
			nameof( LabelResourceKey ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty, OnResourcesChanged ) );

		public static readonly DependencyProperty sLabelTextProperty = DependencyProperty.Register(
			nameof( LabelText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty sLightModeTextProperty = DependencyProperty.Register(
			nameof( LightModeText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty sDarkModeTextProperty = DependencyProperty.Register(
			nameof( DarkModeText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public bool IsDarkThemeActive
		{
			get => (bool) GetValue( sIsDarkThemeActiveProperty );
			set => SetValue( sIsDarkThemeActiveProperty, value );
		}

		public ICommand CheckBoxThemeCommand
		{
			get => (ICommand) GetValue( sCheckBoxThemeCommandProperty );
			set => SetValue( sCheckBoxThemeCommandProperty, value );
		}

		public ResourcesService ResourcesService
		{
			get => (ResourcesService) GetValue( sResourcesServiceProperty );
			set => SetValue( sResourcesServiceProperty, value );
		}

		public string LabelResourceKey
		{
			get => (string) GetValue( sLabelResourceKeyProperty );
			set => SetValue( sLabelResourceKeyProperty, value );
		}

		public string LabelText
		{
			get => (string) GetValue( sLabelTextProperty );
			private set => SetValue( sLabelTextProperty, value );
		}

		public string LightModeText
		{
			get => (string) GetValue( sLightModeTextProperty );
			private set => SetValue( sLightModeTextProperty, value );
		}

		public string DarkModeText
		{
			get => (string) GetValue( sDarkModeTextProperty );
			private set => SetValue( sDarkModeTextProperty, value );
		}

		public ThemeCheckBox()
		{
			InitializeComponent();
		}

		private static void OnResourcesChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is ThemeCheckBox lControl )
			{
				lControl.RefreshTexts();
			}
		}

		private void RefreshTexts()
		{
			ResourcesService lResourcesService = ResourcesService;

			if ( lResourcesService == null )
			{
				LabelText = string.Empty;
				LightModeText = string.Empty;
				DarkModeText = string.Empty;
				return;
			}

			string lLabelKey = LabelResourceKey ?? string.Empty;

			LabelText = lResourcesService[ lLabelKey ];
			LightModeText = lResourcesService[ "ButtonLightMode" ];
			DarkModeText = lResourcesService[ "ButtonDarkMode" ];
		}
	}
}
