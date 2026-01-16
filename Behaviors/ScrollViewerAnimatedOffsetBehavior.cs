// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ResumeApp.Behaviors
{
	public static class ScrollViewerAnimatedOffsetBehavior
	{
		public static readonly DependencyProperty sAnimatedVerticalOffsetProperty =
			DependencyProperty.RegisterAttached(
				"AnimatedVerticalOffset",
				typeof( double ),
				typeof( ScrollViewerAnimatedOffsetBehavior ),
				new PropertyMetadata( 0d, OnAnimatedVerticalOffsetChanged ) );

		public static void SetAnimatedVerticalOffset( DependencyObject pElement, double pValue ) => pElement.SetValue( sAnimatedVerticalOffsetProperty, pValue );

		public static void AnimateVerticalOffset( ScrollViewer? pScrollViewer, double pTargetVerticalOffset, int pDurationMilliseconds )
		{
			if ( pScrollViewer == null )
			{
				return;
			}

			double lClampedTargetOffset = Math.Max( 0d, Math.Min( pTargetVerticalOffset, pScrollViewer.ScrollableHeight ) );

			if ( pDurationMilliseconds <= 0 )
			{
				SetAnimatedVerticalOffset( pScrollViewer, lClampedTargetOffset );
				return;
			}

			double lCurrentOffset = pScrollViewer.VerticalOffset;

			if ( Math.Abs( lCurrentOffset - lClampedTargetOffset ) < 0.5d )
			{
				SetAnimatedVerticalOffset( pScrollViewer, lClampedTargetOffset );
				return;
			}

			pScrollViewer.BeginAnimation( sAnimatedVerticalOffsetProperty, null );

			var lAnimation = new DoubleAnimation
			{
				From = lCurrentOffset,
				To = lClampedTargetOffset,
				Duration = TimeSpan.FromMilliseconds( pDurationMilliseconds ),
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
				FillBehavior = FillBehavior.Stop
			};

			lAnimation.Completed += ( _, _ ) =>
			{
				SetAnimatedVerticalOffset( pScrollViewer, lClampedTargetOffset );
			};

			pScrollViewer.BeginAnimation( sAnimatedVerticalOffsetProperty, lAnimation, HandoffBehavior.SnapshotAndReplace );
		}

		private static void OnAnimatedVerticalOffsetChanged( DependencyObject pDependencyObject, DependencyPropertyChangedEventArgs pArgs )
		{
			if ( pDependencyObject is ScrollViewer lScrollViewer && pArgs.NewValue is double lOffset )
			{
				lScrollViewer.ScrollToVerticalOffset( lOffset );
			}
		}
	}
}
