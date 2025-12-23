// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ResumeApp.Controls
{
	public partial class ThemeCheckBox : UserControl
	{
		public static readonly DependencyProperty IsDarkThemeActiveProperty = DependencyProperty.Register(
			nameof( IsDarkThemeActive ),
			typeof( bool ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( false ) );

		public static readonly DependencyProperty CheckBoxThemeCommandProperty = DependencyProperty.Register(
			nameof( CheckBoxThemeCommand ),
			typeof( ICommand ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( null ) );

		public static readonly DependencyProperty ResourcesServiceProperty = DependencyProperty.Register(
			nameof( ResourcesService ),
			typeof( ResourcesService ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( null, OnResourcesChanged ) );

		public static readonly DependencyProperty LabelResourceKeyProperty = DependencyProperty.Register(
			nameof( LabelResourceKey ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty, OnResourcesChanged ) );

		public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(
			nameof( LabelText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty LightModeTextProperty = DependencyProperty.Register(
			nameof( LightModeText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty DarkModeTextProperty = DependencyProperty.Register(
			nameof( DarkModeText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public ThemeCheckBox()
		{
			InitializeComponent();
		}

		public bool IsDarkThemeActive
		{
			get { return (bool) GetValue( IsDarkThemeActiveProperty ); }
			set { SetValue( IsDarkThemeActiveProperty, value ); }
		}

		public ICommand CheckBoxThemeCommand
		{
			get { return (ICommand) GetValue( CheckBoxThemeCommandProperty ); }
			set { SetValue( CheckBoxThemeCommandProperty, value ); }
		}

		public ResourcesService ResourcesService
		{
			get { return (ResourcesService) GetValue( ResourcesServiceProperty ); }
			set { SetValue( ResourcesServiceProperty, value ); }
		}

		public string LabelResourceKey
		{
			get { return (string) GetValue( LabelResourceKeyProperty ); }
			set { SetValue( LabelResourceKeyProperty, value ); }
		}

		public string LabelText
		{
			get { return (string) GetValue( LabelTextProperty ); }
			private set { SetValue( LabelTextProperty, value ); }
		}

		public string LightModeText
		{
			get { return (string) GetValue( LightModeTextProperty ); }
			private set { SetValue( LightModeTextProperty, value ); }
		}

		public string DarkModeText
		{
			get { return (string) GetValue( DarkModeTextProperty ); }
			private set { SetValue( DarkModeTextProperty, value ); }
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
