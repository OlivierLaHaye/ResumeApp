// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Helpers;
using ResumeApp.Infrastructure;
using ResumeApp.Models;
using ResumeApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class ExperiencePageViewModel : ViewModelBase
	{
		private bool mIsSelectionSynchronizationActive;

		public ObservableCollection<ExperienceTimelineEntryViewModel> TimelineEntries { get; }

		public ObservableCollection<TimelineTimeFrameItem> ExperienceTimeFrames { get; }

		public string TimelineControlInteractionsHelpText => ResourcesService[ "TimelineControlInteractionsHelpText" ];

		private DateTime mTimelineMinDate;
		public DateTime TimelineMinDate
		{
			get => mTimelineMinDate;
			private set => SetProperty( ref mTimelineMinDate, value );
		}

		private DateTime mSelectedDate;
		public DateTime SelectedDate
		{
			get => mSelectedDate;
			set => SetSelectedDate( value );
		}

		private TimelineTimeFrameItem mSelectedTimeFrame;
		public TimelineTimeFrameItem SelectedTimeFrame
		{
			get => mSelectedTimeFrame;
			set => SetSelectedTimeFrame( value );
		}

		private ExperienceTimelineEntryViewModel mSelectedTimelineEntry;
		public ExperienceTimelineEntryViewModel SelectedTimelineEntry
		{
			get => mSelectedTimelineEntry;
			set => SetSelectedTimelineEntry( value );
		}

		private ICommand mSelectExperienceCommand;
		public ICommand SelectExperienceCommand =>
							mSelectExperienceCommand ??
							( mSelectExperienceCommand = new ParameterRelayCommand( ExecuteSelectExperience ) );

		private ICommand mSelectDateCommand;
		public ICommand SelectDateCommand =>
							mSelectDateCommand ??
							( mSelectDateCommand = new ParameterRelayCommand( ExecuteSelectDate ) );

		public ExperiencePageViewModel( ResourcesService pResourcesService, ThemeService pThemeService )
			: base( pResourcesService, pThemeService )
		{
			TimelineEntries = new ObservableCollection<ExperienceTimelineEntryViewModel>();
			ExperienceTimeFrames = new ObservableCollection<TimelineTimeFrameItem>();

			ResourcesService.PropertyChanged += ( pSender, pArgs ) =>
			{
				RaisePropertyChanged( nameof( TimelineControlInteractionsHelpText ) );
				RebuildEntries();
			};

			RebuildEntries();
		}

		private static DateTime ParseIsoDateOrDefault( string pIsoText )
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

			if ( DateTime.TryParseExact(
				pIsoText,
				"yyyy-MM-dd",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeLocal,
				out DateTime lParsedDate ) )
			{
				return lParsedDate;
			}

			return null;
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
			if ( ColorHelper.sAccentBrushKeys == null || ColorHelper.sAccentBrushKeys.Length == 0 )
			{
				return "CommonBlueBrush";
			}

			int lNormalizedIndex = pPaletteIndex < 0 ? 0 : pPaletteIndex;
			return ColorHelper.sAccentBrushKeys[ lNormalizedIndex % ColorHelper.sAccentBrushKeys.Length ];
		}

		private static void AssignLanes( ICollection<ExperienceTimelineEntryViewModel> pEntries )
		{
			if ( pEntries == null || pEntries.Count == 0 )
			{
				return;
			}

			List<ExperienceTimelineEntryViewModel> lValidEntries = pEntries
				.Where( pItem => pItem != null )
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

		private void ExecuteSelectExperience( object pParameter )
		{
			if ( !( pParameter is ExperienceTimelineEntryViewModel lEntry ) )
			{
				return;
			}

			SelectedTimelineEntry = lEntry;
			SelectedDate = lEntry.StartDate;
		}

		private void ExecuteSelectDate( object pParameter )
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
						break;
					}
			}
		}

		private void SetSelectedDate( DateTime pSelectedDate )
		{
			DateTime lClampedDate = ClampToTimelineRange( pSelectedDate.Date );

			if ( lClampedDate == mSelectedDate )
			{
				return;
			}

			bool lHasChanged = SetProperty( ref mSelectedDate, lClampedDate );

			if ( !lHasChanged )
			{
				return;
			}

			SynchronizeSelectionFromSelectedDate();
		}

		private void SetSelectedTimeFrame( TimelineTimeFrameItem pSelectedTimeFrame )
		{
			if ( ReferenceEquals( mSelectedTimeFrame, pSelectedTimeFrame ) )
			{
				return;
			}

			bool lHasChanged = SetProperty( ref mSelectedTimeFrame, pSelectedTimeFrame );

			if ( !lHasChanged )
			{
				return;
			}

			if ( pSelectedTimeFrame == null )
			{
				return;
			}

			if ( mIsSelectionSynchronizationActive )
			{
				return;
			}

			SelectedDate = pSelectedTimeFrame.StartDate;
		}

		private void SetSelectedTimelineEntry( ExperienceTimelineEntryViewModel pSelectedTimelineEntry )
		{
			if ( ReferenceEquals( mSelectedTimelineEntry, pSelectedTimelineEntry ) )
			{
				return;
			}

			bool lHasChanged = SetProperty( ref mSelectedTimelineEntry, pSelectedTimelineEntry );

			if ( !lHasChanged )
			{
				return;
			}

			if ( pSelectedTimelineEntry == null )
			{
				return;
			}

			if ( mIsSelectionSynchronizationActive )
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
				ExperienceTimelineEntryViewModel lEntry = FindEntryClosestAtOrBefore( mSelectedDate );
				if ( lEntry != null && !ReferenceEquals( mSelectedTimelineEntry, lEntry ) )
				{
					SetProperty( ref mSelectedTimelineEntry, lEntry, nameof( SelectedTimelineEntry ) );
				}

				TimelineTimeFrameItem lTimeFrame = FindTimeFrameByStartDate( lEntry?.StartDate ?? mSelectedDate );
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

			if ( pDate > lToday )
			{
				return lToday;
			}

			return pDate;
		}

		private ExperienceTimelineEntryViewModel FindEntryClosestAtOrBefore( DateTime pDate )
		{
			if ( TimelineEntries.Count == 0 )
			{
				return null;
			}

			List<ExperienceTimelineEntryViewModel> lOrdered = TimelineEntries
				.Where( pItem => pItem != null )
				.OrderBy( pItem => pItem.StartDate )
				.ThenBy( pItem => pItem.CompanyText )
				.ThenBy( pItem => pItem.RoleText )
				.ToList();

			ExperienceTimelineEntryViewModel lMatch = lOrdered
				.LastOrDefault( pItem => pItem.StartDate.Date <= pDate.Date );

			return lMatch ?? lOrdered.FirstOrDefault();
		}

		private TimelineTimeFrameItem FindTimeFrameByStartDate( DateTime pStartDate )
		{
			return ExperienceTimeFrames
				.Where( pItem => pItem != null )
				.FirstOrDefault( pItem => pItem.StartDate.Date == pStartDate.Date );
		}

		private void RebuildEntries()
		{
			TimelineEntries.Clear();
			ExperienceTimeFrames.Clear();

			var lEntries = new[]
			{
				CreateEntry(
					pCompanyKey: "ExperienceCreaformUiUxExpertCompany",
					pRoleKey: "ExperienceCreaformUiUxExpertRole",
					pLocationKey: "ExperienceCreaformUiUxExpertLocation",
					pScopeKey: "ExperienceCreaformUiUxExpertScope",
					pTechKey: "ExperienceCreaformUiUxExpertTech",
					pStartIsoKey: "ExperienceCreaformUiUxExpertStartIso",
					pEndIsoKey: "ExperienceCreaformUiUxExpertEndIso",
					pAccomplishmentKeys: new[]
					{
						"ExperienceCreaformUiUxExpertAcc1",
						"ExperienceCreaformUiUxExpertAcc2",
						"ExperienceCreaformUiUxExpertAcc3",
						"ExperienceCreaformUiUxExpertAcc4"
					} ),

				CreateEntry(
					pCompanyKey: "ExperienceCreaformSoftwareDeveloperCompany",
					pRoleKey: "ExperienceCreaformSoftwareDeveloperRole",
					pLocationKey: "ExperienceCreaformSoftwareDeveloperLocation",
					pScopeKey: "ExperienceCreaformSoftwareDeveloperScope",
					pTechKey: "ExperienceCreaformSoftwareDeveloperTech",
					pStartIsoKey: "ExperienceCreaformSoftwareDeveloperStartIso",
					pEndIsoKey: "ExperienceCreaformSoftwareDeveloperEndIso",
					pAccomplishmentKeys: new[]
					{
						"ExperienceCreaformSoftwareDeveloperAcc1",
						"ExperienceCreaformSoftwareDeveloperAcc2",
						"ExperienceCreaformSoftwareDeveloperAcc3"
					} ),

				CreateEntry(
					pCompanyKey: "ExperienceArcaneCompany",
					pRoleKey: "ExperienceArcaneRole",
					pLocationKey: "ExperienceArcaneLocation",
					pScopeKey: "ExperienceArcaneScope",
					pTechKey: "ExperienceArcaneTech",
					pStartIsoKey: "ExperienceArcaneStartIso",
					pEndIsoKey: "ExperienceArcaneEndIso",
					pAccomplishmentKeys: new[]
					{
						"ExperienceArcaneAcc1",
						"ExperienceArcaneAcc2",
						"ExperienceArcaneAcc3"
					} ),

				CreateEntry(
					pCompanyKey: "ExperienceIaCompany",
					pRoleKey: "ExperienceIaRole",
					pLocationKey: "ExperienceIaLocation",
					pScopeKey: "ExperienceIaScope",
					pTechKey: "ExperienceIaTech",
					pStartIsoKey: "ExperienceIaStartIso",
					pEndIsoKey: "ExperienceIaEndIso",
					pAccomplishmentKeys: new[]
					{
						"ExperienceIaAcc1",
						"ExperienceIaAcc2"
					} ),

				CreateEntry(
					pCompanyKey: "ExperienceOlhPhotographieCompany",
					pRoleKey: "ExperienceOlhPhotographieRole",
					pLocationKey: "ExperienceOlhPhotographieLocation",
					pScopeKey: "ExperienceOlhPhotographieScope",
					pTechKey: "ExperienceOlhPhotographieTech",
					pStartIsoKey: "ExperienceOlhPhotographieStartIso",
					pEndIsoKey: "ExperienceOlhPhotographieEndIso",
					pAccomplishmentKeys: new[]
					{
						"ExperienceOlhPhotographieAcc1",
						"ExperienceOlhPhotographieAcc2",
						"ExperienceOlhPhotographieAcc3",
						"ExperienceOlhPhotographieAcc4",
						"ExperienceOlhPhotographieAcc5",
						"ExperienceOlhPhotographieAcc6",
						"ExperienceOlhPhotographieAcc7"
					} )
			}.Where( pItem => pItem != null ).ToList();

			AssignLanes( lEntries );

			foreach ( ExperienceTimelineEntryViewModel lEntry in lEntries.OrderByDescending( pItem => pItem.StartDate ) )
			{
				TimelineEntries.Add( lEntry );
				ExperienceTimeFrames.Add( CreateTimeFrameForEntry( lEntry ) );
			}

			DateTime lMinDate = lEntries.Count > 0 ? lEntries.Min( pItem => pItem.StartDate ).Date : DateTime.Today;
			TimelineMinDate = lMinDate;

			if ( TimelineEntries.Count > 0 )
			{
				if ( SelectedTimelineEntry == null )
				{
					SelectedTimelineEntry = TimelineEntries[ 0 ];
				}

				SelectedDate = mSelectedDate == default ? SelectedTimelineEntry.StartDate : ClampToTimelineRange( mSelectedDate );
			}
			else
			{
				SelectedTimelineEntry = null;
				SelectedTimeFrame = null;
				SelectedDate = DateTime.Today;
			}
		}

		private ExperienceTimelineEntryViewModel CreateEntry(
			string pCompanyKey,
			string pRoleKey,
			string pLocationKey,
			string pScopeKey,
			string pTechKey,
			string pStartIsoKey,
			string pEndIsoKey,
			string[] pAccomplishmentKeys )
		{
			string lCompany = ResourcesService[ pCompanyKey ];
			string lRole = ResourcesService[ pRoleKey ];
			string lLocation = ResourcesService[ pLocationKey ];
			string lScope = ResourcesService[ pScopeKey ];
			string lTech = ResourcesService[ pTechKey ];

			DateTime lStartDate = ParseIsoDateOrDefault( ResourcesService[ pStartIsoKey ] );
			DateTime? lEndDate = ParseIsoDateOrNull( ResourcesService[ pEndIsoKey ] );

			string[] lAccomplishments = ( pAccomplishmentKeys ?? Array.Empty<string>() )
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
