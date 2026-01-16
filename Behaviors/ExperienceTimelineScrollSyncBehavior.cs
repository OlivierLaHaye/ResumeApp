// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ResumeApp.Behaviors
{
	public static class ExperienceTimelineScrollSyncBehavior
	{
		private sealed class ExperienceTimelineScrollSyncState
		{
			public DispatcherTimer DebounceTimer { get; set; }
			public bool IsUpdatingFromScroll { get; set; }
			public bool IsApplyingSelection { get; set; }
			public bool HasPendingSync { get; set; }
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

		private static readonly ConditionalWeakTable<ScrollViewer, ExperienceTimelineScrollSyncState> sStateByScrollViewer =
			new();

		public static void SetIsEnabled( DependencyObject pElement, bool pValue ) => pElement.SetValue( sIsEnabledProperty, pValue );
		public static bool GetIsEnabled( DependencyObject pElement ) => ( bool )pElement.GetValue( sIsEnabledProperty );

		public static void SetItemsControl( DependencyObject pElement, ItemsControl pValue ) => pElement.SetValue( sItemsControlProperty, pValue );
		public static ItemsControl GetItemsControl( DependencyObject pElement ) => ( ItemsControl )pElement.GetValue( sItemsControlProperty );

		public static void SetSelectedDate( DependencyObject pElement, DateTime pValue ) => pElement.SetValue( sSelectedDateProperty, pValue );
		public static DateTime GetSelectedDate( DependencyObject pElement ) => ( DateTime )pElement.GetValue( sSelectedDateProperty );

		public static void SetSelectedItem( DependencyObject pElement, object pValue ) => pElement.SetValue( sSelectedItemProperty, pValue );
		public static object GetSelectedItem( DependencyObject pElement ) => pElement.GetValue( sSelectedItemProperty );

		private static void OnIsEnabledChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is not ScrollViewer lScrollViewer )
			{
				return;
			}

			bool lIsEnabled = pArgs.NewValue is true;

			if ( lIsEnabled )
			{
				ExperienceTimelineScrollSyncState lState = GetOrCreateState( lScrollViewer );
				Attach( lScrollViewer, lState );
				return;
			}

			if ( sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState lExistingState ) )
			{
				Detach( lScrollViewer, lExistingState );
			}
		}

		private static void OnItemsControlChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is not ScrollViewer lScrollViewer )
			{
				return;
			}

			if ( !GetIsEnabled( lScrollViewer ) )
			{
				return;
			}

			if ( !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState lState ) )
			{
				return;
			}

			lState.HasPendingSync = true;
			QueueInitialSync( lScrollViewer, lState );
		}

		private static void OnSelectedDateChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is not ScrollViewer lScrollViewer )
			{
				return;
			}

			if ( !GetIsEnabled( lScrollViewer ) )
			{
				return;
			}

			if ( !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState lState ) )
			{
				return;
			}

			if ( lState.IsUpdatingFromScroll )
			{
				return;
			}

			if ( pArgs.NewValue is not DateTime lNewDate )
			{
				return;
			}

			ItemsControl lItemsControl = GetItemsControl( lScrollViewer );
			if ( lItemsControl == null )
			{
				return;
			}

			object lTargetItem = FindTargetItemForDate( lItemsControl, lNewDate );
			if ( lTargetItem == null )
			{
				return;
			}

			lState.IsApplyingSelection = true;

			try
			{
				SetSelectedItem( lScrollViewer, lTargetItem );
			}
			finally
			{
				lState.IsApplyingSelection = false;
			}

			ScrollToItem( lScrollViewer, lItemsControl, lTargetItem, lState );
		}

		private static void OnSelectedItemChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is not ScrollViewer lScrollViewer )
			{
				return;
			}

			if ( !GetIsEnabled( lScrollViewer ) )
			{
				return;
			}

			if ( !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState lState ) )
			{
				return;
			}

			if ( lState.IsUpdatingFromScroll )
			{
				return;
			}

			if ( lState.IsApplyingSelection )
			{
				return;
			}

			object lNewItem = pArgs.NewValue;
			if ( lNewItem == null )
			{
				return;
			}

			if ( !TryGetStartDate( lNewItem, out DateTime lStartDate ) )
			{
				return;
			}

			lState.IsApplyingSelection = true;

			try
			{
				SetSelectedDate( lScrollViewer, lStartDate );
			}
			finally
			{
				lState.IsApplyingSelection = false;
			}

			ItemsControl lItemsControl = GetItemsControl( lScrollViewer );
			if ( lItemsControl == null )
			{
				return;
			}

			ScrollToItem( lScrollViewer, lItemsControl, lNewItem, lState );
		}

		private static ExperienceTimelineScrollSyncState GetOrCreateState( ScrollViewer pScrollViewer )
		{
			return sStateByScrollViewer.GetValue( pScrollViewer, pViewer => new ExperienceTimelineScrollSyncState() );
		}

		private static void Attach( ScrollViewer pScrollViewer, ExperienceTimelineScrollSyncState pState )
		{
			pScrollViewer.Loaded -= OnScrollViewerLoaded;
			pScrollViewer.Loaded += OnScrollViewerLoaded;

			pScrollViewer.ScrollChanged -= OnScrollChanged;
			pScrollViewer.ScrollChanged += OnScrollChanged;

			if ( pState.DebounceTimer == null )
			{
				pState.DebounceTimer = new DispatcherTimer( DispatcherPriority.Background )
				{
					Interval = TimeSpan.FromMilliseconds( 90 )
				};

				pState.DebounceTimer.Tick += ( pSender, pEventArgs ) =>
				{
					pState.DebounceTimer.Stop();
					UpdateSelectionFromViewport( pScrollViewer, pState );
				};
			}

			pState.HasPendingSync = true;
			QueueInitialSync( pScrollViewer, pState );
		}

		private static void Detach( ScrollViewer pScrollViewer, ExperienceTimelineScrollSyncState pState )
		{
			pScrollViewer.Loaded -= OnScrollViewerLoaded;
			pScrollViewer.ScrollChanged -= OnScrollChanged;

			pState.DebounceTimer?.Stop();
		}

		private static void QueueInitialSync( ScrollViewer pScrollViewer, ExperienceTimelineScrollSyncState pState )
		{
			pScrollViewer.Dispatcher.BeginInvoke( DispatcherPriority.Loaded, new Action( () =>
			{
				if ( !GetIsEnabled( pScrollViewer ) )
				{
					return;
				}

				if ( !pState.HasPendingSync )
				{
					return;
				}

				pState.HasPendingSync = false;
				UpdateSelectionFromViewport( pScrollViewer, pState );
			} ) );
		}

		private static void OnScrollViewerLoaded( object pSender, RoutedEventArgs pArgs )
		{
			if ( pSender is not ScrollViewer lScrollViewer )
			{
				return;
			}

			if ( !GetIsEnabled( lScrollViewer ) )
			{
				return;
			}

			if ( !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState lState ) )
			{
				return;
			}

			lState.HasPendingSync = true;
			QueueInitialSync( lScrollViewer, lState );
		}

		private static void OnScrollChanged( object pSender, ScrollChangedEventArgs pArgs )
		{
			if ( pSender is not ScrollViewer lScrollViewer )
			{
				return;
			}

			if ( !GetIsEnabled( lScrollViewer ) )
			{
				return;
			}

			if ( !sStateByScrollViewer.TryGetValue( lScrollViewer, out ExperienceTimelineScrollSyncState lState ) )
			{
				return;
			}

			if ( lState.IsApplyingSelection )
			{
				return;
			}

			lState.DebounceTimer?.Stop();
			lState.DebounceTimer?.Start();
		}

		private static void UpdateSelectionFromViewport( ScrollViewer pScrollViewer, ExperienceTimelineScrollSyncState pState )
		{
			ItemsControl lItemsControl = GetItemsControl( pScrollViewer );
			if ( lItemsControl == null )
			{
				return;
			}

			object lActiveItem = FindActiveItemNearTop( pScrollViewer, lItemsControl );
			if ( lActiveItem == null )
			{
				return;
			}

			if ( !TryGetStartDate( lActiveItem, out DateTime lStartDate ) )
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

		private static object FindTargetItemForDate( ItemsControl pItemsControl, DateTime pDate )
		{
			var lItemsWithDates = pItemsControl.Items
				.Cast<object>()
				.Select( pItem => new { Item = pItem, HasDate = TryGetStartDate( pItem, out DateTime lStartDate ), StartDate = lStartDate } )
				.Where( pItem => pItem.HasDate )
				.OrderBy( pItem => pItem.StartDate )
				.ToList();

			if ( lItemsWithDates.Count == 0 )
			{
				return null;
			}

			var lMatch = lItemsWithDates
				.LastOrDefault( pItem => pItem.StartDate.Date <= pDate.Date );

			return ( lMatch ?? lItemsWithDates.First() ).Item;
		}

		private static object FindActiveItemNearTop( ScrollViewer pScrollViewer, ItemsControl pItemsControl )
		{
			const double lTopPadding = 8d;

			var lCandidates = pItemsControl.Items
				.Cast<object>()
				.Select( pItem => new { Item = pItem, Container = pItemsControl.ItemContainerGenerator.ContainerFromItem( pItem ) as FrameworkElement } )
				.Where( pItem => pItem.Container != null )
				.Select( pItem => new { pItem.Item, pItem.Container, TopY = GetTopY( pScrollViewer, pItem.Container ) } )
				.ToList();

			if ( lCandidates.Count == 0 )
			{
				return null;
			}

			var lAtOrAboveTop = lCandidates
				.Where( pItem => pItem.TopY <= lTopPadding )
				.OrderByDescending( pItem => pItem.TopY )
				.FirstOrDefault();

			if ( lAtOrAboveTop != null )
			{
				return lAtOrAboveTop.Item;
			}

			return lCandidates
				.OrderBy( pItem => pItem.TopY )
				.First()
				.Item;
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

		private static void ScrollToItem( ScrollViewer pScrollViewer, ItemsControl pItemsControl, object pItem, ExperienceTimelineScrollSyncState pState )
		{
			if ( pItemsControl.ItemContainerGenerator.ContainerFromItem( pItem ) is not FrameworkElement lContainer )
			{
				pItemsControl.UpdateLayout();
				lContainer = pItemsControl.ItemContainerGenerator.ContainerFromItem( pItem ) as FrameworkElement;
			}

			if ( lContainer == null )
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

			if ( pItem == null )
			{
				return false;
			}

			PropertyInfo lPropertyInfo = pItem.GetType().GetProperty( "StartDate", BindingFlags.Public | BindingFlags.Instance );

			if ( lPropertyInfo == null || lPropertyInfo.PropertyType != typeof( DateTime ) )
			{
				return false;
			}

			object lValue = lPropertyInfo.GetValue( pItem, null );

			if ( lValue is not DateTime lDateTime )
			{
				return false;
			}

			pStartDate = lDateTime;
			return true;
		}
	}
}
