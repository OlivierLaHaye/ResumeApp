// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;

namespace ResumeApp.ViewModels
{
	public abstract class ViewModelBase( ResourcesService pResourcesService, ThemeService pThemeService )
		: PropertyChangedNotifier
	{
		public ResourcesService ResourcesService { get; } = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );

		public ThemeService ThemeService { get; } = pThemeService ?? throw new ArgumentNullException( nameof( pThemeService ) );
	}
}
