// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Collections.ObjectModel;

namespace ResumeApp.Models
{
	public sealed class SkillGroupModel
	{
		public string GroupTitleResourceKey { get; }

		public ObservableCollection<SkillItemModel> Skills { get; }

		public SkillGroupModel( string pGroupTitleResourceKey, ObservableCollection<SkillItemModel> pSkills )
		{
			GroupTitleResourceKey = pGroupTitleResourceKey ?? string.Empty;
			Skills = pSkills ?? new ObservableCollection<SkillItemModel>();
		}
	}
}
