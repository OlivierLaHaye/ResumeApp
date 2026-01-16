// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Helpers;
using ResumeApp.Infrastructure;
using ResumeApp.Models;
using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Input;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class ExperiencePageViewModel : ViewModelBase
	{
		private bool mIsSelectionSynchronizationActive;

		private TimelineTimeFrameItem? mSelectedTimeFrame;

		private ExperienceTimelineEntryViewModel? mSelectedTimelineEntry;

		private DateTime mSelectedDate;

		public ObservableCollection<ExperienceTimelineEntryViewModel> TimelineEntries { get; }

		public ObservableCollection<TimelineTimeFrameItem> ExperienceTimeFrames { get; }

		public string TimelineControlInteractionsHelpText => ResourcesService[ "TimelineControlInteractionsHelpText" ];

		public DateTime TimelineMinDate
		{
			get;
			private set => SetProperty( ref field, value );
		}

		public DateTime SelectedDate
		{
			get => mSelectedDate;
			set => SetSelectedDate( value );
		}

		public TimelineTimeFrameItem? SelectedTimeFrame
		{
			get => mSelectedTimeFrame;
			set => SetSelectedTimeFrame( value );
		}

		public ExperienceTimelineEntryViewModel? SelectedTimelineEntry
		{
			get => mSelectedTimelineEntry;
			set => SetSelectedTimelineEntry( value );
		}

		[field: AllowNull, MaybeNull]
		public ICommand SelectExperienceCommand => field ??= new ParameterRelayCommand( ExecuteSelectExperience );

		[field: AllowNull, MaybeNull]
		public ICommand SelectDateCommand => field ??= new ParameterRelayCommand( ExecuteSelectDate );

		public ExperiencePageViewModel( ResourcesService pResourcesService, ThemeService pThemeService )
			: base( pResourcesService, pThemeService )
		{
			TimelineEntries = [ ];
			ExperienceTimeFrames = [ ];

			ResourcesService.PropertyChanged += ( _, _ ) =>
			{
				RaisePropertyChanged( nameof( TimelineControlInteractionsHelpText ) );
				RebuildEntries();
			};

			RebuildEntries();
		}

		private static DateTime ParseIsoDateOrDefault( string? pIsoText )
		{
			return DateTime.TryParseExact(
				pIsoText ?? string.Empty,
				"yyyy-MM-dd",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeLocal,
				out DateTime lParsedDate ) ? lParsedDate : DateTime.Today;
		}

		private static DateTime? ParseIsoDateOrNull( string pIsoText )
		{
			if ( string.IsNullOrWhiteSpace( pIsoText ) )
			{
				return null;
			}

			return DateTime.TryParseExact(
				pIsoText,
				"yyyy-MM-dd",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeLocal,
				out DateTime lParsedDate ) ? lParsedDate : null;
		}

		private static TimelineTimeFrameItem CreateTimeFrameForEntry( ExperienceTimelineEntryViewModel pEntry )
		{
			DateTime lEndDate = ( pEntry.EndDate ?? DateTime.Today ).Date;
			string lAccentKey = GetAccentKeyForPaletteIndex( pEntry.PaletteIndex );

			return new TimelineTimeFrameItem(
				pStartDate: pEntry.StartDate.Date,
				pEndDate: lEndDate,
				pTitle: pEntry.CompanyText,
				pAccentColorKey: lAccentKey );
		}

		private static string GetAccentKeyForPaletteIndex( int pPaletteIndex )
		{
			if ( ColorHelper.sAccentBrushKeys.Length == 0 )
			{
				return "CommonBlueBrush";
			}

			int lNormalizedIndex = pPaletteIndex < 0 ? 0 : pPaletteIndex;
			return ColorHelper.sAccentBrushKeys[ lNormalizedIndex % ColorHelper.sAccentBrushKeys.Length ];
		}

		private static void AssignLanes( ICollection<ExperienceTimelineEntryViewModel>? pEntries )
		{
			if ( pEntries == null || pEntries.Count == 0 )
			{
				return;
			}

			List<ExperienceTimelineEntryViewModel> lValidEntries = pEntries
				.Distinct()
				.ToList();

			if ( lValidEntries.Count == 0 )
			{
				return;
			}

			List<ExperienceTimelineEntryViewModel> lOrderedByStartDate = lValidEntries
				.OrderBy( pEntry => pEntry.StartDate )
				.ThenBy( pEntry => pEntry.EndDate ?? DateTime.Today )
				.ThenBy( pEntry => pEntry.CompanyText )
				.ThenBy( pEntry => pEntry.RoleText )
				.ToList();

			for ( int lEntryIndex = 0; lEntryIndex < lOrderedByStartDate.Count; lEntryIndex++ )
			{
				ExperienceTimelineEntryViewModel lEntry = lOrderedByStartDate[ lEntryIndex ];
				lEntry.SetLaneIndex( lEntryIndex );
			}

			List<ExperienceTimelineEntryViewModel> lOrderedForPalette = lValidEntries
				.OrderByDescending( pItem => pItem.StartDate )
				.ThenBy( pItem => pItem.EndDate ?? DateTime.Today )
				.ThenBy( pItem => pItem.CompanyText )
				.ThenBy( pItem => pItem.RoleText )
				.ToList();

			for ( int lPaletteIndex = 0; lPaletteIndex < lOrderedForPalette.Count; lPaletteIndex++ )
			{
				lOrderedForPalette[ lPaletteIndex ].SetPaletteIndex( lPaletteIndex );
			}
		}

		private void ExecuteSelectExperience( object? pParameter )
		{
			if ( pParameter is not ExperienceTimelineEntryViewModel lEntry )
			{
				return;
			}

			SelectedTimelineEntry = lEntry;
			SelectedDate = lEntry.StartDate;
		}

		private void ExecuteSelectDate( object? pParameter )
		{
			switch ( pParameter )
			{
				case DateTime lDateTime:
					{
						SelectedDate = lDateTime;
						return;
					}
				case string lText when DateTime.TryParse( lText, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out DateTime lParsedDate ):
					{
						SelectedDate = lParsedDate;
						return;
					}
			}
		}

		private void SetSelectedDate( DateTime pSelectedDate )
		{
			DateTime lClampedDate = ClampToTimelineRange( pSelectedDate.Date );

			if ( lClampedDate == mSelectedDate || !SetProperty( ref mSelectedDate, lClampedDate ) )
			{
				return;
			}

			SynchronizeSelectionFromSelectedDate();
		}

		private void SetSelectedTimeFrame( TimelineTimeFrameItem? pSelectedTimeFrame )
		{
			if ( ReferenceEquals( mSelectedTimeFrame, pSelectedTimeFrame )
				 || !SetProperty( ref mSelectedTimeFrame, pSelectedTimeFrame )
				 || pSelectedTimeFrame == null
				 || mIsSelectionSynchronizationActive )
			{
				return;
			}

			SelectedDate = pSelectedTimeFrame.StartDate;
		}

		private void SetSelectedTimelineEntry( ExperienceTimelineEntryViewModel? pSelectedTimelineEntry )
		{
			if ( ReferenceEquals( mSelectedTimelineEntry, pSelectedTimelineEntry )
				 || !SetProperty( ref mSelectedTimelineEntry, pSelectedTimelineEntry )
				 || pSelectedTimelineEntry == null
				 || mIsSelectionSynchronizationActive )
			{
				return;
			}

			SelectedDate = pSelectedTimelineEntry.StartDate;
		}

		private void SynchronizeSelectionFromSelectedDate()
		{
			if ( mIsSelectionSynchronizationActive )
			{
				return;
			}

			mIsSelectionSynchronizationActive = true;

			try
			{
				ExperienceTimelineEntryViewModel? lEntry = FindEntryClosestAtOrBefore( mSelectedDate );
				if ( lEntry != null && !ReferenceEquals( mSelectedTimelineEntry, lEntry ) )
				{
					SetProperty( ref mSelectedTimelineEntry, lEntry, nameof( SelectedTimelineEntry ) );
				}

				DateTime lStartDate = lEntry?.StartDate ?? mSelectedDate;
				TimelineTimeFrameItem? lTimeFrame = FindTimeFrameByStartDate( lStartDate );
				if ( lTimeFrame != null && !ReferenceEquals( mSelectedTimeFrame, lTimeFrame ) )
				{
					SetProperty( ref mSelectedTimeFrame, lTimeFrame, nameof( SelectedTimeFrame ) );
				}
			}
			finally
			{
				mIsSelectionSynchronizationActive = false;
			}
		}

		private DateTime ClampToTimelineRange( DateTime pDate )
		{
			DateTime lToday = DateTime.Today;

			if ( pDate < TimelineMinDate )
			{
				return TimelineMinDate;
			}

			return pDate > lToday ? lToday : pDate;
		}

		private ExperienceTimelineEntryViewModel? FindEntryClosestAtOrBefore( DateTime pDate )
		{
			if ( TimelineEntries.Count == 0 )
			{
				return null;
			}

			List<ExperienceTimelineEntryViewModel> lOrdered = TimelineEntries
				.OrderBy( pItem => pItem.StartDate )
				.ThenBy( pItem => pItem.CompanyText )
				.ThenBy( pItem => pItem.RoleText )
				.ToList();

			return lOrdered.LastOrDefault( pItem => pItem.StartDate.Date <= pDate.Date ) ?? lOrdered.FirstOrDefault();
		}

		private TimelineTimeFrameItem? FindTimeFrameByStartDate( DateTime pStartDate )
		{
			return ExperienceTimeFrames.FirstOrDefault( pItem => pItem.StartDate.Date == pStartDate.Date );
		}

		private void RebuildEntries()
		{
			TimelineEntries.Clear();
			ExperienceTimeFrames.Clear();

			List<ExperienceTimelineEntryViewModel> lEntries =
			[
				CreateEntry(
					pCompanyKey: "ExperienceCreaformUiUxExpertCompany",
					pRoleKey: "ExperienceCreaformUiUxExpertRole",
					pLocationKey: "ExperienceCreaformUiUxExpertLocation",
					pScopeKey: "ExperienceCreaformUiUxExpertScope",
					pTechKey: "ExperienceCreaformUiUxExpertTech",
					pStartIsoKey: "ExperienceCreaformUiUxExpertStartIso",
					pEndIsoKey: "ExperienceCreaformUiUxExpertEndIso",
					pAccomplishmentKeys:
					[
						"ExperienceCreaformUiUxExpertAcc1",
						"ExperienceCreaformUiUxExpertAcc2",
						"ExperienceCreaformUiUxExpertAcc3",
						"ExperienceCreaformUiUxExpertAcc4"
					] ),

				CreateEntry(
					pCompanyKey: "ExperienceCreaformSoftwareDeveloperCompany",
					pRoleKey: "ExperienceCreaformSoftwareDeveloperRole",
					pLocationKey: "ExperienceCreaformSoftwareDeveloperLocation",
					pScopeKey: "ExperienceCreaformSoftwareDeveloperScope",
					pTechKey: "ExperienceCreaformSoftwareDeveloperTech",
					pStartIsoKey: "ExperienceCreaformSoftwareDeveloperStartIso",
					pEndIsoKey: "ExperienceCreaformSoftwareDeveloperEndIso",
					pAccomplishmentKeys:
					[
						"ExperienceCreaformSoftwareDeveloperAcc1",
						"ExperienceCreaformSoftwareDeveloperAcc2",
						"ExperienceCreaformSoftwareDeveloperAcc3"
					] ),

				CreateEntry(
					pCompanyKey: "ExperienceArcaneCompany",
					pRoleKey: "ExperienceArcaneRole",
					pLocationKey: "ExperienceArcaneLocation",
					pScopeKey: "ExperienceArcaneScope",
					pTechKey: "ExperienceArcaneTech",
					pStartIsoKey: "ExperienceArcaneStartIso",
					pEndIsoKey: "ExperienceArcaneEndIso",
					pAccomplishmentKeys:
					[
						"ExperienceArcaneAcc1",
						"ExperienceArcaneAcc2",
						"ExperienceArcaneAcc3"
					] ),

				CreateEntry(
					pCompanyKey: "ExperienceIaCompany",
					pRoleKey: "ExperienceIaRole",
					pLocationKey: "ExperienceIaLocation",
					pScopeKey: "ExperienceIaScope",
					pTechKey: "ExperienceIaTech",
					pStartIsoKey: "ExperienceIaStartIso",
					pEndIsoKey: "ExperienceIaEndIso",
					pAccomplishmentKeys:
					[
						"ExperienceIaAcc1",
						"ExperienceIaAcc2"
					] ),

				CreateEntry(
					pCompanyKey: "ExperienceOlhPhotographieCompany",
					pRoleKey: "ExperienceOlhPhotographieRole",
					pLocationKey: "ExperienceOlhPhotographieLocation",
					pScopeKey: "ExperienceOlhPhotographieScope",
					pTechKey: "ExperienceOlhPhotographieTech",
					pStartIsoKey: "ExperienceOlhPhotographieStartIso",
					pEndIsoKey: "ExperienceOlhPhotographieEndIso",
					pAccomplishmentKeys:
					[
						"ExperienceOlhPhotographieAcc1",
						"ExperienceOlhPhotographieAcc2",
						"ExperienceOlhPhotographieAcc3",
						"ExperienceOlhPhotographieAcc4",
						"ExperienceOlhPhotographieAcc5",
						"ExperienceOlhPhotographieAcc6",
						"ExperienceOlhPhotographieAcc7"
					] )
			];

			AssignLanes( lEntries );

			foreach ( ExperienceTimelineEntryViewModel lEntry in lEntries.OrderByDescending( pItem => pItem.StartDate ) )
			{
				TimelineEntries.Add( lEntry );
				ExperienceTimeFrames.Add( CreateTimeFrameForEntry( lEntry ) );
			}

			TimelineMinDate = lEntries.Count > 0 ? lEntries.Min( pItem => pItem.StartDate ).Date : DateTime.Today;

			if ( TimelineEntries.Count > 0 )
			{
				ExperienceTimelineEntryViewModel lFirstEntry = TimelineEntries[ 0 ];
				SelectedTimelineEntry ??= lFirstEntry;

				ExperienceTimelineEntryViewModel? lSelectedEntry = SelectedTimelineEntry;
				SelectedDate = mSelectedDate == default
					? ( lSelectedEntry?.StartDate ?? lFirstEntry.StartDate )
					: ClampToTimelineRange( mSelectedDate );

				return;
			}

			SelectedTimelineEntry = null;
			SelectedTimeFrame = null;
			SelectedDate = DateTime.Today;
		}

		private ExperienceTimelineEntryViewModel CreateEntry(
			string pCompanyKey,
			string pRoleKey,
			string pLocationKey,
			string pScopeKey,
			string pTechKey,
			string pStartIsoKey,
			string pEndIsoKey,
			string[]? pAccomplishmentKeys )
		{
			string lCompany = ResourcesService[ pCompanyKey ];
			string lRole = ResourcesService[ pRoleKey ];
			string lLocation = ResourcesService[ pLocationKey ];
			string lScope = ResourcesService[ pScopeKey ];
			string lTech = ResourcesService[ pTechKey ];

			DateTime lStartDate = ParseIsoDateOrDefault( ResourcesService[ pStartIsoKey ] );
			DateTime? lEndDate = ParseIsoDateOrNull( ResourcesService[ pEndIsoKey ] );

			string[] lAccomplishments = ( pAccomplishmentKeys ?? [] )
				.Select( pKey => ResourcesService[ pKey ] )
				.Where( pText => !string.IsNullOrWhiteSpace( pText ) )
				.ToArray();

			return new ExperienceTimelineEntryViewModel(
				pCompanyText: lCompany,
				pRoleText: lRole,
				pLocationText: lLocation,
				pScopeText: lScope,
				pTechText: lTech,
				pStartDate: lStartDate,
				pEndDate: lEndDate,
				pAccomplishments: new ObservableCollection<string>( lAccomplishments ),
				pResourcesService: ResourcesService );
		}
	}
}
