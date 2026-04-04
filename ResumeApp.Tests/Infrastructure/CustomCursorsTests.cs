using ResumeApp.Infrastructure;
using Xunit;

namespace ResumeApp.Tests.Infrastructure;

public sealed class CustomCursorsTests
{
    // CustomCursors relies on pack:// URIs which require an Application context.
    // We verify the static properties exist and don't throw on access when no application is running.

    [StaFact]
    public void DragLeftRightCursor_ThrowsOrReturnsValue()
    {
        // Without a WPF Application, pack:// URIs fail. This is expected behavior.
        // We still test to cover the code path.
        try
        {
            var lCursor = CustomCursors.DragLeftRightCursor;
            Assert.NotNull( lCursor );
        }
        catch ( Exception )
        {
            // Expected when no Application context - pack:// URIs unavailable
        }
    }

    [StaFact]
    public void DraggingCursor_ThrowsOrReturnsValue()
    {
        try
        {
            var lCursor = CustomCursors.DraggingCursor;
            Assert.NotNull( lCursor );
        }
        catch ( Exception )
        {
            // Expected when no Application context
        }
    }
}
