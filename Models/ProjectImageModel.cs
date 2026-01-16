// Copyright (C) Olivier La Haye
// All rights reserved.

namespace ResumeApp.Models
{
	public sealed class ProjectImageModel
	{
		public string FileName { get; }

		public string CaptionResourceKey { get; }

		public ProjectImageModel( string pFileName, string? pCaptionResourceKey )
		{
			if ( string.IsNullOrWhiteSpace( pFileName ) )
			{
				throw new ArgumentException( "File name must be provided.", nameof( pFileName ) );
			}

			FileName = pFileName;
			CaptionResourceKey = pCaptionResourceKey ?? string.Empty;
		}
	}
}
