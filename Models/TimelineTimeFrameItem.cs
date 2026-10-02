// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;

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

		private string mAccentColorKey;
		public string AccentColorKey
		{
			get => mAccentColorKey;
			set => SetProperty( ref mAccentColorKey, value );
		}

		private string mSubtitleText;
		public string SubtitleText
		{
			get => mSubtitleText;
			set => SetProperty( ref mSubtitleText, value ?? string.Empty );
		}

		private string mDescriptionText = string.Empty;
		public string DescriptionText
		{
			get => mDescriptionText;
			set => SetProperty( ref mDescriptionText, value ?? string.Empty );
		}

		public TimelineTimeFrameItem( DateTime pStartDate, DateTime pEndDate, string? pTitle, string? pAccentColorKey )
			: this( pStartDate, pEndDate, pTitle, pAccentColorKey, null )
		{
		}

		public TimelineTimeFrameItem( DateTime pStartDate, DateTime pEndDate, string? pTitle, string? pAccentColorKey, string? pSubtitleText )
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
			mSubtitleText = pSubtitleText ?? string.Empty;
		}
	}
}
