// Copyright (C) Olivier La Haye
// All rights reserved.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using ResumeApp.Services;

namespace ResumeApp.Controls
{
	internal static class TimelineResourceLookup
	{
		private sealed class ResourcesServicePropertyCacheEntry( PropertyInfo? pPropertyInfo )
		{
			public PropertyInfo? PropertyInfo { get; } = pPropertyInfo;
		}

		private static readonly ConditionalWeakTable<Type, ResourcesServicePropertyCacheEntry> sResourcesServicePropertyByType = new();

		public static string GetString( FrameworkElement pOwner, string pResourceKey, string pFallback )
		{
			var lValue = ResolveResourcesService( pOwner.DataContext )?[ pResourceKey ];
			return string.IsNullOrEmpty( lValue ) ? pFallback : lValue;
		}

		internal static ResourcesService? ResolveResourcesService( object? pDataContext )
		{
			switch ( pDataContext )
			{
				case null:
					{
						return null;
					}
				case ResourcesService lResourcesService:
					{
						return lResourcesService;
					}
			}

			var lPropertyInfo = GetResourcesServicePropertyInfo( pDataContext.GetType() );
			if ( lPropertyInfo is null )
			{
				return null;
			}

			try
			{
				return lPropertyInfo.GetValue( pDataContext, null ) as ResourcesService;
			}
			catch ( Exception )
			{
				return null;
			}
		}

		private static PropertyInfo? GetResourcesServicePropertyInfo( Type pType )
		{
			var lEntry = sResourcesServicePropertyByType.GetValue( pType, static pCachedType =>
			{
				var lPropertyInfo = pCachedType.GetProperty( "ResourcesService", BindingFlags.Instance | BindingFlags.Public );
				var lHasValidType = lPropertyInfo?.PropertyType != null && typeof( ResourcesService ).IsAssignableFrom( lPropertyInfo.PropertyType );

				return new ResourcesServicePropertyCacheEntry( lHasValidType ? lPropertyInfo : null );
			} );

			return lEntry.PropertyInfo;
		}
	}
}
