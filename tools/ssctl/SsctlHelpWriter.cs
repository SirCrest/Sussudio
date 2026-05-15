using System.IO;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

// Owns the operator-facing ssctl help text. Program decides when help is shown;
// this module keeps the catalog-backed command surface in one place.
internal static class SsctlHelpWriter
{
    internal static void Write(TextWriter writer)
    {
        writer.WriteLine("ssctl");
        writer.WriteLine("Usage:");
        writer.WriteLine("  ssctl [--json] [--pipe NAME] [--timeout MS] <command>");
        writer.WriteLine();
        writer.WriteLine("Query:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetSnapshot, "[--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetDiagnostics, "[--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetCaptureOptions, "[--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetAutomationManifest, "[--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetPerformanceTimeline, "[--json]");
        writer.WriteLine("  memory [--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetAudioRampTrace, "[--json]");
        writer.WriteLine("  presentmon [--seconds N] [--pid PID|--process NAME] [--swapchain HEX] [--app-present-id N] [--app-source-seq N] [--app-present-utc-ms N] [--capture-start-utc-ms N] [--presentmon PATH] [--output PATH] [--keep-csv] [--json]");
        writer.WriteLine($"  diagnostic-session [--scenario {DiagnosticSessionScenarios.HelpList}] [--seconds N] [--sample-ms N] [--output PATH] [--presentmon] [--presentmon-path PATH] [--verify] [--leave-running] [--json]");
        writer.WriteLine();
        writer.WriteLine("Control:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetPreviewEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetRecordingEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.CaptureWindowScreenshot);
        WriteCatalogHelpLine(writer, AutomationCommandKind.CapturePreviewFrame);
        WriteCatalogHelpLine(writer, AutomationCommandKind.OpenRecordingsFolder);
        writer.WriteLine();
        writer.WriteLine("Configure:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetResolution);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFrameRate);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetRecordingFormat);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetQuality);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetCustomBitrate);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetPreset);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetSplitEncodeMode);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetVideoFormat);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetMjpegDecoderCount);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetHdrEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetTrueHdrPreviewEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetAudioEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetAudioPreviewEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetPreviewVolume);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetDeviceAudioMode);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetAnalogAudioGain);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetOutputPath);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetShowAllCaptureOptions);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetMicrophoneEnabled);
        writer.WriteLine();
        writer.WriteLine("Device:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.RefreshDevices);
        writer.WriteLine("  device list");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SelectDevice);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SelectAudioInputDevice);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetCustomAudioInput);
        writer.WriteLine();
        writer.WriteLine("Flashback:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFlashbackEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFlashbackTimelineVisible);
        writer.WriteLine("  flashback play [<ms>]");
        writer.WriteLine("  flashback pause");
        writer.WriteLine("  flashback go-live");
        writer.WriteLine("  flashback seek <ms>");
        writer.WriteLine("  flashback begin-scrub <ms>");
        writer.WriteLine("  flashback update-scrub <ms>");
        writer.WriteLine("  flashback end-scrub [<ms>]");
        writer.WriteLine("  flashback set-in|set-out|clear-range");
        WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackExport);
        WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackGetSegments);
        WriteCatalogHelpLine(writer, AutomationCommandKind.RestartFlashback);
        writer.WriteLine();
        writer.WriteLine("Window:");
        writer.WriteLine("  window close|minimize|maximize|restore|center");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFullScreenEnabled);
        writer.WriteLine("  window snap left|right|top-left|top-right|bottom-left|bottom-right");
        writer.WriteLine("  window move <x> <y>");
        writer.WriteLine("  window resize <w> <h>");
        writer.WriteLine();
        writer.WriteLine("Wait / Verify:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.WaitForCondition, "[--timeout MS] [--poll MS]");
        writer.WriteLine("  verify [path] [--profile NAME|--verification-profile NAME]");
        writer.WriteLine("  assert <json>|<field> <op> <value>");
        writer.WriteLine("  probe source|color");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetStatsVisible);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetStatsSectionVisible);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFrameTimeOverlayVisible);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetSettingsVisible);
        writer.WriteLine();
        writer.WriteLine("Flags:");
        writer.WriteLine("  --json            Print raw JSON responses where supported");
        writer.WriteLine("  --pipe NAME       Named pipe (default: SussudioAutomation)");
        writer.WriteLine("  --timeout MS      Response timeout override for pipe calls");
        writer.WriteLine("  --verbose         On error, print full stack trace + InnerException chain to stderr");
        writer.WriteLine("  --help            Show this help");
    }

    private static void WriteCatalogHelpLine(TextWriter writer, AutomationCommandKind kind, string? suffix = null)
    {
        var command = AutomationCommandCatalog.Get(kind).CliHelp;
        writer.WriteLine(string.IsNullOrWhiteSpace(suffix)
            ? $"  {command}"
            : $"  {command} {suffix}");
    }
}
