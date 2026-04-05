using System.Windows.Threading;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ResumeApp.Tests;

[AttributeUsage( AttributeTargets.Method, AllowMultiple = false )]
[XunitTestCaseDiscoverer( "ResumeApp.Tests.StaFactDiscoverer", "ResumeApp.Tests" )]
public sealed class StaFactAttribute : FactAttribute;

public sealed class StaFactDiscoverer( IMessageSink pDiagnosticMessageSink )
    : IXunitTestCaseDiscoverer
{
    private readonly IMessageSink mDiagnosticMessageSink = pDiagnosticMessageSink;

    public IEnumerable<IXunitTestCase> Discover( ITestFrameworkDiscoveryOptions pDiscoveryOptions,
        ITestMethod pTestMethod, IAttributeInfo pFactAttribute )
    {
        yield return new StaTestCase( mDiagnosticMessageSink, pDiscoveryOptions.MethodDisplayOrDefault(),
            pDiscoveryOptions.MethodDisplayOptionsOrDefault(), pTestMethod );
    }
}

public sealed class StaTestCase : XunitTestCase
{
    [Obsolete( "Called by the deserializer; should only be called by deriving classes for de-serialization purposes" )]
    public StaTestCase() { }

    public StaTestCase( IMessageSink pDiagnosticMessageSink, TestMethodDisplay pDefaultMethodDisplay,
        TestMethodDisplayOptions pDefaultMethodDisplayOptions, ITestMethod pTestMethod,
        object[]? pTestMethodArguments = null )
        : base( pDiagnosticMessageSink, pDefaultMethodDisplay, pDefaultMethodDisplayOptions, pTestMethod, pTestMethodArguments )
    {
    }

    public override Task<RunSummary> RunAsync( IMessageSink pDiagnosticMessageSink,
        IMessageBus pMessageBus, object[] pConstructorArguments, ExceptionAggregator pAggregator,
        CancellationTokenSource pCancellationTokenSource )
    {
        var lTaskCompletionSource = new TaskCompletionSource<RunSummary>();

        var lStaThread = new Thread( () =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext() );

                RunSummary lResult = base.RunAsync( pDiagnosticMessageSink, pMessageBus,
                    pConstructorArguments, pAggregator, pCancellationTokenSource ).GetAwaiter().GetResult();

                lTaskCompletionSource.SetResult( lResult );
            }
            catch ( Exception lException )
            {
                lTaskCompletionSource.SetException( lException );
            }
        } );

        lStaThread.SetApartmentState( ApartmentState.STA );
        lStaThread.IsBackground = true;
        lStaThread.Start();

        return lTaskCompletionSource.Task;
    }
}
