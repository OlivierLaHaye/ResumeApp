using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ResumeApp.Converters;
using Xunit;

namespace ResumeApp.Tests.Converters;

public sealed class ValuesEqualConverterTests
{
    [StaFact]
    public void Convert_EqualValues_ReturnsTrue()
    {
        var lConverter = new ValuesEqualConverter();

        var lResult = lConverter.Convert( [(object)42, (object)42], typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( true, lResult );
    }

    [StaFact]
    public void Convert_DifferentValues_ReturnsFalse()
    {
        var lConverter = new ValuesEqualConverter();

        var lResult = lConverter.Convert( [(object)42, (object)43], typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( false, lResult );
    }

    [StaFact]
    public void Convert_FirstNull_ReturnsFalse()
    {
        var lConverter = new ValuesEqualConverter();

        var lResult = lConverter.Convert( [null!, (object)42], typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( false, lResult );
    }

    [StaFact]
    public void Convert_SecondNull_ReturnsFalse()
    {
        var lConverter = new ValuesEqualConverter();

        var lResult = lConverter.Convert( [(object)42, null!], typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( false, lResult );
    }

    [StaFact]
    public void Convert_SingleValue_ReturnsFalse()
    {
        var lConverter = new ValuesEqualConverter();

        var lResult = lConverter.Convert( [(object)42], typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( false, lResult );
    }

    [StaFact]
    public void Convert_EmptyArray_ReturnsFalse()
    {
        var lConverter = new ValuesEqualConverter();

        var lResult = lConverter.Convert( [], typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( false, lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var lConverter = new ValuesEqualConverter();

        Assert.Throws<NotImplementedException>( () =>
            lConverter.ConvertBack( true, [typeof( object )], null!, CultureInfo.InvariantCulture ) );
    }
}

public sealed class CornerSelectionTests
{
    [Fact]
    public void CornerSelection_Flags_WorkCorrectly()
    {
        var lAll = CornerSelection.All;

        Assert.True( lAll.HasFlag( CornerSelection.TopLeft ) );
        Assert.True( lAll.HasFlag( CornerSelection.TopRight ) );
        Assert.True( lAll.HasFlag( CornerSelection.BottomRight ) );
        Assert.True( lAll.HasFlag( CornerSelection.BottomLeft ) );
    }

    [Fact]
    public void CornerSelection_None_HasNoFlags()
    {
        var lNone = CornerSelection.None;

        Assert.False( lNone.HasFlag( CornerSelection.TopLeft ) );
        Assert.False( lNone.HasFlag( CornerSelection.TopRight ) );
        Assert.False( lNone.HasFlag( CornerSelection.BottomRight ) );
        Assert.False( lNone.HasFlag( CornerSelection.BottomLeft ) );
    }

    [Fact]
    public void CornerSelection_Combination()
    {
        var lCombination = CornerSelection.TopLeft | CornerSelection.BottomRight;

        Assert.True( lCombination.HasFlag( CornerSelection.TopLeft ) );
        Assert.True( lCombination.HasFlag( CornerSelection.BottomRight ) );
        Assert.False( lCombination.HasFlag( CornerSelection.TopRight ) );
        Assert.False( lCombination.HasFlag( CornerSelection.BottomLeft ) );
    }
}

public sealed class SquircleCornerDataConverterTests
{
    [StaFact]
    public void Convert_NullInput_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( null!, typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_AllCornersEnum_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( CornerSelection.All, typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_NoneEnum_TreatsAsAll()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( CornerSelection.None, typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_IntMask_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( (int)( CornerSelection.TopLeft | CornerSelection.TopRight ), typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_IntZero_TreatsAsAll()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( 0, typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringTokens_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( "TopLeft,TopRight", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringShortNames_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( "tl,br", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringWithEquals_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( "topleft=true,bottomright=1", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringEqualsWithFalse_ExcludesCorner()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( "topleft=false", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_EmptyString_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( "", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringNumericIntParsable_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( "3", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_DoubleParameter_UsesAsControlOffset()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( CornerSelection.All, typeof( object ), 20.0, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringParameter_UsesAsControlOffset()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( CornerSelection.All, typeof( object ), "15.5", CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_NegativeOffset_ClampedToZero()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( CornerSelection.All, typeof( object ), -10.0, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_LargeOffset_ClampedToMax()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( CornerSelection.All, typeof( object ), 999.0, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_UnparsableStringParam_UsesDefault()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( CornerSelection.All, typeof( object ), "notanumber", CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_OnlySingleCorner_ProducesGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( CornerSelection.TopLeft, typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_UnknownObjectType_ReturnsGeometry()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( new object(), typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void ConvertBack_ReturnsDoNothing()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.ConvertBack( null!, typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_StringWithKeyValuePairSeparatedBySemicolon()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( "tl=0,br=false", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringEquals_PartialSplit_NoValue()
    {
        var lConverter = new SquircleCornerDataConverter();

        // Key=Value pair where split produces only 1 part (no '=' value)
        var lResult = lConverter.Convert( "topleft=", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }

    [StaFact]
    public void Convert_StringEquals_UnrelatedKey()
    {
        var lConverter = new SquircleCornerDataConverter();

        var lResult = lConverter.Convert( "unknown=true", typeof( object ), null!, CultureInfo.InvariantCulture );

        Assert.NotNull( lResult );
    }
}

public sealed class MathOperationConverterTests
{
    [StaFact]
    public void Convert_AdditionTwoDoubles()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)5.0], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( 15.0, lResult );
    }

    [StaFact]
    public void Convert_SubtractionTwoDoubles()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)3.0], typeof( double ), "subtract", CultureInfo.InvariantCulture );

        Assert.Equal( 7.0, lResult );
    }

    [StaFact]
    public void Convert_MultiplyTwoDoubles()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)3.0], typeof( double ), "multiply", CultureInfo.InvariantCulture );

        Assert.Equal( 30.0, lResult );
    }

    [StaFact]
    public void Convert_DivideTwoDoubles()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)4.0], typeof( double ), "divide", CultureInfo.InvariantCulture );

        Assert.Equal( 2.5, lResult );
    }

    [StaFact]
    public void Convert_DivideByZero_ReturnsZero()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)0.0], typeof( double ), "divide", CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void Convert_ModuloTwoDoubles()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)3.0], typeof( double ), "modulo", CultureInfo.InvariantCulture );

        Assert.Equal( 1.0, lResult );
    }

    [StaFact]
    public void Convert_ModuloByZero_ReturnsZero()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)0.0], typeof( double ), "mod", CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void Convert_NullValues_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( null, typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_EmptyValues_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_NullParameter_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)1.0], typeof( double ), null, CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_InvertSingleValue_ReturnsNegated()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)5.0], typeof( double ), "invert", CultureInfo.InvariantCulture );

        Assert.Equal( -5.0, lResult );
    }

