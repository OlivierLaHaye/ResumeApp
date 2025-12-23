// Copyright (C) Olivier La Haye
// All rights reserved.

namespace ResumeApp.Models
{
	public sealed class SkillItemModel
	{
		public string SkillNameResourceKey { get; }

		public SkillItemModel( string pSkillNameResourceKey )
		{
			SkillNameResourceKey = pSkillNameResourceKey ?? string.Empty;
		}
	}
}
