using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpPreviewFrameCaptureTool_FormatsCaptureResponses()
    {
        var previewFrameCaptureTool = RequireMcpType("McpServer.Tools.PreviewFrameCaptureTools");
        var defaultOutputPath = Path.Combine(Environment.CurrentDirectory, "temp", "preview_capture.bmp");

        var failureText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-failure",
                outputPath: null,
                expectedOutputPath: defaultOutputPath,
                responseJson: "{\"Success\":false,\"Message\":\"preview unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("preview unavailable", failureText, "capture_preview_frame failure message");

        var missingDataText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-missing",
                outputPath: @"C:\captures\missing.bmp",
                expectedOutputPath: @"C:\captures\missing.bmp",
                responseJson: "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No frame capture data returned.", missingDataText, "capture_preview_frame missing data");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "FilePath": "C:\\captures\\preview.bmp",
                             "CapturedWidth": 640,
                             "CapturedHeight": 360,
                             "RendererMode": "D3D11",
                             "AverageR": 10,
                             "AverageG": 20,
                             "AverageB": 30,
                             "AverageLuminance": 25.5,
                             "MinLuminance": 10,
                             "MaxLuminance": 34,
                             "NearBlackPercent": 12.5,
                             "NearWhitePercent": 0,
                             "PureBlackPercent": 96.5,
                             "LetterboxTopRows": 12,
                             "LetterboxBottomRows": 12,
                             "PillarboxLeftCols": 3,
                             "PillarboxRightCols": 4,
                             "ContentWidth": 640,
                             "ContentHeight": 360,
                             "ContentAspectRatio": 1.333,
                             "TotalPixels": 230400,
                             "LuminanceHistogram": [0, 10, 20, 40, 80, 160, 80, 40, 20, 10, 5, 0, 0, 0, 0, 0]
                           }
                         }
                         """;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string activeText;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            activeText = await InvokePreviewFrameCaptureAsync(
                    previewFrameCaptureTool,
                    "preview-frame-active",
                    outputPath: @"C:\captures\preview.bmp",
                    expectedOutputPath: @"C:\captures\preview.bmp",
                    responseJson: activeJson)
                .ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertEqual(
            """
            == Preview Frame Capture ==
            File: C:\captures\preview.bmp
            Resolution: 640 x 360
            Renderer: D3D11

            == Pixel Summary ==
            Average RGB: R=10 G=20 B=30
            Luminance: avg=25.5 min=10 max=34
            Near Black (<16): 12.5%
            Near White (>240): 0%
            Pure Black: 96.5%

            == Framing ==
            Letterbox: top=12 bottom=12 rows
            Pillarbox: left=3 right=4 cols
            Content Area: 640 x 360
            Content Aspect Ratio: 1.333
            Total Pixels: 230400

            == Luminance Histogram (16 bins) ==
              0- 15:  (0)
             16- 31: ## (10)
             32- 47: ### (20)
             48- 63: ###### (40)
             64- 79: ############ (80)
             80- 95: ######################## (160)
             96-111: ############ (80)
            112-127: ###### (40)
            128-143: ### (20)
            144-159: ## (10)
            160-175: # (5)
            176-191:  (0)
            192-207:  (0)
            208-223:  (0)
            224-239:  (0)
            240-255:  (0)

            == Diagnosis ==
            - BLANK FRAME: >95% of pixels are pure black.
            - VERY DARK: average luminance is below 30.
            - LETTERBOXED: top=12, bottom=12, estimated source aspect=1.333 (640x360).
            - PILLARBOXED: left=3, right=4, estimated source aspect=1.333 (640x360).
            - LOW CONTRAST: luminance range is under 30.
            - ASPECT RATIO ALERT: content aspect 1.333 is not close to 16:9 or 16:10.
            """,
            activeText.Replace("\r\n", "\n"),
            "capture_preview_frame exact report");

        var noAnomalyJson = """
                            {
                              "Success": true,
                              "Data": {
                                "FilePath": "temp/preview_capture.bmp",
                                "CapturedWidth": 1920,
                                "CapturedHeight": 1080,
                                "RendererMode": "D3D11VideoProcessor",
                                "AverageR": 120,
                                "AverageG": 130,
                                "AverageB": 140,
                                "AverageLuminance": 128,
                                "MinLuminance": 0,
                                "MaxLuminance": 255,
                                "NearBlackPercent": 0,
                                "NearWhitePercent": 0,
                                "PureBlackPercent": 0,
                                "LetterboxTopRows": 0,
                                "LetterboxBottomRows": 0,
                                "PillarboxLeftCols": 0,
                                "PillarboxRightCols": 0,
                                "ContentWidth": 1920,
                                "ContentHeight": 1080,
                                "ContentAspectRatio": 1.777,
                                "TotalPixels": 2073600
                              }
                            }
                            """;
        var noAnomalyText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-no-anomaly",
                outputPath: "temp/preview_capture.bmp",
                expectedOutputPath: "temp/preview_capture.bmp",
                responseJson: noAnomalyJson)
            .ConfigureAwait(false);
        AssertContains(noAnomalyText, "Histogram unavailable.");
        AssertContains(noAnomalyText, "No obvious anomalies detected.");

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var frenchCultureText = await InvokePreviewFrameCaptureAsync(
                    previewFrameCaptureTool,
                    "preview-frame-culture",
                    outputPath: "temp/preview_capture_culture.bmp",
                    expectedOutputPath: "temp/preview_capture_culture.bmp",
                    responseJson: activeJson)
                .ConfigureAwait(false);
            AssertContains(frenchCultureText, "estimated source aspect=1.333 (640x360).");
            AssertContains(frenchCultureText, "content aspect 1.333 is not close to 16:9 or 16:10.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        var rootText = ReadRepoFile("tools/McpServer/Tools/PreviewFrameCaptureTools.cs")
            .Replace("\r\n", "\n");
        var renderingText = ReadRepoFile("tools/McpServer/Tools/PreviewFrameCaptureTools.Rendering.cs")
            .Replace("\r\n", "\n");
        var histogramText = ReadRepoFile("tools/McpServer/Tools/PreviewFrameCaptureTools.Histogram.cs")
            .Replace("\r\n", "\n");
        var diagnosisText = ReadRepoFile("tools/McpServer/Tools/PreviewFrameCaptureTools.Diagnosis.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "[McpServerToolType]");
        AssertContains(rootText, "public static partial class PreviewFrameCaptureTools");
        AssertContains(rootText, "public static async Task<CallToolResult> capture_preview_frame");
        AssertContains(rootText, "Path.Combine(Environment.CurrentDirectory, \"temp\", \"preview_capture.bmp\")");
        AssertContains(rootText, "SendCommandAsync(AutomationCommandKind.CapturePreviewFrame, payload)");
        AssertDoesNotContain(rootText, "SendCommandAsync(\"CapturePreviewFrame\", payload)");
        AssertContains(rootText, "BuildPreviewFrameCaptureText(data)");
        AssertDoesNotContain(rootText, "new StringBuilder()");
        AssertDoesNotContain(rootText, "LuminanceHistogram");
        AssertDoesNotContain(rootText, "BLANK FRAME");
        AssertDoesNotContain(rootText, "IsNear(");

        AssertContains(renderingText, "private static string BuildPreviewFrameCaptureText(");
        AssertContains(renderingText, "== Preview Frame Capture ==");
        AssertContains(renderingText, "== Pixel Summary ==");
        AssertContains(renderingText, "AppendLuminanceHistogram(builder, data)");
        AssertContains(renderingText, "AppendPreviewFrameCaptureDiagnosis(builder, data)");

        AssertContains(histogramText, "private static void AppendLuminanceHistogram(");
        AssertContains(histogramText, "LuminanceHistogram");
        AssertContains(histogramText, "while (bins.Count < 16)");
        AssertContains(histogramText, "* 24.0");
        AssertContains(histogramText, "new string('#', Math.Max(0, barLength))");

        AssertContains(diagnosisText, "private static List<string> BuildPreviewFrameCaptureDiagnosis(");
        AssertContains(diagnosisText, "pureBlackPercent > 95.0");
        AssertContains(diagnosisText, "averageLuminance < 30.0");
        AssertContains(diagnosisText, "averageLuminance > 230.0");
        AssertContains(diagnosisText, "(maxLuminance - minLuminance) < 30.0");
        AssertContains(diagnosisText, "private static string FormatAspectRatio(");
        AssertContains(diagnosisText, "AutomationSnapshotFormatter.FormatNumber(aspectRatio, \"0.###\")");
        AssertContains(diagnosisText, "private static bool IsNear(");
    }

    private static async Task<string> InvokePreviewFrameCaptureAsync(
        Type previewFrameCaptureTool,
        string pipeSuffix,
        string? outputPath,
        string expectedOutputPath,
        string responseJson)
    {
        var pipeName = NewMcpToolPipeName(pipeSuffix);
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewFrameCaptureTool,
                            "capture_preview_frame",
                            pipeClient,
                            outputPath)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "CapturePreviewFrame", ("outputPath", expectedOutputPath));
        return result;
    }
}
