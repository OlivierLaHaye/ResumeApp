// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ResumeApp.Controls
{
	public sealed class TimelineControl : Control
	{
		private sealed class TimeFrameHitInfo( TimelineTimeFrameItem pItem, Rect pHitRect )
		{
			public TimelineTimeFrameItem Item { get; } = pItem;

			public Rect HitRect { get; } = pHitRect;
		}

		private sealed class VisibleTimeFrame(
			TimelineTimeFrameItem pItem,
			DateTime pStartDate,
			DateTime pEndDate,
			double pStartX,
			double pEndX,
			int pLaneIndex )
		{
			public TimelineTimeFrameItem Item { get; } = pItem;

			public DateTime StartDate { get; } = pStartDate;

			public DateTime EndDate { get; } = pEndDate;

			public double StartX { get; } = pStartX;

			public double EndX { get; } = pEndX;

			public int LaneIndex { get; } = pLaneIndex;
		}

		private sealed class TimeFrameTitleDrawInfo( TimelineTimeFrameItem pItem, FormattedText pText, Rect pRect )
		{
			public TimelineTimeFrameItem Item { get; } = pItem;

			public FormattedText Text { get; } = pText;

			public Rect Rect { get; } = pRect;
		}

		private readonly struct PanSample( DateTime pTimestampUtc, double pPointerX )
		{
			public DateTime TimestampUtc { get; } = pTimestampUtc;

			public double PointerX { get; } = pPointerX;
		}

		private readonly struct RadiusXy( double pX, double pY )
		{
			public double X { get; } = pX;

			public double Y { get; } = pY;
		}

		private readonly struct TickLabelSchedule( TickGranularity pGranularity, int pStep )
		{
			public TickGranularity Granularity { get; } = pGranularity;

			public int Step { get; } = Math.Max( 1, pStep );
		}

		private readonly struct FormattedTextCacheKey( string pText, double pFontSize )
			: IEquatable<FormattedTextCacheKey>
		{
			public string Text { get; } = pText ?? string.Empty;

			public double FontSize { get; } = pFontSize;

			public bool Equals( FormattedTextCacheKey pOther )
			{
				return string.Equals( Text, pOther.Text, StringComparison.Ordinal )
					   && Math.Abs( FontSize - pOther.FontSize ) < 0.000001;
			}

			public override bool Equals( object pObject )
			{
				return pObject is FormattedTextCacheKey lOther && Equals( lOther );
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var lHash = 17;
					lHash = ( lHash * 31 ) + StringComparer.Ordinal.GetHashCode( Text );
					lHash = ( lHash * 31 ) + FontSize.GetHashCode();
					return lHash;
				}
			}
		}

		private enum TickGranularity
		{
			Years,
			Months,
			Weeks,
			Days
		}

		private enum KeyboardStep
		{
			Day,
			Week,
			Month,
			Year,
			Decade
		}

		private const double MinimumZoomPixelsPerDay = 0.08;
		private const double MaximumZoomPixelsPerDay = 48.0;
		private const double WheelZoomFactorPerNotch = 1.12;

		private const double DefaultPadding = 16.0;

		private const double TimeFrameBarHeight = 22.0;
		private const double TimeFrameLaneHeight = 28.0;
		private const double TimeFrameLaneGapPixels = 8.0;

		private const double TickMajorHeight = 8.0;
		private const double TickMinorHeight = 4.0;

		private const double DateLabelFontSize = 12.0;
		private const double TimeFrameLabelFontSize = 12.0;

		private const double MajorTickLabelGapPixels = 20.0;

		private const double DragActivationThresholdPixels = 3.0;

		private const double FitAnimationDurationMilliseconds = 240.0;

		private const double TimeFrameTitleDotToTextGapPixels = 6.0;
		private const double TimeFrameTitleInnerPaddingPixels = 8.0;
		private const double TimeFrameTitleMinimumSpacingPixels = 10.0;

		private const int ClickMaximumDurationMilliseconds = 320;

		public static readonly DependencyProperty sMinDateProperty =
			DependencyProperty.Register(
				nameof( MinDate ),
				typeof( DateTime ),
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata(
					DateTime.MinValue,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnMinDateChanged,
					CoerceMinDate ) );

		public static readonly DependencyProperty sSelectedDateProperty =
			DependencyProperty.Register(
				nameof( SelectedDate ),
				typeof( DateTime ),
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata(
					DateTime.Today,
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender,
					OnSelectedDateChanged,
					CoerceSelectedDate ) );

		public static readonly DependencyProperty sSelectedTimeFrameProperty =
			DependencyProperty.Register(
				nameof( SelectedTimeFrame ),
				typeof( TimelineTimeFrameItem ),
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata(
					null,
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender,
					OnSelectedTimeFrameChanged ) );

		public static readonly DependencyProperty sTimeFramesProperty =
			DependencyProperty.Register(
				nameof( TimeFrames ),
				typeof( ObservableCollection<TimelineTimeFrameItem> ),
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata(
					null,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnTimeFramesChanged ) );

		public static readonly DependencyProperty sZoomLevelProperty =
			DependencyProperty.Register(
				nameof( ZoomLevel ),
				typeof( double ),
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata(
					2.0,
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender,
					OnZoomLevelChanged,
					CoerceZoomLevel ) );

		public static readonly DependencyProperty sViewportStartTicksProperty =
			DependencyProperty.Register(
				nameof( ViewportStartTicks ),
				typeof( double ),
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata(
					( double )DateTime.Today.AddYears( -1 ).Ticks,
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender,
					OnViewportStartTicksChanged,
					CoerceViewportStartTicks ) );

		private readonly List<TimeFrameHitInfo> mTimeFrameHitInfos;
		private readonly List<PanSample> mPanSamples;

		private bool mIsPointerDown;
		private bool mHasDragged;
		private Point mPointerDownPoint;
		private double mViewportStartTicksAtPointerDown;

		private DateTime mPointerDownTimestampUtc;
		private TimelineTimeFrameItem mPointerDownTimeFrameItem;

		private bool mIsInertiaActive;
		private double mInertiaVelocityTicksPerSecond;
		private DateTime mLastRenderTick;

		private bool mIsFitAnimationActive;
		private DateTime mFitAnimationStartUtc;
		private double mFitStartViewportTicks;
		private double mFitTargetViewportTicks;
		private double mFitStartZoom;
		private double mFitTargetZoom;

		private readonly Dictionary<FormattedTextCacheKey, FormattedText> mFormattedTextCache;

		private bool mIsRenderingSubscribed;

		private bool mHasPendingPan;
		private double mPendingPanPointerX;
		private DateTime mPendingPanTimestampUtc;

		private bool mIsInternalSelectedDateUpdate;

		private bool mIsHandCursorActive;

		private string mTextCacheCultureName;
		private FontFamily mTextCacheFontFamily;
		private FontStyle mTextCacheFontStyle;
		private FontWeight mTextCacheFontWeight;
		private FontStretch mTextCacheFontStretch;
		private double mTextCachePixelsPerDip;
		private Brush mTextCacheBrush;

		private bool mHasUserInteracted;
		private bool mHasAppliedInitialFit;
		private bool mHasSuppressEnsureDateVisible;
		private bool mHasSuppressSelectedTimeFrameToSelectedDateSync;
		private int mSelectedTimeFrameToSelectedDateSyncSuppressionVersion;

		public DateTime MinDate
		{
			get => ( DateTime )GetValue( sMinDateProperty );
			set => SetCurrentValue( sMinDateProperty, value );
		}

		public DateTime SelectedDate
		{
			get => ( DateTime )GetValue( sSelectedDateProperty );
			set => SetCurrentValue( sSelectedDateProperty, value );
		}

		public TimelineTimeFrameItem SelectedTimeFrame
		{
			get => ( TimelineTimeFrameItem )GetValue( sSelectedTimeFrameProperty );
			set => SetCurrentValue( sSelectedTimeFrameProperty, value );
		}

		public ObservableCollection<TimelineTimeFrameItem> TimeFrames
		{
			get => ( ObservableCollection<TimelineTimeFrameItem> )GetValue( sTimeFramesProperty );
			set => SetCurrentValue( sTimeFramesProperty, value );
		}

		public double ZoomLevel
		{
			get => ( double )GetValue( sZoomLevelProperty );
			set => SetCurrentValue( sZoomLevelProperty, value );
		}

		public double ViewportStartTicks
		{
			get => ( double )GetValue( sViewportStartTicksProperty );
			set => SetCurrentValue( sViewportStartTicksProperty, value );
		}

		public DateTime ViewportStartDate
		{
			get => new( Math.Max( 0L, ( long )ViewportStartTicks ) );
			set => ViewportStartTicks = value.Ticks;
		}

		private DateTime EffectiveMinDate => GetEffectiveMinDate();

		static TimelineControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata( typeof( TimelineControl ), new FrameworkPropertyMetadata( typeof( TimelineControl ) ) );
		}

		public TimelineControl()
		{
			mTimeFrameHitInfos = new List<TimeFrameHitInfo>();
			mPanSamples = new List<PanSample>();
			mFormattedTextCache = new Dictionary<FormattedTextCacheKey, FormattedText>();

			Focusable = true;
			ClipToBounds = true;

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private static void DrawRoundedRect( DrawingContext pDrawingContext, Rect pRect, Brush pBrush, RadiusXy pRadius )
		{
			if ( pRect.Width <= 0.0 || pRect.Height <= 0.0 || pBrush == null )
			{
				return;
			}

			pDrawingContext.DrawRoundedRectangle( pBrush, null, pRect, pRadius.X, pRadius.Y );
		}

		private static double Lerp( double pStart, double pEnd, double pT ) => pStart + ( ( pEnd - pStart ) * pT );

		private static double EaseOutCubic( double pT )
		{
			var lT = Math.Max( 0.0, Math.Min( 1.0, pT ) );
			var lInv = 1.0 - lT;
			return 1.0 - ( lInv * lInv * lInv );
		}

		private static Brush CreateOpacityBrush( Brush pBrush, double pOpacity )
		{
			if ( pBrush == null )
			{
				return null;
			}

			var lClampedOpacity = Math.Max( 0.0, Math.Min( 1.0, pOpacity ) );

			var lClone = pBrush.CloneCurrentValue();
			lClone.Opacity = lClampedOpacity;

			if ( lClone.CanFreeze )
			{
				lClone.Freeze();
			}

			return lClone;
		}

		private static int FindLaneIndex( IList<double> pLaneEnds, double pStartX )
		{
			for ( var lCurrentIndex = 0; lCurrentIndex < pLaneEnds.Count; lCurrentIndex++ )
			{
				var lLaneEnd = pLaneEnds[ lCurrentIndex ];
				if ( pStartX >= ( lLaneEnd + TimeFrameLaneGapPixels ) )
				{
					return lCurrentIndex;
				}
			}

			return pLaneEnds.Count;
		}

		private static DateTime AlignToMonday( DateTime pDate )
		{
			var lOffset = ( ( int )pDate.DayOfWeek + 6 ) % 7;
			return pDate.AddDays( -lOffset ).Date;
		}

		private static string FormatTickLabel( DateTime pDate, TickGranularity pGranularity )
		{
			switch ( pGranularity )
			{
				case TickGranularity.Years:
					{
						return pDate.ToString( "yyyy", CultureInfo.CurrentCulture );
					}
				case TickGranularity.Months:
					{
						return pDate.ToString( "MMM yyyy", CultureInfo.CurrentCulture );
					}
				default:
					{
						return pDate.ToString( "MMM d", CultureInfo.CurrentCulture );
					}
			}
		}

		private static KeyboardStep GetKeyboardStep( double pZoomLevel, bool pIsControlDown )
		{
			var lZoom = Math.Max( MinimumZoomPixelsPerDay, pZoomLevel );

			KeyboardStep lBaseStep;
			if ( lZoom >= 20.0 )
			{
				lBaseStep = KeyboardStep.Day;
			}
			else if ( lZoom >= 6.0 )
			{
				lBaseStep = KeyboardStep.Week;
			}
			else if ( lZoom >= 1.5 )
			{
				lBaseStep = KeyboardStep.Month;
			}
			else
			{
				lBaseStep = KeyboardStep.Year;
			}

			return pIsControlDown ? PromoteStep( lBaseStep ) : lBaseStep;
		}

		private static KeyboardStep PromoteStep( KeyboardStep pStep )
		{
			switch ( pStep )
			{
				case KeyboardStep.Day:
					{
						return KeyboardStep.Week;
					}
				case KeyboardStep.Week:
					{
						return KeyboardStep.Month;
					}
				case KeyboardStep.Month:
					{
						return KeyboardStep.Year;
					}
				default:
					{
						return KeyboardStep.Decade;
					}
			}
		}

		private static DateTime AddStep( DateTime pDate, KeyboardStep pStep, int pDirection )
		{
			switch ( pStep )
			{
				case KeyboardStep.Day:
					{
						return pDate.AddDays( 1.0 * pDirection );
					}
				case KeyboardStep.Week:
					{
						return pDate.AddDays( 7.0 * pDirection );
					}
				case KeyboardStep.Month:
					{
						return pDate.AddMonths( 1 * pDirection );
					}
				case KeyboardStep.Year:
					{
						return pDate.AddYears( 1 * pDirection );
					}
				default:
					{
						return pDate.AddYears( 10 * pDirection );
					}
			}
		}

		private static object CoerceMinDate( DependencyObject pDependencyObject, object pBaseValue )
		{
			if ( pBaseValue is not DateTime lDate )
			{
				return DateTime.MinValue;
			}

			return lDate.Date;
		}

		private static void OnMinDateChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not TimelineControl lControl )
			{
				return;
			}

			lControl.CoerceValue( sZoomLevelProperty );
			lControl.CoerceValue( sViewportStartTicksProperty );

			lControl.SetViewportStartDateCurrentValue( lControl.ClampViewportStartDate( lControl.ViewportStartDate, lControl.GetContentRect() ) );
			lControl.SetSelectedDateCurrentValue( lControl.ClampDateToRange( lControl.SelectedDate ) );

			lControl.EnsureInitialFitIfNeeded();
			lControl.InvalidateVisual();
		}

		private static object CoerceSelectedDate( DependencyObject pDependencyObject, object pBaseValue )
		{
			if ( pBaseValue is not DateTime lDate )
			{
				return DateTime.Today;
			}

			if ( pDependencyObject is TimelineControl lControl )
			{
				return lControl.ClampDateToRange( lDate.Date );
			}

			return lDate.Date;
		}

		private static void OnSelectedDateChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not TimelineControl lControl )
			{
				return;
			}

			if ( lControl.mIsInternalSelectedDateUpdate )
			{
				return;
			}

			lControl.mIsFitAnimationActive = false;
			lControl.mHasSuppressEnsureDateVisible = false;

			lControl.mIsInertiaActive = false;
			lControl.mHasPendingPan = false;

			var lContentRect = lControl.GetContentRect();
			var lSelected = lControl.ClampDateToRange( lControl.SelectedDate );

			if ( lSelected != lControl.SelectedDate )
			{
				lControl.SetSelectedDateInternal( lSelected );
			}

			lControl.EnsureDateVisible( lControl.SelectedDate, lContentRect );
			lControl.UpdateRenderingSubscriptionIfNeeded();
		}

		private static void OnSelectedTimeFrameChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not TimelineControl lControl )
			{
				return;
			}

			lControl.InvalidateVisual();

			if ( lControl.mHasSuppressSelectedTimeFrameToSelectedDateSync )
			{
				return;
			}

			if ( pEventArgs.NewValue is not TimelineTimeFrameItem lNewTimeFrame )
			{
				return;
			}

			var lTargetDate = lNewTimeFrame.StartDate.Date;

			if ( lControl.SelectedDate.Date == lTargetDate )
			{
				return;
			}

			lControl.SetSelectedDateCurrentValue( lTargetDate );
		}

		private static void OnTimeFramesChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not TimelineControl lControl )
			{
				return;
			}

			if ( pEventArgs.OldValue is ObservableCollection<TimelineTimeFrameItem> lOldCollection )
			{
				lOldCollection.CollectionChanged -= lControl.OnTimeFramesCollectionChanged;
			}

			if ( pEventArgs.NewValue is ObservableCollection<TimelineTimeFrameItem> lNewCollection )
			{
				lNewCollection.CollectionChanged += lControl.OnTimeFramesCollectionChanged;
			}

			lControl.CoerceValue( sZoomLevelProperty );
			lControl.CoerceValue( sViewportStartTicksProperty );

			lControl.MinDate = lControl.MinDate;
			lControl.SetSelectedDateCurrentValue( lControl.SelectedDate );
			lControl.SetViewportStartDateCurrentValue( lControl.ClampViewportStartDate( lControl.ViewportStartDate, lControl.GetContentRect() ) );

			lControl.EnsureInitialFitIfNeeded();
			lControl.InvalidateVisual();
		}

		private static object CoerceZoomLevel( DependencyObject pDependencyObject, object pBaseValue )
		{
			if ( pBaseValue is not double lZoom )
			{
				return 2.0;
			}

			if ( pDependencyObject is TimelineControl lControl )
			{
				return lControl.CoerceZoomValueByContent( lZoom, lControl.GetContentRect() );
			}

			return CoerceZoomValue( lZoom );
		}

		private static void OnZoomLevelChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not TimelineControl lControl )
			{
				return;
			}

			lControl.SetViewportStartDateCurrentValue( lControl.ClampViewportStartDate( lControl.ViewportStartDate, lControl.GetContentRect() ) );

			if ( !lControl.mHasSuppressEnsureDateVisible )
			{
				lControl.EnsureDateVisible( lControl.SelectedDate, lControl.GetContentRect() );
			}

			lControl.UpdateRenderingSubscriptionIfNeeded();
		}

		private static double CoerceZoomValue( double pZoom )
		{
			if ( double.IsNaN( pZoom ) || double.IsInfinity( pZoom ) )
			{
				return 2.0;
			}

			if ( pZoom < MinimumZoomPixelsPerDay )
			{
				return MinimumZoomPixelsPerDay;
			}

			return pZoom > MaximumZoomPixelsPerDay ? MaximumZoomPixelsPerDay : pZoom;
		}

		private static object CoerceViewportStartTicks( DependencyObject pDependencyObject, object pBaseValue )
		{
			if ( pBaseValue is not double lTicks )
			{
				return ( double )DateTime.Today.AddYears( -1 ).Ticks;
			}

			if ( pDependencyObject is not TimelineControl lControl )
			{
				return lTicks;
			}

			var lClamped = lControl.ClampViewportStartDate( new DateTime( Math.Max( 0L, ( long )lTicks ) ), lControl.GetContentRect() );
			return ( double )lClamped.Ticks;
		}

		private static void OnViewportStartTicksChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is TimelineControl lControl )
			{
				lControl.UpdateRenderingSubscriptionIfNeeded();
			}
		}

		private static IEnumerable<DateTime> EnumerateMajorTicks( DateTime pStart, DateTime pEnd, TickGranularity pGranularity, int pStep )
		{
			var lStart = pStart.Date;
			var lEnd = pEnd.Date;
			var lStep = Math.Max( 1, pStep );

			switch ( pGranularity )
			{
				case TickGranularity.Years:
					{
						var lFirstYear = lStart.Year;
						var lRemainder = lFirstYear % lStep;
						var lAlignedYear = lRemainder == 0 ? lFirstYear : lFirstYear + ( lStep - lRemainder );

						var lCurrent = new DateTime( lAlignedYear, 1, 1 );
						if ( lCurrent < lStart )
						{
							lCurrent = lCurrent.AddYears( lStep );
						}

						while ( lCurrent <= lEnd )
						{
							yield return lCurrent;
							lCurrent = lCurrent.AddYears( lStep );
						}

						yield break;
					}
				case TickGranularity.Months:
					{
						var lStartMonthIndex = ( lStart.Year * 12 ) + ( lStart.Month - 1 );
						var lRemainder = lStartMonthIndex % lStep;
						var lAlignedMonthIndex = lRemainder == 0 ? lStartMonthIndex : lStartMonthIndex + ( lStep - lRemainder );

						var lAlignedYear = lAlignedMonthIndex / 12;
						var lAlignedMonth = ( lAlignedMonthIndex % 12 ) + 1;

						var lCurrent = new DateTime( lAlignedYear, lAlignedMonth, 1 );
						if ( lCurrent < lStart )
						{
							lCurrent = lCurrent.AddMonths( lStep );
						}

						while ( lCurrent <= lEnd )
						{
							yield return lCurrent;
							lCurrent = lCurrent.AddMonths( lStep );
						}

						yield break;
					}
				case TickGranularity.Weeks:
					{
						var lCurrent = AlignToMonday( lStart );
						if ( lCurrent < lStart )
						{
							lCurrent = lCurrent.AddDays( 7.0 * lStep );
						}

						while ( lCurrent <= lEnd )
						{
							yield return lCurrent;
							lCurrent = lCurrent.AddDays( 7.0 * lStep );
						}

						yield break;
					}
				default:
					{
						var lCurrent = lStart;
						while ( lCurrent <= lEnd )
						{
							yield return lCurrent;
							lCurrent = lCurrent.AddDays( 1.0 * lStep );
						}

						break;
					}
			}
		}

		private static IEnumerable<DateTime> EnumerateMinorTicks( DateTime pStart, DateTime pEnd, TickGranularity pGranularity, int pMajorStep )
		{
			var lStart = pStart.Date;
			var lEnd = pEnd.Date;
			var lStep = Math.Max( 1, pMajorStep );

			switch ( pGranularity )
			{
				case TickGranularity.Years:
					{
						var lMajorTicks = EnumerateMajorTicks( lStart, lEnd, TickGranularity.Years, lStep );
						foreach ( var lMajor in lMajorTicks )
						{
							if ( lStep == 1 )
							{
								var lMinor = lMajor.AddMonths( 6 );
								if ( lMinor >= lStart && lMinor <= lEnd )
								{
									yield return lMinor;
								}

								continue;
							}

							if ( ( lStep % 2 ) == 0 )
							{
								var lMinor = lMajor.AddYears( lStep / 2 );
								if ( lMinor >= lStart && lMinor <= lEnd )
								{
									yield return lMinor;
								}
							}
						}

						yield break;
					}
				case TickGranularity.Months when lStep < 2 || ( lStep % 2 ) != 0:
					{
						yield break;
					}
				case TickGranularity.Months:
					{
						var lMajorTicks = EnumerateMajorTicks( lStart, lEnd, TickGranularity.Months, lStep );
						foreach ( var lMajor in lMajorTicks )
						{
							var lMinor = lMajor.AddMonths( lStep / 2 );
							if ( lMinor >= lStart && lMinor <= lEnd )
							{
								yield return lMinor;
							}
						}

						yield break;
					}
				case TickGranularity.Weeks:
					{
						var lMajorTicks = EnumerateMajorTicks( lStart, lEnd, TickGranularity.Weeks, lStep );
						foreach ( var lMajor in lMajorTicks )
						{
							var lMinor = lMajor.AddDays( ( 7.0 * lStep ) * 0.5 );
							if ( lMinor >= lStart && lMinor <= lEnd )
							{
								yield return lMinor;
							}
						}

						yield break;
					}
				default:
					{
						if ( lStep < 2 )
						{
							yield break;
						}

						var lMajorTicks = EnumerateMajorTicks( lStart, lEnd, TickGranularity.Days, lStep );
						foreach ( var lMajor in lMajorTicks )
						{
							var lMinor = lMajor.AddDays( ( 1.0 * lStep ) * 0.5 );
							if ( lMinor >= lStart && lMinor <= lEnd )
							{
								yield return lMinor;
							}
						}

						break;
					}
			}
		}

		private static double PlaceLeftWithLaneSpacing(
			double pDesiredLeft,
			double pWidth,
			double pMinLeft,
			double pMaxLeft,
			double pDesiredCenterX,
			IReadOnlyList<Rect> pPlacedRects )
		{
			var lClampedDesiredLeft = ClampToRange( pDesiredLeft, pMinLeft, pMaxLeft );
			var lAttemptLeft = lClampedDesiredLeft;

			for ( var lIterationCount = 0; lIterationCount < 6; lIterationCount++ )
			{
				var lAttemptRect = new Rect( lAttemptLeft, 0.0, pWidth, 1.0 );

				var lConflicts = pPlacedRects
					.Where( pExisting => ( lAttemptRect.Right + TimeFrameTitleMinimumSpacingPixels ) > pExisting.Left && ( pExisting.Right + TimeFrameTitleMinimumSpacingPixels ) > lAttemptRect.Left )
					.ToList();

				if ( lConflicts.Count == 0 )
				{
					return lAttemptLeft;
				}

				var lShiftRightLeft = lConflicts.Max( pExisting => pExisting.Right + TimeFrameTitleMinimumSpacingPixels );
				var lShiftLeftLeft = lConflicts.Min( pExisting => pExisting.Left - TimeFrameTitleMinimumSpacingPixels - pWidth );

				var lRightCandidate = ClampToRange( lShiftRightLeft, pMinLeft, pMaxLeft );
				var lLeftCandidate = ClampToRange( lShiftLeftLeft, pMinLeft, pMaxLeft );

				var lRightDelta = Math.Abs( ( lRightCandidate + ( pWidth * 0.5 ) ) - pDesiredCenterX );
				var lLeftDelta = Math.Abs( ( lLeftCandidate + ( pWidth * 0.5 ) ) - pDesiredCenterX );

				var lHasRightRoom = lRightCandidate <= pMaxLeft + 0.000001;
				var lHasLeftRoom = lLeftCandidate >= pMinLeft - 0.000001;

				if ( lHasLeftRoom && ( !lHasRightRoom || lLeftDelta <= lRightDelta ) )
				{
					lAttemptLeft = lLeftCandidate;
					continue;
				}

				if ( lHasRightRoom )
				{
					lAttemptLeft = lRightCandidate;
					continue;
				}

				return lAttemptLeft;
			}

			return lAttemptLeft;
		}

		private static double ClampToRange( double pValue, double pMin, double pMax )
		{
			if ( pValue < pMin )
			{
				return pMin;
			}

			return pValue > pMax ? pMax : pValue;
		}

		protected override void OnRender( DrawingContext pDrawingContext )
		{
			base.OnRender( pDrawingContext );

			var lActualWidth = ActualWidth;
			var lActualHeight = ActualHeight;

			if ( lActualWidth <= 1.0 || lActualHeight <= 1.0 )
			{
				return;
			}

			var lPadding = Padding;
			if ( lPadding is { Left: <= 0.0, Top: <= 0.0, Right: <= 0.0, Bottom: <= 0.0 } )
			{
				lPadding = new Thickness( DefaultPadding );
			}

			var lContentRect = new Rect(
				lPadding.Left,
				lPadding.Top,
				Math.Max( 0.0, lActualWidth - ( lPadding.Left + lPadding.Right ) ),
				Math.Max( 0.0, lActualHeight - ( lPadding.Top + lPadding.Bottom ) ) );

			if ( lContentRect.Width <= 1.0 || lContentRect.Height <= 1.0 )
			{
				return;
			}

			DrawBackground( pDrawingContext, lContentRect );
			DrawTimeline( pDrawingContext, lContentRect );
			DrawFocusOutlineIfNeeded( pDrawingContext );
		}

		protected override void OnMouseWheel( MouseWheelEventArgs pEventArgs )
		{
			base.OnMouseWheel( pEventArgs );

			mHasUserInteracted = true;

			mIsFitAnimationActive = false;
			mHasSuppressEnsureDateVisible = false;

			mIsInertiaActive = false;
			mHasPendingPan = false;

			var lPosition = pEventArgs.GetPosition( this );
			var lContentRect = GetContentRect();

			if ( !lContentRect.Contains( lPosition ) )
			{
				UpdateCursorAtPosition( lPosition );
				return;
			}

			var lAnchorDate = PixelToDate( lPosition.X, lContentRect );
			var lNotchCount = pEventArgs.Delta / 120.0;

			var lCurrentZoom = ZoomLevel;
			var lRequestedZoom = lCurrentZoom * Math.Pow( WheelZoomFactorPerNotch, lNotchCount );
			var lTargetZoom = CoerceZoomValueByContent( lRequestedZoom, lContentRect );

			var lAnchorOffsetPixels = lPosition.X - lContentRect.Left;
			var lNewViewportStart = lAnchorDate.AddDays( -( lAnchorOffsetPixels / Math.Max( MinimumZoomPixelsPerDay, lTargetZoom ) ) );

			SetZoomLevelCurrentValue( lTargetZoom );
			SetViewportStartDateCurrentValue( ClampViewportStartDate( lNewViewportStart, lContentRect ) );

			UpdateCursorAtPosition( lPosition );
			UpdateRenderingSubscriptionIfNeeded();

			pEventArgs.Handled = true;
		}

		protected override void OnMouseLeftButtonDown( MouseButtonEventArgs pEventArgs )
		{
			base.OnMouseLeftButtonDown( pEventArgs );

			Focus();

			mHasUserInteracted = true;

			mIsFitAnimationActive = false;
			mHasSuppressEnsureDateVisible = false;

			mIsInertiaActive = false;
			mHasPendingPan = false;

			var lPosition = pEventArgs.GetPosition( this );

			mIsPointerDown = true;
			mHasDragged = false;
			mPointerDownPoint = lPosition;
			mViewportStartTicksAtPointerDown = ViewportStartTicks;

			mPointerDownTimestampUtc = DateTime.UtcNow;
			mPointerDownTimeFrameItem = GetTimeFrameHitInfoAtPosition( lPosition )?.Item;

			mPanSamples.Clear();
			AddPanSample( DateTime.UtcNow, lPosition.X );

			UpdateCursorAtPosition( lPosition );

			CaptureMouse();

			pEventArgs.Handled = true;
		}

		protected override void OnMouseMove( MouseEventArgs pEventArgs )
		{
			base.OnMouseMove( pEventArgs );

			var lPosition = pEventArgs.GetPosition( this );

			if ( !mIsPointerDown )
			{
				UpdateCursorAtPosition( lPosition );
				return;
			}

			var lDeltaPixels = lPosition.X - mPointerDownPoint.X;

			if ( !mHasDragged && Math.Abs( lDeltaPixels ) < DragActivationThresholdPixels )
			{
				UpdateCursorAtPosition( lPosition );
				return;
			}

			mHasUserInteracted = true;

			mHasDragged = true;
			mIsInertiaActive = false;

			mIsFitAnimationActive = false;
			mHasSuppressEnsureDateVisible = false;

			mHasPendingPan = true;
			mPendingPanPointerX = lPosition.X;
			mPendingPanTimestampUtc = DateTime.UtcNow;

			UpdateRenderingSubscriptionIfNeeded();
			UpdateCursorAtPosition( lPosition );

			pEventArgs.Handled = true;
		}

		protected override void OnMouseLeftButtonUp( MouseButtonEventArgs pEventArgs )
		{
			base.OnMouseLeftButtonUp( pEventArgs );

			if ( !mIsPointerDown )
			{
				return;
			}

			var lPosition = pEventArgs.GetPosition( this );

			ApplyPendingPanIfNeeded();

			mIsPointerDown = false;
			ReleaseMouseCapture();

			var lElapsedMilliseconds = ( DateTime.UtcNow - mPointerDownTimestampUtc ).TotalMilliseconds;
			var lIsClick = !mHasDragged && lElapsedMilliseconds <= ClickMaximumDurationMilliseconds;

			if ( lIsClick )
			{
				var lContentRect = GetContentRect();
				var lHitOnUp = GetTimeFrameHitInfoAtPosition( lPosition )?.Item;

				if ( mPointerDownTimeFrameItem != null && lHitOnUp != null && ReferenceEquals( mPointerDownTimeFrameItem, lHitOnUp ) )
				{
					SetSelectedTimeFrameCurrentValue( mPointerDownTimeFrameItem );
					SetSelectedDateFromUserInteraction( mPointerDownTimeFrameItem.StartDate, lContentRect );

					UpdateCursorAtPosition( lPosition );
					UpdateRenderingSubscriptionIfNeeded();

					pEventArgs.Handled = true;
					return;
				}

				if ( lContentRect.Contains( lPosition ) )
				{
					SetSelectedDateFromUserInteraction( PixelToDate( lPosition.X, lContentRect ), lContentRect );
				}

				UpdateCursorAtPosition( lPosition );
				UpdateRenderingSubscriptionIfNeeded();

				pEventArgs.Handled = true;
				return;
			}

			if ( mHasDragged )
			{
				StartInertiaIfPossible();
			}

			UpdateCursorAtPosition( lPosition );
			UpdateRenderingSubscriptionIfNeeded();

			pEventArgs.Handled = true;
		}

		protected override void OnMouseLeave( MouseEventArgs pEventArgs )
		{
			base.OnMouseLeave( pEventArgs );
			SetCursorIsHand( false );
		}

		protected override void OnKeyDown( KeyEventArgs pEventArgs )
		{
			base.OnKeyDown( pEventArgs );

			mHasUserInteracted = true;

			mIsFitAnimationActive = false;
			mHasSuppressEnsureDateVisible = false;

			var lContentRect = GetContentRect();
			var lIsControlDown = ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control;

			switch ( pEventArgs.Key )
			{
				case Key.Home:
					{
						SetSelectedDateCurrentValue( ClampDateToRange( EffectiveMinDate ) );
						EnsureDateVisible( SelectedDate, lContentRect );
						pEventArgs.Handled = true;
						return;
					}
				case Key.End:
					{
						SetSelectedDateCurrentValue( ClampDateToRange( DateTime.Today ) );
						EnsureDateVisible( SelectedDate, lContentRect );
						pEventArgs.Handled = true;
						return;
					}
			}

			if ( pEventArgs.Key != Key.Left && pEventArgs.Key != Key.Right )
			{
				return;
			}

			var lDirection = pEventArgs.Key == Key.Left ? -1 : 1;
			var lStep = GetKeyboardStep( ZoomLevel, lIsControlDown );
			var lNewDate = AddStep( SelectedDate, lStep, lDirection );

			SetSelectedDateCurrentValue( ClampDateToRange( lNewDate ) );
			EnsureDateVisible( SelectedDate, lContentRect );

			pEventArgs.Handled = true;
		}

		protected override void OnRenderSizeChanged( SizeChangedInfo pSizeInfo )
		{
			base.OnRenderSizeChanged( pSizeInfo );

			EnsureInitialFitIfNeeded();

			CoerceValue( sZoomLevelProperty );
			CoerceValue( sViewportStartTicksProperty );

			var lContentRect = GetContentRect();
			SetViewportStartDateCurrentValue( ClampViewportStartDate( ViewportStartDate, lContentRect ) );
			EnsureDateVisible( SelectedDate, lContentRect );
		}

		private void EnsureInitialFitIfNeeded()
		{
			if ( mHasAppliedInitialFit || mHasUserInteracted )
			{
				return;
			}

			if ( HasExplicitLocalNonBindingValue( sZoomLevelProperty ) || HasExplicitLocalNonBindingValue( sViewportStartTicksProperty ) )
			{
				mHasAppliedInitialFit = true;
				return;
			}

			if ( !HasInitialFitRangeAvailable() )
			{
				return;
			}

			var lContentRect = GetContentRect();
			if ( lContentRect.Width <= 1.0 )
			{
				return;
			}

			ApplyInitialFitToFullTimeline( lContentRect );
			mHasAppliedInitialFit = true;
		}

		private void ApplyInitialFitToFullTimeline( Rect pContentRect )
		{
			var lStartDate = EffectiveMinDate;
			var lEndDate = DateTime.Today;

			if ( lEndDate < lStartDate )
			{
				lStartDate = lEndDate;
			}

			var lRangeDays = Math.Max( 1.0, ( lEndDate - lStartDate ).TotalDays );
			var lFitZoom = pContentRect.Width / lRangeDays;
			var lTargetZoom = CoerceZoomValueByContent( lFitZoom, pContentRect );
			var lTargetViewportStart = ClampViewportStartDate( lStartDate, pContentRect, lTargetZoom );

			mFitAnimationStartUtc = DateTime.UtcNow;
			mFitStartViewportTicks = ViewportStartTicks;
			mFitTargetViewportTicks = lTargetViewportStart.Ticks;
			mFitStartZoom = ZoomLevel;
			mFitTargetZoom = lTargetZoom;

			mHasSuppressEnsureDateVisible = true;
			try
			{
				SetZoomLevelCurrentValue( lTargetZoom );
				SetViewportStartTicksCurrentValue( lTargetViewportStart.Ticks );
			}
			finally
			{
				mHasSuppressEnsureDateVisible = false;
			}

			UpdateBindingSourceIfNeeded( sZoomLevelProperty );
			UpdateBindingSourceIfNeeded( sViewportStartTicksProperty );
		}

		private List<TimeFrameTitleDrawInfo> LayoutTimeFrameTitles(
			IEnumerable<VisibleTimeFrame> pVisibleFrames,
			Rect pContentRect,
			IDictionary<TimelineTimeFrameItem, Rect> pBarRectsByItem,
			Typeface pTypeface,
			Brush pTextBrush,
			double pPixelsPerDip )
		{
			var lVisibleFrames = pVisibleFrames?.Where( pFrame => pFrame?.Item != null ).ToList() ?? new List<VisibleTimeFrame>();
			if ( lVisibleFrames.Count == 0 || pBarRectsByItem == null || pBarRectsByItem.Count == 0 || pTypeface == null || pTextBrush == null )
			{
				return new List<TimeFrameTitleDrawInfo>();
			}

			var lCandidates = lVisibleFrames
				.Where( pFrame => pBarRectsByItem.ContainsKey( pFrame.Item ) )
				.Select( pFrame =>
				{
					var lBarRect = pBarRectsByItem[ pFrame.Item ];
					var lTitle = pFrame.Item.Title ?? string.Empty;

					var lMaxFontSize = Math.Max( 8.0, lBarRect.Height - 8.0 );
					var lFontSize = Math.Min( TimeFrameLabelFontSize, lMaxFontSize );

					var lText = CreateFormattedTextCached( lTitle, pTypeface, lFontSize, pTextBrush, pPixelsPerDip );
					var lTextWidth = lText?.WidthIncludingTrailingWhitespace ?? 0.0;
					var lTextHeight = lText?.Height ?? 0.0;

					var lDotRadius = Math.Max( 3.0, Math.Min( 5.0, lBarRect.Height * 0.22 ) );
					var lDotDiameter = lDotRadius * 2.0;

					var lGroupWidth = lDotDiameter + TimeFrameTitleDotToTextGapPixels + lTextWidth;
					var lGroupHeight = Math.Max( lTextHeight, lDotDiameter );

					var lCenterX = pContentRect.Left + ( ( pFrame.StartX + pFrame.EndX ) * 0.5 );

					return new
					{
						pFrame.Item,
						pFrame.LaneIndex,
						Text = lText,
						TextWidth = lTextWidth,
						TextHeight = lTextHeight,
						BarRect = lBarRect,
						GroupWidth = lGroupWidth,
						GroupHeight = lGroupHeight,
						CenterX = lCenterX,
						Title = lTitle
					};
				} )
				.Where( pCandidate => pCandidate.Text != null && pCandidate.TextWidth > 0.0 && pCandidate.GroupWidth > 0.0 )
				.OrderBy( pCandidate => pCandidate.LaneIndex )
				.ThenBy( pCandidate => pCandidate.CenterX )
				.ThenBy( pCandidate => pCandidate.Title, StringComparer.OrdinalIgnoreCase )
				.ToList();

			if ( lCandidates.Count == 0 )
			{
				return new List<TimeFrameTitleDrawInfo>();
			}

			var lPlacedByLane = new Dictionary<int, List<Rect>>();
			var lResults = new List<TimeFrameTitleDrawInfo>( lCandidates.Count );

			foreach ( var lCandidate in lCandidates )
			{
				if ( !lPlacedByLane.TryGetValue( lCandidate.LaneIndex, out var lPlacedRects ) )
				{
					lPlacedRects = new List<Rect>();
					lPlacedByLane[ lCandidate.LaneIndex ] = lPlacedRects;
				}

				var lMinLeft = Math.Max( pContentRect.Left, lCandidate.BarRect.Left + TimeFrameTitleInnerPaddingPixels );
				var lMaxLeft = Math.Min( pContentRect.Right - lCandidate.GroupWidth, lCandidate.BarRect.Right - TimeFrameTitleInnerPaddingPixels - lCandidate.GroupWidth );

				if ( lMaxLeft < lMinLeft )
				{
					lMaxLeft = lMinLeft;
				}

				var lDesiredLeft = lCandidate.CenterX - ( lCandidate.GroupWidth * 0.5 );
				var lPlacedLeft = PlaceLeftWithLaneSpacing( lDesiredLeft, lCandidate.GroupWidth, lMinLeft, lMaxLeft, lCandidate.CenterX, lPlacedRects );

				var lGroupTop = lCandidate.BarRect.Top + Math.Max( 0.0, ( lCandidate.BarRect.Height - lCandidate.GroupHeight ) * 0.5 );
				var lGroupRect = new Rect( lPlacedLeft, lGroupTop, lCandidate.GroupWidth, lCandidate.GroupHeight );

				lPlacedRects.Add( lGroupRect );
				lPlacedRects.Sort( ( pLeftRect, pRightRect ) => pLeftRect.Left.CompareTo( pRightRect.Left ) );

				lResults.Add( new TimeFrameTitleDrawInfo( lCandidate.Item, lCandidate.Text, lGroupRect ) );
			}

			return lResults;
		}

		private void SetSelectedDateInternal( DateTime pDate )
		{
			mIsInternalSelectedDateUpdate = true;
			try
			{
				SetSelectedDateCurrentValue( pDate );
			}
			finally
			{
				mIsInternalSelectedDateUpdate = false;
			}
		}

		private void UpdateCursorAtPosition( Point pPosition )
		{
			var lContentRect = GetContentRect();
			var lIsClickable = lContentRect.Contains( pPosition );
			SetCursorIsHand( lIsClickable );
		}

		private FormattedText CreateFormattedTextCached(
			string pText,
			Typeface pTypeface,
			double pFontSize,
			Brush pBrush,
			double pPixelsPerDip )
		{
			if ( string.IsNullOrEmpty( pText ) || pTypeface == null || pBrush == null )
			{
				return null;
			}

			EnsureFormattedTextCacheContext( pBrush, pPixelsPerDip );

			var lKey = new FormattedTextCacheKey( pText, pFontSize );

			if ( mFormattedTextCache.TryGetValue( lKey, out var lCached ) )
			{
				return lCached;
			}

			var lText = new FormattedText(
				pText,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				pTypeface,
				pFontSize,
				pBrush,
				pPixelsPerDip );

			mFormattedTextCache[ lKey ] = lText;
			return lText;
		}

		private void EnsureFormattedTextCacheContext( Brush pBrush, double pPixelsPerDip )
		{
			var lCultureName = CultureInfo.CurrentCulture?.Name ?? string.Empty;

			var lHasSameContext = ReferenceEquals( pBrush, mTextCacheBrush )
								  && Math.Abs( pPixelsPerDip - mTextCachePixelsPerDip ) < 0.000001
								  && Equals( FontFamily, mTextCacheFontFamily )
								  && FontStyle.Equals( mTextCacheFontStyle )
								  && FontWeight.Equals( mTextCacheFontWeight )
								  && FontStretch.Equals( mTextCacheFontStretch )
								  && string.Equals( lCultureName, mTextCacheCultureName, StringComparison.Ordinal );

			if ( lHasSameContext )
			{
				return;
			}

			mFormattedTextCache.Clear();

			mTextCacheBrush = pBrush;
			mTextCachePixelsPerDip = pPixelsPerDip;
			mTextCacheFontFamily = FontFamily;
			mTextCacheFontStyle = FontStyle;
			mTextCacheFontWeight = FontWeight;
			mTextCacheFontStretch = FontStretch;
			mTextCacheCultureName = lCultureName;
		}

		private void SetCursorIsHand( bool pIsHand )
		{
			if ( pIsHand == mIsHandCursorActive )
			{
				return;
			}

			mIsHandCursorActive = pIsHand;
			Cursor = pIsHand ? Cursors.Hand : Cursors.Arrow;
		}

		private void SetSelectedDateFromUserInteraction( DateTime pDate, Rect pContentRect )
		{
			mHasUserInteracted = true;

			var lClamped = ClampDateToRange( pDate );

			mHasSuppressSelectedTimeFrameToSelectedDateSync = true;
			var lSuppressionVersion = ++mSelectedTimeFrameToSelectedDateSyncSuppressionVersion;

			Dispatcher.BeginInvoke( new Action( () =>
			{
				if ( lSuppressionVersion != mSelectedTimeFrameToSelectedDateSyncSuppressionVersion )
				{
					return;
				}

				mHasSuppressSelectedTimeFrameToSelectedDateSync = false;
			} ) );

			mIsInternalSelectedDateUpdate = true;
			try
			{
				SetSelectedDateCurrentValue( lClamped );
			}
			finally
			{
				mIsInternalSelectedDateUpdate = false;
			}

			UpdateBindingSourceIfNeeded( sSelectedDateProperty );
			EnsureDateVisible( lClamped, pContentRect );
		}

		private void ApplyPendingPanIfNeeded()
		{
			if ( !mHasPendingPan )
			{
				return;
			}

			var lContentRect = GetContentRect();
			var lZoom = CoerceZoomValueByContent( ZoomLevel, lContentRect );

			var lDeltaPixels = mPendingPanPointerX - mPointerDownPoint.X;
			var lDeltaDays = lDeltaPixels / Math.Max( MinimumZoomPixelsPerDay, lZoom );
			var lDeltaTicks = lDeltaDays * TimeSpan.TicksPerDay;

			SetViewportStartTicksCurrentValue( mViewportStartTicksAtPointerDown - lDeltaTicks );

			var lSampleTimestampUtc = mPendingPanTimestampUtc == default ? DateTime.UtcNow : mPendingPanTimestampUtc;
			AddPanSample( lSampleTimestampUtc, mPendingPanPointerX );

			mHasPendingPan = false;
		}

		private void UpdateRenderingSubscriptionIfNeeded()
		{
			var lShouldSubscribe = mIsFitAnimationActive
								   || mIsInertiaActive
								   || ( mIsPointerDown && mHasDragged )
								   || mHasPendingPan;

			if ( lShouldSubscribe == mIsRenderingSubscribed )
			{
				return;
			}

			if ( lShouldSubscribe )
			{
				CompositionTarget.Rendering += OnCompositionTargetRendering;
				mIsRenderingSubscribed = true;
				return;
			}

			CompositionTarget.Rendering -= OnCompositionTargetRendering;
			mIsRenderingSubscribed = false;
		}

		private DateTime GetEffectiveMinDate()
		{
			var lExplicitMin = MinDate;
			if ( lExplicitMin != DateTime.MinValue )
			{
				return lExplicitMin.Date;
			}

			var lTimeFrames = TimeFrames;
			if ( lTimeFrames == null || lTimeFrames.Count == 0 )
			{
				return DateTime.Today.AddYears( -1 );
			}

			return lTimeFrames.Min( pItem => pItem.StartDate ).Date;
		}

		private void OnLoaded( object pSender, RoutedEventArgs pEventArgs )
		{
			EnsureInitialFitIfNeeded();
			UpdateRenderingSubscriptionIfNeeded();
			InvalidateVisual();
		}

		private void OnUnloaded( object pSender, RoutedEventArgs pEventArgs )
		{
			if ( mIsRenderingSubscribed )
			{
				CompositionTarget.Rendering -= OnCompositionTargetRendering;
				mIsRenderingSubscribed = false;
			}
		}

		private void OnCompositionTargetRendering( object pSender, EventArgs pEventArgs )
		{
			ApplyPendingPanIfNeeded();
			UpdateAnimationFrameIfNeeded();

			var lMousePosition = Mouse.GetPosition( this );
			UpdateCursorAtPosition( lMousePosition );

			UpdateRenderingSubscriptionIfNeeded();
		}

		private void UpdateAnimationFrameIfNeeded()
		{
			var lNowUtc = DateTime.UtcNow;

			if ( mLastRenderTick == default )
			{
				mLastRenderTick = lNowUtc;
				return;
			}

			var lDeltaSeconds = Math.Max( 0.0, ( lNowUtc - mLastRenderTick ).TotalSeconds );
			mLastRenderTick = lNowUtc;

			if ( mIsFitAnimationActive )
			{
				UpdateFitAnimation( lNowUtc );
			}

			if ( mIsInertiaActive && lDeltaSeconds > 0.0 && !mIsPointerDown && !mIsFitAnimationActive )
			{
				UpdateInertia( lDeltaSeconds );
			}
		}

		private void UpdateFitAnimation( DateTime pNowUtc )
		{
			var lElapsedMilliseconds = ( pNowUtc - mFitAnimationStartUtc ).TotalMilliseconds;
			var lProgress = Math.Max( 0.0, Math.Min( 1.0, lElapsedMilliseconds / FitAnimationDurationMilliseconds ) );
			var lEased = EaseOutCubic( lProgress );

			var lContentRect = GetContentRect();

			var lNewViewportTicks = Lerp( mFitStartViewportTicks, mFitTargetViewportTicks, lEased );
			var lNewZoom = Lerp( mFitStartZoom, mFitTargetZoom, lEased );

			SetZoomLevelCurrentValue( CoerceZoomValueByContent( lNewZoom, lContentRect ) );

			var lCandidateStart = new DateTime( Math.Max( 0L, ( long )lNewViewportTicks ) );
			var lClampedStart = ClampViewportStartDate( lCandidateStart, lContentRect );
			SetViewportStartTicksCurrentValue( lClampedStart.Ticks );

			if ( lProgress < 1.0 )
			{
				return;
			}

			mIsFitAnimationActive = false;
			mHasSuppressEnsureDateVisible = false;

			SetViewportStartDateCurrentValue( ClampViewportStartDate( ViewportStartDate, lContentRect ) );
		}

		private void UpdateInertia( double pDeltaSeconds )
		{
			var lContentRect = GetContentRect();

			var lDeltaTicks = mInertiaVelocityTicksPerSecond * pDeltaSeconds;
			var lNewViewportTicks = ViewportStartTicks - lDeltaTicks;

			SetViewportStartTicksCurrentValue( lNewViewportTicks );

			var lFriction = Math.Pow( 0.12, pDeltaSeconds );
			mInertiaVelocityTicksPerSecond *= lFriction;

			var lHasReachedStop = Math.Abs( mInertiaVelocityTicksPerSecond ) < ( TimeSpan.TicksPerDay * 0.02 );
			if ( lHasReachedStop )
			{
				mIsInertiaActive = false;
			}

			SetViewportStartDateCurrentValue( ClampViewportStartDate( ViewportStartDate, lContentRect ) );
		}

		private void StartInertiaIfPossible()
		{
			var lNowUtc = DateTime.UtcNow;

			var lSamples = mPanSamples
				.Where( pSample => ( lNowUtc - pSample.TimestampUtc ).TotalMilliseconds <= 120.0 )
				.OrderBy( pSample => pSample.TimestampUtc )
				.ToList();

			if ( lSamples.Count < 2 )
			{
				return;
			}

			var lFirst = lSamples.First();
			var lLast = lSamples.Last();

			var lDeltaX = lLast.PointerX - lFirst.PointerX;
			var lDeltaSeconds = Math.Max( 0.001, ( lLast.TimestampUtc - lFirst.TimestampUtc ).TotalSeconds );

			var lVelocityPixelsPerSecond = lDeltaX / lDeltaSeconds;
			var lContentRect = GetContentRect();
			var lZoom = CoerceZoomValueByContent( ZoomLevel, lContentRect );

			var lVelocityDaysPerSecond = lVelocityPixelsPerSecond / Math.Max( MinimumZoomPixelsPerDay, lZoom );
			var lVelocityTicksPerSecond = lVelocityDaysPerSecond * TimeSpan.TicksPerDay;

			mInertiaVelocityTicksPerSecond = lVelocityTicksPerSecond;
			mIsInertiaActive = Math.Abs( mInertiaVelocityTicksPerSecond ) > ( TimeSpan.TicksPerDay * 0.08 );

			UpdateRenderingSubscriptionIfNeeded();
		}

		private void AddPanSample( DateTime pTimestampUtc, double pPointerX )
		{
			mPanSamples.Add( new PanSample( pTimestampUtc, pPointerX ) );

			if ( mPanSamples.Count <= 6 )
			{
				return;
			}

			mPanSamples.RemoveAt( 0 );
		}

		private void DrawBackground( DrawingContext pDrawingContext, Rect pContentRect )
		{
			var lBackgroundBrush = Background ?? TryFindResource( "CommonBlackBrush" ) as Brush;
			if ( lBackgroundBrush != null )
			{
				pDrawingContext.DrawRectangle( lBackgroundBrush, null, new Rect( 0.0, 0.0, ActualWidth, ActualHeight ) );
			}
		}

		private void DrawTimeline( DrawingContext pDrawingContext, Rect pContentRect )
		{
			mTimeFrameHitInfos.Clear();

			var lDpi = VisualTreeHelper.GetDpi( this );
			var lPixelsPerDip = lDpi.PixelsPerDip;

			var lForegroundBrush = Foreground ?? TryFindResource( "CommonWhiteBrush" ) as Brush;
			var lDividerBrush = TryFindResource( "OnSurfaceDividerOnDarkBrush" ) as Brush;
			var lHairlineBrush = TryFindResource( "HairlineTwoToneBrush" ) as Brush;
			var lAccentBrush = TryFindResource( "CommonBrush" ) as Brush;

			var lEffectiveMinDate = EffectiveMinDate;
			var lEffectiveMaxDate = DateTime.Today;

			var lViewportStart = ClampViewportStartDate( ViewportStartDate, pContentRect );
			var lZoom = CoerceZoomValueByContent( ZoomLevel, pContentRect );

			var lVisibleDays = pContentRect.Width / Math.Max( MinimumZoomPixelsPerDay, lZoom );
			var lViewportEndCandidate = lViewportStart.AddDays( lVisibleDays );
			var lViewportEnd = lViewportEndCandidate > lEffectiveMaxDate ? lEffectiveMaxDate : lViewportEndCandidate;

			var lBaselineY = pContentRect.Bottom - 34.0;
			var lTickLabelY = lBaselineY + 10.0;

			var lLaneTopLimit = pContentRect.Top;
			var lLaneBottomLimit = lBaselineY - 12.0;
			var lAvailableLaneHeight = Math.Max( 0.0, lLaneBottomLimit - lLaneTopLimit );

			var lTimeFrames = TimeFrames ?? Enumerable.Empty<TimelineTimeFrameItem>();
			var lVisibleTimeFrames = BuildVisibleTimeFrames( lTimeFrames, lEffectiveMinDate, lEffectiveMaxDate, lViewportStart, lViewportEnd, pContentRect );

			var lLaneCount = lVisibleTimeFrames.Select( pFrame => pFrame.LaneIndex ).DefaultIfEmpty( -1 ).Max() + 1;
			lLaneCount = Math.Max( 0, lLaneCount );

			var lDesiredLaneHeight = TimeFrameLaneHeight;
			var lCompressedLaneHeight = lLaneCount > 0 ? Math.Max( 1.0, lAvailableLaneHeight / lLaneCount ) : 0.0;
			var lEffectiveLaneHeight = lLaneCount > 0 ? Math.Min( lDesiredLaneHeight, lCompressedLaneHeight ) : 0.0;

			var lLaneAreaHeight = lLaneCount * lEffectiveLaneHeight;
			var lLaneTopOffset = lLaneTopLimit + Math.Max( 0.0, ( lAvailableLaneHeight - lLaneAreaHeight ) * 0.5 );

			var lEffectiveBarHeight = Math.Min( TimeFrameBarHeight, Math.Max( 2.0, lEffectiveLaneHeight - 6.0 ) );

			var lBarRectsByItem = new Dictionary<TimelineTimeFrameItem, Rect>();

			foreach ( var lFrame in lVisibleTimeFrames )
			{
				var lLaneTop = lLaneTopOffset + ( lFrame.LaneIndex * lEffectiveLaneHeight );
				var lBarY = lLaneTop + ( ( lEffectiveLaneHeight - lEffectiveBarHeight ) * 0.5 );

				var lBarRect = new Rect(
					pContentRect.Left + lFrame.StartX,
					lBarY,
					Math.Max( 2.0, lFrame.EndX - lFrame.StartX ),
					lEffectiveBarHeight );

				var lFrameBrush = ResolveTimeFrameBrush( lFrame.Item ) ?? lAccentBrush;
				var lBarCornerRadius = Math.Min( 10.0, lBarRect.Height * 0.5 );
				var lBarRadius = new RadiusXy( lBarCornerRadius, lBarCornerRadius );

				DrawRoundedRect( pDrawingContext, lBarRect, lFrameBrush, lBarRadius );
				lBarRectsByItem[ lFrame.Item ] = lBarRect;

				var lIsSelected = SelectedTimeFrame != null && ReferenceEquals( SelectedTimeFrame, lFrame.Item );
				if ( lIsSelected )
				{
					var lPen = TryFindResource( "OnSurfaceCardStrokeOnDarkBrush" ) is Brush lStrokeBrush ? new Pen( lStrokeBrush, 1.0 ) : null;
					if ( lPen != null )
					{
						pDrawingContext.DrawRoundedRectangle( null, lPen, lBarRect, lBarCornerRadius, lBarCornerRadius );
					}
				}
			}

			var lTypeface = new Typeface( FontFamily, FontStyle, FontWeight, FontStretch );
			var lTitleDrawInfos = LayoutTimeFrameTitles( lVisibleTimeFrames, pContentRect, lBarRectsByItem, lTypeface, lForegroundBrush, lPixelsPerDip );

			foreach ( var lTitleInfo in lTitleDrawInfos )
			{
				if ( !lBarRectsByItem.TryGetValue( lTitleInfo.Item, out var lBarRect ) )
				{
					continue;
				}

				var lBarCornerRadius = Math.Min( 10.0, lBarRect.Height * 0.5 );
				var lClipRect = new Rect(
					lBarRect.Left + 1.0,
					lBarRect.Top + 1.0,
					Math.Max( 0.0, lBarRect.Width - 2.0 ),
					Math.Max( 0.0, lBarRect.Height - 2.0 ) );

				if ( lClipRect.Width <= 0.0 || lClipRect.Height <= 0.0 )
				{
					continue;
				}

				var lDotRadius = Math.Max( 3.0, Math.Min( 5.0, lBarRect.Height * 0.22 ) );
				var lDotCenterX = lTitleInfo.Rect.Left + lDotRadius;
				var lDotCenterY = lBarRect.Top + ( lBarRect.Height * 0.5 );

				var lTextLeft = lTitleInfo.Rect.Left + ( lDotRadius * 2.0 ) + TimeFrameTitleDotToTextGapPixels;
				var lTextTop = lBarRect.Top + Math.Max( 0.0, ( lBarRect.Height - lTitleInfo.Text.Height ) * 0.5 );

				pDrawingContext.PushClip( new RectangleGeometry( lClipRect, lBarCornerRadius, lBarCornerRadius ) );
				try
				{
					if ( lForegroundBrush != null )
					{
						pDrawingContext.DrawEllipse( lForegroundBrush, null, new Point( lDotCenterX, lDotCenterY ), lDotRadius, lDotRadius );
						pDrawingContext.DrawText( lTitleInfo.Text, new Point( lTextLeft, lTextTop ) );
					}
				}
				finally
				{
					pDrawingContext.Pop();
				}

				var lHitRect = Rect.Union( lBarRect, lTitleInfo.Rect );
				mTimeFrameHitInfos.Add( new TimeFrameHitInfo( lTitleInfo.Item, lHitRect ) );
			}

			if ( lHairlineBrush != null )
			{
				var lPen = new Pen( lHairlineBrush, 1.0 );
				pDrawingContext.DrawLine( lPen, new Point( pContentRect.Left, lBaselineY ), new Point( pContentRect.Right, lBaselineY ) );
			}

			if ( lDividerBrush != null )
			{
				DrawTicksAndLabels(
					pDrawingContext,
					pContentRect,
					lViewportStart,
					lViewportEnd,
					lBaselineY,
					lTickLabelY,
					lDividerBrush,
					lForegroundBrush,
					lPixelsPerDip );
			}

			DrawSelectedIndicator(
				pDrawingContext,
				pContentRect,
				lViewportStart,
				lBaselineY,
				lAccentBrush,
				lForegroundBrush,
				lPixelsPerDip );
		}

		private void DrawFocusOutlineIfNeeded( DrawingContext pDrawingContext )
		{
			if ( !IsKeyboardFocusWithin )
			{
				return;
			}

			if ( TryFindResource( "CommonBrush" ) is not Brush lFocusBrush )
			{
				return;
			}

			var lPen = new Pen( lFocusBrush, 2.0 );
			var lRect = new Rect( 1.0, 1.0, Math.Max( 0.0, ActualWidth - 2.0 ), Math.Max( 0.0, ActualHeight - 2.0 ) );

			pDrawingContext.DrawRoundedRectangle( null, lPen, lRect, 12.0, 12.0 );
		}

		private TickLabelSchedule ChooseTickLabelSchedule(
			Rect pContentRect,
			DateTime pViewportStart,
			DateTime pViewportEnd,
			Typeface pTypeface,
			Brush pLabelBrush,
			double pPixelsPerDip )
		{
			var lSchedules = new[]
			{
				new TickLabelSchedule( TickGranularity.Days, 1 ),
				new TickLabelSchedule( TickGranularity.Days, 2 ),
				new TickLabelSchedule( TickGranularity.Days, 7 ),
				new TickLabelSchedule( TickGranularity.Days, 14 ),

				new TickLabelSchedule( TickGranularity.Weeks, 1 ),
				new TickLabelSchedule( TickGranularity.Weeks, 2 ),
				new TickLabelSchedule( TickGranularity.Weeks, 4 ),

				new TickLabelSchedule( TickGranularity.Months, 1 ),
				new TickLabelSchedule( TickGranularity.Months, 2 ),
				new TickLabelSchedule( TickGranularity.Months, 4 ),
				new TickLabelSchedule( TickGranularity.Months, 6 ),

				new TickLabelSchedule( TickGranularity.Years, 1 ),
				new TickLabelSchedule( TickGranularity.Years, 2 ),
				new TickLabelSchedule( TickGranularity.Years, 5 ),
				new TickLabelSchedule( TickGranularity.Years, 10 )
			};

			foreach ( var lSchedule in lSchedules )
			{
				Rect? lPreviousRect = null;
				var lHasOverlap = false;

				foreach ( var lTickDate in EnumerateMajorTicks( pViewportStart, pViewportEnd, lSchedule.Granularity, lSchedule.Step ) )
				{
					var lLabelText = FormatTickLabel( lTickDate, lSchedule.Granularity );
					var lText = CreateFormattedTextCached( lLabelText, pTypeface, DateLabelFontSize, pLabelBrush, pPixelsPerDip );

					var lWidth = lText?.WidthIncludingTrailingWhitespace ?? 0.0;
					var lX = DateToPixel( lTickDate, pViewportStart, pContentRect );
					var lLeft = pContentRect.Left + lX - ( lWidth * 0.5 );
					var lRect = new Rect( lLeft, 0.0, lWidth, 1.0 );

					if ( lRect.Right < pContentRect.Left || lRect.Left > pContentRect.Right )
					{
						continue;
					}

					if ( lPreviousRect.HasValue && lRect.Left < ( lPreviousRect.Value.Right + MajorTickLabelGapPixels ) )
					{
						lHasOverlap = true;
						break;
					}

					lPreviousRect = lRect;
				}

				if ( !lHasOverlap )
				{
					return lSchedule;
				}
			}

			return lSchedules.Last();
		}

		private void DrawTicksAndLabels(
			DrawingContext pDrawingContext,
			Rect pContentRect,
			DateTime pViewportStart,
			DateTime pViewportEnd,
			double pBaselineY,
			double pLabelY,
			Brush pTickBrush,
			Brush pLabelBrush,
			double pPixelsPerDip )
		{
			var lTypeface = new Typeface( FontFamily, FontStyle, FontWeight, FontStretch );
			var lSchedule = ChooseTickLabelSchedule( pContentRect, pViewportStart, pViewportEnd, lTypeface, pLabelBrush, pPixelsPerDip );

			var lPen = new Pen( pTickBrush, 1.0 );

			foreach ( var lTickDate in EnumerateMajorTicks( pViewportStart, pViewportEnd, lSchedule.Granularity, lSchedule.Step ) )
			{
				var lX = DateToPixel( lTickDate, pViewportStart, pContentRect );

				pDrawingContext.DrawLine(
					lPen,
					new Point( pContentRect.Left + lX, pBaselineY ),
					new Point( pContentRect.Left + lX, pBaselineY - TickMajorHeight ) );

				var lLabelText = FormatTickLabel( lTickDate, lSchedule.Granularity );
				var lText = CreateFormattedTextCached( lLabelText, lTypeface, DateLabelFontSize, pLabelBrush, pPixelsPerDip );

				if ( lText == null )
				{
					continue;
				}

				var lTextX = pContentRect.Left + lX - ( lText.WidthIncludingTrailingWhitespace * 0.5 );
				var lTextRect = new Rect( lTextX, pLabelY, lText.WidthIncludingTrailingWhitespace, lText.Height );

				if ( lTextRect.Right < pContentRect.Left || lTextRect.Left > pContentRect.Right )
				{
					continue;
				}

				pDrawingContext.DrawText( lText, lTextRect.TopLeft );
			}

			foreach ( var lTickDate in EnumerateMinorTicks( pViewportStart, pViewportEnd, lSchedule.Granularity, lSchedule.Step ) )
			{
				var lX = DateToPixel( lTickDate, pViewportStart, pContentRect );

				pDrawingContext.DrawLine(
					lPen,
					new Point( pContentRect.Left + lX, pBaselineY ),
					new Point( pContentRect.Left + lX, pBaselineY - TickMinorHeight ) );
			}
		}

		private void DrawSelectedIndicator(
			DrawingContext pDrawingContext,
			Rect pContentRect,
			DateTime pViewportStart,
			double pBaselineY,
			Brush pAccentBrush,
			Brush pTextBrush,
			double pPixelsPerDip )
		{
			var lSelected = ClampDateToRange( SelectedDate );

			var lX = DateToPixel( lSelected, pViewportStart, pContentRect );
			var lLineX = pContentRect.Left + lX;

			if ( pAccentBrush != null )
			{
				var lPen = new Pen( pAccentBrush, 2.0 );
				pDrawingContext.DrawLine( lPen, new Point( lLineX, pContentRect.Top + 6.0 ), new Point( lLineX, pContentRect.Bottom - 10.0 ) );
			}

			var lTypeface = new Typeface( FontFamily, FontStyle, FontWeight, FontStretch );

			var lLabel = lSelected.ToString( "MMM d, yyyy", CultureInfo.CurrentCulture );
			var lText = CreateFormattedTextCached( lLabel, lTypeface, 12.0, pTextBrush, pPixelsPerDip );

			if ( lText == null )
			{
				return;
			}

			const double lPillPaddingX = 10.0;
			const double lPillPaddingY = 5.0;

			var lPillWidth = lText.WidthIncludingTrailingWhitespace + ( lPillPaddingX * 2.0 );
			var lPillHeight = lText.Height + ( lPillPaddingY * 2.0 );

			var lPillX = Math.Max( pContentRect.Left, Math.Min( pContentRect.Right - lPillWidth, lLineX - ( lPillWidth * 0.5 ) ) );
			var lPillY = pBaselineY - lPillHeight - 12.0;

			var lPillRect = new Rect( lPillX, lPillY, lPillWidth, lPillHeight );

			var lPillBackground = CreateOpacityBrush( pAccentBrush, 0.22 ) ?? TryFindResource( "OnSurfaceCardStrokeOnDarkBrush" ) as Brush;
			DrawRoundedRect( pDrawingContext, lPillRect, lPillBackground, new RadiusXy( 12.0, 12.0 ) );

			pDrawingContext.DrawText( lText, new Point( lPillRect.Left + lPillPaddingX, lPillRect.Top + lPillPaddingY ) );
		}

		private Rect GetContentRect()
		{
			var lPadding = Padding;
			if ( lPadding is { Left: <= 0.0, Top: <= 0.0, Right: <= 0.0, Bottom: <= 0.0 } )
			{
				lPadding = new Thickness( DefaultPadding );
			}

			return new Rect(
				lPadding.Left,
				lPadding.Top,
				Math.Max( 0.0, ActualWidth - ( lPadding.Left + lPadding.Right ) ),
				Math.Max( 0.0, ActualHeight - ( lPadding.Top + lPadding.Bottom ) ) );
		}

		private double CoerceZoomValueByContent( double pZoom, Rect pContentRect )
		{
			var lRangeDays = Math.Max( 1.0, ( DateTime.Today - EffectiveMinDate ).TotalDays );
			var lContentWidth = Math.Max( 1.0, pContentRect.Width );

			var lFitZoom = lContentWidth / lRangeDays;
			var lEffectiveMinZoom = Math.Max( MinimumZoomPixelsPerDay, lFitZoom );
			var lEffectiveMaxZoom = Math.Max( MaximumZoomPixelsPerDay, lEffectiveMinZoom );

			if ( double.IsNaN( pZoom ) || double.IsInfinity( pZoom ) )
			{
				return lEffectiveMinZoom;
			}

			if ( pZoom < lEffectiveMinZoom )
			{
				return lEffectiveMinZoom;
			}

			return pZoom > lEffectiveMaxZoom ? lEffectiveMaxZoom : pZoom;
		}

		private DateTime ClampDateToRange( DateTime pDate )
		{
			var lMin = EffectiveMinDate;
			var lMax = DateTime.Today;

			if ( pDate < lMin )
			{
				return lMin;
			}

			return pDate > lMax ? lMax : pDate;
		}

		private DateTime ClampViewportStartDate( DateTime pViewportStart, Rect pContentRect )
		{
			var lMin = EffectiveMinDate;
			var lMax = DateTime.Today;

			var lZoom = CoerceZoomValueByContent( ZoomLevel, pContentRect );
			var lVisibleDays = pContentRect.Width / Math.Max( MinimumZoomPixelsPerDay, lZoom );

			var lLatestStart = lMax.AddDays( -lVisibleDays );

			if ( lLatestStart < lMin )
			{
				return lMin;
			}

			if ( pViewportStart < lMin )
			{
				return lMin;
			}

			return pViewportStart > lLatestStart ? lLatestStart : pViewportStart;
		}

		private DateTime ClampViewportStartDate( DateTime pViewportStart, Rect pContentRect, double pZoomLevel )
		{
			var lMin = EffectiveMinDate;
			var lMax = DateTime.Today;

			var lZoom = CoerceZoomValue( pZoomLevel );
			var lVisibleDays = pContentRect.Width / Math.Max( MinimumZoomPixelsPerDay, lZoom );

			var lLatestStart = lMax.AddDays( -lVisibleDays );

			if ( lLatestStart < lMin )
			{
				return lMin;
			}

			if ( pViewportStart < lMin )
			{
				return lMin;
			}

			return pViewportStart > lLatestStart ? lLatestStart : pViewportStart;
		}

		private void EnsureDateVisible( DateTime pDate, Rect pContentRect )
		{
			var lDate = ClampDateToRange( pDate );
			var lZoom = CoerceZoomValueByContent( ZoomLevel, pContentRect );

			var lVisibleDays = pContentRect.Width / Math.Max( MinimumZoomPixelsPerDay, lZoom );

			var lStart = ClampViewportStartDate( ViewportStartDate, pContentRect );
			var lEnd = lStart.AddDays( lVisibleDays );

			if ( lDate >= lStart && lDate <= lEnd )
			{
				return;
			}

			var lCenteredStart = lDate.AddDays( -( lVisibleDays * 0.5 ) );
			SetViewportStartDateCurrentValue( ClampViewportStartDate( lCenteredStart, pContentRect ) );
		}

		private DateTime PixelToDate( double pPixelX, Rect pContentRect )
		{
			var lOffsetPixels = pPixelX - pContentRect.Left;
			var lZoom = CoerceZoomValueByContent( ZoomLevel, pContentRect );

			var lDays = lOffsetPixels / Math.Max( MinimumZoomPixelsPerDay, lZoom );
			return ClampDateToRange( ViewportStartDate.AddDays( lDays ) );
		}

		private double DateToPixel( DateTime pDate, DateTime pViewportStart, Rect pContentRect )
		{
			var lZoom = CoerceZoomValueByContent( ZoomLevel, pContentRect );
			var lDays = ( pDate - pViewportStart ).TotalDays;
			return lDays * Math.Max( MinimumZoomPixelsPerDay, lZoom );
		}

		private Brush ResolveTimeFrameBrush( TimelineTimeFrameItem pItem )
		{
			if ( pItem == null )
			{
				return null;
			}

			if ( pItem.AccentBrush != null )
			{
				return pItem.AccentBrush;
			}

			var lKey = pItem.AccentColorKey;
			if ( string.IsNullOrWhiteSpace( lKey ) )
			{
				return null;
			}

			return TryFindResource( lKey ) as Brush;
		}

		private TimeFrameHitInfo GetTimeFrameHitInfoAtPosition( Point pPosition )
		{
			if ( mTimeFrameHitInfos.Count == 0 )
			{
				return null;
			}

			return mTimeFrameHitInfos
				.Where( pHitInfo => pHitInfo.HitRect.Contains( pPosition ) )
				.OrderByDescending( pHitInfo => pHitInfo.HitRect.Width )
				.FirstOrDefault();
		}

		private List<VisibleTimeFrame> BuildVisibleTimeFrames(
			IEnumerable<TimelineTimeFrameItem> pTimeFrames,
			DateTime pMinDate,
			DateTime pMaxDate,
			DateTime pViewportStart,
			DateTime pViewportEnd,
			Rect pContentRect )
		{
			var lFrames = pTimeFrames
				.Where( pItem => pItem != null )
				.Select( pItem =>
				{
					var lStart = pItem.StartDate.Date;
					var lEnd = pItem.EndDate.Date;

					if ( lEnd < lStart )
					{
						(lStart, lEnd) = (lEnd, lStart);
					}

					lStart = lStart < pMinDate ? pMinDate : lStart;
					lEnd = lEnd > pMaxDate ? pMaxDate : lEnd;

					return new { Item = pItem, Start = lStart, End = lEnd };
				} )
				.Where( pItem => pItem.End >= pItem.Start )
				.Where( pItem => pItem.End >= pViewportStart && pItem.Start <= pViewportEnd )
				.Select( pItem =>
				{
					var lStartX = DateToPixel( pItem.Start, pViewportStart, pContentRect );
					var lEndX = DateToPixel( pItem.End, pViewportStart, pContentRect );
					return new VisibleTimeFrame( pItem.Item, pItem.Start, pItem.End, lStartX, lEndX, 0 );
				} )
				.OrderBy( pItem => pItem.StartX )
				.ThenByDescending( pItem => pItem.EndX )
				.ThenBy( pItem => pItem.Item?.Title ?? string.Empty, StringComparer.OrdinalIgnoreCase )
				.ToList();

			var lLaneEnds = new List<double>();

			for ( var lCurrentIndex = 0; lCurrentIndex < lFrames.Count; lCurrentIndex++ )
			{
				var lFrame = lFrames[ lCurrentIndex ];

				var lLaneIndex = FindLaneIndex( lLaneEnds, lFrame.StartX );
				if ( lLaneIndex >= lLaneEnds.Count )
				{
					lLaneEnds.Add( lFrame.EndX );
				}
				else
				{
					lLaneEnds[ lLaneIndex ] = lFrame.EndX;
				}

				lFrames[ lCurrentIndex ] = new VisibleTimeFrame( lFrame.Item, lFrame.StartDate, lFrame.EndDate, lFrame.StartX, lFrame.EndX, lLaneIndex );
			}

			return lFrames;
		}

		private void OnTimeFramesCollectionChanged( object pSender, NotifyCollectionChangedEventArgs pEventArgs )
		{
			CoerceValue( sZoomLevelProperty );
			CoerceValue( sViewportStartTicksProperty );

			MinDate = MinDate;

			EnsureInitialFitIfNeeded();
			InvalidateVisual();
		}

		private bool HasExplicitLocalNonBindingValue( DependencyProperty pDependencyProperty )
		{
			var lLocalValue = ReadLocalValue( pDependencyProperty );
			if ( lLocalValue == DependencyProperty.UnsetValue )
			{
				return false;
			}

			return lLocalValue is not BindingExpressionBase;
		}

		private bool HasInitialFitRangeAvailable()
		{
			if ( MinDate != DateTime.MinValue )
			{
				return true;
			}

			var lTimeFrames = TimeFrames;
			return lTimeFrames is { Count: > 0 };
		}

		private void UpdateBindingSourceIfNeeded( DependencyProperty pDependencyProperty )
		{
			if ( BindingOperations.GetBindingExpression( this, pDependencyProperty ) is not BindingExpression lBindingExpression )
			{
				return;
			}

			var lMode = lBindingExpression.ParentBinding?.Mode ?? BindingMode.Default;
			var lIsUpdateAllowed = lMode == BindingMode.TwoWay || lMode == BindingMode.OneWayToSource || lMode == BindingMode.Default;

			if ( !lIsUpdateAllowed )
			{
				return;
			}

			lBindingExpression.UpdateSource();
		}

		private void SetZoomLevelCurrentValue( double pZoomLevel )
		{
			SetCurrentValue( sZoomLevelProperty, pZoomLevel );
		}

		private void SetViewportStartTicksCurrentValue( double pViewportStartTicks )
		{
			SetCurrentValue( sViewportStartTicksProperty, pViewportStartTicks );
		}

		private void SetViewportStartDateCurrentValue( DateTime pViewportStartDate )
		{
			SetViewportStartTicksCurrentValue( pViewportStartDate.Ticks );
		}

		private void SetSelectedDateCurrentValue( DateTime pSelectedDate )
		{
			SetCurrentValue( sSelectedDateProperty, pSelectedDate );
		}

		private void SetSelectedTimeFrameCurrentValue( TimelineTimeFrameItem pSelectedTimeFrame )
		{
			SetCurrentValue( sSelectedTimeFrameProperty, pSelectedTimeFrame );
		}
	}
}
