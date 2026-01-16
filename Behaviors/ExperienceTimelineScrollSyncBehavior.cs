// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ResumeApp.Behaviors;

public static class ExperienceTimelineScrollSyncBehavior
{
	private sealed class ExperienceTimelineScrollSyncState
	{
		public DispatcherTimer? DebounceTimer { get; set; }
		public bool IsUpdatingFromScroll { get; set; }
		public bool IsApplyingSelection { get; set; }
		public bool HasPendingSync { get; set; }
		public WeakReference<ScrollViewer>? ScrollViewerReference { get; set; }
	}

	private sealed class StartDatePropertyCacheEntry( PropertyInfo? pPropertyInfo )
	{
		public PropertyInfo? PropertyInfo { get; } = pPropertyInfo;
	}

	public static readonly DependencyProperty sIsEnabledProperty =
		DependencyProperty.RegisterAttached(
			"IsEnabled",
			typeof( bool ),
			typeof( ExperienceTimelineScrollSyncBehavior ),
			new PropertyMetadata( false, OnIsEnabledChanged ) );

	public static readonly DependencyProperty sItemsControlProperty =
		DependencyProperty.RegisterAttached(
			"ItemsControl",
			typeof( ItemsControl ),
			typeof( ExperienceTimelineScrollSyncBehavior ),
			new PropertyMetadata( null, OnItemsControlChanged ) );

	public static readonly DependencyProperty sSelectedDateProperty =
		DependencyProperty.RegisterAttached(
			"SelectedDate",
			typeof( DateTime ),
			typeof( ExperienceTimelineScrollSyncBehavior ),
			new PropertyMetadata( default( DateTime ), OnSelectedDateChanged ) );

	public static readonly DependencyProperty sSelectedItemProperty =
		DependencyProperty.RegisterAttached(
			"SelectedItem",
			typeof( object ),
			typeof( ExperienceTimelineScrollSyncBehavior ),
			new PropertyMetadata( null, OnSelectedItemChanged ) );

	private static readonly ConditionalWeakTable<ScrollViewer, ExperienceTimelineScrollSyncState> sStateByScrollViewer = new();
	private static readonly ConditionalWeakTable<Type, StartDatePropertyCacheEntry> sStartDatePropertyByType = new();

	public static void SetIsEnabled( DependencyObject pElement, bool pValue ) => pElement.SetValue( sIsEnabledProperty, pValue );
	public static bool GetIsEnabled( DependencyObject pElement ) => ( bool )pElement.GetValue( sIsEnabledProperty );

	public static void SetItemsControl( DependencyObject pElement, ItemsControl? pValue ) => pElement.SetValue( sItemsControlProperty, pValue );
	public static ItemsControl? GetItemsControl( DependencyObject pElement ) => pElement.GetValue( sItemsControlProperty ) is ItemsControl lItemsControl ? lItemsControl : null;

	public static void SetSelectedDate( DependencyObject pElement, DateTime pValue ) => pElement.SetValue( sSelectedDateProperty, pValue );
	public static DateTime GetSelectedDate( DependencyObject pElement ) => ( DateTime )pElement.GetValue( sSelectedDateProperty );

	public static void SetSelectedItem( DependencyObject pElement, object? pValue ) => pElement.SetValue( sSelectedItemProperty, pValue );
	public static object? GetSelectedItem( DependencyObject pElement ) => pElement.GetValue( sSelectedItemProperty );

	private static void OnIsEnabledChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
	{
		if ( pDependencyObject is not ScrollViewer lScrollViewer )
		{
			return;
		}

		if ( pArgs.NewValue is true )
		{
			ExperienceTimelineScrollSyncState lState = GetOrCreateState( lScrollViewer );
			Attach( lScrollViewer, lState );
			return;
		}

		if ( !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState? lExistingState ) || lExistingState is null )
		{
			return;
		}

