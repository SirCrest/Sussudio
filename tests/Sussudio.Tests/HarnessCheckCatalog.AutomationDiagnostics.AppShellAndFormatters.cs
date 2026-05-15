using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsAppShellAndFormatterChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "App wires recoverable and fatal unhandled exception policy",
            App_Xaml_WiresUnhandledExceptionPolicy);
        await AddCheckAsync(results,
            "Bool converters preserve inversion and visibility mappings",
            BoolConverters_PreserveInversionAndVisibilityMappings);
        await AddCheckAsync(results,
            "Display formatters map source HDR states",
            DisplayFormatters_FormatSourceHdr_MapsKnownAndUnknownStates);
        await AddCheckAsync(results,
            "Logging JSON context serializes structured snapshot payloads",
            LoggingJsonContext_SerializesStructuredSnapshotPayloads);
        await AddCheckAsync(results,
            "UI automation commands are not blocked on device readiness",
            UiAutomationCommands_AreNotBlockedOnDeviceReadiness);
    }
}
