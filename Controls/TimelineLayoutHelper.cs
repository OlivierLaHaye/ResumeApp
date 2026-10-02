// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Globalization;
using ResumeApp.AttachedProperties;

namespace ResumeApp.Controls
{
	internal enum TimelineTickGranularity
	{
		Years,
		Months
	}

	internal readonly struct TimelineTickSchedule( TimelineTickGranularity pGranularity, int pStep )
	{
		public TimelineTickGranularity Granularity { get; } = pGranularity;

		public int Step { get; } = Math.Max( 1, pStep );
	}

	internal readonly struct TimelineAxisTick( DateTime pDate, bool pIsMajor, bool pIsGrid, string? pLabel, bool pIsEmphasized )
	{
		public DateTime Date { get; } = pDate;

		public bool IsMajor { get; } = pIsMajor;

		public bool IsGrid { get; } = pIsGrid;

		public string? Label { get; } = pLabel;

		public bool IsEmphasized { get; } = pIsEmphasized;
	}

	internal readonly struct TimelineBarLabelSegment( string pText, bool pIsBold )
	{
		public string Text { get; } = pText;

		public bool IsBold { get; } = pIsBold;
	}

	internal readonly struct TimelineSpan( DateTime pStart, DateTime pEnd )
	{
		public DateTime Start { get; } = pStart;

		public DateTime End { get; } = pEnd;

		public bool IsValid => End >= Start;
	}

	internal readonly struct TimelineGeometry(
		double pPaddingLeft,
		double pPaddingTop,
		double pPaddingRight,
		double pPaddingBottom,
		double pMarkerRowHeight,
		double pBarHeight,
		double pLanePitch,
		double pBarCornerRadius,
		double pLaneToBaselineGap,
		double pAxisFontSize,
		double pBarLabelFontSize,
		double pLabelRowHeight,
		double pLabelPadding )
	{
		public double PaddingLeft { get; } = pPaddingLeft;

		public double PaddingTop { get; } = pPaddingTop;

		public double PaddingRight { get; } = pPaddingRight;

		public double PaddingBottom { get; } = pPaddingBottom;

		public double MarkerRowHeight { get; } = pMarkerRowHeight;

		public double BarHeight { get; } = pBarHeight;

		public double LanePitch { get; } = pLanePitch;

		public double BarCornerRadius { get; } = pBarCornerRadius;

		public double LaneToBaselineGap { get; } = pLaneToBaselineGap;

		public double AxisFontSize { get; } = pAxisFontSize;

		public double BarLabelFontSize { get; } = pBarLabelFontSize;

		public double LabelRowHeight { get; } = pLabelRowHeight;

		public double LabelPadding { get; } = pLabelPadding;
	}

	internal readonly struct TimelineFitRange( DateTime pStart, DateTime pEnd, double pZoom, double pMaxZoom, double pSpanDays )
	{
		public DateTime Start { get; } = pStart;

		public DateTime End { get; } = pEnd;

		public double Zoom { get; } = pZoom;

		public double MaxZoom { get; } = pMaxZoom;

		public double SpanDays { get; } = pSpanDays;
	}

	internal static class TimelineLayoutHelper
	{
		public const double FitPaddingLeftPixels = 12.0;
		public const double FitPaddingRightPixels = 20.0;
		public const double MinimumSpanDays = 180.0;
		public const double ZoomStepFactor = 1.25;
		public const double MinimumEllipsisWidth = 56.0;
		public const double MinimumHitWidth = 16.0;
		public const double MinimumLabelGapPixels = 20.0;
		public const double PillPaddingX = 8.0;
		public const double PillTopGap = 4.0;
		public const string Ellipsis = "\u2026";

		private const double AverageDaysPerMonth = 30.44;
		private const double AverageDaysPerYear = 365.25;
		private const double QuarterTickMinimumPixels = 8.0;
		private const double AverageDaysPerQuarter = 91.0;

		private static readonly int[] sMonthSteps = [ 1, 3, 6 ];
		private static readonly int[] sYearSteps = [ 1, 2, 5, 10 ];

		public static TimelineGeometry GetGeometry( LayoutSizeClass pSizeClass )
		{
			return pSizeClass switch
			{
				LayoutSizeClass.Compact => new TimelineGeometry( 4.0, 6.0, 4.0, 4.0, 12.0, 22.0, 28.0, 5.0, 10.0, 11.0, 11.0, 18.0, 8.0 ),
				LayoutSizeClass.Wide => new TimelineGeometry( 8.0, 10.0, 8.0, 8.0, 16.0, 30.0, 40.0, 6.0, 12.0, 12.0, 13.0, 22.0, 10.0 ),
				_ => new TimelineGeometry( 8.0, 8.0, 8.0, 6.0, 16.0, 28.0, 36.0, 6.0, 12.0, 12.0, 12.0, 22.0, 10.0 )
			};
		}

