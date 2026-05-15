using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpVerificationTools_FormatVerificationResponses()
    {
        var verificationTools = RequireMcpType("McpServer.Tools.VerificationTools");

        var blankAssertions = await InvokeMcpToolStringAsync(
            verificationTools,
            "assert_snapshot",
            CreateMcpPipeClient(NewMcpToolPipeName("assert-empty")),
            string.Empty).ConfigureAwait(false);
        AssertEqual("The assertions parameter must be a JSON array string.", blankAssertions, "assert_snapshot blank input");

        var invalidAssertions = await InvokeMcpToolStringAsync(
            verificationTools,
            "assert_snapshot",
            CreateMcpPipeClient(NewMcpToolPipeName("assert-invalid")),
            "{\"field\":\"IsRecording\"}").ConfigureAwait(false);
        AssertEqual("The assertions parameter must be a JSON array string.", invalidAssertions, "assert_snapshot non-array input");

        var recordingResult = string.Empty;
        var fileResult = string.Empty;
        var assertResult = string.Empty;
        var missingRecordingResult = string.Empty;
        var missingFileResult = string.Empty;
        var pipeName = NewMcpToolPipeName("verification");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 5,
                async () =>
                {
                    recordingResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_recording",
                            pipeClient)
                        .ConfigureAwait(false);
                    fileResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_file",
                            pipeClient,
                            @"C:\captures\clip.mp4",
                            "flashback-export")
                        .ConfigureAwait(false);
                    assertResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "assert_snapshot",
                            pipeClient,
                            """[{"field":"IsRecording","op":"eq","value":false}]""")
                        .ConfigureAwait(false);
                    missingRecordingResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_recording",
                            pipeClient)
                        .ConfigureAwait(false);
                    missingFileResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_file",
                            pipeClient,
                            @"C:\captures\missing.mp4",
                            null)
                        .ConfigureAwait(false);
                },
                i => i switch
                {
                    0 => """
                         {
                           "Success": true,
                           "Message": "last recording verified",
                           "Data": {
                             "Verification": {
                               "OutputPath": "C:\\captures\\latest.mp4",
                               "FileExists": true,
                               "FileSizeBytes": 123456,
                               "VerificationMode": "LastRecording",
                               "DetectedVideoCodec": "hevc",
                               "DetectedPixelFormat": "p010le",
                               "DetectedWidth": 3840,
                               "DetectedHeight": 2160,
                               "DetectedFrameRate": 59.94,
                               "HdrVerificationLevel": "Strict",
                               "HdrMetadataPresent": true,
                               "HdrColorimetryValid": true,
                               "HdrMasteringMetadataPresent": false,
                               "Mismatches": []
                             }
                           }
                         }
                         """,
                    1 => """
                         {
                           "Success": false,
                           "Message": "file mismatch",
                           "Snapshot": {
                             "LastVerification": {
                               "FileExists": true,
                               "FileSizeBytes": 42,
                               "DetectedVideoCodec": "h264",
                               "DetectedPixelFormat": "yuv420p",
                               "DetectedWidth": 1920,
                               "DetectedHeight": 1080,
                               "DetectedFrameRate": 30
                             }
                           }
                         }
                         """,
                    2 => """
                         {
                           "Success": false,
                           "Message": "1 assertion failed",
                           "Data": {
                             "assertions": 1,
                             "passed": false,
                             "failures": ["IsRecording expected false"]
                           }
                         }
                         """,
                    3 => "{\"Success\":true,\"Message\":\"no verification data\"}",
                    _ => "{\"Success\":false,\"Message\":\"file not found\"}"
                })
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "VerifyLastRecording");
        AssertCommandRequest(requests[1], "VerifyFile", ("filePath", @"C:\captures\clip.mp4"), ("verificationProfile", "flashback-export"));
        AssertAutomationCommandId(requests[2], "AssertSnapshot");
        AssertCommandRequest(requests[3], "VerifyLastRecording");
        AssertCommandRequest(requests[4], "VerifyFile", ("filePath", @"C:\captures\missing.mp4"));
        var assertPayload = requests[2].GetProperty("payload");
        AssertJsonObjectPropertyNames(assertPayload, "assertions");
        var assertions = requests[2].GetProperty("payload").GetProperty("assertions");
        AssertEqual(JsonValueKind.Array, assertions.ValueKind, "AssertSnapshot assertions payload kind");
        AssertEqual(1, assertions.GetArrayLength(), "AssertSnapshot assertions payload count");
        var assertion = assertions[0];
        AssertJsonObjectPropertyNames(assertion, "field", "op", "value");
        AssertEqual("IsRecording", assertion.GetProperty("field").GetString(), "AssertSnapshot field payload");
        AssertEqual("eq", assertion.GetProperty("op").GetString(), "AssertSnapshot op payload");
        AssertEqual(JsonValueKind.False, assertion.GetProperty("value").ValueKind, "AssertSnapshot value payload kind");

        AssertEqual(
            """
            == Recording Verification: PASS ==
            Message: last recording verified
            Output: C:\captures\latest.mp4 | Exists: true | Size: 123456 bytes
            Mode: LastRecording | Codec: hevc | Pixel Format: p010le
            Resolution: 3840 x 2160 | FPS: 59.94
            HDR: Level=Strict Metadata=true Colorimetry=true Mastering=false
            Mismatches: None
            """,
            recordingResult.Replace("\r\n", "\n"),
            "verify_recording exact text");
        AssertEqual(
            """
            == File Verification: FAIL ==
            Message: file mismatch
            File: C:\captures\clip.mp4 | Exists: true | Size: 42 bytes
            Codec: h264 | Pixel Format: yuv420p
            Resolution: 1920 x 1080 | FPS: 30
            """,
            fileResult.Replace("\r\n", "\n"),
            "verify_file exact text");
        AssertEqual(
            """
            Snapshot assertions: FAIL
            Message: 1 assertion failed
            Assertions: 1
            Passed: false
            Failures: IsRecording expected false
            """,
            assertResult.Replace("\r\n", "\n"),
            "assert_snapshot exact text");
        AssertEqual("no verification data", missingRecordingResult, "verify_recording missing verification fallback");
        AssertEqual("file not found", missingFileResult, "verify_file missing verification fallback");

        var verificationRootText = ReadRepoFile("tools/McpServer/Tools/VerificationTools.cs")
            .Replace("\r\n", "\n");
        var verificationAssertionsText = ReadRepoFile("tools/McpServer/Tools/VerificationTools.Assertions.cs")
            .Replace("\r\n", "\n");
        var verificationFormattingText = ReadRepoFile("tools/McpServer/Tools/VerificationTools.Formatting.cs")
            .Replace("\r\n", "\n");
        var verificationParsingText = ReadRepoFile("tools/McpServer/Tools/VerificationTools.Parsing.cs")
            .Replace("\r\n", "\n");

        AssertContains(verificationRootText, "[McpServerToolType]");
        AssertContains(verificationRootText, "public static partial class VerificationTools");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> verify_recording");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> assert_snapshot");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> verify_file");
        AssertContains(verificationRootText, "SendCommandAsync(\"VerifyLastRecording\", responseTimeoutMs: 60000)");
        AssertContains(verificationRootText, "SendCommandAsync(\"AssertSnapshot\", payload)");
        AssertContains(verificationRootText, "SendCommandAsync(\"VerifyFile\", payload, responseTimeoutMs: 60000)");
        AssertContains(verificationRootText, "TryParseAssertionArray(assertions, out var parsedAssertions, out var parseError)");
        AssertContains(verificationRootText, "BuildRecordingVerificationText(response, verification, message)");
        AssertContains(verificationRootText, "BuildSnapshotAssertionText(response)");
        AssertContains(verificationRootText, "BuildFileVerificationText(filePath, response, verification, message)");
        AssertDoesNotContain(verificationRootText, "new StringBuilder()");
        AssertDoesNotContain(verificationRootText, "Mismatches:");
        AssertDoesNotContain(verificationRootText, "RootElement.Clone()");

        AssertContains(verificationAssertionsText, "private static bool TryParseAssertionArray(");
        AssertContains(verificationAssertionsText, "string.IsNullOrWhiteSpace(assertions)");
        AssertContains(verificationAssertionsText, "JsonDocument.Parse(assertions)");
        AssertContains(verificationAssertionsText, "RootElement.Clone()");
        AssertContains(verificationAssertionsText, "Invalid assertions JSON: {ex.Message}");

        AssertContains(verificationFormattingText, "private static string BuildRecordingVerificationText(");
        AssertContains(verificationFormattingText, "== Recording Verification: PASS ==");
        AssertContains(verificationFormattingText, "FormatJsonArrayList(verification, \"Mismatches\", \"Mismatches\")");
        AssertContains(verificationFormattingText, "private static string BuildSnapshotAssertionText(");
        AssertContains(verificationFormattingText, "FormatJsonArrayList(failures, \"Failures\")");
        AssertContains(verificationFormattingText, "\"{label}: None\"");
        AssertContains(verificationFormattingText, "private static string BuildFileVerificationText(");
        AssertContains(verificationFormattingText, "== File Verification: PASS ==");

        AssertContains(verificationParsingText, "private static bool TryGetVerification(");
        AssertContains(verificationParsingText, "response.TryGetProperty(\"Data\", out var data)");
        AssertContains(verificationParsingText, "data.TryGetProperty(\"Verification\", out verification)");
        AssertContains(verificationParsingText, "response.TryGetProperty(\"Snapshot\", out var snapshot)");
        AssertContains(verificationParsingText, "snapshot.TryGetProperty(\"LastVerification\", out verification)");

        AssertMcpCommandRoutingTestsUseCommandIdHelper();
    }
}
