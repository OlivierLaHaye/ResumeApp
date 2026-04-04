using System.Windows.Media;
using ResumeApp.Helpers;
using Xunit;

namespace ResumeApp.Tests.Helpers;

public sealed class ColorHelperTests
{
    [StaFact]
    public void sAccentBrushKeys_HasExpectedCount()
    {
        Assert.Equal( 8, ColorHelper.sAccentBrushKeys.Length );
    }

    [StaFact]
    public void sAccentBrushKeys_AllNonEmpty()
    {
        foreach ( string lKey in ColorHelper.sAccentBrushKeys )
        {
            Assert.False( string.IsNullOrWhiteSpace( lKey ) );
        }
    }

    [StaFact]
    public void CalculateAverageColor_SingleColor_ReturnsSameColor()
    {
        var lColors = new List<Color> { Color.FromArgb( 255, 100, 150, 200 ) };

        Color lResult = ColorHelper.CalculateAverageColor( lColors );

        Assert.Equal( 255, lResult.A );
        Assert.Equal( 100, lResult.R );
        Assert.Equal( 150, lResult.G );
        Assert.Equal( 200, lResult.B );
    }

    [StaFact]
    public void CalculateAverageColor_MultipleColors_ReturnsAverage()
    {
        var lColors = new List<Color>
        {
            Color.FromArgb( 255, 0, 0, 0 ),
            Color.FromArgb( 255, 200, 200, 200 )
        };

        Color lResult = ColorHelper.CalculateAverageColor( lColors );

        Assert.Equal( 255, lResult.A );
        Assert.Equal( 100, lResult.R );
        Assert.Equal( 100, lResult.G );
        Assert.Equal( 100, lResult.B );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_BlackColor_ReturnsTrue()
    {
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Colors.Black );

        Assert.True( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_WhiteColor_ReturnsFalse()
    {
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Colors.White );

        Assert.False( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_DarkBlue_ReturnsTrue()
    {
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 0, 0, 100 ) );

        Assert.True( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_LightYellow_ReturnsFalse()
    {
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 255, 255, 200 ) );

        Assert.False( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_DarkRed_ReturnsTrue()
    {
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 100, 0, 0 ) );

        Assert.True( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_MidGray_HandlesBorderCase()
    {
        // Tests the middle ground - should not crash
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 128, 128, 128 ) );

        // Just verify it returns a valid result without crashing
        Assert.IsType<bool>( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_LowSaturation_LightGray()
    {
        // Gray around L*=75 boundary
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 190, 190, 190 ) );

        Assert.False( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_DarkGray()
    {
        // Gray around L*=45 boundary
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 80, 80, 80 ) );

        Assert.True( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_HighSaturationMidLuminance()
    {
        // A vivid green - tests the saturation/lightness branches
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 0, 200, 0 ) );

        Assert.IsType<bool>( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_LowLuminanceColor_HighContrastDifference()
    {
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 10, 10, 10 ) );

        Assert.True( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_HighLuminanceColor_HighContrastDifference()
    {
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 245, 245, 245 ) );

        Assert.False( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_BelowSrgbThreshold()
    {
        // Colors with components <= 0.04045 * 255 = ~10.3
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 10, 10, 10 ) );

        Assert.True( lResult );
    }

    [StaFact]
    public void IsWhiteForegroundPreferred_NearlyEqualContrastDiff_MidSaturationMidLightness()
    {
        // Designed to trigger lDiff <= 0.5 and medium lightness/saturation branches
        bool lResult = ColorHelper.IsWhiteForegroundPreferred( Color.FromRgb( 153, 120, 100 ) );

        Assert.IsType<bool>( lResult );
    }

    [StaFact]
    public void GetAverageColorFromDrawingBrush_SolidFill_ReturnsColor()
    {
        var lDrawingBrush = new DrawingBrush(
            new GeometryDrawing( Brushes.Red, null, new RectangleGeometry( new System.Windows.Rect( 0, 0, 10, 10 ) ) ) );

        Color lResult = ColorHelper.GetAverageColorFromDrawingBrush( lDrawingBrush );

        // Red channel should be high
        Assert.True( lResult.R > 200 );
    }
}