    [StaFact]
    public void Convert_InvertWithOperation()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)5.0], typeof( double ), "add inverse", CultureInfo.InvariantCulture );

        Assert.Equal( 5.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Plus()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)5.0], typeof( double ), "plus", CultureInfo.InvariantCulture );

        Assert.Equal( 15.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Minus()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)5.0], typeof( double ), "minus", CultureInfo.InvariantCulture );

        Assert.Equal( 5.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Times()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)5.0], typeof( double ), "times", CultureInfo.InvariantCulture );

        Assert.Equal( 50.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Div()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)2.0], typeof( double ), "div", CultureInfo.InvariantCulture );

        Assert.Equal( 5.0, lResult );
    }

    [StaFact]
    public void Convert_StringValue_ParsedAsDouble()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)"10.0", (object)"5.0"], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( 15.0, lResult );
    }

    [StaFact]
    public void Convert_ThicknessFirstValue_AddScalar()
    {
        var lConverter = new MathOperationConverter();
        var lThickness = new Thickness( 1, 2, 3, 4 );

        var lResult = lConverter.Convert( [(object)lThickness, (object)10.0], typeof( Thickness ), "add", CultureInfo.InvariantCulture );

        Assert.IsType<Thickness>( lResult );
        var lResultThickness = (Thickness)lResult;
        Assert.Equal( 11.0, lResultThickness.Left );
        Assert.Equal( 12.0, lResultThickness.Top );
    }

    [StaFact]
    public void Convert_ThicknessWithThickness_AddThicknesses()
    {
        var lConverter = new MathOperationConverter();
        var lT1 = new Thickness( 1, 2, 3, 4 );
        var lT2 = new Thickness( 10, 20, 30, 40 );

        var lResult = lConverter.Convert( [(object)lT1, (object)lT2], typeof( Thickness ), "addition", CultureInfo.InvariantCulture );

        Assert.IsType<Thickness>( lResult );
        var lResultThickness = (Thickness)lResult;
        Assert.Equal( 11.0, lResultThickness.Left );
        Assert.Equal( 22.0, lResultThickness.Top );
        Assert.Equal( 33.0, lResultThickness.Right );
        Assert.Equal( 44.0, lResultThickness.Bottom );
    }

    [StaFact]
    public void Convert_InvertThickness_NegatesAll()
    {
        var lConverter = new MathOperationConverter();
        var lThickness = new Thickness( 1, 2, 3, 4 );

        var lResult = lConverter.Convert( [(object)lThickness], typeof( Thickness ), "invert", CultureInfo.InvariantCulture );

        Assert.IsType<Thickness>( lResult );
        var lResultThickness = (Thickness)lResult;
        Assert.Equal( -1.0, lResultThickness.Left );
        Assert.Equal( -2.0, lResultThickness.Top );
    }

    [StaFact]
    public void Convert_UnsetValuesFiltered()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [DependencyProperty.UnsetValue, (object)5.0, (object)3.0], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( 8.0, lResult );
    }

    [StaFact]
    public void Convert_DoNothingValuesFiltered()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [Binding.DoNothing, (object)5.0, (object)3.0], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( 8.0, lResult );
    }

    [StaFact]
    public void Convert_AllFilteredOut_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [DependencyProperty.UnsetValue], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_NoOperationMultipleValues_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)5.0, (object)3.0], typeof( double ), "unknown", CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_InvertNonNumericSingleValue_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)"notanumber"], typeof( double ), "negate", CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_FirstValueNotDoubleOrThickness_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)true, (object)5.0], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_SecondValueNotParsable_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)5.0, (object)true], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void Convert_ConvertibleValue_ConvertsToDouble()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10, (object)5], typeof( double ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( 15.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Addition()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)1.0, (object)2.0], typeof( double ), "addition", CultureInfo.InvariantCulture );

        Assert.Equal( 3.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Sub()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)3.0], typeof( double ), "sub", CultureInfo.InvariantCulture );

        Assert.Equal( 7.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Subtraction()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)3.0], typeof( double ), "subtraction", CultureInfo.InvariantCulture );

        Assert.Equal( 7.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Mul()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)3.0, (object)4.0], typeof( double ), "mul", CultureInfo.InvariantCulture );

        Assert.Equal( 12.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Multiplication()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)3.0, (object)4.0], typeof( double ), "multiplication", CultureInfo.InvariantCulture );

        Assert.Equal( 12.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Division()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)12.0, (object)4.0], typeof( double ), "division", CultureInfo.InvariantCulture );

        Assert.Equal( 3.0, lResult );
    }

    [StaFact]
    public void Convert_OperationAliases_Remainder()
    {
        var lConverter = new MathOperationConverter();

        var lResult = lConverter.Convert( [(object)10.0, (object)3.0], typeof( double ), "remainder", CultureInfo.InvariantCulture );

        Assert.Equal( 1.0, lResult );
    }

    [StaFact]
    public void Convert_InvertAliases()
    {
        var lConverter = new MathOperationConverter();

        foreach ( var lAlias in new[] { "inverse", "inversey", "revers", "reverse", "opposite" } )
        {
            var lResult = lConverter.Convert( [(object)5.0], typeof( double ), lAlias, CultureInfo.InvariantCulture );
            Assert.Equal( -5.0, lResult );
        }
    }

    [StaFact]
    public void Convert_ThicknessInvertedWithAdd()
    {
        var lConverter = new MathOperationConverter();
        var lT1 = new Thickness( 10, 20, 30, 40 );
        var lT2 = new Thickness( 1, 2, 3, 4 );

        var lResult = lConverter.Convert( [(object)lT1, (object)lT2], typeof( Thickness ), "add inverse", CultureInfo.InvariantCulture );

        Assert.IsType<Thickness>( lResult );
        var lResultThickness = (Thickness)lResult;
        Assert.Equal( 9.0, lResultThickness.Left );
    }

    [StaFact]
    public void Convert_ThicknessWithNonParsableSecondValue_ReturnsDoNothing()
    {
        var lConverter = new MathOperationConverter();
        var lT1 = new Thickness( 10, 20, 30, 40 );

        var lResult = lConverter.Convert( [(object)lT1, (object)true], typeof( Thickness ), "add", CultureInfo.InvariantCulture );

        Assert.Equal( Binding.DoNothing, lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var lConverter = new MathOperationConverter();

        Assert.Throws<NotImplementedException>( () =>
            lConverter.ConvertBack( 5.0, [typeof( double )], "add", CultureInfo.InvariantCulture ) );
    }

    [StaFact]
    public void MathOperation_EnumValues()
    {
        Assert.Equal( 0, (int)MathOperation.Addition );
        Assert.Equal( 1, (int)MathOperation.Subtraction );
        Assert.Equal( 2, (int)MathOperation.Multiply );
        Assert.Equal( 3, (int)MathOperation.Division );
        Assert.Equal( 4, (int)MathOperation.Modulo );
    }
}

