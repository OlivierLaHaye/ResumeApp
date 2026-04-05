using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using ResumeApp.Converters;
using Xunit;

namespace ResumeApp.Tests.Converters;

public sealed class BoolToCollapseConverterTests
{
    [StaFact]
    public void Convert_True_ReturnsVisible()
    {
        var lConverter = new BoolToCollapseConverter();

        var lResult = lConverter.Convert( true, typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_False_ReturnsCollapsed()
    {
        var lConverter = new BoolToCollapseConverter();

        var lResult = lConverter.Convert( false, typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NonBool_ReturnsUnsetValue()
    {
        var lConverter = new BoolToCollapseConverter();

        var lResult = lConverter.Convert( "notabool", typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( DependencyProperty.UnsetValue, lResult );
    }

    [StaFact]
    public void Convert_CustomTrueAndFalseValues()
    {
        var lConverter = new BoolToCollapseConverter
        {
            TrueValue = Visibility.Hidden,
            FalseValue = Visibility.Visible
        };

        Assert.Equal( Visibility.Hidden, lConverter.Convert( true, typeof( Visibility ), null!, CultureInfo.InvariantCulture ) );
        Assert.Equal( Visibility.Visible, lConverter.Convert( false, typeof( Visibility ), null!, CultureInfo.InvariantCulture ) );
    }

    [StaFact]
    public void ConvertBack_MatchesTrueValue_ReturnsTrue()
    {
        var lConverter = new BoolToCollapseConverter();

        var lResult = lConverter.ConvertBack( Visibility.Visible, typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( true, lResult );
    }

    [StaFact]
    public void ConvertBack_MatchesFalseValue_ReturnsFalse()
    {
        var lConverter = new BoolToCollapseConverter();

        var lResult = lConverter.ConvertBack( Visibility.Collapsed, typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( false, lResult );
    }

    [StaFact]
    public void ConvertBack_MatchesNeither_ReturnsUnsetValue()
    {
        var lConverter = new BoolToCollapseConverter();

        var lResult = lConverter.ConvertBack( Visibility.Hidden, typeof( bool ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( DependencyProperty.UnsetValue, lResult );
    }
}

public sealed class EnumToVisibilityConverterTests
{
    private enum TestEnum { Alpha, Beta, Gamma }

    [StaFact]
    public void Convert_MatchingEnum_ReturnsVisible()
    {
        var lConverter = new EnumToVisibilityConverter();

        var lResult = lConverter.Convert( TestEnum.Alpha, typeof( Visibility ), "Alpha", CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_NonMatchingEnum_ReturnsCollapsed()
    {
        var lConverter = new EnumToVisibilityConverter();

        var lResult = lConverter.Convert( TestEnum.Alpha, typeof( Visibility ), "Beta", CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_MultipleValuesWithPipe_MatchesAny()
    {
        var lConverter = new EnumToVisibilityConverter();

        var lResult = lConverter.Convert( TestEnum.Beta, typeof( Visibility ), "Alpha|Beta", CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_NullValue_ReturnsFalseValue()
    {
        var lConverter = new EnumToVisibilityConverter();

        var lResult = lConverter.Convert( null, typeof( Visibility ), "Alpha", CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NullParameter_ReturnsFalseValue()
    {
        var lConverter = new EnumToVisibilityConverter();

        var lResult = lConverter.Convert( TestEnum.Alpha, typeof( Visibility ), null, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_EmptyParameter_ReturnsFalseValue()
    {
        var lConverter = new EnumToVisibilityConverter();

        var lResult = lConverter.Convert( TestEnum.Alpha, typeof( Visibility ), "", CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }
}

public sealed class IsNullToCollapseConverterTests
{
    [StaFact]
    public void Convert_NonNull_ReturnsVisible()
    {
        var lConverter = new IsNullToCollapseConverter();

        var lResult = lConverter.Convert( "something", typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_Null_ReturnsCollapsed()
    {
        var lConverter = new IsNullToCollapseConverter();

        var lResult = lConverter.Convert( null, typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }
}

public sealed class StringNullOrEmptyToVisibilityConverterTests
{
    [StaFact]
    public void Convert_NonEmptyString_ReturnsVisible()
    {
        var lConverter = new StringNullOrEmptyToVisibilityConverter();

        var lResult = lConverter.Convert( "hello", typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_EmptyString_ReturnsCollapsed()
    {
        var lConverter = new StringNullOrEmptyToVisibilityConverter();

        var lResult = lConverter.Convert( "", typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_Null_ReturnsCollapsed()
    {
        var lConverter = new StringNullOrEmptyToVisibilityConverter();

        var lResult = lConverter.Convert( null!, typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NonString_ReturnsCollapsed()
    {
        var lConverter = new StringNullOrEmptyToVisibilityConverter();

        var lResult = lConverter.Convert( 42, typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }
}

public sealed class StringToVisibilityConverterTests
{
    [StaFact]
    public void Convert_MatchingStrings_ReturnsVisible()
    {
        var lConverter = new StringToVisibilityConverter();

        var lResult = lConverter.Convert( "hello", typeof( Visibility ), "hello", CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_CaseInsensitiveMatch_ReturnsVisible()
    {
        var lConverter = new StringToVisibilityConverter();

        var lResult = lConverter.Convert( "Hello", typeof( Visibility ), "hello", CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_NonMatchingStrings_ReturnsCollapsed()
    {
        var lConverter = new StringToVisibilityConverter();

        var lResult = lConverter.Convert( "hello", typeof( Visibility ), "world", CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NonStringValue_ReturnsUnsetValue()
    {
        var lConverter = new StringToVisibilityConverter();

        var lResult = lConverter.Convert( 42, typeof( Visibility ), "42", CultureInfo.InvariantCulture );

        Assert.Equal( DependencyProperty.UnsetValue, lResult );
    }

    [StaFact]
    public void Convert_NonStringParameter_ReturnsUnsetValue()
    {
        var lConverter = new StringToVisibilityConverter();

        var lResult = lConverter.Convert( "hello", typeof( Visibility ), 42, CultureInfo.InvariantCulture );

        Assert.Equal( DependencyProperty.UnsetValue, lResult );
    }
}

public sealed class CollectionToVisibilityConverterTests
{
    [StaFact]
    public void Convert_NonEmptyCollection_ReturnsVisible()
    {
        var lConverter = new CollectionToVisibilityConverter();

        var lResult = lConverter.Convert( new List<int> { 1, 2, 3 }, typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_EmptyCollection_ReturnsCollapsed()
    {
        var lConverter = new CollectionToVisibilityConverter();

        var lResult = lConverter.Convert( new List<int>(), typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NonCollection_ReturnsUnsetValue()
    {
        var lConverter = new CollectionToVisibilityConverter();

        var lResult = lConverter.Convert( "notacollection", typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( DependencyProperty.UnsetValue, lResult );
    }
}

public sealed class AnyNotNullOrEmptyToVisibilityConverterTests
{
    [StaFact]
    public void Convert_AnyNotNullOrEmpty_ReturnsVisible()
    {
        var lConverter = new AnyNotNullOrEmptyToVisibilityConverter();

        var lResult = lConverter.Convert( [ null!, "", "hello" ], typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_AllNullOrEmpty_ReturnsCollapsed()
    {
        var lConverter = new AnyNotNullOrEmptyToVisibilityConverter();

        var lResult = lConverter.Convert( [ null!, "" ], typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var lConverter = new AnyNotNullOrEmptyToVisibilityConverter();

        Assert.Throws<NotImplementedException>( () => lConverter.ConvertBack( Visibility.Visible, [typeof( object )], null!, CultureInfo.InvariantCulture ) );
    }
}

public sealed class BoolOrToVisibilityConverterTests
{
    [StaFact]
    public void Convert_AnyTrue_ReturnsVisible()
    {
        var lConverter = new BoolOrToVisibilityConverter();

        var lResult = lConverter.Convert( [(object)false, (object)true], typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_AllFalse_ReturnsCollapsed()
    {
        var lConverter = new BoolOrToVisibilityConverter();

        var lResult = lConverter.Convert( [(object)false, (object)false], typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NullValues_ReturnsCollapsed()
    {
        var lConverter = new BoolOrToVisibilityConverter();

        var lResult = lConverter.Convert( null, typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NonBoolValues_ReturnsCollapsed()
    {
        var lConverter = new BoolOrToVisibilityConverter();

        var lResult = lConverter.Convert( [(object)"notbool"], typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }
}

public sealed class BoolAndToVisibilityConverterTests
{
    [StaFact]
    public void Convert_AllTrue_ReturnsVisible()
    {
        var lConverter = new BoolAndToVisibilityConverter();

        var lResult = lConverter.Convert( [(object)true, (object)true], typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Visible, lResult );
    }

    [StaFact]
    public void Convert_AnyFalse_ReturnsCollapsed()
    {
        var lConverter = new BoolAndToVisibilityConverter();

        var lResult = lConverter.Convert( [(object)true, (object)false], typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NullValues_ReturnsCollapsed()
    {
        var lConverter = new BoolAndToVisibilityConverter();

        var lResult = lConverter.Convert( null, typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }

    [StaFact]
    public void Convert_NonBoolValues_ReturnsCollapsed()
    {
        var lConverter = new BoolAndToVisibilityConverter();

        var lResult = lConverter.Convert( [(object)42], typeof( Visibility ), null!, CultureInfo.InvariantCulture );

        Assert.Equal( Visibility.Collapsed, lResult );
    }
}
