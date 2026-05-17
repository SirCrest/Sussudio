using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_WaitAndAssertCommands_LiveWithSupportOwners()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var waitConditionsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.WaitConditions.cs")
            .Replace("\r\n", "\n");
        var assertionsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.WaitForCondition:");
        AssertContains(customCommandsText, "ExecuteWaitForConditionCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.AssertSnapshot:");
        AssertContains(customCommandsText, "ExecuteAssertSnapshotCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "ParseWaitCondition(payload)");
        AssertDoesNotContain(customCommandsText, "WaitForConditionAsync(condition, timeoutMs, pollMs, cancellationToken)");
        AssertDoesNotContain(customCommandsText, "ParseAssertions(payload)");
        AssertDoesNotContain(customCommandsText, "TryEvaluateAssertion(snapshot, assertion, out var failure)");

        AssertContains(waitConditionsText, "private async Task<AutomationCommandResponse> ExecuteWaitForConditionCommandAsync(");
        AssertContains(waitConditionsText, "var condition = ParseWaitCondition(payload);");
        AssertContains(waitConditionsText, "Math.Clamp(GetInt(payload, \"timeoutMs\") ?? DefaultWaitTimeoutMs, 250, 300_000)");
        AssertContains(waitConditionsText, "WaitForConditionAsync(condition, timeoutMs, pollMs, cancellationToken)");
        AssertContains(waitConditionsText, "errorCode: met ? null : \"timeout\"");
        AssertContains(waitConditionsText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync(");
        AssertContains(waitConditionsText, "private static bool ConditionSatisfied(");

        AssertContains(assertionsText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(assertionsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(assertionsText, "var assertions = ParseAssertions(payload);");
        AssertContains(assertionsText, "TryEvaluateAssertion(snapshot, assertion, out var failure)");
        AssertContains(assertionsText, "errorCode: passed ? null : \"assertion-failed\"");
        AssertContains(assertionsText, "private static List<SnapshotAssertion> ParseAssertions(");
        AssertContains(assertionsText, "private static bool TryEvaluateAssertion(");

        return Task.CompletedTask;
    }
}
