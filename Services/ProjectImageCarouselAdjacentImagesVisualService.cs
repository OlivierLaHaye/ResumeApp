// Copyright (C) Olivier La Haye
// All rights reserved.

using System;

namespace ResumeApp.Services
{
	public sealed class ProjectImageCarouselAdjacentImagesVisualService
	{
		private const double ScaleStepFactor = 0.32;
		private const double OpacityStepFactor = 0.9;
		private const double TranslateBaseRatio = 0.18;
		private const double TranslateStepFactor = 0.50;

		public static ProjectImageCarouselSlotVisualTargets GetSlotVisualTargets( int pStep, int pDirection, double pContainerWidth )
		{
			double lTargetScale = pStep <= 0 ? 1.0 : Math.Exp( -pStep * ScaleStepFactor );
			double lTargetOpacity = pStep <= 0 ? 1.0 : Math.Exp( -pStep * OpacityStepFactor );

			double lOffsetX = pStep <= 0
				? 0.0
				: pContainerWidth * TranslateBaseRatio * ( Math.Exp( pStep * TranslateStepFactor ) - 1.0 );

			double lTargetTranslateX = pDirection < 0 ? -lOffsetX : ( pDirection > 0 ? lOffsetX : 0.0 );

			return new ProjectImageCarouselSlotVisualTargets( lTargetScale, lTargetOpacity, lTargetTranslateX );
		}
	}

	public struct ProjectImageCarouselSlotVisualTargets
	{
		public double Scale { get; }
		public double Opacity { get; }
		public double TranslateX { get; }

		public ProjectImageCarouselSlotVisualTargets( double pScale, double pOpacity, double pTranslateX )
		{
			Scale = pScale;
			Opacity = pOpacity;
			TranslateX = pTranslateX;
		}
	}
}
