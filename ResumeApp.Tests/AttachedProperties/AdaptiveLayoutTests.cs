using System.Windows.Controls;
using ResumeApp.AttachedProperties;
using Xunit;

namespace ResumeApp.Tests.AttachedProperties;

public sealed class AdaptiveLayoutTests
{
    [Theory]
    [InlineData( 0.0, LayoutSizeClass.Compact )]
    [InlineData( 960.0, LayoutSizeClass.Compact )]
    [InlineData( 1099.9, LayoutSizeClass.Compact )]
    [InlineData( 1100.0, LayoutSizeClass.Regular )]
    [InlineData( 1599.9, LayoutSizeClass.Regular )]
    [InlineData( 1600.0, LayoutSizeClass.Wide )]
    [InlineData( 3840.0, LayoutSizeClass.Wide )]
    [InlineData( double.NaN, LayoutSizeClass.Compact )]
    public void GetSizeClassForWidth_UsesDocumentedBreakpoints( double pWidth, LayoutSizeClass pExpected )
    {
        Assert.Equal( pExpected, AdaptiveLayout.GetSizeClassForWidth( pWidth ) );
    }

    [StaFact]
    public void SizeClass_IsInheritedByDescendants()
    {
        var lChild = new Border();
        var lParent = new Grid();
        lParent.Children.Add( lChild );

        AdaptiveLayout.SetSizeClass( lParent, LayoutSizeClass.Wide );

        Assert.Equal( LayoutSizeClass.Wide, AdaptiveLayout.GetSizeClass( lChild ) );
    }

    [StaFact]
    public void SizeClass_DefaultIsRegular()
    {
        Assert.Equal( LayoutSizeClass.Regular, AdaptiveLayout.GetSizeClass( new Border() ) );
    }
}
