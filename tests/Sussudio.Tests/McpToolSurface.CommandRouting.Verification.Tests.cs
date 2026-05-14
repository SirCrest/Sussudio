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
        AssertEqual(GetExpectedAutomationCommandValue("AssertSnapshot"), requests[2].GetProperty("command").GetInt32(), "AssertSnapshot command id");
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

        AssertContains(recordingResult, "== Recording Verification: PASS ==");
        AssertContainsOrdinal(recordingResult, "Output: C:\\captures\\latest.mp4 | Exists: true | Size: 123456 bytes");
        AssertContains(recordingResult, "Mismatches: None");
        AssertContains(fileResult, "== File Verification: FAIL ==");
        AssertContainsOrdinal(fileResult, "File: C:\\captures\\clip.mp4 | Exists: true | Size: 42 bytes");
        AssertContains(assertResult, "Snapshot assertions: FAIL");
        AssertContains(assertResult, "Failures: IsRecording expected false");
        AssertEqual("no verification data", missingRecordingResult, "verify_recording missing verification fallback");
        AssertEqual("file not found", missingFileResult, "verify_file missing verification fallback");
    }
}
