static partial class Program
{
    private static void AssertDiagnosticsRefreshCoreOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertDiagnosticsRefreshEvaluationOwnership(diagnostics);
        AssertDiagnosticsRefreshRuntimeOwnership(diagnostics);
        AssertDiagnosticsRefreshSnapshotConstructionOwnership(diagnostics);
    }
}
