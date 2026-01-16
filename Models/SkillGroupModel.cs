// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Collections.ObjectModel;

namespace ResumeApp.Models
{
	public sealed class SkillGroupModel( string pGroupTitleResourceKey, ObservableCollection<SkillItemModel> pSkills )
	{
		public string GroupTitleResourceKey { get; } = pGroupTitleResourceKey ?? string.Empty;

		public ObservableCollection<SkillItemModel> Skills { get; } = pSkills ?? new ObservableCollection<SkillItemModel>();
	}
}