		public static double ComputeLaneTop( TimelineGeometry pGeometry ) => pGeometry.PaddingTop + pGeometry.MarkerRowHeight;

		public static double ComputeBaselineY( TimelineGeometry pGeometry, int pLaneCount )
		{
			return ComputeLaneTop( pGeometry ) + ( Math.Max( 0, pLaneCount ) * pGeometry.LanePitch ) + pGeometry.LaneToBaselineGap;
		}

		public static double ComputeHeight( TimelineGeometry pGeometry, int pLaneCount )
		{
			return ComputeBaselineY( pGeometry, pLaneCount ) + pGeometry.LabelRowHeight + pGeometry.PaddingBottom;
		}

		public static double ComputePillHeight( TimelineGeometry pGeometry )
		{
			var lPreferred = pGeometry.AxisFontSize + 8.0;
			var lAvailable = pGeometry.LabelRowHeight + pGeometry.PaddingBottom - PillTopGap - 1.0;
			return Math.Min( lPreferred, lAvailable );
		}

		public static int[] AssignStableLanes( IReadOnlyList<TimelineSpan> pSpans, out int pLaneCount )
		{
			var lLaneByIndex = new int[ pSpans.Count ];
			Array.Fill( lLaneByIndex, -1 );

			var lOrder = Enumerable.Range( 0, pSpans.Count )
				.Where( pIndex => pSpans[ pIndex ].IsValid )
				.OrderBy( pIndex => pSpans[ pIndex ].Start )
				.ThenByDescending( pIndex => pSpans[ pIndex ].End )
				.ThenBy( pIndex => pIndex )
				.ToList();

			var lLaneEnds = new List<DateTime>();
			var lLaneMembers = new List<List<int>>();

			foreach ( var lIndex in lOrder )
			{
				var lSpan = pSpans[ lIndex ];
				var lLane = lLaneEnds.FindIndex( pEnd => pEnd <= lSpan.Start );

				if ( lLane < 0 )
				{
					lLane = lLaneEnds.Count;
					lLaneEnds.Add( lSpan.End );
					lLaneMembers.Add( [] );
				}
				else
				{
					lLaneEnds[ lLane ] = lSpan.End;
				}

				lLaneMembers[ lLane ].Add( lIndex );
			}

			var lLaneOrder = Enumerable.Range( 0, lLaneMembers.Count )
				.OrderByDescending( pLane => lLaneMembers[ pLane ].Max( pIndex => pSpans[ pIndex ].Start ) )
				.ThenBy( pLane => pLane )
				.ToList();

			for ( var lFinalLane = 0; lFinalLane < lLaneOrder.Count; lFinalLane++ )
			{
				foreach ( var lIndex in lLaneMembers[ lLaneOrder[ lFinalLane ] ] )
				{
					lLaneByIndex[ lIndex ] = lFinalLane;
				}
			}

			pLaneCount = lLaneMembers.Count;
			return lLaneByIndex;
		}

		public static void FindTouchingNeighbors( IReadOnlyList<TimelineSpan> pSpans, IReadOnlyList<int> pLanes, out int[] pPrevious, out int[] pNext )
		{
			pPrevious = new int[ pSpans.Count ];
			pNext = new int[ pSpans.Count ];
			Array.Fill( pPrevious, -1 );
			Array.Fill( pNext, -1 );

			for ( var lIndex = 0; lIndex < pSpans.Count; lIndex++ )
			{
				if ( pLanes[ lIndex ] < 0 )
				{
					continue;
				}

				for ( var lOther = 0; lOther < pSpans.Count; lOther++ )
				{
					if ( lOther == lIndex || pLanes[ lOther ] != pLanes[ lIndex ] || pSpans[ lOther ].End != pSpans[ lIndex ].Start )
					{
						continue;
					}

					pPrevious[ lIndex ] = lOther;
					pNext[ lOther ] = lIndex;
					break;
				}
			}
		}

