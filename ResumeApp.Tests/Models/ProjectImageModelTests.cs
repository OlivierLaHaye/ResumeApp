using ResumeApp.Models;
using Xunit;

namespace ResumeApp.Tests.Models;

public sealed class ProjectImageModelTests
{
    [Fact]
    public void Constructor_ValidFileName_SetsProperties()
    {
        var lModel = new ProjectImageModel( "image.png", "CaptionKey" );

        Assert.Equal( "image.png", lModel.FileName );
        Assert.Equal( "CaptionKey", lModel.CaptionResourceKey );
    }

    [Fact]
    public void Constructor_NullCaptionResourceKey_DefaultsToEmpty()
    {
        var lModel = new ProjectImageModel( "image.png", null );

        Assert.Equal( string.Empty, lModel.CaptionResourceKey );
    }

    [Fact]
    public void Constructor_NullFileName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>( () => new ProjectImageModel( null!, "key" ) );
    }

    [Fact]
    public void Constructor_EmptyFileName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>( () => new ProjectImageModel( "", "key" ) );
    }

    [Fact]
    public void Constructor_WhitespaceFileName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>( () => new ProjectImageModel( "   ", "key" ) );
    }
}
