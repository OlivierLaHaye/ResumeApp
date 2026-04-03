using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ResumeApp.Converters;
using Xunit;

namespace ResumeApp.Tests.Converters;

public sealed class IsDarkBrushConverterTests
{
    [StaFact]
    public void Convert_BlackBrush_ReturnsTrue()
    {
        var lConverter = new IsDarkBrushConverter();

        var lResult = lConverter.Convert( Brushes.Black, typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.IsType<bool>( lResult );
        Assert.True( (bool)lResult );
    }

    [StaFact]
    public void Convert_WhiteBrush_ReturnsFalse()
    {
        var lConverter = new IsDarkBrushConverter();

        var lResult = lConverter.Convert( Brushes.White, typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.IsType<bool>( lResult );
        Assert.False( (bool)lResult );
    }

    [StaFact]
    public void Convert_NonBrush_ReturnsUnsetValue()
    {
        var lConverter = new IsDarkBrushConverter();

        var lResult = lConverter.Convert( "notabrush", typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( DependencyProperty.UnsetValue, lResult );
    }

    [StaFact]
    public void Convert_GradientBrush_ReturnsBoolBasedOnAverageColor()
    {
        var lConverter = new IsDarkBrushConverter();
        var lGradientBrush = new LinearGradientBrush( Colors.Black, Colors.DarkGray, 0 );
        lGradientBrush.Freeze();

        var lResult = lConverter.Convert( lGradientBrush, typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.IsType<bool>( lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        var lConverter = new IsDarkBrushConverter();

        Assert.Throws<NotSupportedException>( () =>
            lConverter.ConvertBack( true, typeof( Brush ), null!, CultureInfo.InvariantCulture ) );
    }
}

public sealed class PaletteIndexToBrushConverterTests
{
    [StaFact]
    public void Convert_IntIndex_ReturnsBrush()
    {
        var lConverter = new PaletteIndexToBrushConverter();

        var lResult = lConverter.Convert( 0, typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringIndex_ReturnsBrush()
    {
        var lConverter = new PaletteIndexToBrushConverter();

        var lResult = lConverter.Convert( "1", typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_NonParsable_DefaultsToZero()
    {
        var lConverter = new PaletteIndexToBrushConverter();

        var lResult = lConverter.Convert( "notanumber", typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_NegativeIndex_NormalizedToZero()
    {
        var lConverter = new PaletteIndexToBrushConverter();

        var lResult = lConverter.Convert( -1, typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_LargeIndex_WrapsAround()
    {
        var lConverter = new PaletteIndexToBrushConverter();

        var lResult = lConverter.Convert( 100, typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void ConvertBack_ReturnsDoNothing()
    {
        var lConverter = new PaletteIndexToBrushConverter();

        var lResult = lConverter.ConvertBack( Brushes.Red, typeof( int ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_OtherType_DefaultsToZero()
    {
        var lConverter = new PaletteIndexToBrushConverter();

        var lResult = lConverter.Convert( true, typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }
}
