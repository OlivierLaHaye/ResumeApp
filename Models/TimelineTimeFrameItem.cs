// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using System.Windows.Media;

namespace ResumeApp.Models
{
	public sealed class TimelineTimeFrameItem : PropertyChangedNotifier
	{

		private DateTime mStartDate;
		public DateTime StartDate
		{
			get => mStartDate;
			set => SetProperty( ref mStartDate, value.Date );
		}

		private DateTime mEndDate;
		public DateTime EndDate
		{
			get => mEndDate;
			set => SetProperty( ref mEndDate, value.Date );
		}

		private string mTitle;
		public string Title
		{
			get => mTitle;
			set => SetProperty( ref mTitle, value );
		}

		private Brush mAccentBrush;
		public Brush AccentBrush
		{
			get => mAccentBrush;
			set => SetProperty( ref mAccentBrush, value );
		}

		private string mAccentColorKey;
		public string AccentColorKey
		{
			get => mAccentColorKey;
			set => SetProperty( ref mAccentColorKey, value );
		}

		public TimelineTimeFrameItem( DateTime pStartDate, DateTime pEndDate, string pTitle, string pAccentColorKey )
		{
			DateTime lStartDate = pStartDate.Date;
			DateTime lEndDate = pEndDate.Date;

			if ( lEndDate < lStartDate )
			{
				(lStartDate, lEndDate) = (lEndDate, lStartDate);
			}

			mStartDate = lStartDate;
			mEndDate = lEndDate;
			mTitle = pTitle ?? string.Empty;
			mAccentColorKey = pAccentColorKey ?? string.Empty;
		}
	}
}
