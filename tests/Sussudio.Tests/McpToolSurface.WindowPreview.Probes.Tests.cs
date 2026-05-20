using System;
using System.Globalization;
using System.Threading.Tasks;

static partial class Program
{
    internal static async Task McpPreviewColorProbeTool_FormatsProbeResponses()
    {
        var previewColorProbeTool = RequireMcpType("McpServer.Tools.PreviewColorProbeTools");

        var failureText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":false,\"Message\":\"preview unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("preview unavailable", failureText, "probe_preview_color failure message");

        var missingDataText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No probe data returned.", missingDataText, "probe_preview_color missing data");

        var inactiveText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":true,\"Data\":{\"SessionActive\":false}}")
            .ConfigureAwait(false);
        AssertContains(inactiveText, "== Preview Color Probe ==");
        AssertContainsOrdinal(inactiveText, "Session Active: false");
        AssertContains(inactiveText, "No active preview session. Start preview first.");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "SessionActive": true,
                             "RendererMode": "D3D11VideoProcessor",
                             "NegotiatedSubtype": "P010",
                             "SourceWidth": 3840,
                             "SourceHeight": 2160,
                             "SourceFrameRate": 59.94,
                             "NominalRangeLabel": "Full",
                             "NominalRange": 2,
                             "TransferFunctionLabel": "PQ",
                             "TransferFunction": 16,
                             "VideoPrimariesLabel": "BT.2020",
                             "VideoPrimaries": 9,
                             "YuvMatrixLabel": "BT.2020",
                             "YuvMatrix": 9,
                             "D3DInputColorSpace": "BT2020_PQ",
                             "D3DOutputColorSpace": "RGB_Full",
                             "LumaSampleCount": 100,
                             "LumaMin": 0,
                             "LumaMax": 255,
                             "LumaMean": 128.5,
                             "LumaBelow16Count": 5,
                             "LumaAbove235Count": 10,
                             "FormatProperties": {
                               "MF_MT_SUBTYPE": "P010"
                             }
                           }
                         }
                         """;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string activeText;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            activeText = await InvokePreviewColorProbeAsync(previewColorProbeTool, activeJson).ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(activeText, "Renderer: D3D11VideoProcessor");
        AssertContains(activeText, "Format: P010 3840x2160 @ 59.94fps");
        AssertContains(activeText, "== Color Attributes ==");
        AssertContains(activeText, "Nominal Range: Full (raw=2)");
        AssertContains(activeText, "== D3D11 Video Processor ==");
        AssertContains(activeText, "== Luma (Y Plane) Analysis ==");
        AssertContains(activeText, "Diagnosis: Data uses FULL range (0-255). 10.0% super-white, 5.0% super-black.");
        AssertContains(activeText, "== Raw MF Properties ==");
        AssertContains(activeText, "MF_MT_SUBTYPE = P010");
    }

    internal static async Task McpVideoSourceProbeTool_FormatsProbeResponses()
    {
        var videoSourceProbeTool = RequireMcpType("McpServer.Tools.VideoSourceProbeTools");

        var failureText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":false,\"Message\":\"source unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("source unavailable", failureText, "probe_video_source failure message");

        var missingDataText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No probe data returned.", missingDataText, "probe_video_source missing data");

        var inactiveText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":true,\"Data\":{\"SessionActive\":false}}")
            .ConfigureAwait(false);
        AssertContains(inactiveText, "== Video Source Probe ==");
        AssertContainsOrdinal(inactiveText, "Session Active: false");
        AssertContains(inactiveText, "No active ingest session. Start preview first.");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "SessionActive": true,
                             "MemoryPreference": "D3D11",
                             "CurrentSubtype": "P010",
                             "CurrentWidth": 3840,
                             "CurrentHeight": 2160,
                             "CurrentFrameRate": 59.94,
                             "P010Available": true,
                             "Nv12Available": true,
                             "SupportedSubtypes": ["P010", "NV12", ""],
                             "TotalFormatCount": 2,
                             "Formats": [
                               { "Summary": "3840x2160 P010 59.94fps" },
                               { "Summary": "1920x1080 NV12 60fps" }
                             ]
                           }
                         }
                         """;
        var activeText = await InvokeVideoSourceProbeAsync(videoSourceProbeTool, activeJson).ConfigureAwait(false);
        AssertContains(activeText, "Memory Preference: D3D11");
        AssertContains(activeText, "Current Format: P010 3840x2160@59.94fps");
        AssertContainsOrdinal(activeText, "P010 Available: true | NV12 Available: true");
        AssertContains(activeText, "Supported Subtypes: P010, NV12");
        AssertContains(activeText, "Total Format Count: 2");
        AssertContains(activeText, "== Format Table ==");
        AssertContains(activeText, "[0] 3840x2160 P010 59.94fps");
        AssertContains(activeText, "[1] 1920x1080 NV12 60fps");
    }

    private static async Task<string> InvokePreviewColorProbeAsync(Type previewColorProbeTool, string responseJson)
    {
        var pipeName = NewMcpToolPipeName("color");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewColorProbeTool,
                            "probe_preview_color",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "ProbePreviewColor");
        return result;
    }

    private static async Task<string> InvokeVideoSourceProbeAsync(Type videoSourceProbeTool, string responseJson)
    {
        var pipeName = NewMcpToolPipeName("source");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            videoSourceProbeTool,
                            "probe_video_source",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "ProbeVideoSource");
        return result;
    }
}