		public static TimelineFitRange ComputeFitRange( DateTime pMinDate, DateTime pToday, double pPlotWidth )
		{
			var lWidth = Math.Max( 1.0, pPlotWidth );
			var lRangeDays = Math.Max( 1.0, ( pToday - pMinDate ).TotalDays );
			var lUsableWidth = lWidth > ( ( FitPaddingLeftPixels + FitPaddingRightPixels ) * 2.0 )
				? lWidth - ( FitPaddingLeftPixels + FitPaddingRightPixels )
				: lWidth * 0.5;

			var lZoom = lUsableWidth / lRangeDays;
			var lStart = pMinDate.AddDays( -( FitPaddingLeftPixels / lZoom ) );
			var lEnd = pToday.AddDays( FitPaddingRightPixels / lZoom );
			var lMaxZoom = Math.Max( lZoom, lWidth / MinimumSpanDays );

			return new TimelineFitRange( lStart, lEnd, lZoom, lMaxZoom, lWidth / lZoom );
		}

		public static bool IsShowingAll( double pPlotWidth, double pZoom, TimelineFitRange pFitRange )
		{
			if ( pZoom <= 0.0 )
			{
				return true;
			}

			var lVisibleDays = Math.Max( 1.0, pPlotWidth ) / pZoom;
			return lVisibleDays >= pFitRange.SpanDays - 1.0;
		}

		public static TimelineTickSchedule ChooseTickSchedule( double pPixelsPerDay, double pRequiredLabelSpacing )
		{
			foreach ( var lStep in sMonthSteps )
			{
				if ( pPixelsPerDay * AverageDaysPerMonth * lStep >= pRequiredLabelSpacing )
				{
					return new TimelineTickSchedule( TimelineTickGranularity.Months, lStep );
				}
			}

			foreach ( var lStep in sYearSteps )
			{
				if ( pPixelsPerDay * AverageDaysPerYear * lStep >= pRequiredLabelSpacing )
				{
					return new TimelineTickSchedule( TimelineTickGranularity.Years, lStep );
				}
			}

			return new TimelineTickSchedule( TimelineTickGranularity.Years, sYearSteps[ ^1 ] );
		}

		public static List<TimelineAxisTick> BuildAxisTicks(
			DateTime pViewStart,
			DateTime pViewEnd,
			DateTime pToday,
			double pPixelsPerDay,
			TimelineTickSchedule pSchedule,
			CultureInfo pCulture )
		{
			var lTicks = new List<TimelineAxisTick>();
			var lAxisEnd = pViewEnd < pToday ? pViewEnd : pToday;

			if ( lAxisEnd < pViewStart )
			{
				return lTicks;
			}

			var lFirstYear = pViewStart.Year;
			var lLastYear = lAxisEnd.Year;

			for ( var lYear = lFirstYear; lYear <= lLastYear; lYear++ )
			{
				if ( pSchedule.Granularity == TimelineTickGranularity.Months )
				{
					AddMonthTicks( lTicks, lYear, pViewStart, lAxisEnd, pSchedule.Step, pCulture );
					continue;
				}

				AddYearTicks( lTicks, lYear, pViewStart, lAxisEnd, pSchedule.Step, pPixelsPerDay, pCulture );
			}

			return lTicks;
		}

		public static string FormatAxisLabel( DateTime pDate, TimelineTickGranularity pGranularity, CultureInfo pCulture )
		{
			if ( pGranularity == TimelineTickGranularity.Years || pDate.Month == 1 )
			{
				return pDate.ToString( "yyyy", pCulture );
			}

			return pDate.ToString( "MMM", pCulture );
		}

		public static string FormatPillLabel( DateTime pDate, CultureInfo pCulture ) => pDate.ToString( "MMM yyyy", pCulture );

		public static IReadOnlyList<TimelineBarLabelSegment> ChooseBarLabel(
			string? pCompany,
			string? pRole,
			string pSeparator,
			bool pPreferRoleOnly,
			double pAvailableWidth,
			Func<string, bool, double> pMeasure )
		{
			var lCompany = pCompany ?? string.Empty;
			var lRole = pRole ?? string.Empty;

			if ( pAvailableWidth <= 0.0 || ( lCompany.Length == 0 && lRole.Length == 0 ) )
			{
				return [];
			}

			if ( ( pPreferRoleOnly && lRole.Length > 0 ) || lCompany.Length == 0 )
			{
				return FitSingle( lRole, false, pAvailableWidth, pMeasure );
			}

			if ( lRole.Length == 0 )
			{
				return FitSingle( lCompany, true, pAvailableWidth, pMeasure );
			}

			var lCompanyWidth = pMeasure( lCompany, true );
			var lRoleText = pSeparator + lRole;

			if ( lCompanyWidth + pMeasure( lRoleText, false ) <= pAvailableWidth )
			{
				return [ new TimelineBarLabelSegment( lCompany, true ), new TimelineBarLabelSegment( lRoleText, false ) ];
			}

			return FitSingle( lCompany, true, pAvailableWidth, pMeasure );
		}

