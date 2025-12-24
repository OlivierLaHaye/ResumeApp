// ViewModels/Pages/ProjectCardViewModel.cs

using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class ProjectCardViewModel : PropertyChangedNotifier
	{
		private readonly ResourcesService mResourcesService;

		private readonly string mTitleResourceKey;
		private readonly string mContextResourceKey;
		private readonly string mConstraintsResourceKey;
		private readonly string mImpactResourceKey;
		private readonly string mTechResourceKey;

		public string TitleText => mResourcesService[ mTitleResourceKey ];

		private string mContextValueText;
		public string ContextValueText
		{
			get => mContextValueText;
			private set => SetProperty( ref mContextValueText, value );
		}

		private string mConstraintsValueText;
		public string ConstraintsValueText
		{
			get => mConstraintsValueText;
			private set => SetProperty( ref mConstraintsValueText, value );
		}

		private string mImpactValueText;
		public string ImpactValueText
		{
			get => mImpactValueText;
			private set => SetProperty( ref mImpactValueText, value );
		}

		public ObservableCollection<LocalizedResourceItemViewModel> WhatIBuiltItems { get; }

		public ObservableCollection<string> TechItems { get; }

		public ObservableCollection<ImageSource> Images { get; }

		public ProjectCardViewModel(
			ResourcesService pResourcesService,
			string pTitleResourceKey,
			string pContextResourceKey,
			string pConstraintsResourceKey,
			string[] pWhatIBuiltItemResourceKeys,
			string pImpactResourceKey,
			string pTechResourceKey )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );

			mTitleResourceKey = pTitleResourceKey ?? string.Empty;
			mContextResourceKey = pContextResourceKey ?? string.Empty;
			mConstraintsResourceKey = pConstraintsResourceKey ?? string.Empty;
			mImpactResourceKey = pImpactResourceKey ?? string.Empty;
			mTechResourceKey = pTechResourceKey ?? string.Empty;

			WhatIBuiltItems = new ObservableCollection<LocalizedResourceItemViewModel>( ( pWhatIBuiltItemResourceKeys ?? Array.Empty<string>() )
				.Where( pKey => !string.IsNullOrWhiteSpace( pKey ) )
				.Select( pKey => new LocalizedResourceItemViewModel( mResourcesService, pKey ) ) );

			TechItems = new ObservableCollection<string>();
			Images = new ObservableCollection<ImageSource>();

			RefreshFromResources();

			mResourcesService.PropertyChanged += OnResourcesServicePropertyChanged;
		}

		private static string ExtractValueAfterFirstColon( string pText )
		{
			if ( string.IsNullOrWhiteSpace( pText ) )
			{
				return string.Empty;
			}

			int lColonIndex = pText.IndexOf( ':' );

			if ( lColonIndex < 0 )
			{
				return pText.Trim();
			}

			return lColonIndex >= pText.Length - 1 ? string.Empty : pText.Substring( lColonIndex + 1 ).Trim();
		}

		private static string[] SplitCommaSeparatedItems( string pText )
		{
			if ( string.IsNullOrWhiteSpace( pText ) )
			{
				return Array.Empty<string>();
			}

			return pText
				.Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
				.Select( pItem => pItem.Trim() )
				.Where( pItem => !string.IsNullOrWhiteSpace( pItem ) )
				.ToArray();
		}

		private static void ReplaceObservableItems( ICollection<string> pTarget, string[] pItems )
		{
			if ( pTarget == null )
			{
				return;
			}

			pTarget.Clear();

			foreach ( string lItem in pItems ?? Array.Empty<string>() )
			{
				pTarget.Add( lItem );
			}
		}

		public void RefreshFromResources()
		{
			ContextValueText = ExtractValueAfterFirstColon( mResourcesService[ mContextResourceKey ] );
			ConstraintsValueText = ExtractValueAfterFirstColon( mResourcesService[ mConstraintsResourceKey ] );
			ImpactValueText = ExtractValueAfterFirstColon( mResourcesService[ mImpactResourceKey ] );

			string lTechValueText = ExtractValueAfterFirstColon( mResourcesService[ mTechResourceKey ] );

			ReplaceObservableItems( TechItems, SplitCommaSeparatedItems( lTechValueText ) );

			RaisePropertyChanged( nameof( TitleText ) );
		}

		private void OnResourcesServicePropertyChanged( object pSender, PropertyChangedEventArgs pArgs ) => RefreshFromResources();
	}
}