		Detach( lScrollViewer, lExistingState );
	}

	private static void OnItemsControlChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
	{
		if ( !TryGetEnabledState( pDependencyObject, out ScrollViewer lScrollViewer, out ExperienceTimelineScrollSyncState lState ) )
		{
			return;
		}

		lState.HasPendingSync = true;
		QueueInitialSync( lScrollViewer, lState );
	}

	private static void OnSelectedDateChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
	{
		if ( !TryGetEnabledState( pDependencyObject, out ScrollViewer lScrollViewer, out ExperienceTimelineScrollSyncState lState ) || lState.IsUpdatingFromScroll )
		{
			return;
		}

		if ( pArgs.NewValue is not DateTime lNewDate || !TryGetItemsControl( lScrollViewer, out ItemsControl lItemsControl ) )
		{
			return;
		}

		object? lTargetItem = FindTargetItemForDate( lItemsControl, lNewDate );
		if ( lTargetItem == null )
		{
			return;
		}

		ApplySelection( lState, () => SetSelectedItem( lScrollViewer, lTargetItem ) );
		ScrollToItem( lScrollViewer, lItemsControl, lTargetItem );
	}

	private static void OnSelectedItemChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
	{
		if ( !TryGetEnabledState( pDependencyObject, out ScrollViewer lScrollViewer, out ExperienceTimelineScrollSyncState lState )
			|| lState.IsUpdatingFromScroll
			|| lState.IsApplyingSelection )
		{
			return;
		}

		if ( pArgs.NewValue is not { } lNewItem || !TryGetStartDate( lNewItem, out DateTime lStartDate ) )
		{
			return;
		}

		ApplySelection( lState, () => SetSelectedDate( lScrollViewer, lStartDate ) );

		if ( !TryGetItemsControl( lScrollViewer, out ItemsControl lItemsControl ) )
		{
			return;
		}

		ScrollToItem( lScrollViewer, lItemsControl, lNewItem );
	}

	private static bool TryGetEnabledState( DependencyObject pDependencyObject, out ScrollViewer pScrollViewer, out ExperienceTimelineScrollSyncState pState )
	{
		pScrollViewer = null!;
		pState = null!;

		if ( pDependencyObject is not ScrollViewer lScrollViewer
			|| !GetIsEnabled( lScrollViewer )
			|| !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState? lState )
			|| lState is null )
		{
			return false;
		}

		pScrollViewer = lScrollViewer;
		pState = lState;
		return true;
	}

	private static bool TryGetItemsControl( ScrollViewer pScrollViewer, out ItemsControl pItemsControl )
	{
		pItemsControl = null!;

		if ( GetItemsControl( pScrollViewer ) is not ItemsControl lItemsControl )
		{
			return false;
		}

		pItemsControl = lItemsControl;
		return true;
	}

	private static ExperienceTimelineScrollSyncState GetOrCreateState( ScrollViewer pScrollViewer )
	{
		return sStateByScrollViewer.GetValue( pScrollViewer, _ => new ExperienceTimelineScrollSyncState() );
	}

	private static void Attach( ScrollViewer pScrollViewer, ExperienceTimelineScrollSyncState pState )
	{
		pScrollViewer.Loaded -= OnScrollViewerLoaded;
		pScrollViewer.Loaded += OnScrollViewerLoaded;

		pScrollViewer.ScrollChanged -= OnScrollChanged;
		pScrollViewer.ScrollChanged += OnScrollChanged;

		pState.ScrollViewerReference ??= new WeakReference<ScrollViewer>( pScrollViewer );
		pState.DebounceTimer ??= CreateDebounceTimer( pState );

		pState.HasPendingSync = true;
		QueueInitialSync( pScrollViewer, pState );
	}

	private static void Detach( ScrollViewer pScrollViewer, ExperienceTimelineScrollSyncState pState )
	{
		pScrollViewer.Loaded -= OnScrollViewerLoaded;
		pScrollViewer.ScrollChanged -= OnScrollChanged;

		pState.DebounceTimer?.Stop();
	}

	private static DispatcherTimer CreateDebounceTimer( ExperienceTimelineScrollSyncState pState )
	{
		var lTimer = new DispatcherTimer( DispatcherPriority.Background )
		{
			Interval = TimeSpan.FromMilliseconds( 90 )
		};

		lTimer.Tick += OnDebounceTimerTick;
		return lTimer;

		void OnDebounceTimerTick( object? pSender, EventArgs pEventArgs )
		{
			if ( pSender is not DispatcherTimer lDispatcherTimer )
			{
				return;
			}

			lDispatcherTimer.Stop();

			if ( pState.ScrollViewerReference == null
				|| !pState.ScrollViewerReference.TryGetTarget( out ScrollViewer? lScrollViewer )
				|| lScrollViewer is null )
			{
				return;
			}

			UpdateSelectionFromViewport( lScrollViewer, pState );
		}
	}

	private static void QueueInitialSync( ScrollViewer pScrollViewer, ExperienceTimelineScrollSyncState pState )
	{
		pScrollViewer.Dispatcher.BeginInvoke( DispatcherPriority.Loaded, new Action( () =>
		{
			if ( !GetIsEnabled( pScrollViewer ) || !pState.HasPendingSync )
			{
				return;
			}

			pState.HasPendingSync = false;
			UpdateSelectionFromViewport( pScrollViewer, pState );
		} ) );
	}

	private static void OnScrollViewerLoaded( object pSender, RoutedEventArgs pArgs )
	{
		if ( pSender is not ScrollViewer lScrollViewer
			|| !GetIsEnabled( lScrollViewer )
			|| !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState? lState )
			|| lState is null )
		{
			return;
		}

		lState.HasPendingSync = true;
		QueueInitialSync( lScrollViewer, lState );
	}

	private static void OnScrollChanged( object pSender, ScrollChangedEventArgs pArgs )
	{
		if ( pSender is not ScrollViewer lScrollViewer
			|| !GetIsEnabled( lScrollViewer )
			|| !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState? lState )
			|| lState is null
			|| lState.IsApplyingSelection )
		{
			return;
		}

		lState.DebounceTimer?.Stop();
		lState.DebounceTimer?.Start();
	}

	private static void UpdateSelectionFromViewport( ScrollViewer pScrollViewer, ExperienceTimelineScrollSyncState pState )
	{
		if ( !TryGetItemsControl( pScrollViewer, out ItemsControl lItemsControl ) )
		{
			return;
		}

		object? lActiveItem = FindActiveItemNearTop( pScrollViewer, lItemsControl );
		if ( lActiveItem == null || !TryGetStartDate( lActiveItem, out DateTime lStartDate ) )
		{
			return;
		}

		pState.IsUpdatingFromScroll = true;

		try
		{
			SetSelectedItem( pScrollViewer, lActiveItem );
			SetSelectedDate( pScrollViewer, lStartDate );
		}
		finally
		{
			pState.IsUpdatingFromScroll = false;
		}
	}

	private static void ApplySelection( ExperienceTimelineScrollSyncState pState, Action pApply )
	{
		pState.IsApplyingSelection = true;

		try
		{
			pApply();
		}
		finally
		{
			pState.IsApplyingSelection = false;
		}
	}

	private static object? FindTargetItemForDate( ItemsControl pItemsControl, DateTime pDate )
	{
		DateTime lTargetDate = pDate.Date;

		object? lFirstItem = null;
		DateTime lFirstStartDate = DateTime.MaxValue;

		object? lBestMatchItem = null;
		DateTime lBestMatchStartDate = DateTime.MinValue;

		foreach ( object? lItem in pItemsControl.Items.Cast<object?>() )
		{
			if ( lItem is null || !TryGetStartDate( lItem, out DateTime lStartDate ) )
			{
				continue;
			}

			DateTime lItemDate = lStartDate.Date;

			if ( lFirstItem == null || lItemDate < lFirstStartDate )
			{
				lFirstItem = lItem;
				lFirstStartDate = lItemDate;
			}

			if ( lItemDate > lTargetDate || lItemDate < lBestMatchStartDate )
			{
				continue;
			}

			lBestMatchItem = lItem;
			lBestMatchStartDate = lItemDate;
		}

		return lBestMatchItem ?? lFirstItem;
	}

	private static object? FindActiveItemNearTop( ScrollViewer pScrollViewer, ItemsControl pItemsControl )
	{
		const double lTopPadding = 8d;

		object? lBestAtOrAboveTopItem = null;
		double lBestAtOrAboveTopY = double.MinValue;

		object? lBestBelowTopItem = null;
		double lBestBelowTopY = double.MaxValue;

		foreach ( object? lItem in pItemsControl.Items.Cast<object?>() )
		{
			if ( lItem is null || pItemsControl.ItemContainerGenerator.ContainerFromItem( lItem ) is not FrameworkElement lContainer )
			{
				continue;
			}

			double lTopY = GetTopY( pScrollViewer, lContainer );
			switch ( lTopY )
			{
				case double.MaxValue:
					{
						continue;
					}
				case <= lTopPadding:
					{
						if ( lTopY > lBestAtOrAboveTopY )
						{
							lBestAtOrAboveTopY = lTopY;
							lBestAtOrAboveTopItem = lItem;
						}

						continue;
					}
			}

			if ( !( lTopY < lBestBelowTopY ) )
			{
				continue;
			}

			lBestBelowTopY = lTopY;
			lBestBelowTopItem = lItem;
		}

		return lBestAtOrAboveTopItem ?? lBestBelowTopItem;
	}

	private static double GetTopY( ScrollViewer pScrollViewer, FrameworkElement pContainer )
	{
		try
		{
			Point lPoint = pContainer.TransformToAncestor( pScrollViewer ).Transform( new Point( 0, 0 ) );
			return lPoint.Y;
		}
		catch ( Exception )
		{
			// ignored
		}

		return double.MaxValue;
	}

	private static void ScrollToItem( ScrollViewer pScrollViewer, ItemsControl pItemsControl, object pItem )
	{
		if ( !TryGetContainer( pItemsControl, pItem, out FrameworkElement lContainer ) )
		{
			return;
		}

		double lTargetOffset = ComputeTargetVerticalOffset( pScrollViewer, lContainer );

		if ( double.IsNaN( lTargetOffset ) || double.IsInfinity( lTargetOffset ) )
		{
			return;
		}

		ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset( pScrollViewer, lTargetOffset, 220 );
	}

	private static bool TryGetContainer( ItemsControl pItemsControl, object pItem, out FrameworkElement pContainer )
	{
		pContainer = null!;

		if ( pItemsControl.ItemContainerGenerator.ContainerFromItem( pItem ) is FrameworkElement lContainer )
		{
			pContainer = lContainer;
			return true;
		}

		pItemsControl.UpdateLayout();

		if ( pItemsControl.ItemContainerGenerator.ContainerFromItem( pItem ) is not FrameworkElement lUpdatedContainer )
		{
			return false;
		}

		pContainer = lUpdatedContainer;
		return true;
	}

	private static double ComputeTargetVerticalOffset( ScrollViewer pScrollViewer, FrameworkElement pContainer )
	{
		const double lTopPadding = 8d;

		try
		{
			Point lPoint = pContainer.TransformToAncestor( pScrollViewer ).Transform( new Point( 0, 0 ) );
			double lRawTargetOffset = pScrollViewer.VerticalOffset + lPoint.Y - lTopPadding;
			return Math.Max( 0d, Math.Min( lRawTargetOffset, pScrollViewer.ScrollableHeight ) );
		}
		catch ( Exception )
		{
			// ignored
		}

		return pScrollViewer.VerticalOffset;
	}

	private static bool TryGetStartDate( object pItem, out DateTime pStartDate )
	{
		pStartDate = default;

		PropertyInfo? lPropertyInfo = GetStartDatePropertyInfo( pItem );
		if ( lPropertyInfo == null )
		{
			return false;
		}

		object? lValue = lPropertyInfo.GetValue( pItem, null );
		if ( lValue is not DateTime lDateTime )
		{
			return false;
		}

		pStartDate = lDateTime;
		return true;
	}

	private static PropertyInfo? GetStartDatePropertyInfo( object pItem )
	{
		Type lItemType = pItem.GetType();

		StartDatePropertyCacheEntry lCacheEntry = sStartDatePropertyByType.GetValue( lItemType, pType =>
		{
			PropertyInfo? lPropertyInfo = pType.GetProperty( "StartDate", BindingFlags.Public | BindingFlags.Instance );
			return new StartDatePropertyCacheEntry( lPropertyInfo?.PropertyType == typeof( DateTime ) ? lPropertyInfo : null );
		} );

		return lCacheEntry.PropertyInfo;
	}
}
