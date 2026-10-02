// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ResumeApp.AttachedProperties;
using ResumeApp.Infrastructure;
using ResumeApp.Models;
using ResumeApp.Services;

namespace ResumeApp.Controls
{
	[ExcludeFromCodeCoverage( Justification = "Custom WPF Control with OnRender/DrawingContext rendering, mouse/touch/keyboard input handlers, pan/zoom inertia animation via CompositionTarget.Rendering, and VisualTreeHelper operations requiring a running WPF desktop." )]
	public sealed class TimelineControl : Control
	{
		private sealed class TimeFrameHitInfo( TimelineTimeFrameItem pItem, Rect pHitRect )
		{
			public TimelineTimeFrameItem Item { get; } = pItem;

			public Rect HitRect { get; } = pHitRect;
		}

		private sealed class FrameLayout(
			TimelineTimeFrameItem pItem,
			DateTime pStartDate,
			DateTime pEndDate,
			int pLaneIndex,
			int pPreviousIndex,
			int pNextIndex )
		{
			public TimelineTimeFrameItem Item { get; } = pItem;

			public DateTime StartDate { get; } = pStartDate;

			public DateTime EndDate { get; } = pEndDate;

			public int LaneIndex { get; } = pLaneIndex;

			public int PreviousIndex { get; } = pPreviousIndex;

			public int NextIndex { get; } = pNextIndex;
		}

		private sealed class RenderContext
		{
			public required TimelineGeometry Geometry { get; init; }

			public required Rect PlotRect { get; init; }

			public required double BaselineY { get; init; }

			public required double LaneTopY { get; init; }

			public required double Zoom { get; init; }

			public required DateTime ViewportStart { get; init; }

			public required DateTime ViewportEnd { get; init; }

			public required double PixelsPerDip { get; init; }

			public required CultureInfo Culture { get; init; }

			public required bool IsHighContrast { get; init; }

			public required double InactiveOpacity { get; init; }

			public required Brush AccentBrush { get; init; }

			public required Brush AccentTextBrush { get; init; }

			public required Brush BorderStrongBrush { get; init; }

			public required Brush BorderSubtleBrush { get; init; }

			public required Brush DividerBrush { get; init; }

			public required Brush FocusRingBrush { get; init; }

			public required Brush LabelOnBarBrush { get; init; }

			public required Brush PillFillBrush { get; init; }

			public required Brush PillBorderBrush { get; init; }

			public required Brush SurfaceBrush { get; init; }

			public required Brush TextPrimaryBrush { get; init; }

			public required Brush TextSecondaryBrush { get; init; }

			public double DateToX( DateTime pDate ) => PlotRect.Left + ( ( pDate - ViewportStart ).TotalDays * Zoom );
		}

		private readonly struct PanSample( DateTime pTimestampUtc, double pPointerX )
		{
			public DateTime TimestampUtc { get; } = pTimestampUtc;

			public double PointerX { get; } = pPointerX;
		}

