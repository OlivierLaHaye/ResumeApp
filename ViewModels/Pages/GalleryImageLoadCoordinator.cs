// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Services;
using System.Windows.Media;

namespace ResumeApp.ViewModels.Pages
{
	internal enum GalleryImageLoadState
	{
		Loading,
		HasImages,
		Empty,
		Failed
	}

	internal readonly record struct GalleryImageLoadResult( int AttemptedCount, int PublishedCount, bool HasFailed )
	{
		public GalleryImageLoadState State
		{
			get
			{
				if ( PublishedCount > 0 )
				{
					return GalleryImageLoadState.HasImages;
				}

				return HasFailed || AttemptedCount > 0 ? GalleryImageLoadState.Failed : GalleryImageLoadState.Empty;
			}
		}
	}

	internal static class GalleryImageLoadCoordinator
	{
		internal const int MaximumConcurrentDecodes = 2;

		private const string LoadingResourceKey = "LabelImagesLoading";
		private const string EmptyResourceKey = "LabelProjectNoImagesYet";
		private const string FailedResourceKey = "LabelImagesLoadFailed";

		private static readonly SemaphoreSlim sDecodeSemaphore = new( MaximumConcurrentDecodes, MaximumConcurrentDecodes );

		internal static int AvailableDecodeSlots => sDecodeSemaphore.CurrentCount;

		internal static string ResolveStatusText( ResourcesService pResourcesService, GalleryImageLoadState pState )
		{
			return pState switch
			{
				GalleryImageLoadState.Loading => pResourcesService[ LoadingResourceKey ],
				GalleryImageLoadState.Empty => pResourcesService[ EmptyResourceKey ],
				GalleryImageLoadState.Failed => pResourcesService[ FailedResourceKey ],
				_ => string.Empty
			};
		}

		internal static Task<T> RunLimitedAsync<T>( Func<T> pWork ) => RunLimitedAsync( pWork, CancellationToken.None );

		internal static async Task<T> RunLimitedAsync<T>( Func<T> pWork, CancellationToken pCancellationToken )
		{
			await sDecodeSemaphore.WaitAsync( pCancellationToken );

			try
			{
				return await Task.Run( pWork, pCancellationToken );
			}
			finally
			{
				sDecodeSemaphore.Release();
			}
		}

		internal static async Task<GalleryImageLoadResult> LoadAsync( Func<IEnumerable<ImageSource?>> pSequenceFactory, Action<ImageSource> pPublish )
		{
			int lAttemptedCount = 0;
			int lPublishedCount = 0;
			bool lHasFailed = false;
			IEnumerator<ImageSource?>? lEnumerator = null;

			try
			{
				while ( true )
				{
					( bool lHasNext, ImageSource? lImage ) = await RunLimitedAsync<( bool, ImageSource? )>( () =>
					{
						lEnumerator ??= pSequenceFactory().GetEnumerator();

						return lEnumerator.MoveNext() ? ( true, lEnumerator.Current ) : ( false, null );
					} );

					if ( !lHasNext )
					{
						break;
					}

					lAttemptedCount++;

					if ( lImage == null )
					{
						continue;
					}

					pPublish( lImage );
					lPublishedCount++;
				}
			}
			catch ( Exception )
			{
				lHasFailed = true;
			}
			finally
			{
				lEnumerator?.Dispose();
			}

			return new GalleryImageLoadResult( lAttemptedCount, lPublishedCount, lHasFailed );
		}
	}
}
