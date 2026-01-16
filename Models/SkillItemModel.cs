// Copyright (C) Olivier La Haye
// All rights reserved.

namespace ResumeApp.Models
{
	public sealed class SkillItemModel( string pSkillNameResourceKey )
	{
		public string SkillNameResourceKey { get; } = pSkillNameResourceKey ?? string.Empty;
	}
}
