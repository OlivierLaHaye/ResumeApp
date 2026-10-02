// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ResumeApp.ViewModels.Pages;

namespace ResumeApp.Pages
{
	[ExcludeFromCodeCoverage( Justification = "XAML code-behind: view wiring that requires XAML resource loading and a live visual tree at runtime." )]
	public partial class ExperiencePage
	{
		public ExperiencePage()
		{
			InitializeComponent();
		}

		private static T? FindFirstDescendant<T>( DependencyObject pParent ) where T : DependencyObject
		{
			int lChildCount = VisualTreeHelper.GetChildrenCount( pParent );

			for ( int lIndex = 0; lIndex < lChildCount; lIndex++ )
			{
				DependencyObject lChild = VisualTreeHelper.GetChild( pParent, lIndex );

				if ( lChild is T lMatch )
				{
					return lMatch;
				}

				T? lDescendant = FindFirstDescendant<T>( lChild );

				if ( lDescendant != null )
				{
					return lDescendant;
				}
			}

			return null;
		}

		private void OnTimelineZoomOutClick( object pSender, RoutedEventArgs pEventArgs )
		{
			mExperienceTimelineControl.ZoomOut();
		}

		private void OnTimelineZoomInClick( object pSender, RoutedEventArgs pEventArgs )
		{
			mExperienceTimelineControl.ZoomIn();
		}

		private void OnTimelineShowAllClick( object pSender, RoutedEventArgs pEventArgs )
		{
			mExperienceTimelineControl.ShowAll();
			mExperienceTimelineControl.Focus();
		}

		private void OnTimelineSelectionActivated( object pSender, RoutedEventArgs pEventArgs )
		{
			if ( DataContext is not ExperiencePageViewModel { SelectedTimelineEntry: { } lEntry } )
			{
				return;
			}

			if ( mExperienceItemsControl.ItemContainerGenerator.ContainerFromItem( lEntry ) is not FrameworkElement lContainer )
			{
				return;
			}

			Button? lCardButton = FindFirstDescendant<Button>( lContainer );

			if ( lCardButton == null )
			{
				return;
			}

			lCardButton.BringIntoView();
			lCardButton.Focus();
		}
	}
}
