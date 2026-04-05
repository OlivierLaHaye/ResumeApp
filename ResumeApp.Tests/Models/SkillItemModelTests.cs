using ResumeApp.Models;
using Xunit;

namespace ResumeApp.Tests.Models;

public sealed class SkillItemModelTests
{
    [Fact]
    public void Constructor_WithKey_SetsSkillNameResourceKey()
    {
        var lModel = new SkillItemModel( "TestKey" );

        Assert.Equal( "TestKey", lModel.SkillNameResourceKey );
    }

    [Fact]
    public void Constructor_WithNull_DefaultsToEmpty()
    {
        var lModel = new SkillItemModel( null );

        Assert.Equal( string.Empty, lModel.SkillNameResourceKey );
    }
}
