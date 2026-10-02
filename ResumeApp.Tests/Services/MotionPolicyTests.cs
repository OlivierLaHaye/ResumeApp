using System.Windows;
using ResumeApp.Services;
using Xunit;

namespace ResumeApp.Tests.Services;

[CollectionDefinition( Name, DisableParallelization = true )]
public sealed class MotionPolicyCollection
{
    public const string Name = "MotionPolicy";
}

[Collection( MotionPolicyCollection.Name )]
public sealed class MotionPolicyTests
{
    [Fact]
    public void IsAnimationEnabled_WithoutOverride_MatchesSystemSetting()
    {
        MotionPolicy.SetAnimationEnabledOverride( null );

        Assert.Equal( SystemParameters.ClientAreaAnimation, MotionPolicy.IsAnimationEnabled );
    }

    [Fact]
    public void GetDuration_WhenReducedMotion_ReturnsZero()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( false );

            Assert.Equal( TimeSpan.Zero, MotionPolicy.GetDuration( TimeSpan.FromMilliseconds( 220 ) ).TimeSpan );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }

    [Fact]
    public void GetDuration_WhenAnimationEnabled_ReturnsRequestedDuration()
    {
        try
        {
            MotionPolicy.SetAnimationEnabledOverride( true );

            Assert.Equal( TimeSpan.FromMilliseconds( 160 ), MotionPolicy.GetDuration( TimeSpan.FromMilliseconds( 160 ) ).TimeSpan );
        }
        finally
        {
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
    }

    [Fact]
    public void SetAnimationEnabledOverride_RaisesChanged()
    {
        int lChangedCount = 0;
        EventHandler lHandler = ( _, _ ) => lChangedCount++;
        MotionPolicy.Changed += lHandler;

        try
        {
            MotionPolicy.SetAnimationEnabledOverride( false );
            MotionPolicy.SetAnimationEnabledOverride( null );
        }
        finally
        {
            MotionPolicy.Changed -= lHandler;
        }

        Assert.Equal( 2, lChangedCount );
    }

    [Theory]
    [InlineData( nameof( SystemParameters.ClientAreaAnimation ), true )]
    [InlineData( nameof( SystemParameters.MenuAnimation ), true )]
    [InlineData( nameof( SystemParameters.HighContrast ), false )]
    [InlineData( null, false )]
    public void IsMotionRelatedSystemParameter_RecognizesAnimationSettings( string? pPropertyName, bool pExpected )
    {
        Assert.Equal( pExpected, MotionPolicy.IsMotionRelatedSystemParameter( pPropertyName ) );
    }
}