		public static IReadOnlyList<TimelineBarLabelSegment> Ellipsize(
			string pText,
			bool pIsBold,
			double pAvailableWidth,
			Func<string, bool, double> pMeasure )
		{
			if ( pAvailableWidth < MinimumEllipsisWidth || pText.Length == 0 )
			{
				return [];
			}

			var lText = pText;
			while ( lText.Length > 1 && pMeasure( lText.TrimEnd() + Ellipsis, pIsBold ) > pAvailableWidth )
			{
				lText = lText[ ..^1 ];
			}

			return [ new TimelineBarLabelSegment( lText.TrimEnd() + Ellipsis, pIsBold ) ];
		}

		public static string? GetEdgeBrushKey( string? pAccentBrushKey )
		{
			const string StrongSuffix = "StrongBrush";

			if ( string.IsNullOrWhiteSpace( pAccentBrushKey ) || !pAccentBrushKey.EndsWith( StrongSuffix, StringComparison.Ordinal ) )
			{
				return null;
			}

			return string.Concat( pAccentBrushKey.AsSpan( 0, pAccentBrushKey.Length - StrongSuffix.Length ), "EdgeBrush" );
		}

		public static string FormatFrameHeadline( string? pTitle, string? pSubtitle, string pSeparator )
		{
			var lTitle = pTitle ?? string.Empty;
			var lSubtitle = pSubtitle ?? string.Empty;

			if ( lTitle.Length == 0 )
			{
				return lSubtitle;
			}

			return lSubtitle.Length == 0 ? lTitle : lTitle + pSeparator + lSubtitle;
		}

		private static IReadOnlyList<TimelineBarLabelSegment> FitSingle(
			string pText,
			bool pIsBold,
			double pAvailableWidth,
			Func<string, bool, double> pMeasure )
		{
			if ( pMeasure( pText, pIsBold ) <= pAvailableWidth )
			{
				return [ new TimelineBarLabelSegment( pText, pIsBold ) ];
			}

			return Ellipsize( pText, pIsBold, pAvailableWidth, pMeasure );
		}

		private static void AddMonthTicks(
			List<TimelineAxisTick> pTicks,
			int pYear,
			DateTime pViewStart,
			DateTime pAxisEnd,
			int pStep,
			CultureInfo pCulture )
		{
			for ( var lMonth = 1; lMonth <= 12; lMonth++ )
			{
				var lDate = new DateTime( pYear, lMonth, 1 );
				if ( lDate < pViewStart || lDate > pAxisEnd )
				{
					continue;
				}

				var lIsMajor = ( ( lMonth - 1 ) % pStep ) == 0;
				var lIsJanuary = lMonth == 1;
				var lLabel = lIsMajor ? FormatAxisLabel( lDate, TimelineTickGranularity.Months, pCulture ) : null;

				pTicks.Add( new TimelineAxisTick( lDate, lIsMajor, lIsJanuary && lIsMajor, lLabel, lIsJanuary ) );
			}
		}

		private static void AddYearTicks(
			List<TimelineAxisTick> pTicks,
			int pYear,
			DateTime pViewStart,
			DateTime pAxisEnd,
			int pStep,
			double pPixelsPerDay,
			CultureInfo pCulture )
		{
			var lYearStart = new DateTime( pYear, 1, 1 );
			if ( lYearStart >= pViewStart && lYearStart <= pAxisEnd )
			{
				var lIsMajor = ( pYear % pStep ) == 0;
				var lLabel = lIsMajor ? FormatAxisLabel( lYearStart, TimelineTickGranularity.Years, pCulture ) : null;

				pTicks.Add( new TimelineAxisTick( lYearStart, lIsMajor, lIsMajor, lLabel, false ) );
			}

			if ( pStep != 1 || pPixelsPerDay * AverageDaysPerQuarter < QuarterTickMinimumPixels )
			{
				return;
			}

			foreach ( var lMonth in new[] { 4, 7, 10 } )
			{
				var lQuarter = new DateTime( pYear, lMonth, 1 );
				if ( lQuarter >= pViewStart && lQuarter <= pAxisEnd )
				{
					pTicks.Add( new TimelineAxisTick( lQuarter, false, false, null, false ) );
				}
			}
		}
	}
}
