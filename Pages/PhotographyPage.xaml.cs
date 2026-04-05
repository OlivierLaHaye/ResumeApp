// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace ResumeApp.Pages
{
	[ExcludeFromCodeCoverage( Justification = "XAML code-behind: only calls InitializeComponent which requires XAML resource loading at runtime." )]
	public partial class PhotographyPage
	{
		public PhotographyPage()
		{
			InitializeComponent();
		}
	}
}
