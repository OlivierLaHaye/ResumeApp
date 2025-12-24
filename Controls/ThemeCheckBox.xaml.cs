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
		public static readonly DependencyProperty sIsDarkThemeActiveProperty = DependencyProperty.Register(
			nameof( sIsDarkThemeActive ),
			typeof( bool ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( false ) );

		public static readonly DependencyProperty sCheckBoxThemeCommandProperty = DependencyProperty.Register(
			nameof( sCheckBoxThemeCommand ),
			typeof( ICommand ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( null ) );

		public static readonly DependencyProperty sResourcesServiceProperty = DependencyProperty.Register(
			nameof( sResourcesService ),
			typeof( ResourcesService ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( null, OnResourcesChanged ) );

		public static readonly DependencyProperty sLabelResourceKeyProperty = DependencyProperty.Register(
			nameof( sLabelResourceKey ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty, OnResourcesChanged ) );

		public static readonly DependencyProperty sLabelTextProperty = DependencyProperty.Register(
			nameof( sLabelText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty sLightModeTextProperty = DependencyProperty.Register(
			nameof( sLightModeText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty sDarkModeTextProperty = DependencyProperty.Register(
			nameof( sDarkModeText ),
			typeof( string ),
			typeof( ThemeCheckBox ),
			new PropertyMetadata( string.Empty ) );

		public ThemeCheckBox()
		{
			InitializeComponent();
		}

		public bool sIsDarkThemeActive
		{
			get { return (bool) GetValue( sIsDarkThemeActiveProperty ); }
			set { SetValue( sIsDarkThemeActiveProperty, value ); }
		}

		public ICommand sCheckBoxThemeCommand
		{
			get { return (ICommand) GetValue( sCheckBoxThemeCommandProperty ); }
			set { SetValue( sCheckBoxThemeCommandProperty, value ); }
		}

		public ResourcesService sResourcesService
		{
			get { return (ResourcesService) GetValue( sResourcesServiceProperty ); }
			set { SetValue( sResourcesServiceProperty, value ); }
		}

		public string sLabelResourceKey
		{
			get { return (string) GetValue( sLabelResourceKeyProperty ); }
			set { SetValue( sLabelResourceKeyProperty, value ); }
		}

		public string sLabelText
		{
			get { return (string) GetValue( sLabelTextProperty ); }
			private set { SetValue( sLabelTextProperty, value ); }
		}

		public string sLightModeText
		{
			get { return (string) GetValue( sLightModeTextProperty ); }
			private set { SetValue( sLightModeTextProperty, value ); }
		}

		public string sDarkModeText
		{
			get { return (string) GetValue( sDarkModeTextProperty ); }
			private set { SetValue( sDarkModeTextProperty, value ); }
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
			ResourcesService lResourcesService = sResourcesService;

			if ( lResourcesService == null )
			{
				sLabelText = string.Empty;
				sLightModeText = string.Empty;
				sDarkModeText = string.Empty;
				return;
			}

			string lLabelKey = sLabelResourceKey ?? string.Empty;

			sLabelText = lResourcesService[ lLabelKey ];
			sLightModeText = lResourcesService[ "ButtonLightMode" ];
			sDarkModeText = lResourcesService[ "ButtonDarkMode" ];
		}
	}
}
