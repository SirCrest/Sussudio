using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Tools;

public static partial class AutomationCommandCatalog
{
    private static void RegisterUiEntries(Dictionary<AutomationCommandKind, AutomationCommandMetadata> entries)
    {
        Set(entries, AutomationCommandKind.ArmClose, "{ armed?: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "window close --arm", "Arm the next window close command.", Optional("armed", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.WindowAction, "{ action?: string, x?: int, y?: int, width?: int, height?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "window <action>", "Control application window state and placement.", Optional("action", AutomationPayloadFieldType.String), Optional("x", AutomationPayloadFieldType.Integer), Optional("y", AutomationPayloadFieldType.Integer), Optional("width", AutomationPayloadFieldType.Integer), Optional("height", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.SetShowAllCaptureOptions, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set show-all on|off", "Show or hide advanced capture options.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetPreviewVolume, "{ previewVolumePercent: double }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set volume <value>", "Set preview audio monitor volume.", Required("previewVolumePercent", AutomationPayloadFieldType.Number));
        Set(entries, AutomationCommandKind.SetStatsVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "stats show|hide", "Show or hide stats panel.", Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetStatsSectionVisible, "{ section: string, visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "stats section <name> show|hide", "Show or hide a stats section.", Required("section", AutomationPayloadFieldType.String), Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetSettingsVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "settings show|hide", "Show or hide settings panel.", Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetFrameTimeOverlayVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "frametime show|hide", "Show or hide the frametime graph overlay.", Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetFlashbackTimelineVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback timeline show|hide", "Show or hide the Flashback timeline UI.", Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetFullScreenEnabled, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "window fullscreen on|off", "Enter or exit full-screen mode.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.OpenRecordingsFolder, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "recordings open", "Open the current recordings output folder.");
    }
}
