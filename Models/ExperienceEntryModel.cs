// Copyright (C) Olivier La Haye
// All rights reserved.

using System;

namespace ResumeApp.Models
{
	public sealed class ExperienceEntryModel
	{
		public string CompanyResourceKey { get; }

		public string RoleResourceKey { get; }

		public string LocationResourceKey { get; }

		public DateTime StartDate { get; }

		public DateTime? EndDate { get; }

		public string ScopeResourceKey { get; }

		public string AccomplishmentsResourceKey { get; }

		public string TechResourceKey { get; }

		public string NonColorCueResourceKey { get; }

		public ExperienceEntryModel(
			string pCompanyResourceKey,
			string pRoleResourceKey,
			string pLocationResourceKey,
			DateTime pStartDate,
			DateTime? pEndDate,
			string pScopeResourceKey,
			string pAccomplishmentsResourceKey,
			string pTechResourceKey,
			string pNonColorCueResourceKey )
		{
			CompanyResourceKey = pCompanyResourceKey ?? string.Empty;
			RoleResourceKey = pRoleResourceKey ?? string.Empty;
			LocationResourceKey = pLocationResourceKey ?? string.Empty;
			StartDate = pStartDate;
			EndDate = pEndDate;
			ScopeResourceKey = pScopeResourceKey ?? string.Empty;
			AccomplishmentsResourceKey = pAccomplishmentsResourceKey ?? string.Empty;
			TechResourceKey = pTechResourceKey ?? string.Empty;
			NonColorCueResourceKey = pNonColorCueResourceKey ?? string.Empty;
		}
	}
}
