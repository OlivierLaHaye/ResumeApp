// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class ExperienceTimelineEntryViewModel : PropertyChangedNotifier
	{
		private readonly ResourcesService mResourcesService;

		public string CompanyText { get; }
		public string RoleText { get; }
		public string LocationText { get; }
		public string ScopeText { get; }
		public string TechText { get; }

		public ObservableCollection<string> TechItems { get; }

		public DateTime StartDate { get; }
		public DateTime? EndDate { get; }

		public ObservableCollection<string> Accomplishments { get; }

		public string DateRangeText { get; private set; }

		private int mPaletteIndex;
		public int PaletteIndex
		{
			get => mPaletteIndex;
			private set => SetProperty( ref mPaletteIndex, value );
		}

		private int mLaneIndex;
		public int LaneIndex
		{
			get => mLaneIndex;
			private set => SetProperty( ref mLaneIndex, value );
		}

		private Thickness mLaneLeftMargin;
		public Thickness LaneLeftMargin
		{
			get => mLaneLeftMargin;
			private set => SetProperty( ref mLaneLeftMargin, value );
		}

		public string MarkerGlyph { get; private set; }

		public ExperienceTimelineEntryViewModel(
			string pCompanyText,
			string pRoleText,
			string pLocationText,
			string pScopeText,
			string pTechText,
			DateTime pStartDate,
			DateTime? pEndDate,
			ObservableCollection<string> pAccomplishments,
			ResourcesService pResourcesService )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );

			CompanyText = pCompanyText ?? string.Empty;
			RoleText = pRoleText ?? string.Empty;
			LocationText = pLocationText ?? string.Empty;
			ScopeText = pScopeText ?? string.Empty;
			TechText = pTechText ?? string.Empty;

			TechItems = new ObservableCollection<string>( SplitTechTextToItems( TechText ) );

			StartDate = pStartDate;
			EndDate = pEndDate;

			Accomplishments = pAccomplishments ?? new ObservableCollection<string>();

			UpdateDateRangeText();
			SetLaneIndex( 0 );
			SetPaletteIndex( 0 );
			MarkerGlyph = ResolveMarkerGlyph( 0 );
		}

		private static IEnumerable<string> SplitTechTextToItems( string pTechText )
		{
			if ( string.IsNullOrWhiteSpace( pTechText ) )
			{
				return Enumerable.Empty<string>();
			}

			string lNormalizedText = pTechText
				.Replace( " / ", "," )
				.Replace( "•", "," )
				.Replace( "·", "," )
				.Replace( "|", "," )
				.Replace( ";", "," );

			string[] lParts = lNormalizedText.Split(
				new[] { ',', '\r', '\n', '\t' },
				StringSplitOptions.RemoveEmptyEntries );

			return lParts
				.Select( pPart => ( pPart ?? string.Empty ).Trim() )
				.Where( pItem => !string.IsNullOrWhiteSpace( pItem ) )
				.Distinct( StringComparer.OrdinalIgnoreCase )
				.ToList();
		}

		private static double GetDoubleResourceOrDefault( string pKey, double pDefault )
		{
			if ( Application.Current == null )
			{
				return pDefault;
			}

			if ( Application.Current.Resources[ pKey ] is double lValue )
			{
				return lValue;
			}

			return pDefault;
		}

		public void SetLaneIndex( int pLaneIndex )
		{
			LaneIndex = Math.Max( 0, pLaneIndex );
			MarkerGlyph = ResolveMarkerGlyph( LaneIndex );
			RaisePropertyChanged( nameof( MarkerGlyph ) );

			UpdateLaneMargin();
		}

		public void SetPaletteIndex( int pPaletteIndex )
		{
			int lNormalizedPaletteIndex = pPaletteIndex < 0 ? 0 : pPaletteIndex;

			if ( lNormalizedPaletteIndex == PaletteIndex )
			{
				return;
			}

			PaletteIndex = lNormalizedPaletteIndex;
		}

		private void UpdateDateRangeText()
		{
			string lStartText = StartDate.ToString( "yyyy-MM", mResourcesService.ActiveCulture );
			string lSeparator = mResourcesService[ "ExperienceDateRangeSeparator" ];

			string lEndText = EndDate.HasValue
				? EndDate.Value.ToString( "yyyy-MM", mResourcesService.ActiveCulture )
				: mResourcesService[ "LabelPresent" ];

			DateRangeText = string.Concat( lStartText, lSeparator, lEndText );
			RaisePropertyChanged( nameof( DateRangeText ) );
		}

		private void UpdateLaneMargin()
		{
			double lZero = GetDoubleResourceOrDefault( "NoBlurRadius", 0 );
			double lLaneSpacing = GetDoubleResourceOrDefault( "LargeThicknessValue", 20 );

			double lLeft = LaneIndex * lLaneSpacing;

			LaneLeftMargin = new Thickness( lLeft, lZero, lZero, lZero );
		}

		private string ResolveMarkerGlyph( int pLaneIndex )
		{
			int lIndex = Math.Abs( pLaneIndex ) % 4;

			switch ( lIndex )
			{
				case 1:
					{
						return mResourcesService[ "GlyphMarkerSquare" ];
					}
				case 2:
					{
						return mResourcesService[ "GlyphMarkerDiamond" ];
					}
				case 3:
					{
						return mResourcesService[ "GlyphMarkerTriangle" ];
					}
				default:
					{
						return mResourcesService[ "GlyphMarkerCircle" ];
					}
			}
		}
	}
}
