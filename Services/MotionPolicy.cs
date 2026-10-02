// Copyright (C) Olivier La Haye
// All rights reserved.

using System.ComponentModel;
using System.Windows;

namespace ResumeApp.Services;

public static class MotionPolicy
{
	private static readonly Lock sOverrideLock = new();
	private static bool? sIsAnimationEnabledOverride;

	public static event EventHandler? Changed;

	public static bool IsAnimationEnabled
	{
		get
		{
			lock ( sOverrideLock )
			{
				if ( sIsAnimationEnabledOverride is bool lOverride )
				{
					return lOverride;
				}
			}

			return ReadSystemAnimationPreference();
		}
	}

	static MotionPolicy()
	{
		SystemParameters.StaticPropertyChanged += OnSystemParametersStaticPropertyChanged;
	}

	public static Duration GetDuration( TimeSpan pDuration ) => IsAnimationEnabled ? new Duration( pDuration ) : new Duration( TimeSpan.Zero );

	internal static void SetAnimationEnabledOverride( bool? pIsAnimationEnabled )
	{
		lock ( sOverrideLock )
		{
			sIsAnimationEnabledOverride = pIsAnimationEnabled;
		}

		Changed?.Invoke( null, EventArgs.Empty );
	}

	internal static bool IsMotionRelatedSystemParameter( string? pPropertyName )
	{
		return pPropertyName is nameof( SystemParameters.ClientAreaAnimation ) or nameof( SystemParameters.MenuAnimation );
	}

	private static bool ReadSystemAnimationPreference()
	{
		try
		{
			return SystemParameters.ClientAreaAnimation;
		}
		catch ( Exception )
		{
			return true;
		}
	}

	private static void OnSystemParametersStaticPropertyChanged( object? pSender, PropertyChangedEventArgs pEventArgs )
	{
		if ( !IsMotionRelatedSystemParameter( pEventArgs.PropertyName ) )
		{
			return;
		}

		Changed?.Invoke( null, EventArgs.Empty );
	}
}
