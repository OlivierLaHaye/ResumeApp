// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Infrastructure;
using ResumeApp.Services;

namespace ResumeApp.ViewModels
{
	public abstract class ViewModelBase : PropertyChangedNotifier
	{
		public ResourcesService ResourcesService { get; }

		public ThemeService ThemeService { get; }

		protected ViewModelBase( ResourcesService pResourcesService, ThemeService pThemeService )
		{
			ResourcesService = pResourcesService ?? throw new ArgumentNullException( nameof( pResourcesService ) );
			ThemeService = pThemeService ?? throw new ArgumentNullException( nameof( pThemeService ) );
		}
	}
}