public sealed class GradientOffsetConverterTests
{
    [StaFact]
    public void GradientOffsetLeftConverter_ValidValues_ReturnsOffset()
    {
        var lConverter = new GradientOffsetLeftConverter();

        var lResult = lConverter.Convert( [(object)200.0, (object)100.0], typeof( double ), 20.0, CultureInfo.InvariantCulture );

        Assert.IsType<double>( lResult );
        Assert.True( (double)lResult >= 0.0 );
    }

    [StaFact]
    public void GradientOffsetLeftConverter_InvalidValues_ReturnsZero()
    {
        var lConverter = new GradientOffsetLeftConverter();

        var lResult = lConverter.Convert( [(object)"bad"], typeof( double ), 20.0, CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void GradientOffsetRightConverter_ValidValues_ReturnsOffset()
    {
        var lConverter = new GradientOffsetRightConverter();

        var lResult = lConverter.Convert( [(object)200.0, (object)50.0, (object)150.0], typeof( double ), 20.0, CultureInfo.InvariantCulture );

        Assert.IsType<double>( lResult );
    }

    [StaFact]
    public void GradientOffsetRightConverter_InvalidValues_ReturnsOne()
    {
        var lConverter = new GradientOffsetRightConverter();

        var lResult = lConverter.Convert( [(object)"bad"], typeof( double ), 20.0, CultureInfo.InvariantCulture );

        Assert.Equal( 1.0, lResult );
    }

    [StaFact]
    public void GradientOffsetTopConverter_ValidValues_ReturnsOffset()
    {
        var lConverter = new GradientOffsetTopConverter();

        var lResult = lConverter.Convert( [(object)200.0, (object)100.0], typeof( double ), 20.0, CultureInfo.InvariantCulture );

        Assert.IsType<double>( lResult );
    }

    [StaFact]
    public void GradientOffsetTopConverter_InvalidValues_ReturnsZero()
    {
        var lConverter = new GradientOffsetTopConverter();

        var lResult = lConverter.Convert( [(object)"bad"], typeof( double ), 20.0, CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void GradientOffsetBottomConverter_ValidValues_ReturnsOffset()
    {
        var lConverter = new GradientOffsetBottomConverter();

        var lResult = lConverter.Convert( [(object)200.0, (object)50.0, (object)150.0], typeof( double ), 20.0, CultureInfo.InvariantCulture );

        Assert.IsType<double>( lResult );
    }

    [StaFact]
    public void GradientOffsetBottomConverter_InvalidValues_ReturnsOne()
    {
        var lConverter = new GradientOffsetBottomConverter();

        var lResult = lConverter.Convert( [(object)"bad"], typeof( double ), 20.0, CultureInfo.InvariantCulture );

        Assert.Equal( 1.0, lResult );
    }

    [StaFact]
    public void GradientOffsetConverterBase_ConvertBack_ThrowsNotImplementedException()
    {
        var lConverter = new GradientOffsetLeftConverter();

        Assert.Throws<NotImplementedException>( () =>
            lConverter.ConvertBack( 0.5, [typeof( double )], null!, CultureInfo.InvariantCulture ) );
    }
}

public sealed class TopOffsetConverterTests
{
    [StaFact]
    public void Convert_NullValues_ReturnsZero()
    {
        var lConverter = new TopOffsetConverter();

        var lResult = lConverter.Convert( null, typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void Convert_LessThan2Values_ReturnsZero()
    {
        var lConverter = new TopOffsetConverter();

        var lResult = lConverter.Convert( [(object)"only one"], typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void Convert_WrongTypes_ReturnsZero()
    {
        var lConverter = new TopOffsetConverter();

        var lResult = lConverter.Convert( [(object)"notgrid", (object)"notpath"], typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var lConverter = new TopOffsetConverter();

        Assert.Throws<NotImplementedException>( () =>
            lConverter.ConvertBack( 0.5, [typeof( double )], null!, CultureInfo.InvariantCulture ) );
    }
}

public sealed class BottomOffsetConverterTests
{
    [StaFact]
    public void Convert_NullValues_ReturnsOne()
    {
        var lConverter = new BottomOffsetConverter();

        var lResult = lConverter.Convert( null, typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 1.0, lResult );
    }

    [StaFact]
    public void Convert_WrongTypes_ReturnsOne()
    {
        var lConverter = new BottomOffsetConverter();

        var lResult = lConverter.Convert( [(object)"a", (object)"b"], typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 1.0, lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var lConverter = new BottomOffsetConverter();

        Assert.Throws<NotImplementedException>( () =>
            lConverter.ConvertBack( 0.5, [typeof( double )], null!, CultureInfo.InvariantCulture ) );
    }
}

public sealed class LeftOffsetConverterTests
{
    [StaFact]
    public void Convert_NullValues_ReturnsZero()
    {
        var lConverter = new LeftOffsetConverter();

        var lResult = lConverter.Convert( null, typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void Convert_WrongTypes_ReturnsZero()
    {
        var lConverter = new LeftOffsetConverter();

        var lResult = lConverter.Convert( [(object)"a", (object)"b"], typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 0.0, lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var lConverter = new LeftOffsetConverter();

        Assert.Throws<NotImplementedException>( () =>
            lConverter.ConvertBack( 0.5, [typeof( double )], null!, CultureInfo.InvariantCulture ) );
    }
}

public sealed class RightOffsetConverterTests
{
    [StaFact]
    public void Convert_NullValues_ReturnsOne()
    {
        var lConverter = new RightOffsetConverter();

        var lResult = lConverter.Convert( null, typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 1.0, lResult );
    }

    [StaFact]
    public void Convert_WrongTypes_ReturnsOne()
    {
        var lConverter = new RightOffsetConverter();

        var lResult = lConverter.Convert( [(object)"a", (object)"b"], typeof( double ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( 1.0, lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var lConverter = new RightOffsetConverter();

        Assert.Throws<NotImplementedException>( () =>
            lConverter.ConvertBack( 0.5, [typeof( double )], null!, CultureInfo.InvariantCulture ) );
    }
}
