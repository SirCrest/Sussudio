using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static AutomationSnapshot BuildAutomationSnapshotFromProjections(
        AutomationSnapshotProjectionSet projections)
    {
        var flattened = BuildAutomationSnapshotFlattenedProjectionSet(projections);
        return BuildAutomationSnapshotFromFlattenedProjections(flattened);
    }
}
