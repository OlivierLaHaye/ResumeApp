using System.Windows;
using System.Windows.Controls;
using ResumeApp.AttachedProperties;
using Xunit;

namespace ResumeApp.Tests.AttachedProperties;

public sealed class CornerRadiusAssistTests
{
    [StaFact]
    public void sCornerRadiusProperty_IsRegistered()
    {
        Assert.NotNull( CornerRadiusAssist.sCornerRadiusProperty );
    }

    [StaFact]
    public void SetsCornerRadius_AndGetsCornerRadius_RoundTrips()
    {
        var lElement = new Border();
        var lCornerRadius = new CornerRadius( 10, 20, 30, 40 );

        CornerRadiusAssist.SetsCornerRadius( lElement, lCornerRadius );
        var lResult = CornerRadiusAssist.GetsCornerRadius( lElement );

        Assert.Equal( lCornerRadius, lResult );
    }

    [StaFact]
    public void DefaultValue_IsDefaultCornerRadius()
    {
        var lElement = new Border();

        var lResult = CornerRadiusAssist.GetsCornerRadius( lElement );

        Assert.Equal( default( CornerRadius ), lResult );
    }
}
