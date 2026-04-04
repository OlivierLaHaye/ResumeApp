using ResumeApp.Infrastructure;
using Xunit;

namespace ResumeApp.Tests.Infrastructure;

public sealed class PropertyChangedNotifierTests
{
    private sealed class TestNotifier : PropertyChangedNotifier
    {
        private string mName = string.Empty;
        public string Name
        {
            get => mName;
            set => SetProperty( ref mName, value );
        }

        private int mAge;
        public int Age
        {
            get => mAge;
            set => SetProperty( ref mAge, value );
        }

        public void RaiseManually( string? pPropertyName = null )
        {
            RaisePropertyChanged( pPropertyName );
        }

        public bool SetPropertyDirect<T>( ref T pStorage, T pValue, string? pPropertyName = null )
        {
            return SetProperty( ref pStorage, pValue, pPropertyName );
        }
    }

    [Fact]
    public void SetProperty_WhenValueChanges_RaisesPropertyChanged()
    {
        var lNotifier = new TestNotifier();
        string? lRaisedPropertyName = null;
        lNotifier.PropertyChanged += ( _, pArgs ) => lRaisedPropertyName = pArgs.PropertyName;

        lNotifier.Name = "Test";

        Assert.Equal( "Name", lRaisedPropertyName );
        Assert.Equal( "Test", lNotifier.Name );
    }

    [Fact]
    public void SetProperty_WhenValueIsSame_DoesNotRaisePropertyChanged()
    {
        var lNotifier = new TestNotifier { Name = "Test" };
        bool lWasRaised = false;
        lNotifier.PropertyChanged += ( _, _ ) => lWasRaised = true;

        lNotifier.Name = "Test";

        Assert.False( lWasRaised );
    }

    [Fact]
    public void SetProperty_ReturnsTrueWhenChanged()
    {
        var lNotifier = new TestNotifier();
        int lStorage = 0;

        bool lResult = lNotifier.SetPropertyDirect( ref lStorage, 42, "Test" );

        Assert.True( lResult );
        Assert.Equal( 42, lStorage );
    }

    [Fact]
    public void SetProperty_ReturnsFalseWhenNotChanged()
    {
        var lNotifier = new TestNotifier();
        int lStorage = 42;

        bool lResult = lNotifier.SetPropertyDirect( ref lStorage, 42, "Test" );

        Assert.False( lResult );
    }

    [Fact]
    public void RaisePropertyChanged_ManuallyRaisesEvent()
    {
        var lNotifier = new TestNotifier();
        string? lRaisedPropertyName = null;
        lNotifier.PropertyChanged += ( _, pArgs ) => lRaisedPropertyName = pArgs.PropertyName;

        lNotifier.RaiseManually( "CustomProperty" );

        Assert.Equal( "CustomProperty", lRaisedPropertyName );
    }

    [Fact]
    public void RaisePropertyChanged_WithNullPropertyName_RaisesWithNull()
    {
        var lNotifier = new TestNotifier();
        string? lRaisedPropertyName = "initial";
        lNotifier.PropertyChanged += ( _, pArgs ) => lRaisedPropertyName = pArgs.PropertyName;

        lNotifier.RaiseManually( null );

        Assert.Null( lRaisedPropertyName );
    }

    [Fact]
    public void SetProperty_WithNoSubscribers_DoesNotThrow()
    {
        var lNotifier = new TestNotifier();

        var lException = Record.Exception( () => lNotifier.Name = "Test" );

        Assert.Null( lException );
    }

    [Fact]
    public void SetProperty_IntValueChange_RaisesPropertyChanged()
    {
        var lNotifier = new TestNotifier();
        string? lRaisedPropertyName = null;
        lNotifier.PropertyChanged += ( _, pArgs ) => lRaisedPropertyName = pArgs.PropertyName;

        lNotifier.Age = 25;

        Assert.Equal( "Age", lRaisedPropertyName );
        Assert.Equal( 25, lNotifier.Age );
    }

    [Fact]
    public void SetProperty_MultipleSubscribers_AllNotified()
    {
        var lNotifier = new TestNotifier();
        int lCount = 0;
        lNotifier.PropertyChanged += ( _, _ ) => lCount++;
        lNotifier.PropertyChanged += ( _, _ ) => lCount++;

        lNotifier.Name = "Test";

        Assert.Equal( 2, lCount );
    }
}
