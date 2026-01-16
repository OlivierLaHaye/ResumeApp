// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;

namespace ResumeApp.ViewModels.Pages
{
	public sealed class LocalizedResourceItemViewModel : PropertyChangedNotifier
	{
		private readonly ResourcesService mResourcesService;

		public string ResourceKey { get; }

		public string DisplayText => mResourcesService[ ResourceKey ];

		public LocalizedResourceItemViewModel( ResourcesService pResourcesService, string? pResourceKey )
		{
			mResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );
			ResourceKey = pResourceKey ?? string.Empty;

			mResourcesService.PropertyChanged += ( _, _ ) => RaisePropertyChanged( nameof( DisplayText ) );
		}
	}
}
