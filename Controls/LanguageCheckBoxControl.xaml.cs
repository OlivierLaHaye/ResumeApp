using ResumeApp.Services;
using System.Windows;
using System.Windows.Input;

namespace ResumeApp.Controls
{
	public partial class LanguageCheckBoxControl
	{
		public static readonly DependencyProperty sIsFrenchLanguageActiveProperty = DependencyProperty.Register(
			nameof( IsFrenchLanguageActive ),
			typeof( bool ),
			typeof( LanguageCheckBoxControl ),
			new PropertyMetadata( false ) );

		public static readonly DependencyProperty sCheckBoxLanguageCommandProperty = DependencyProperty.Register(
			nameof( CheckBoxLanguageCommand ),
			typeof( ICommand ),
			typeof( LanguageCheckBoxControl ),
			new PropertyMetadata( null ) );

		public static readonly DependencyProperty sResourcesServiceProperty = DependencyProperty.Register(
			nameof( ResourcesService ),
			typeof( ResourcesService ),
			typeof( LanguageCheckBoxControl ),
			new PropertyMetadata( null, OnResourcesChanged ) );

		public static readonly DependencyProperty sLabelResourceKeyProperty = DependencyProperty.Register(
			nameof( LabelResourceKey ),
			typeof( string ),
			typeof( LanguageCheckBoxControl ),
			new PropertyMetadata( "LabelActiveLanguage", OnResourcesChanged ) );

		public static readonly DependencyProperty sLabelTextProperty = DependencyProperty.Register(
			nameof( LabelText ),
			typeof( string ),
			typeof( LanguageCheckBoxControl ),
			new PropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty sEnglishLanguageTextProperty = DependencyProperty.Register(
			nameof( EnglishLanguageText ),
			typeof( string ),
			typeof( LanguageCheckBoxControl ),
			new PropertyMetadata( string.Empty ) );

		public static readonly DependencyProperty sFrenchLanguageTextProperty = DependencyProperty.Register(
			nameof( FrenchLanguageText ),
			typeof( string ),
			typeof( LanguageCheckBoxControl ),
			new PropertyMetadata( string.Empty ) );

		public LanguageCheckBoxControl()
		{
			InitializeComponent();
		}

		public bool IsFrenchLanguageActive
		{
			get => ( bool )GetValue( sIsFrenchLanguageActiveProperty );
			set => SetValue( sIsFrenchLanguageActiveProperty, value );
		}

		public ICommand CheckBoxLanguageCommand
		{
			get => ( ICommand )GetValue( sCheckBoxLanguageCommandProperty );
			set => SetValue( sCheckBoxLanguageCommandProperty, value );
		}

		public ResourcesService ResourcesService
		{
			get => ( ResourcesService )GetValue( sResourcesServiceProperty );
			set => SetValue( sResourcesServiceProperty, value );
		}

		public string LabelResourceKey
		{
			get => ( string )GetValue( sLabelResourceKeyProperty );
			set => SetValue( sLabelResourceKeyProperty, value );
		}

		public string LabelText
		{
			get => ( string )GetValue( sLabelTextProperty );
			private set => SetValue( sLabelTextProperty, value );
		}

		public string EnglishLanguageText
		{
			get => ( string )GetValue( sEnglishLanguageTextProperty );
			private set => SetValue( sEnglishLanguageTextProperty, value );
		}

		public string FrenchLanguageText
		{
			get => ( string )GetValue( sFrenchLanguageTextProperty );
			private set => SetValue( sFrenchLanguageTextProperty, value );
		}

		private static void OnResourcesChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is LanguageCheckBoxControl lControl )
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
				EnglishLanguageText = string.Empty;
				FrenchLanguageText = string.Empty;
				return;
			}

			string lLabelKey = string.IsNullOrWhiteSpace( LabelResourceKey ) ? "LabelActiveLanguage" : LabelResourceKey;

			LabelText = lResourcesService[ lLabelKey ];
			EnglishLanguageText = lResourcesService[ "LanguageEnglishCanadaDisplayName" ];
			FrenchLanguageText = lResourcesService[ "LanguageFrenchCanadaDisplayName" ];
		}
	}
}
