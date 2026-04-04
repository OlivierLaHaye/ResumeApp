using System.Collections.ObjectModel;
using ResumeApp.Models;
using Xunit;

namespace ResumeApp.Tests.Models;

public sealed class SkillGroupModelTests
{
    [Fact]
    public void Constructor_WithValues_SetsProperties()
    {
        var lSkills = new ObservableCollection<SkillItemModel> { new( "skill1" ) };
        var lModel = new SkillGroupModel( "GroupTitle", lSkills );

        Assert.Equal( "GroupTitle", lModel.GroupTitleResourceKey );
        Assert.Same( lSkills, lModel.Skills );
    }

    [Fact]
    public void Constructor_NullGroupTitle_DefaultsToEmpty()
    {
        var lModel = new SkillGroupModel( null, null );

        Assert.Equal( string.Empty, lModel.GroupTitleResourceKey );
    }

    [Fact]
    public void Constructor_NullSkills_DefaultsToEmptyCollection()
    {
        var lModel = new SkillGroupModel( "title", null );

        Assert.NotNull( lModel.Skills );
        Assert.Empty( lModel.Skills );
    }
}
