// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class EducationItemViewModel : PropertyChangedNotifier
	{
		private readonly ResourcesService mResourcesService;
		private readonly string mPrefix;

		public string TitleText => mResourcesService[ mPrefix + "Title" ];

		public string LocationDatesText => mResourcesService[ mPrefix + "LocationDates" ];

		public string NotesText => mResourcesService[ mPrefix + "Notes" ];

		public bool HasNotes => !string.IsNullOrWhiteSpace( NotesText );

		public EducationItemViewModel( ResourcesService pResourcesService, string? pPrefix )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );
			mPrefix = pPrefix ?? string.Empty;

			mResourcesService.PropertyChanged += ( _, _ ) => RefreshFromResources();
		}

		public void RefreshFromResources()
		{
			RaisePropertyChanged( nameof( TitleText ) );
			RaisePropertyChanged( nameof( LocationDatesText ) );
			RaisePropertyChanged( nameof( NotesText ) );
			RaisePropertyChanged( nameof( HasNotes ) );
		}
	}
}
