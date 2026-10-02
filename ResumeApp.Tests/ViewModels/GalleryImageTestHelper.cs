using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ResumeApp.Tests.ViewModels;

internal static class GalleryImageTestHelper
{
    internal static ImageSource CreateFrozenImage()
    {
        BitmapSource lImage = BitmapSource.Create( 1, 1, 96, 96, PixelFormats.Gray8, null, new byte[ 1 ], 1 );
        lImage.Freeze();

        return lImage;
    }

    internal static async Task WaitUntilAsync( Func<bool> pCondition, int pTimeoutMilliseconds = 5000 )
    {
        DateTime lDeadline = DateTime.UtcNow.AddMilliseconds( pTimeoutMilliseconds );

        while ( !pCondition() )
        {
            if ( DateTime.UtcNow > lDeadline )
            {
                throw new TimeoutException( "Condition was not met in time." );
            }

            await Task.Delay( 10 );
        }
    }
}