		private readonly struct FormattedTextCacheKey( string? pText, double pFontSize, int pWeight, Brush? pBrush )
			: IEquatable<FormattedTextCacheKey>
		{
			public string Text { get; } = pText ?? string.Empty;

			public double FontSize { get; } = pFontSize;

			public int Weight { get; } = pWeight;

			public Brush? Brush { get; } = pBrush;

			public bool Equals( FormattedTextCacheKey pOther )
			{
				return string.Equals( Text, pOther.Text, StringComparison.Ordinal )
					   && Math.Abs( FontSize - pOther.FontSize ) < 0.000001
					   && Weight == pOther.Weight
					   && ReferenceEquals( Brush, pOther.Brush );
			}

			public override bool Equals( object? pObject )
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
					lHash = ( lHash * 31 ) + Weight;
					lHash = ( lHash * 31 ) + ( Brush is null ? 0 : RuntimeHelpers.GetHashCode( Brush ) );
					return lHash;
				}
			}
		}

		private enum KeyboardStep
		{
			Day,
			Week,
			Month,
			Year,
			Decade
		}

		private enum CursorKind
		{
			Default,
			Plot,
			Hand,
			Dragging
		}

		private const double MinimumZoomPixelsPerDay = 0.08;
		private const double MaximumZoomPixelsPerDay = 48.0;
		private const double WheelZoomFactorPerNotch = 1.12;

		private const double TodayLabelFontSize = 11.0;
		private const double TodayLabelGapPixels = 4.0;
		private const double TodayLabelBaselineInsetPixels = 3.0;

		private const double TickMajorHeight = 4.0;
		private const double TickMinorHeight = 2.0;
		private const double TickLabelTopGap = 6.0;

		private const double SelectedLabelCollisionGapPixels = 6.0;
		private const double TickLabelCollisionGapPixels = 6.0;
		private const double PillTopGap = TimelineLayoutHelper.PillTopGap;
		private const double ScrubberTopExtension = 4.0;
		private const double ScrubberHandleRadius = 4.5;
		private const double SelectedRingInflate = 3.0;
		private const double HitRectMinimumWidth = TimelineLayoutHelper.MinimumHitWidth;

		private const double DragActivationThresholdPixels = 3.0;

		private const double ViewAnimationDurationMilliseconds = 240.0;

		private const int ClickMaximumDurationMilliseconds = 320;

		private const int FormattedTextCacheMaxEntries = 256;

		private const string DefaultTodayMarkerText = "Today";
		private const string DefaultLabelSeparator = " \u00B7 ";
		private const string LabelSeparatorResourceKey = "TimelineBarLabelSeparator";

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
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
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

		public static readonly DependencyProperty sTodayMarkerTextProperty =
			DependencyProperty.Register(
				nameof( TodayMarkerText ),
				typeof( string ),
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata(
					DefaultTodayMarkerText,
					FrameworkPropertyMetadataOptions.AffectsRender ) );

		public static readonly DependencyProperty sSizeClassProperty =
			AdaptiveLayout.sSizeClassProperty.AddOwner(
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata(
					LayoutSizeClass.Regular,
					FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender ) );

		private static readonly DependencyPropertyKey sIsShowingAllPropertyKey =
			DependencyProperty.RegisterReadOnly(
				nameof( IsShowingAll ),
				typeof( bool ),
				typeof( TimelineControl ),
				new FrameworkPropertyMetadata( true ) );

		public static readonly DependencyProperty sIsShowingAllProperty = sIsShowingAllPropertyKey.DependencyProperty;

		public static readonly RoutedEvent sSelectionActivatedEvent =
			EventManager.RegisterRoutedEvent(
				nameof( SelectionActivated ),
				RoutingStrategy.Bubble,
				typeof( RoutedEventHandler ),
				typeof( TimelineControl ) );

		private readonly List<TimeFrameHitInfo> mTimeFrameHitInfos;
		private readonly List<PanSample> mPanSamples;
		private readonly Dictionary<FormattedTextCacheKey, FormattedText> mFormattedTextCache;

		private List<FrameLayout> mFrameLayouts;
		private int mLaneCount;
		private bool mIsLayoutDirty;
		private DateTime mLayoutToday;

		private bool mIsPointerDown;
		private bool mHasDragged;
		private Point mPointerDownPoint;
		private double mViewportStartTicksAtPointerDown;

		private DateTime mPointerDownTimestampUtc;
		private TimelineTimeFrameItem? mPointerDownTimeFrameItem;

		private bool mIsInertiaActive;
		private double mInertiaVelocityTicksPerSecond;
		private DateTime mLastRenderTick;

		private bool mIsViewAnimationActive;
		private DateTime mViewAnimationStartUtc;
		private double mViewAnimationStartViewportTicks;
		private double mViewAnimationTargetViewportTicks;
		private double mViewAnimationStartZoom;
		private double mViewAnimationTargetZoom;

		private bool mIsRenderingSubscribed;

		private ThemeService? mSubscribedThemeService;

		private bool mHasPendingPan;
		private double mPendingPanPointerX;
		private DateTime mPendingPanTimestampUtc;

		private bool mIsInternalSelectedDateUpdate;

		private CursorKind mActiveCursorKind;

		private TimelineTimeFrameItem? mHoveredTimeFrameItem;

		private string? mTextCacheCultureName;
		private FontFamily? mTextCacheFontFamily;
		private FontStyle mTextCacheFontStyle;
		private FontStretch mTextCacheFontStretch;
		private double mTextCachePixelsPerDip;

		private bool mIsFitPending;
		private bool mHasAppliedInitialFit;
		private bool mHasSuppressSelectedTimeFrameToSelectedDateSync;
		private int mSelectedTimeFrameToSelectedDateSyncSuppressionVersion;

		private string? mLabelSeparator;

		public event RoutedEventHandler SelectionActivated
		{
			add => AddHandler( sSelectionActivatedEvent, value );
			remove => RemoveHandler( sSelectionActivatedEvent, value );
		}

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

		public TimelineTimeFrameItem? SelectedTimeFrame
		{
			get => GetValue( sSelectedTimeFrameProperty ) as TimelineTimeFrameItem;
			set => SetCurrentValue( sSelectedTimeFrameProperty, value );
		}

		public ObservableCollection<TimelineTimeFrameItem>? TimeFrames
		{
			get => GetValue( sTimeFramesProperty ) as ObservableCollection<TimelineTimeFrameItem>;
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

		public string TodayMarkerText
		{
			get => ( GetValue( sTodayMarkerTextProperty ) as string ) ?? DefaultTodayMarkerText;
			set => SetCurrentValue( sTodayMarkerTextProperty, value ?? DefaultTodayMarkerText );
		}

		public bool IsShowingAll => ( bool )GetValue( sIsShowingAllProperty );

		public LayoutSizeClass SizeClass => AdaptiveLayout.GetSizeClass( this );

		private DateTime EffectiveMinDate => GetEffectiveMinDate();

		static TimelineControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata( typeof( TimelineControl ), new FrameworkPropertyMetadata( typeof( TimelineControl ) ) );
		}

		public TimelineControl()
		{
			mTimeFrameHitInfos = [];
			mPanSamples = [];
			mFormattedTextCache = new Dictionary<FormattedTextCacheKey, FormattedText>();
			mFrameLayouts = [];
			mIsLayoutDirty = true;
			mIsFitPending = true;
			mActiveCursorKind = CursorKind.Default;

			Focusable = true;
			FocusVisualStyle = null;
			ClipToBounds = true;

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
			DataContextChanged += OnDataContextChanged;
		}

		public void ZoomIn() => ZoomByFactor( TimelineLayoutHelper.ZoomStepFactor );

		public void ZoomOut() => ZoomByFactor( 1.0 / TimelineLayoutHelper.ZoomStepFactor );

		public void ShowAll()
		{
			CancelMotion();
			mIsFitPending = false;
			ApplyFit( true );
		}

		internal string GetSelectedTimeFrameSummary() => FormatFrameSummary( SelectedTimeFrame, GetLabelSeparator() );

		internal static string FormatFrameSummary( TimelineTimeFrameItem? pItem, string? pSeparator = null )
		{
			if ( pItem is null )
			{
				return string.Empty;
			}

			var lHeadline = TimelineLayoutHelper.FormatFrameHeadline( pItem.Title, pItem.SubtitleText, pSeparator ?? DefaultLabelSeparator );
			var lDescription = pItem.DescriptionText;

			if ( string.IsNullOrWhiteSpace( lDescription ) )
			{
				return lHeadline;
			}

			return lHeadline.Length == 0 ? lDescription : lHeadline + ", " + lDescription;
		}

		internal static string FormatFrameToolTip( TimelineTimeFrameItem pItem, string pSeparator )
		{
			var lHeadline = TimelineLayoutHelper.FormatFrameHeadline( pItem.Title, pItem.SubtitleText, pSeparator );
			var lDescription = pItem.DescriptionText;

			return string.IsNullOrWhiteSpace( lDescription ) ? lHeadline : lHeadline + "\n" + lDescription;
		}

		internal static string FormatPillDateLabel( DateTime pDate, CultureInfo pCulture ) => TimelineLayoutHelper.FormatPillLabel( pDate, pCulture );

		internal static double ClampTickLabelLeft( double pLeft, double pWidth, double pMinX, double pMaxX )
		{
			var lMaxLeft = pMaxX - pWidth;
			return lMaxLeft <= pMinX ? pMinX : Math.Max( pMinX, Math.Min( pLeft, lMaxLeft ) );
		}

		internal static bool TryGetTickLabelRect(
			double pTickX,
			double pLabelWidth,
			double pLabelY,
			double pLabelHeight,
			Rect pContentRect,
			out Rect pLabelRect )
		{
			if ( pTickX < 0.0 || pTickX > pContentRect.Width )
			{
				pLabelRect = Rect.Empty;
				return false;
			}

			var lLeft = ClampTickLabelLeft(
				pContentRect.Left + pTickX - ( pLabelWidth * 0.5 ),
				pLabelWidth,
				pContentRect.Left,
				pContentRect.Right );

			pLabelRect = new Rect( lLeft, pLabelY, pLabelWidth, pLabelHeight );
			return true;
		}

		internal static bool ShouldSkipTickLabel( Rect pLabelRect, Rect? pSelectedLabelRect, double pGapPixels )
		{
			if ( !pSelectedLabelRect.HasValue || pSelectedLabelRect.Value.IsEmpty )
			{
				return false;
			}

			var lGuardRect = pSelectedLabelRect.Value;
			lGuardRect.Inflate( pGapPixels, 0.0 );

			return lGuardRect.IntersectsWith( pLabelRect );
		}

		private static double Lerp( double pStart, double pEnd, double pT ) => pStart + ( ( pEnd - pStart ) * pT );

		private static double EaseOutCubic( double pT )
		{
			var lT = Math.Max( 0.0, Math.Min( 1.0, pT ) );
			var lInv = 1.0 - lT;
			return 1.0 - ( lInv * lInv * lInv );
		}

		private static double SnapToPixelCenter( double pValue ) => Math.Floor( pValue ) + 0.5;

		private static double SafeZoom( double pZoom ) => pZoom > 0.0 ? pZoom : MinimumZoomPixelsPerDay;

		private static KeyboardStep GetKeyboardStep( double pZoomLevel, bool pIsControlDown )
		{
			var lZoom = Math.Max( MinimumZoomPixelsPerDay, pZoomLevel );

			KeyboardStep lBaseStep = lZoom switch
			{
				>= 20.0 => KeyboardStep.Day,
				>= 6.0 => KeyboardStep.Week,
				>= 1.5 => KeyboardStep.Month,
				_ => KeyboardStep.Year
			};

			return pIsControlDown ? PromoteStep( lBaseStep ) : lBaseStep;
		}

		private static KeyboardStep PromoteStep( KeyboardStep pStep )
		{
			return pStep switch
			{
				KeyboardStep.Day => KeyboardStep.Week,
				KeyboardStep.Week => KeyboardStep.Month,
				KeyboardStep.Month => KeyboardStep.Year,
				_ => KeyboardStep.Decade
			};
		}

		private static DateTime AddStep( DateTime pDate, KeyboardStep pStep, int pDirection )
		{
			return pStep switch
			{
				KeyboardStep.Day => pDate.AddDays( 1.0 * pDirection ),
				KeyboardStep.Week => pDate.AddDays( 7.0 * pDirection ),
				KeyboardStep.Month => pDate.AddMonths( 1 * pDirection ),
				KeyboardStep.Year => pDate.AddYears( 1 * pDirection ),
				_ => pDate.AddYears( 10 * pDirection )
			};
		}

		private static Cursor ResolveCustomCursor( Func<Cursor> pFactory )
		{
			try
			{
				return pFactory();
			}
			catch ( Exception )
			{
				return Cursors.SizeWE;
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

			lControl.mIsLayoutDirty = true;

			lControl.SetSelectedDateCurrentValue( lControl.ClampDateToRange( lControl.SelectedDate ) );

			if ( lControl.IsShowingAll || lControl.mIsFitPending )
			{
				lControl.RequestFit();
			}
			else
			{
				lControl.ReclampViewport();
			}

			lControl.InvalidateMeasure();
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
			if ( pDependencyObject is not TimelineControl lControl || lControl.mIsInternalSelectedDateUpdate )
			{
				return;
			}

			lControl.mIsViewAnimationActive = false;

			lControl.mIsInertiaActive = false;
			lControl.mHasPendingPan = false;

			var lContentRect = lControl.GetContentRect();
			var lSelected = lControl.ClampDateToRange( lControl.SelectedDate );

			if ( lSelected != lControl.SelectedDate )
			{
				lControl.SetSelectedDateInternal( lSelected );
			}

			lControl.EnsureDateVisible( lControl.SelectedDate, lContentRect, true );
			lControl.UpdateRenderingSubscriptionIfNeeded();
		}

		private static void OnSelectedTimeFrameChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pEventArgs )
		{
			if ( pDependencyObject is not TimelineControl lControl )
			{
				return;
			}

			lControl.InvalidateVisual();
			lControl.RaiseAutomationValueChanged( pEventArgs.OldValue as TimelineTimeFrameItem, pEventArgs.NewValue as TimelineTimeFrameItem );

			if ( lControl.mHasSuppressSelectedTimeFrameToSelectedDateSync || pEventArgs.NewValue is not TimelineTimeFrameItem lNewTimeFrame )
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

			lControl.mIsLayoutDirty = true;

			lControl.SetSelectedDateCurrentValue( lControl.SelectedDate );

			lControl.RequestFit();
			lControl.InvalidateMeasure();
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
			lControl.UpdateIsShowingAll();
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

		protected override AutomationPeer OnCreateAutomationPeer() => new TimelineControlAutomationPeer( this );

		protected override Size MeasureOverride( Size pAvailableSize )
		{
			EnsureStableLayout();

			var lWidth = double.IsNaN( pAvailableSize.Width ) || double.IsInfinity( pAvailableSize.Width ) ? 0.0 : pAvailableSize.Width;
			return new Size( lWidth, TimelineLayoutHelper.ComputeHeight( GetGeometry(), mLaneCount ) );
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

			var lContentRect = GetContentRect();
			if ( lContentRect.Width <= 1.0 )
			{
				return;
			}

			EnsureStableLayout();

			pDrawingContext.DrawRectangle( Background ?? Brushes.Transparent, null, new Rect( 0.0, 0.0, lActualWidth, lActualHeight ) );
			DrawTimeline( pDrawingContext, lContentRect );
			DrawFocusOutlineIfNeeded( pDrawingContext );
		}

		protected override void OnMouseWheel( MouseWheelEventArgs pEventArgs )
		{
			base.OnMouseWheel( pEventArgs );

			CancelMotion();

			var lPosition = pEventArgs.GetPosition( this );
			var lContentRect = GetContentRect();

			if ( !lContentRect.Contains( lPosition ) )
			{
				UpdateCursorAtPosition( lPosition );
				return;
			}

			var lZoom = SafeZoom( CoerceZoomValueByContent( ZoomLevel, lContentRect ) );
			var lViewportStart = ClampViewportStartDate( ViewportStartDate, lContentRect );
			var lAnchorOffsetPixels = lPosition.X - lContentRect.Left;
			var lAnchorDate = lViewportStart.AddDays( lAnchorOffsetPixels / lZoom );

			var lNotchCount = pEventArgs.Delta / 120.0;
			var lRequestedZoom = lZoom * Math.Pow( WheelZoomFactorPerNotch, lNotchCount );
			var lTargetZoom = CoerceZoomValueByContent( lRequestedZoom, lContentRect );

			ApplyZoom( lTargetZoom, lAnchorDate, lAnchorOffsetPixels, lContentRect );

			UpdateCursorAtPosition( lPosition );
			UpdateRenderingSubscriptionIfNeeded();

			pEventArgs.Handled = true;
		}

		protected override void OnMouseLeftButtonDown( MouseButtonEventArgs pEventArgs )
		{
			base.OnMouseLeftButtonDown( pEventArgs );

			Focus();

			CancelMotion();

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

			mHasDragged = true;
			mIsInertiaActive = false;

			mIsViewAnimationActive = false;

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

			if ( mIsPointerDown )
			{
				return;
			}

			SetCursorKind( CursorKind.Default );
			SetHoveredTimeFrame( null );
		}

		protected override void OnLostMouseCapture( MouseEventArgs pEventArgs )
		{
			base.OnLostMouseCapture( pEventArgs );

			if ( !mIsPointerDown )
			{
				return;
			}

			mIsPointerDown = false;
			mHasDragged = false;
			mHasPendingPan = false;
			mIsInertiaActive = false;
			mPointerDownTimeFrameItem = null;
			mPanSamples.Clear();

			if ( IsMouseOver )
			{
				UpdateCursorAtPosition( Mouse.GetPosition( this ) );
			}
			else
			{
				SetCursorKind( CursorKind.Default );
				SetHoveredTimeFrame( null );
			}

			UpdateRenderingSubscriptionIfNeeded();
		}

		protected override void OnGotKeyboardFocus( KeyboardFocusChangedEventArgs pEventArgs )
		{
			base.OnGotKeyboardFocus( pEventArgs );
			InvalidateVisual();
		}

		protected override void OnLostKeyboardFocus( KeyboardFocusChangedEventArgs pEventArgs )
		{
			base.OnLostKeyboardFocus( pEventArgs );
			InvalidateVisual();
		}

		protected override void OnKeyDown( KeyEventArgs pEventArgs )
		{
			base.OnKeyDown( pEventArgs );

			mIsViewAnimationActive = false;

			var lContentRect = GetContentRect();
			var lIsControlDown = ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control;
			var lIsAltDown = ( Keyboard.Modifiers & ModifierKeys.Alt ) == ModifierKeys.Alt;

			switch ( pEventArgs.Key )
			{
				case Key.Home:
					{
						SetSelectedDateCurrentValue( ClampDateToRange( EffectiveMinDate ) );
						EnsureDateVisible( SelectedDate, lContentRect, true );
						pEventArgs.Handled = true;
						return;
					}
				case Key.End:
					{
						SetSelectedDateCurrentValue( ClampDateToRange( DateTime.Today ) );
						EnsureDateVisible( SelectedDate, lContentRect, true );
						pEventArgs.Handled = true;
						return;
					}
				case Key.OemPlus or Key.Add when !lIsAltDown:
					{
						ZoomIn();
						pEventArgs.Handled = true;
						return;
					}
				case Key.OemMinus or Key.Subtract when !lIsAltDown:
					{
						ZoomOut();
						pEventArgs.Handled = true;
						return;
					}
				case Key.D0 or Key.NumPad0 when !lIsAltDown:
					{
						ShowAll();
						pEventArgs.Handled = true;
						return;
					}
				case Key.Enter when !lIsAltDown:
					{
						RaiseEvent( new RoutedEventArgs( sSelectionActivatedEvent, this ) );
						pEventArgs.Handled = true;
						return;
					}
			}

			if ( pEventArgs.Key is Key.Up or Key.Down )
			{
				NavigateTimeFrameByDirection( pEventArgs.Key == Key.Up ? -1 : 1, lContentRect );
				pEventArgs.Handled = true;
				return;
			}

			if ( pEventArgs.Key != Key.Left && pEventArgs.Key != Key.Right )
			{
				return;
			}

			var lDirection = pEventArgs.Key == Key.Left ? -1 : 1;
			var lStep = GetKeyboardStep( ZoomLevel, lIsControlDown );
			var lNewDate = AddStep( SelectedDate, lStep, lDirection );

			SetSelectedDateCurrentValue( ClampDateToRange( lNewDate ) );
			EnsureDateVisible( SelectedDate, lContentRect, true );

			pEventArgs.Handled = true;
		}

		protected override void OnRenderSizeChanged( SizeChangedInfo pSizeInfo )
		{
			base.OnRenderSizeChanged( pSizeInfo );

			if ( IsShowingAll || mIsFitPending )
			{
				mIsFitPending = true;
				TryApplyPendingFit();
			}
			else
			{
				ReclampViewport();
			}

			EnsureDateVisible( SelectedDate, GetContentRect(), false );
			UpdateIsShowingAll();
		}

		private void RequestFit()
		{
			mIsFitPending = true;
			TryApplyPendingFit();
		}

		private void TryApplyPendingFit()
		{
			if ( !mIsFitPending || !HasInitialFitRangeAvailable() || GetContentRect().Width <= 1.0 )
			{
				return;
			}

			mIsFitPending = false;

			if ( !mHasAppliedInitialFit && ( HasExplicitLocalNonBindingValue( sZoomLevelProperty ) || HasExplicitLocalNonBindingValue( sViewportStartTicksProperty ) ) )
			{
				mHasAppliedInitialFit = true;
				UpdateIsShowingAll();
				return;
			}

			mHasAppliedInitialFit = true;
			CancelMotion();
			ApplyFit( false );
		}

		private void ApplyFit( bool pAnimated )
		{
			var lContentRect = GetContentRect();
			if ( lContentRect.Width <= 1.0 )
			{
				return;
			}

			var lFitRange = GetFitRange( lContentRect );

			StartViewTransition( lFitRange.Zoom, lFitRange.Start.Ticks, pAnimated );

			UpdateBindingSourceIfNeeded( sZoomLevelProperty );
			UpdateBindingSourceIfNeeded( sViewportStartTicksProperty );
			UpdateIsShowingAll();
		}

		private void StartViewTransition( double pTargetZoom, double pTargetViewportTicks, bool pAnimated )
		{
			var lCanAnimate = pAnimated && IsLoaded && ActualWidth > 1.0 && MotionPolicy.IsAnimationEnabled;

			if ( !lCanAnimate )
			{
				mIsViewAnimationActive = false;
				SetZoomLevelCurrentValue( pTargetZoom );
				SetViewportStartTicksCurrentValue( pTargetViewportTicks );
				UpdateRenderingSubscriptionIfNeeded();
				return;
			}

			mViewAnimationStartUtc = DateTime.UtcNow;
			mViewAnimationStartViewportTicks = ViewportStartTicks;
			mViewAnimationTargetViewportTicks = pTargetViewportTicks;
			mViewAnimationStartZoom = ZoomLevel;
			mViewAnimationTargetZoom = pTargetZoom;
			mLastRenderTick = default;
			mIsViewAnimationActive = true;

			UpdateRenderingSubscriptionIfNeeded();
		}

		private void CancelMotion()
		{
			mIsViewAnimationActive = false;
			mIsInertiaActive = false;
			mHasPendingPan = false;
		}

		private void ZoomByFactor( double pFactor )
		{
			var lContentRect = GetContentRect();
			if ( lContentRect.Width <= 1.0 )
			{
				return;
			}

			CancelMotion();

			var lZoom = SafeZoom( CoerceZoomValueByContent( ZoomLevel, lContentRect ) );
			var lViewportStart = ClampViewportStartDate( ViewportStartDate, lContentRect );
			var lAnchorDate = ClampDateToRange( SelectedDate );
			var lAnchorOffsetPixels = ( lAnchorDate - lViewportStart ).TotalDays * lZoom;

			ApplyZoom( CoerceZoomValueByContent( lZoom * pFactor, lContentRect ), lAnchorDate, lAnchorOffsetPixels, lContentRect );

			UpdateBindingSourceIfNeeded( sZoomLevelProperty );
			UpdateBindingSourceIfNeeded( sViewportStartTicksProperty );
			UpdateRenderingSubscriptionIfNeeded();
		}

		private void ApplyZoom( double pTargetZoom, DateTime pAnchorDate, double pAnchorOffsetPixels, Rect pContentRect )
		{
			SetZoomLevelCurrentValue( pTargetZoom );

			var lNewViewportStart = pAnchorDate.AddDays( -( pAnchorOffsetPixels / SafeZoom( pTargetZoom ) ) );
			SetViewportStartDateCurrentValue( ClampViewportStartDate( lNewViewportStart, pContentRect ) );

			UpdateIsShowingAll();
		}

		private void ReclampViewport()
		{
			var lContentRect = GetContentRect();

			SetZoomLevelCurrentValue( CoerceZoomValueByContent( ZoomLevel, lContentRect ) );
			SetViewportStartDateCurrentValue( ClampViewportStartDate( ViewportStartDate, lContentRect ) );

			UpdateIsShowingAll();
		}

		private void UpdateIsShowingAll()
		{
			var lContentRect = GetContentRect();
			var lIsShowingAll = true;

			if ( HasInitialFitRangeAvailable() && lContentRect.Width > 1.0 )
			{
				var lZoom = CoerceZoomValueByContent( ZoomLevel, lContentRect );
				lIsShowingAll = TimelineLayoutHelper.IsShowingAll( lContentRect.Width, lZoom, GetFitRange( lContentRect ) );
			}

			if ( IsShowingAll != lIsShowingAll )
			{
				SetValue( sIsShowingAllPropertyKey, lIsShowingAll );
			}
		}

		private TimelineFitRange GetFitRange( Rect pContentRect )
		{
			return TimelineLayoutHelper.ComputeFitRange( EffectiveMinDate, DateTime.Today, pContentRect.Width );
		}

		private TimelineGeometry GetGeometry() => TimelineLayoutHelper.GetGeometry( AdaptiveLayout.GetSizeClass( this ) );

		private void EnsureStableLayout()
		{
			var lToday = DateTime.Today;
			if ( !mIsLayoutDirty && mLayoutToday == lToday )
			{
				return;
			}

			mIsLayoutDirty = false;
			mLayoutToday = lToday;

			var lItems = ( TimeFrames ?? [] ).ToList();
			var lMinDate = EffectiveMinDate;

			var lSpans = lItems.Select( pItem => CreateSpan( pItem, lMinDate, lToday ) ).ToList();
			var lLanes = TimelineLayoutHelper.AssignStableLanes( lSpans, out var lLaneCount );
			TimelineLayoutHelper.FindTouchingNeighbors( lSpans, lLanes, out var lPrevious, out var lNext );

			var lLayouts = new List<FrameLayout>( lItems.Count );
			for ( var lIndex = 0; lIndex < lItems.Count; lIndex++ )
			{
				lLayouts.Add( new FrameLayout( lItems[ lIndex ], lSpans[ lIndex ].Start, lSpans[ lIndex ].End, lLanes[ lIndex ], lPrevious[ lIndex ], lNext[ lIndex ] ) );
			}

			mFrameLayouts = lLayouts;
			mLaneCount = lLaneCount;
		}

		private static TimelineSpan CreateSpan( TimelineTimeFrameItem pItem, DateTime pMinDate, DateTime pToday )
		{
			var lStart = pItem.StartDate.Date;
			var lEnd = pItem.EndDate.Date;

			if ( lEnd < lStart )
			{
				( lStart, lEnd ) = ( lEnd, lStart );
			}

			lStart = lStart < pMinDate ? pMinDate : lStart;
			lEnd = lEnd > pToday ? pToday : lEnd;

			return new TimelineSpan( lStart, lEnd );
		}

		private string GetLabelSeparator()
		{
			return mLabelSeparator ??= TimelineResourceLookup.GetString( this, LabelSeparatorResourceKey, DefaultLabelSeparator );
		}

		private void RaiseAutomationValueChanged( TimelineTimeFrameItem? pOldItem, TimelineTimeFrameItem? pNewItem )
		{
			if ( UIElementAutomationPeer.FromElement( this ) is not TimelineControlAutomationPeer lPeer )
			{
				return;
			}

			var lSeparator = GetLabelSeparator();
			lPeer.RaiseValueChanged( FormatFrameSummary( pOldItem, lSeparator ), FormatFrameSummary( pNewItem, lSeparator ) );
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
			var lIsDragging = mIsPointerDown && mHasDragged;
			var lHitItem = lIsDragging ? null : GetTimeFrameHitInfoAtPosition( pPosition )?.Item;

			CursorKind lKind;
			if ( lIsDragging )
			{
				lKind = CursorKind.Dragging;
			}
			else if ( lHitItem != null )
			{
				lKind = CursorKind.Hand;
			}
			else
			{
				lKind = GetContentRect().Contains( pPosition ) ? CursorKind.Plot : CursorKind.Default;
			}

			SetCursorKind( lKind );
			SetHoveredTimeFrame( lHitItem );
		}

		private void SetCursorKind( CursorKind pKind )
		{
			if ( pKind == mActiveCursorKind )
			{
				return;
			}

			mActiveCursorKind = pKind;

			Cursor = pKind switch
			{
				CursorKind.Hand => Cursors.Hand,
				CursorKind.Plot => ResolveCustomCursor( () => CustomCursors.DragLeftRightCursor ),
				CursorKind.Dragging => ResolveCustomCursor( () => CustomCursors.DraggingCursor ),
				_ => Cursors.Arrow
			};
		}

		private void SetHoveredTimeFrame( TimelineTimeFrameItem? pItem )
		{
			if ( ReferenceEquals( mHoveredTimeFrameItem, pItem ) )
			{
				return;
			}

			mHoveredTimeFrameItem = pItem;
			ToolTip = pItem is null ? null : FormatFrameToolTip( pItem, GetLabelSeparator() );
			InvalidateVisual();
		}

		private FormattedText? GetText( string pText, double pFontSize, FontWeight pWeight, Brush pBrush, double pPixelsPerDip )
		{
			if ( string.IsNullOrEmpty( pText ) )
			{
				return null;
			}

			EnsureFormattedTextCacheContext( pPixelsPerDip );

			var lKey = new FormattedTextCacheKey( pText, pFontSize, pWeight.ToOpenTypeWeight(), pBrush );

			if ( mFormattedTextCache.TryGetValue( lKey, out var lCached ) )
			{
				return lCached;
			}

			if ( mFormattedTextCache.Count >= FormattedTextCacheMaxEntries )
			{
				mFormattedTextCache.Clear();
			}

			var lText = CreateFormattedText( pText, pFontSize, pWeight, pBrush, pPixelsPerDip );
			mFormattedTextCache[ lKey ] = lText;
			return lText;
		}

		private FormattedText CreateFormattedText( string pText, double pFontSize, FontWeight pWeight, Brush pBrush, double pPixelsPerDip )
		{
			return new FormattedText(
				pText,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				new Typeface( FontFamily, FontStyle, pWeight, FontStretch ),
				pFontSize,
				pBrush,
				pPixelsPerDip );
		}

		private void EnsureFormattedTextCacheContext( double pPixelsPerDip )
		{
			var lCultureName = CultureInfo.CurrentCulture?.Name ?? string.Empty;

			var lHasSameContext = Math.Abs( pPixelsPerDip - mTextCachePixelsPerDip ) < 0.000001
								  && Equals( FontFamily, mTextCacheFontFamily )
								  && FontStyle.Equals( mTextCacheFontStyle )
								  && FontStretch.Equals( mTextCacheFontStretch )
								  && string.Equals( lCultureName, mTextCacheCultureName, StringComparison.Ordinal );

			if ( lHasSameContext )
			{
				return;
			}

			mFormattedTextCache.Clear();

			mTextCachePixelsPerDip = pPixelsPerDip;
			mTextCacheFontFamily = FontFamily;
			mTextCacheFontStyle = FontStyle;
			mTextCacheFontStretch = FontStretch;
			mTextCacheCultureName = lCultureName;
		}

		private void SetSelectedDateFromUserInteraction( DateTime pDate, Rect pContentRect )
		{
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
			EnsureDateVisible( lClamped, pContentRect, true );
		}

		private void ApplyPendingPanIfNeeded()
		{
			if ( !mHasPendingPan )
			{
				return;
			}

			var lContentRect = GetContentRect();
			var lZoom = SafeZoom( CoerceZoomValueByContent( ZoomLevel, lContentRect ) );

			var lDeltaPixels = mPendingPanPointerX - mPointerDownPoint.X;
			var lDeltaDays = lDeltaPixels / lZoom;
			var lDeltaTicks = lDeltaDays * TimeSpan.TicksPerDay;

			SetViewportStartTicksCurrentValue( mViewportStartTicksAtPointerDown - lDeltaTicks );

			var lSampleTimestampUtc = mPendingPanTimestampUtc == default ? DateTime.UtcNow : mPendingPanTimestampUtc;
			AddPanSample( lSampleTimestampUtc, mPendingPanPointerX );

			mHasPendingPan = false;
		}

		private void UpdateRenderingSubscriptionIfNeeded()
		{
			var lShouldSubscribe = mIsViewAnimationActive
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
			SubscribeToThemeService();
			TryApplyPendingFit();
			UpdateRenderingSubscriptionIfNeeded();
			InvalidateVisual();
		}

		private void OnDataContextChanged( object pSender, DependencyPropertyChangedEventArgs pEventArgs )
		{
			mLabelSeparator = null;
			InvalidateVisual();
		}

		private void SubscribeToThemeService()
		{
			ThemeService? lThemeService = ThemeService.Instance;
			if ( lThemeService is null || ReferenceEquals( lThemeService, mSubscribedThemeService ) )
			{
				return;
			}

			UnsubscribeFromThemeService();

			mSubscribedThemeService = lThemeService;
			mSubscribedThemeService.PropertyChanged += OnThemeServicePropertyChanged;
		}

		private void UnsubscribeFromThemeService()
		{
			if ( mSubscribedThemeService is null )
			{
				return;
			}

			mSubscribedThemeService.PropertyChanged -= OnThemeServicePropertyChanged;
			mSubscribedThemeService = null;
		}

		private void OnThemeServicePropertyChanged( object? pSender, PropertyChangedEventArgs pEventArgs )
		{
			if ( !string.IsNullOrEmpty( pEventArgs.PropertyName )
				 && pEventArgs.PropertyName != nameof( ThemeService.ActiveTheme )
				 && pEventArgs.PropertyName != nameof( ThemeService.IsHighContrastActive ) )
			{
				return;
			}

			InvalidateVisual();
		}

		private void OnUnloaded( object pSender, RoutedEventArgs pEventArgs )
		{
			UnsubscribeFromThemeService();

			if ( mIsRenderingSubscribed )
			{
				CompositionTarget.Rendering -= OnCompositionTargetRendering;
				mIsRenderingSubscribed = false;
			}
		}

		private void OnCompositionTargetRendering( object? pSender, EventArgs pEventArgs )
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
				if ( !mIsViewAnimationActive )
				{
					return;
				}
			}

			var lDeltaSeconds = Math.Max( 0.0, ( lNowUtc - mLastRenderTick ).TotalSeconds );
			mLastRenderTick = lNowUtc;

			if ( mIsInertiaActive && !MotionPolicy.IsAnimationEnabled )
			{
				mIsInertiaActive = false;
			}

			if ( mIsViewAnimationActive )
			{
				UpdateViewAnimation( lNowUtc );
			}

			if ( mIsInertiaActive && lDeltaSeconds > 0.0 && !mIsPointerDown && !mIsViewAnimationActive )
			{
				UpdateInertia( lDeltaSeconds );
			}
		}

		private void UpdateViewAnimation( DateTime pNowUtc )
		{
			var lElapsedMilliseconds = ( pNowUtc - mViewAnimationStartUtc ).TotalMilliseconds;
			var lProgress = Math.Max( 0.0, Math.Min( 1.0, lElapsedMilliseconds / ViewAnimationDurationMilliseconds ) );
			var lEased = EaseOutCubic( lProgress );

			var lContentRect = GetContentRect();

			var lNewViewportTicks = Lerp( mViewAnimationStartViewportTicks, mViewAnimationTargetViewportTicks, lEased );
			var lNewZoom = Lerp( mViewAnimationStartZoom, mViewAnimationTargetZoom, lEased );

			SetZoomLevelCurrentValue( CoerceZoomValueByContent( lNewZoom, lContentRect ) );

			var lCandidateStart = new DateTime( Math.Max( 0L, ( long )lNewViewportTicks ) );
			var lClampedStart = ClampViewportStartDate( lCandidateStart, lContentRect );
			SetViewportStartTicksCurrentValue( lClampedStart.Ticks );

			if ( lProgress < 1.0 )
			{
				return;
			}

			mIsViewAnimationActive = false;

			SetViewportStartDateCurrentValue( ClampViewportStartDate( ViewportStartDate, lContentRect ) );
			UpdateIsShowingAll();
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
			if ( !MotionPolicy.IsAnimationEnabled )
			{
				mIsInertiaActive = false;
				return;
			}

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
			var lZoom = SafeZoom( CoerceZoomValueByContent( ZoomLevel, lContentRect ) );

			var lVelocityDaysPerSecond = lVelocityPixelsPerSecond / lZoom;
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

		private Brush FindBrush( string pResourceKey, Brush pFallback )
		{
			return TryFindResource( pResourceKey ) as Brush ?? pFallback;
		}

		private RenderContext CreateRenderContext( Rect pPlotRect )
		{
			var lGeometry = GetGeometry();
			var lForeground = Foreground ?? Brushes.White;
			var lIsHighContrast = ThemeService.Instance?.IsHighContrastActive == true;

			var lTextPrimary = FindBrush( "TextPrimaryBrush", lForeground );
			var lZoom = SafeZoom( CoerceZoomValueByContent( ZoomLevel, pPlotRect ) );
			var lViewportStart = ClampViewportStartDate( ViewportStartDate, pPlotRect );
			var lAccent = FindBrush( "AccentBrush", FindBrush( "CommonBrush", lForeground ) );

			var lInactiveOpacity = TryFindResource( "TimelineInactiveBarOpacity" ) is double lOpacity ? lOpacity : 1.0;

			return new RenderContext
			{
				Geometry = lGeometry,
				PlotRect = pPlotRect,
				BaselineY = TimelineLayoutHelper.ComputeBaselineY( lGeometry, mLaneCount ) + 0.5,
				LaneTopY = TimelineLayoutHelper.ComputeLaneTop( lGeometry ),
				Zoom = lZoom,
				ViewportStart = lViewportStart,
				ViewportEnd = lViewportStart.AddDays( pPlotRect.Width / lZoom ),
				PixelsPerDip = VisualTreeHelper.GetDpi( this ).PixelsPerDip,
				Culture = CultureInfo.CurrentCulture,
				IsHighContrast = lIsHighContrast,
				InactiveOpacity = lIsHighContrast ? 1.0 : Math.Max( 0.0, Math.Min( 1.0, lInactiveOpacity ) ),
				AccentBrush = lAccent,
				AccentTextBrush = FindBrush( "AccentTextBrush", lAccent ),
				BorderStrongBrush = FindBrush( "BorderStrongBrush", lForeground ),
				BorderSubtleBrush = FindBrush( "BorderSubtleBrush", lForeground ),
				DividerBrush = FindBrush( "OnSurfaceDividerOnDarkBrush", lForeground ),
				FocusRingBrush = FindBrush( "FocusRingBrush", lAccent ),
				LabelOnBarBrush = lIsHighContrast ? FindBrush( "TextOnSelectedBrush", lTextPrimary ) : lTextPrimary,
				PillFillBrush = FindBrush( "SurfacePillSelectedBrush", Brushes.Transparent ),
				PillBorderBrush = FindBrush( "SurfacePillSelectedBorderBrush", Brushes.Transparent ),
				SurfaceBrush = FindBrush( "CommonDarkerGrayBrush", Brushes.Transparent ),
				TextPrimaryBrush = lTextPrimary,
				TextSecondaryBrush = FindBrush( "TextSecondaryBrush", lForeground )
			};
		}

		private void DrawTimeline( DrawingContext pDrawingContext, Rect pPlotRect )
		{
			mTimeFrameHitInfos.Clear();

			var lContext = CreateRenderContext( pPlotRect );
			var lFrameRects = ComputeFrameRects( lContext );

			var lSchedule = ChooseSchedule( lContext );
			var lTicks = TimelineLayoutHelper.BuildAxisTicks( lContext.ViewportStart, lContext.ViewportEnd, DateTime.Today, lContext.Zoom, lSchedule, lContext.Culture );

			DrawGridLines( pDrawingContext, lContext, lTicks );
			DrawBars( pDrawingContext, lContext, lFrameRects );
			DrawBarLabels( pDrawingContext, lContext, lFrameRects );
			RegisterHitRects( lContext, lFrameRects );
			DrawTodayMarker( pDrawingContext, lContext );
			DrawAxis( pDrawingContext, lContext, lTicks );
			DrawSelectedRoleSpan( pDrawingContext, lContext );

			var lSelectedDate = ClampDateToRange( SelectedDate );
			var lScrubberX = lContext.DateToX( lSelectedDate );
			var lHasScrubber = lScrubberX >= pPlotRect.Left - 0.5 && lScrubberX <= pPlotRect.Right + 0.5;

			Rect? lPillRect = null;
			FormattedText? lPillText = null;

			if ( lHasScrubber )
			{
				DrawScrubberLine( pDrawingContext, lContext, lScrubberX );

				lPillText = GetText(
					TimelineLayoutHelper.FormatPillLabel( lSelectedDate, lContext.Culture ),
					lContext.Geometry.AxisFontSize,
					FontWeights.SemiBold,
					lContext.AccentTextBrush,
					lContext.PixelsPerDip );

				if ( lPillText != null )
				{
					var lPillWidth = lPillText.Width + ( TimelineLayoutHelper.PillPaddingX * 2.0 );
					var lPillHeight = TimelineLayoutHelper.ComputePillHeight( lContext.Geometry );
					var lPillX = Math.Max( pPlotRect.Left, Math.Min( pPlotRect.Right - lPillWidth, lScrubberX - ( lPillWidth * 0.5 ) ) );

					lPillRect = new Rect( lPillX, lContext.BaselineY + PillTopGap, lPillWidth, lPillHeight );
				}
			}

			DrawTickLabels( pDrawingContext, lContext, lTicks, lPillRect );

			if ( lHasScrubber )
			{
				DrawPillAndHandle( pDrawingContext, lContext, lScrubberX, lPillRect, lPillText );
			}
		}

		private TimelineTickSchedule ChooseSchedule( RenderContext pContext )
		{
			var lFontSize = pContext.Geometry.AxisFontSize;
			var lMaximumWidth = 0.0;

			for ( var lMonth = 1; lMonth <= 12; lMonth++ )
			{
				var lName = new DateTime( 2020, lMonth, 1 ).ToString( "MMM", pContext.Culture );
				lMaximumWidth = Math.Max( lMaximumWidth, GetText( lName, lFontSize, FontWeights.Regular, pContext.TextSecondaryBrush, pContext.PixelsPerDip )?.Width ?? 0.0 );
			}

			var lYearLabel = TimelineLayoutHelper.FormatAxisLabel( DateTime.Today, TimelineTickGranularity.Years, pContext.Culture );
			lMaximumWidth = Math.Max( lMaximumWidth, GetText( lYearLabel, lFontSize, FontWeights.SemiBold, pContext.TextPrimaryBrush, pContext.PixelsPerDip )?.Width ?? 0.0 );

			return TimelineLayoutHelper.ChooseTickSchedule( pContext.Zoom, lMaximumWidth + TimelineLayoutHelper.MinimumLabelGapPixels );
		}

		private Rect?[] ComputeFrameRects( RenderContext pContext )
		{
			var lGeometry = pContext.Geometry;
			var lRects = new Rect?[ mFrameLayouts.Count ];

			for ( var lIndex = 0; lIndex < mFrameLayouts.Count; lIndex++ )
			{
				var lFrame = mFrameLayouts[ lIndex ];
				if ( lFrame.LaneIndex < 0 )
				{
					continue;
				}

				var lX1 = pContext.DateToX( lFrame.StartDate );
				var lX2 = pContext.DateToX( lFrame.EndDate );

				if ( lFrame.PreviousIndex >= 0 )
				{
					lX1 += 1.0;
				}

				if ( lFrame.NextIndex >= 0 )
				{
					lX2 -= 1.0;
				}

				var lY = pContext.LaneTopY + ( lFrame.LaneIndex * lGeometry.LanePitch ) + ( ( lGeometry.LanePitch - lGeometry.BarHeight ) * 0.5 );
				lRects[ lIndex ] = new Rect( lX1, lY, Math.Max( 1.0, lX2 - lX1 ), lGeometry.BarHeight );
			}

			return lRects;
		}

		private bool IsRectVisible( Rect pRect, RenderContext pContext )
		{
			return pRect.Right >= pContext.PlotRect.Left - 8.0 && pRect.Left <= pContext.PlotRect.Right + 8.0;
		}

		private void DrawGridLines( DrawingContext pDrawingContext, RenderContext pContext, IReadOnlyList<TimelineAxisTick> pTicks )
		{
			var lPen = new Pen( pContext.DividerBrush, 1.0 );

			foreach ( var lTick in pTicks.Where( pTick => pTick.IsGrid ) )
			{
				var lX = SnapToPixelCenter( pContext.DateToX( lTick.Date ) );
				pDrawingContext.DrawLine( lPen, new Point( lX, pContext.LaneTopY ), new Point( lX, pContext.BaselineY ) );
			}
		}

		private void DrawBars( DrawingContext pDrawingContext, RenderContext pContext, Rect?[] pFrameRects )
		{
			var lSelected = SelectedTimeFrame;
			var lSelectedIndex = -1;

			pDrawingContext.PushClip( new RectangleGeometry( pContext.PlotRect ) );

			try
			{
				for ( var lIndex = 0; lIndex < mFrameLayouts.Count; lIndex++ )
				{
					if ( pFrameRects[ lIndex ] is not Rect lRect || !IsRectVisible( lRect, pContext ) )
					{
						continue;
					}

					var lItem = mFrameLayouts[ lIndex ].Item;
					if ( lSelected != null && ReferenceEquals( lSelected, lItem ) )
					{
						lSelectedIndex = lIndex;
						continue;
					}

					var lIsHovered = mHoveredTimeFrameItem != null && ReferenceEquals( mHoveredTimeFrameItem, lItem );
					DrawBar( pDrawingContext, pContext, lItem, lRect, pContext.InactiveOpacity, lIsHovered ? 2.0 : 1.0 );
				}

				if ( lSelectedIndex >= 0 && pFrameRects[ lSelectedIndex ] is Rect lSelectedRect )
				{
					var lRingRect = new Rect(
						lSelectedRect.X - SelectedRingInflate,
						lSelectedRect.Y - SelectedRingInflate,
						lSelectedRect.Width + ( SelectedRingInflate * 2.0 ),
						lSelectedRect.Height + ( SelectedRingInflate * 2.0 ) );

					var lRingRadius = pContext.Geometry.BarCornerRadius + SelectedRingInflate;
					pDrawingContext.DrawRoundedRectangle( null, new Pen( pContext.FocusRingBrush, 2.0 ), lRingRect, lRingRadius, lRingRadius );

					DrawBar( pDrawingContext, pContext, mFrameLayouts[ lSelectedIndex ].Item, lSelectedRect, 1.0, 1.0 );
				}
			}
			finally
			{
				pDrawingContext.Pop();
			}
		}

		private void DrawBar( DrawingContext pDrawingContext, RenderContext pContext, TimelineTimeFrameItem pItem, Rect pRect, double pFillOpacity, double pStrokeThickness )
		{
			var lFillBrush = ResolveTimeFrameBrush( pItem ) ?? pContext.AccentBrush;
			var lEdgeBrush = ResolveTimeFrameEdgeBrush( pItem ) ?? lFillBrush;
			var lRadius = pContext.Geometry.BarCornerRadius;

			var lInset = pStrokeThickness * 0.5;
			var lStrokeRect = new Rect( pRect.X + lInset, pRect.Y + lInset, Math.Max( 0.0, pRect.Width - pStrokeThickness ), Math.Max( 0.0, pRect.Height - pStrokeThickness ) );

			if ( pFillOpacity < 1.0 )
			{
				pDrawingContext.PushOpacity( pFillOpacity );
			}

			pDrawingContext.DrawRoundedRectangle( lFillBrush, null, pRect, lRadius, lRadius );

			if ( pFillOpacity < 1.0 )
			{
				pDrawingContext.Pop();
			}

			pDrawingContext.DrawRoundedRectangle( null, new Pen( lEdgeBrush, pStrokeThickness ), lStrokeRect, Math.Max( 0.0, lRadius - lInset ), Math.Max( 0.0, lRadius - lInset ) );
		}

		private void DrawBarLabels( DrawingContext pDrawingContext, RenderContext pContext, Rect?[] pFrameRects )
		{
			var lGeometry = pContext.Geometry;
			var lSeparator = GetLabelSeparator();
			var lFontSize = lGeometry.BarLabelFontSize;

			double Measure( string pText, bool pIsBold )
			{
				return GetText( pText, lFontSize, pIsBold ? FontWeights.SemiBold : FontWeights.Regular, pContext.LabelOnBarBrush, pContext.PixelsPerDip )?.Width ?? 0.0;
			}

			for ( var lIndex = 0; lIndex < mFrameLayouts.Count; lIndex++ )
			{
				if ( pFrameRects[ lIndex ] is not Rect lRect || !IsRectVisible( lRect, pContext ) )
				{
					continue;
				}

				var lFrame = mFrameLayouts[ lIndex ];
				var lVisibleLeft = Math.Max( lRect.Left, pContext.PlotRect.Left );
				var lVisibleRight = Math.Min( lRect.Right, pContext.PlotRect.Right );
				var lAvailableWidth = lVisibleRight - lVisibleLeft - ( 2.0 * lGeometry.LabelPadding );

				var lPreviousItem = lFrame.PreviousIndex >= 0 ? mFrameLayouts[ lFrame.PreviousIndex ].Item : null;
				var lPreferRoleOnly = lPreviousItem != null && string.Equals( lPreviousItem.Title, lFrame.Item.Title, StringComparison.Ordinal );

				var lSegments = TimelineLayoutHelper.ChooseBarLabel( lFrame.Item.Title, lFrame.Item.SubtitleText, lSeparator, lPreferRoleOnly, lAvailableWidth, Measure );
				if ( lSegments.Count == 0 )
				{
					continue;
				}

				var lText = CreateFormattedText( string.Concat( lSegments.Select( pSegment => pSegment.Text ) ), lFontSize, FontWeights.Regular, pContext.LabelOnBarBrush, pContext.PixelsPerDip );

				var lOffset = 0;
				foreach ( var lSegment in lSegments )
				{
					if ( lSegment.IsBold )
					{
						lText.SetFontWeight( FontWeights.SemiBold, lOffset, lSegment.Text.Length );
					}

					lOffset += lSegment.Text.Length;
				}

				var lClipRect = new Rect( lVisibleLeft, lRect.Top, Math.Max( 0.0, lVisibleRight - lVisibleLeft ), lRect.Height );
				var lPoint = new Point( lVisibleLeft + lGeometry.LabelPadding, lRect.Top + ( ( lRect.Height - lText.Height ) * 0.5 ) );

				pDrawingContext.PushClip( new RectangleGeometry( lClipRect ) );
				try
				{
					pDrawingContext.DrawText( lText, lPoint );
				}
				finally
				{
					pDrawingContext.Pop();
				}
			}
		}

		private void RegisterHitRects( RenderContext pContext, Rect?[] pFrameRects )
		{
			var lGeometry = pContext.Geometry;

			for ( var lIndex = 0; lIndex < mFrameLayouts.Count; lIndex++ )
			{
				if ( pFrameRects[ lIndex ] is not Rect lRect )
				{
					continue;
				}

				var lVisibleLeft = Math.Max( lRect.Left, pContext.PlotRect.Left );
				var lVisibleRight = Math.Min( lRect.Right, pContext.PlotRect.Right );

				if ( lVisibleRight < pContext.PlotRect.Left || lVisibleLeft > pContext.PlotRect.Right )
				{
					continue;
				}

				var lWidth = lVisibleRight - lVisibleLeft;
				var lLeft = lVisibleLeft;

				if ( lWidth < HitRectMinimumWidth )
				{
					lLeft -= ( HitRectMinimumWidth - lWidth ) * 0.5;
					lWidth = HitRectMinimumWidth;
				}

				var lLaneTop = pContext.LaneTopY + ( mFrameLayouts[ lIndex ].LaneIndex * lGeometry.LanePitch );
				mTimeFrameHitInfos.Add( new TimeFrameHitInfo( mFrameLayouts[ lIndex ].Item, new Rect( lLeft, lLaneTop, lWidth, lGeometry.LanePitch ) ) );
			}
		}

		private void DrawTodayMarker( DrawingContext pDrawingContext, RenderContext pContext )
		{
			var lToday = DateTime.Today;
			var lX = pContext.DateToX( lToday );

			if ( lX < pContext.PlotRect.Left || lX > pContext.PlotRect.Right )
			{
				return;
			}

			var lGeometry = pContext.Geometry;
			var lSnappedX = SnapToPixelCenter( lX );

			pDrawingContext.DrawLine( new Pen( pContext.BorderStrongBrush, 1.0 ), new Point( lSnappedX, lGeometry.PaddingTop ), new Point( lSnappedX, pContext.BaselineY ) );

			var lLabel = string.IsNullOrWhiteSpace( TodayMarkerText ) ? DefaultTodayMarkerText : TodayMarkerText;
			var lText = GetText( lLabel, TodayLabelFontSize, FontWeights.Regular, pContext.TextSecondaryBrush, pContext.PixelsPerDip );

			if ( lText is null )
			{
				return;
			}

			var lLeft = Math.Max( pContext.PlotRect.Left, lX - TodayLabelGapPixels - lText.Width );
			var lTop = lGeometry.PaddingTop + lGeometry.MarkerRowHeight - TodayLabelBaselineInsetPixels - lText.Baseline;

			pDrawingContext.DrawText( lText, new Point( lLeft, lTop ) );
		}

		private void DrawAxis( DrawingContext pDrawingContext, RenderContext pContext, IReadOnlyList<TimelineAxisTick> pTicks )
		{
			var lPlot = pContext.PlotRect;
			var lBaselineY = pContext.BaselineY;

			pDrawingContext.DrawLine( new Pen( pContext.BorderStrongBrush, 1.0 ), new Point( lPlot.Left, lBaselineY ), new Point( lPlot.Right, lBaselineY ) );

			var lMajorPen = new Pen( pContext.BorderStrongBrush, 1.0 );
			var lMinorPen = new Pen( pContext.BorderSubtleBrush, 1.0 );

			foreach ( var lTick in pTicks )
			{
				var lX = pContext.DateToX( lTick.Date );
				if ( lX < lPlot.Left || lX > lPlot.Right )
				{
					continue;
				}

				var lSnappedX = SnapToPixelCenter( lX );
				var lLength = lTick.IsMajor ? TickMajorHeight : TickMinorHeight;

				pDrawingContext.DrawLine( lTick.IsMajor ? lMajorPen : lMinorPen, new Point( lSnappedX, lBaselineY + 0.5 ), new Point( lSnappedX, lBaselineY + 0.5 + lLength ) );
			}
		}

		private void DrawSelectedRoleSpan( DrawingContext pDrawingContext, RenderContext pContext )
		{
			var lSelected = SelectedTimeFrame;
			var lFrame = lSelected is null ? null : mFrameLayouts.FirstOrDefault( pFrame => ReferenceEquals( pFrame.Item, lSelected ) );

			if ( lFrame is null || lFrame.LaneIndex < 0 )
			{
				return;
			}

			var lStartX = Math.Max( pContext.DateToX( lFrame.StartDate ), pContext.PlotRect.Left + 1.5 );
			var lEndX = Math.Min( pContext.DateToX( lFrame.EndDate ), pContext.PlotRect.Right - 1.5 );

			if ( lEndX <= lStartX )
			{
				return;
			}

			var lPen = new Pen( pContext.AccentBrush, 3.0 )
			{
				StartLineCap = PenLineCap.Round,
				EndLineCap = PenLineCap.Round
			};

			pDrawingContext.DrawLine( lPen, new Point( lStartX, pContext.BaselineY ), new Point( lEndX, pContext.BaselineY ) );
		}

		private void DrawScrubberLine( DrawingContext pDrawingContext, RenderContext pContext, double pScrubberX )
		{
			var lGeometry = pContext.Geometry;
			var lFirstBarTop = pContext.LaneTopY + ( ( lGeometry.LanePitch - lGeometry.BarHeight ) * 0.5 );

			pDrawingContext.DrawLine(
				new Pen( pContext.AccentBrush, 1.5 ),
				new Point( pScrubberX, lFirstBarTop - ScrubberTopExtension ),
				new Point( pScrubberX, pContext.BaselineY ) );
		}

		private void DrawTickLabels( DrawingContext pDrawingContext, RenderContext pContext, IReadOnlyList<TimelineAxisTick> pTicks, Rect? pPillRect )
		{
			var lGeometry = pContext.Geometry;
			var lLastRight = double.NegativeInfinity;

			foreach ( var lTick in pTicks.Where( pTick => pTick.Label != null ) )
			{
				var lBrush = lTick.IsEmphasized ? pContext.TextPrimaryBrush : pContext.TextSecondaryBrush;
				var lText = GetText( lTick.Label!, lGeometry.AxisFontSize, lTick.IsEmphasized ? FontWeights.SemiBold : FontWeights.Regular, lBrush, pContext.PixelsPerDip );

				if ( lText is null )
				{
					continue;
				}

				var lTop = pContext.BaselineY + TickLabelTopGap + lGeometry.AxisFontSize - lText.Baseline;
				var lTickX = pContext.DateToX( lTick.Date ) - pContext.PlotRect.Left;

				if ( !TryGetTickLabelRect( lTickX, lText.Width, lTop, lText.Height, pContext.PlotRect, out var lRect ) )
				{
					continue;
				}

				if ( lRect.Left < lLastRight + TickLabelCollisionGapPixels || ShouldSkipTickLabel( lRect, pPillRect, SelectedLabelCollisionGapPixels ) )
				{
					continue;
				}

				lLastRight = lRect.Right;
				pDrawingContext.DrawText( lText, lRect.TopLeft );
			}
		}

		private void DrawPillAndHandle( DrawingContext pDrawingContext, RenderContext pContext, double pScrubberX, Rect? pPillRect, FormattedText? pPillText )
		{
			if ( pPillRect is Rect lPillRect && pPillText != null )
			{
				var lRadius = lPillRect.Height * 0.5;

				pDrawingContext.DrawRoundedRectangle( pContext.PillFillBrush, new Pen( pContext.PillBorderBrush, 1.0 ), lPillRect, lRadius, lRadius );
				pDrawingContext.DrawText( pPillText, new Point( lPillRect.Left + TimelineLayoutHelper.PillPaddingX, lPillRect.Top + ( ( lPillRect.Height - pPillText.Height ) * 0.5 ) ) );
			}

			pDrawingContext.DrawEllipse(
				pContext.AccentBrush,
				new Pen( pContext.SurfaceBrush, 2.0 ),
				new Point( pScrubberX, pContext.BaselineY ),
				ScrubberHandleRadius,
				ScrubberHandleRadius );
		}

		private void DrawFocusOutlineIfNeeded( DrawingContext pDrawingContext )
		{
			if ( !IsKeyboardFocusWithin )
			{
				return;
			}

			if ( TryFindResource( "FocusRingBrush" ) is not Brush lFocusBrush )
			{
				return;
			}

			var lRect = new Rect( 1.0, 1.0, Math.Max( 0.0, ActualWidth - 2.0 ), Math.Max( 0.0, ActualHeight - 2.0 ) );
			pDrawingContext.DrawRoundedRectangle( null, new Pen( lFocusBrush, 2.0 ), lRect, 8.0, 8.0 );
		}

		private Rect GetContentRect()
		{
			var lGeometry = GetGeometry();

			return new Rect(
				lGeometry.PaddingLeft,
				lGeometry.PaddingTop,
				Math.Max( 0.0, ActualWidth - ( lGeometry.PaddingLeft + lGeometry.PaddingRight ) ),
				Math.Max( 0.0, ActualHeight - ( lGeometry.PaddingTop + lGeometry.PaddingBottom ) ) );
		}

		private double CoerceZoomValueByContent( double pZoom, Rect pContentRect )
		{
			if ( pContentRect.Width <= 1.0 )
			{
				return CoerceZoomValue( pZoom );
			}

			var lFitRange = GetFitRange( pContentRect );

			if ( double.IsNaN( pZoom ) || double.IsInfinity( pZoom ) || pZoom < lFitRange.Zoom )
			{
				return lFitRange.Zoom;
			}

			return pZoom > lFitRange.MaxZoom ? lFitRange.MaxZoom : pZoom;
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
			return ClampViewportStartDate( pViewportStart, pContentRect, CoerceZoomValueByContent( ZoomLevel, pContentRect ) );
		}

		private DateTime ClampViewportStartDate( DateTime pViewportStart, Rect pContentRect, double pZoomLevel )
		{
			var lFitRange = GetFitRange( pContentRect );
			var lVisibleDays = Math.Max( 1.0, pContentRect.Width ) / SafeZoom( pZoomLevel );

			var lLatestStart = lFitRange.End.AddDays( -lVisibleDays );

			if ( lLatestStart < lFitRange.Start || pViewportStart < lFitRange.Start )
			{
				return lFitRange.Start;
			}

			return pViewportStart > lLatestStart ? lLatestStart : pViewportStart;
		}

		private void NavigateTimeFrameByDirection( int pDirection, Rect pContentRect )
		{
			var lTimeFrames = TimeFrames;
			if ( lTimeFrames == null || lTimeFrames.Count == 0 )
			{
				return;
			}

			var lOrdered = lTimeFrames
				.OrderBy( pItem => pItem.StartDate )
				.ThenBy( pItem => pItem.Title ?? string.Empty, StringComparer.OrdinalIgnoreCase )
				.ToList();

			var lCurrentIndex = -1;
			for ( var lIndex = 0; lIndex < lOrdered.Count; lIndex++ )
			{
				if ( ReferenceEquals( lOrdered[ lIndex ], SelectedTimeFrame ) )
				{
					lCurrentIndex = lIndex;
					break;
				}
			}

			int lTargetIndex;
			if ( lCurrentIndex < 0 )
			{
				lTargetIndex = pDirection > 0 ? 0 : lOrdered.Count - 1;
			}
			else
			{
				lTargetIndex = lCurrentIndex + pDirection;
			}

			if ( lTargetIndex < 0 || lTargetIndex >= lOrdered.Count )
			{
				return;
			}

			var lTarget = lOrdered[ lTargetIndex ];
			SetSelectedTimeFrameCurrentValue( lTarget );
			SetSelectedDateFromUserInteraction( lTarget.StartDate, pContentRect );
		}

		private void EnsureDateVisible( DateTime pDate, Rect pContentRect, bool pAnimated )
		{
			if ( pContentRect.Width <= 1.0 )
			{
				return;
			}

			var lDate = ClampDateToRange( pDate );
			var lZoom = SafeZoom( CoerceZoomValueByContent( ZoomLevel, pContentRect ) );

			var lVisibleDays = pContentRect.Width / lZoom;

			var lStart = ClampViewportStartDate( ViewportStartDate, pContentRect );
			var lEnd = lStart.AddDays( lVisibleDays );

			if ( lDate >= lStart && lDate <= lEnd )
			{
				return;
			}

			var lCenteredStart = lDate.AddDays( -( lVisibleDays * 0.5 ) );
			var lTargetStart = ClampViewportStartDate( lCenteredStart, pContentRect, lZoom );

			StartViewTransition( lZoom, lTargetStart.Ticks, pAnimated );
		}

		private DateTime PixelToDate( double pPixelX, Rect pContentRect )
		{
			var lOffsetPixels = pPixelX - pContentRect.Left;
			var lZoom = SafeZoom( CoerceZoomValueByContent( ZoomLevel, pContentRect ) );

			var lDays = lOffsetPixels / lZoom;
			return ClampDateToRange( ClampViewportStartDate( ViewportStartDate, pContentRect ).AddDays( lDays ) );
		}

		private Brush? ResolveTimeFrameBrush( TimelineTimeFrameItem? pItem )
		{
			if ( pItem == null )
			{
				return null;
			}

			var lKey = pItem.AccentColorKey;
			if ( string.IsNullOrWhiteSpace( lKey ) )
			{
				return null;
			}

			return TryFindResource( lKey ) as Brush;
		}

		private Brush? ResolveTimeFrameEdgeBrush( TimelineTimeFrameItem? pItem )
		{
			var lKey = TimelineLayoutHelper.GetEdgeBrushKey( pItem?.AccentColorKey );
			return lKey is null ? null : TryFindResource( lKey ) as Brush;
		}

		private TimeFrameHitInfo? GetTimeFrameHitInfoAtPosition( Point pPosition )
		{
			if ( mTimeFrameHitInfos.Count == 0 )
			{
				return null;
			}

			TimeFrameHitInfo? lBest = null;
			var lBestWidth = double.MaxValue;

			for ( var lIndex = 0; lIndex < mTimeFrameHitInfos.Count; lIndex++ )
			{
				var lHitInfo = mTimeFrameHitInfos[ lIndex ];
				if ( !lHitInfo.HitRect.Contains( pPosition ) )
				{
					continue;
				}

				if ( lBest == null || lHitInfo.HitRect.Width < lBestWidth )
				{
					lBest = lHitInfo;
					lBestWidth = lHitInfo.HitRect.Width;
				}
			}

			return lBest;
		}

		private void OnTimeFramesCollectionChanged( object? pSender, NotifyCollectionChangedEventArgs pEventArgs )
		{
			mIsLayoutDirty = true;

			if ( mHoveredTimeFrameItem != null && TimeFrames?.Contains( mHoveredTimeFrameItem ) != true )
			{
				SetHoveredTimeFrame( null );
			}

			RequestFit();
			InvalidateMeasure();
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
			var lIsUpdateAllowed = lMode is BindingMode.TwoWay or BindingMode.OneWayToSource or BindingMode.Default;

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

		private void SetSelectedTimeFrameCurrentValue( TimelineTimeFrameItem? pSelectedTimeFrame )
		{
			SetCurrentValue( sSelectedTimeFrameProperty, pSelectedTimeFrame );
		}
	}
}
