using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpPipelineSettingsTools_RoutePipelineAndAudioCommands()
    {
        var pipeName = NewMcpToolPipeName("pipeline");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var pipelineTools = RequireMcpType("McpServer.Tools.PipelineSettingsTools");

        var empty = await InvokeMcpToolStringAsync(
            pipelineTools,
            "configure_pipeline",
            pipeClient,
            null,
            null,
            null,
            null,
            null).ConfigureAwait(false);
        AssertEqual("No pipeline setting changes requested.", empty, "configure_pipeline empty result");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 7,
                async () =>
                {
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_pipeline",
                        pipeClient,
                        true,
                        false,
                        true,
                        false,
                        @"C:\captures").ConfigureAwait(false);
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_audio_mode",
                        pipeClient,
                        "Analog").ConfigureAwait(false);
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_analog_gain",
                        pipeClient,
                        42.5d).ConfigureAwait(false);
                })
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetHdrEnabled", ("enabled", true));
        AssertCommandRequest(requests[1], "SetTrueHdrPreviewEnabled", ("enabled", false));
        AssertCommandRequest(requests[2], "SetAudioEnabled", ("enabled", false));
        AssertCommandRequest(requests[3], "SetAudioPreviewEnabled", ("enabled", true));
        AssertCommandRequest(requests[4], "SetOutputPath", ("outputPath", @"C:\captures"));
        AssertCommandRequest(requests[5], "SetDeviceAudioMode", ("mode", "analog"));
        AssertCommandRequest(requests[6], "SetAnalogAudioGain", ("gain", 42.5d));
    }
}
