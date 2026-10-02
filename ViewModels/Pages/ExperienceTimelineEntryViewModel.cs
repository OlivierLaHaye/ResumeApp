// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using ResumeApp.Infrastructure;
using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class ExperienceTimelineEntryViewModel : PropertyChangedNotifier
	{
		private readonly ResourcesService mResourcesService;
		private readonly Func<DateTime> mTodayProvider;

		public string CompanyText { get; }
		public string RoleText { get; }
		public string LocationText { get; }
		public string ScopeText { get; }
		public string TechText { get; }

		public ObservableCollection<string> TechItems { get; }

		public DateTime StartDate { get; }
		public DateTime? EndDate { get; }

		public ObservableCollection<string> Accomplishments { get; }

		public string DateRangeText
		{
			get;
			private set => SetProperty( ref field, value );
		}

		public string DurationText
		{
			get;
			private set => SetProperty( ref field, value );
		}

		public int PaletteIndex
		{
			get;
			private set => SetProperty( ref field, value );
		}

		public int LaneIndex
		{
			get;
			private set => SetProperty( ref field, value );
		}

		public Thickness LaneLeftMargin
		{
			get;
			private set => SetProperty( ref field, value );
		}

		public string MarkerGlyph
		{
			get;
			private set => SetProperty( ref field, value );
		}

		public bool IsSelected
		{
			get;
			set => SetProperty( ref field, value );
		}

		public ExperienceTimelineEntryViewModel(
			string? pCompanyText,
			string? pRoleText,
			string? pLocationText,
			string? pScopeText,
			string? pTechText,
			DateTime pStartDate,
			DateTime? pEndDate,
			ObservableCollection<string>? pAccomplishments,
			ResourcesService pResourcesService,
			Func<DateTime>? pTodayProvider = null )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );
			mTodayProvider = pTodayProvider ?? ( () => DateTime.Today );

			CompanyText = pCompanyText ?? string.Empty;
			RoleText = pRoleText ?? string.Empty;
			LocationText = pLocationText ?? string.Empty;
			ScopeText = pScopeText ?? string.Empty;
			TechText = pTechText ?? string.Empty;

			TechItems = new ObservableCollection<string>( SplitTechTextToItems( TechText ) );

			StartDate = pStartDate;
			EndDate = pEndDate;

			Accomplishments = pAccomplishments ?? [];

			DateRangeText = string.Empty;
			DurationText = string.Empty;
			MarkerGlyph = string.Empty;
			LaneLeftMargin = new Thickness( 0 );

			UpdateDateRangeText();
			UpdateDurationText();
			SetLaneIndex( 0 );
			SetPaletteIndex( 0 );
		}

		private static IEnumerable<string> SplitTechTextToItems( string pTechText )
		{
			if ( string.IsNullOrWhiteSpace( pTechText ) )
			{
				return [];
			}

			return pTechText
				.Split( ", ", StringSplitOptions.RemoveEmptyEntries )
				.Select( pPart => pPart.Trim().TrimEnd( '.' ).Trim() )
				.Where( pItem => !string.IsNullOrWhiteSpace( pItem ) )
				.Distinct( StringComparer.OrdinalIgnoreCase )
				.ToList();
		}

		[ExcludeFromCodeCoverage( Justification = "Reads from Application.Current.Resources which requires a running WPF Application instance." )]
		private static double GetDoubleResourceOrDefault( string pKey, double pDefault )
		{
			return Application.Current?.Resources[ pKey ] is double lValue ? lValue : pDefault;
		}

		public void SetLaneIndex( int pLaneIndex )
		{
			LaneIndex = Math.Max( 0, pLaneIndex );
			MarkerGlyph = ResolveMarkerGlyph( LaneIndex );
			UpdateLaneMargin();
		}

		public void SetPaletteIndex( int pPaletteIndex )
		{
			int lNormalizedPaletteIndex = Math.Max( 0, pPaletteIndex );

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
				: mResourcesService[ "ExperienceDateRangeOpenEnd" ];

			DateRangeText = string.Concat( lStartText, lSeparator, lEndText );
		}

		private void UpdateDurationText()
		{
			DateTime lEndDate = EndDate ?? mTodayProvider();
			int lTotalMonths = Math.Max( 0, ( ( lEndDate.Year - StartDate.Year ) * 12 ) + lEndDate.Month - StartDate.Month );
			int lYears = lTotalMonths / 12;
			int lMonths = lTotalMonths % 12;

			List<string> lParts = [ ];

			if ( lYears > 0 )
			{
				lParts.Add( FormatDurationPart( lYears == 1 ? "ExperienceDurationYearOne" : "ExperienceDurationYearMany", lYears ) );
			}

			if ( lMonths > 0 )
			{
				lParts.Add( FormatDurationPart( lMonths == 1 ? "ExperienceDurationMonthOne" : "ExperienceDurationMonthMany", lMonths ) );
			}

			DurationText = string.Join( " ", lParts );
		}

		private string FormatDurationPart( string pResourceKey, int pValue )
		{
			return string.Format( mResourcesService.ActiveCulture, mResourcesService[ pResourceKey ], pValue );
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

			return lIndex switch
			{
				1 => mResourcesService[ "GlyphMarkerSquare" ],
				2 => mResourcesService[ "GlyphMarkerDiamond" ],
				3 => mResourcesService[ "GlyphMarkerTriangle" ],
				_ => mResourcesService[ "GlyphMarkerCircle" ]
			};
		}
	}
}
