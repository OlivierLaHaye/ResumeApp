using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ResumeApp.Converters;
using ResumeApp.Services;
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
    public void Convert_DrawingBrush_ReturnsBool()
    {
        var lConverter = new IsDarkBrushConverter();
        var lDrawingGroup = new DrawingGroup();
        lDrawingGroup.Children.Add( new GeometryDrawing( Brushes.Black, null, new RectangleGeometry( new Rect( 0, 0, 10, 10 ) ) ) );
        var lDrawingBrush = new DrawingBrush( lDrawingGroup );
        lDrawingBrush.Freeze();

        var lResult = lConverter.Convert( lDrawingBrush, typeof( bool ), null!, CultureInfo.InvariantCulture );

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

public sealed class PaletteIndexToBrushMultiConverterTests
{
    [StaFact]
    public void Convert_IndexAndThemeInputs_ReturnsBrush()
    {
        var lConverter = new PaletteIndexToBrushMultiConverter();

        var lResult = lConverter.Convert( [ 2, AppTheme.Dark, false ], typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.IsAssignableFrom<Brush>( lResult );
    }

    [StaFact]
    public void Convert_MatchesSingleValueConverterForSameIndex()
    {
        var lMultiConverter = new PaletteIndexToBrushMultiConverter();
        var lSingleConverter = new PaletteIndexToBrushConverter();

        var lMultiResult = lMultiConverter.Convert( [ 3, AppTheme.Light, true ], typeof( Brush ), null!, CultureInfo.InvariantCulture );
        var lSingleResult = lSingleConverter.Convert( 3, typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( lSingleResult, lMultiResult );
    }

    [StaFact]
    public void Convert_EmptyValues_DefaultsToFirstPaletteBrush()
    {
        var lConverter = new PaletteIndexToBrushMultiConverter();

        var lResult = lConverter.Convert( [ ], typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.IsAssignableFrom<Brush>( lResult );
    }

    [StaFact]
    public void Convert_UnsetIndex_DefaultsToFirstPaletteBrush()
    {
        var lConverter = new PaletteIndexToBrushMultiConverter();

        var lResult = lConverter.Convert( [ DependencyProperty.UnsetValue ], typeof( Brush ), null!, CultureInfo.InvariantCulture );

        Assert.IsAssignableFrom<Brush>( lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var lConverter = new PaletteIndexToBrushMultiConverter();

        Assert.Throws<NotSupportedException>( () =>
            lConverter.ConvertBack( Brushes.Red, [ typeof( int ) ], null!, CultureInfo.InvariantCulture ) );
    }
}
