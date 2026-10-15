using System.ComponentModel;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP projection over the agent senses (docs/architecture/performance-telemetry.md sections 2.9 and 2.10): hearing the
// app's own audio output and reading and operating its live UI controls. Every tool returns the app's structured payload
// unchanged. Unlike the query tools, a failed call keeps its payload as structured content (the candidate list of an
// ambiguous target, the analysis of a silent capture), because that evidence is how the agent recovers.
public static class AgentSenseTools
{
    [McpServerTool(UseStructuredContent = true), Description("Listen to what Sussudio actually renders: records the app's own audio output for a few seconds (Windows process loopback of this process, mixed before the endpoint, so system sounds and other apps are excluded) and returns an analysis: RMS and peak dBFS per channel, channel balance, clipping, clicks, dropouts, packet discontinuities, a verdict and plain-language findings. outputPath also saves the capture as a float WAV. measureLatency estimates the delay from capture delivery to the mixer by correlating the app's capture-delivery tap with the loopback signal; it excludes device input latency and endpoint output latency (the response states this scope). Silence is reported as status no-audio-rendered (a failed call that still returns the analysis), never as a pass; turn the preview audio on first. Needs Windows 10 build 20348 or later.")]
    public static async Task<CallToolResult> listen_audio_output(
        PipeClient pipeClient,
        [Description("Capture length in seconds, 0.5 to 30 (default 5). Out-of-range values are rejected, not clamped.")] double? seconds = null,
        [Description("Optional absolute path ending in .wav to save the captured audio (float32 stereo).")] string? outputPath = null,
        [Description("Also measure capture-delivery-to-mixer latency (default false).")] bool measureLatency = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?>();
        if (seconds.HasValue)
        {
            payload[AutomationPayloadKeys.Seconds] = seconds.Value;
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            payload[AutomationPayloadKeys.OutputPath] = outputPath;
        }

        if (measureLatency)
        {
            payload[AutomationPayloadKeys.MeasureLatency] = true;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.CaptureAudioOutput, payload, "Audio output capture was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("Read the live WinUI visual tree of the Sussudio window, including open popups and flyouts, as JSON. Per element: path, type, x:Name, AutomationId, automation name, visible text, enabled state and reason, effective visibility and reason, bounds in window DIPs, supported automation patterns, toggle state, range value, selected item, expand state and KeyboardAccelerators. Use the names and paths from here as targets for invoke_ui_element. The output is bounded: maxDepth (1 to 64, default 10), at most 1500 nodes, and the response says when it truncated and why. interactiveOnly (default true) lists only elements that expose an operable automation pattern (buttons, toggles, sliders, text boxes, selectors, expanders) without the layout containers around them; pass false to list every element, including plain text and containers. Passwords are never reported.")]
    public static async Task<CallToolResult> get_ui_tree(
        PipeClient pipeClient,
        [Description("Optional element to start from: an x:Name, AutomationId, automation name, visible text or a path from an earlier tree. Default is the whole window plus open popups.")] string? root = null,
        [Description("Maximum depth below the root, 1 to 64 (default 10).")] int? maxDepth = null,
        [Description("Only elements with an operable automation pattern (default true); false lists every element.")] bool interactiveOnly = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(root))
        {
            payload[AutomationPayloadKeys.Root] = root;
        }

        if (maxDepth.HasValue)
        {
            payload[AutomationPayloadKeys.MaxDepth] = maxDepth.Value;
        }

        if (!interactiveOnly)
        {
            payload[AutomationPayloadKeys.InteractiveOnly] = false;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.GetUiTree, payload, "UI tree was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("Operate a Sussudio UI control through its automation peer, as an assistive technology would. target is resolved by exact x:Name, then AutomationId, then automation name, then visible text (the first level that matches decides); a path from get_ui_tree selects one element exactly. A target that matches several elements is rejected with the candidate paths, never guessed. action is invoke (buttons), toggle, set-value (text boxes and sliders; a range value outside its limits is rejected, not clamped), select (list items, or combo and list boxes with value as the item text or '#N' index), expand, collapse, focus or scroll-into-view; the element must support the action, and the rejection lists what it does support. Omitting action picks the control's natural one. Hidden and disabled elements are rejected unless force is true; force skips only that pre-check: it never resolves an ambiguity, and the control's automation peer may still refuse (a disabled control usually does), which the response reports. The response names the mechanism used (via) and the element state re-read after the action settled. Keyboard accelerators cannot be raised through a public API: invoke the element that owns the accelerator instead.")]
    public static async Task<CallToolResult> invoke_ui_element(
        PipeClient pipeClient,
        [Description("x:Name, AutomationId, automation name, visible text, or a path from get_ui_tree.")] string target,
        [Description("invoke, toggle, set-value, select, expand, collapse, focus or scroll-into-view. Default: the element's natural action.")] string? action = null,
        [Description("Value for set-value (number for sliders, text for text boxes) or select (item text or '#N' zero-based index). Omit for other actions.")] string? value = null,
        [Description("Act on a hidden or disabled element anyway (default false). Does not resolve ambiguity.")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Target] = target };
        if (!string.IsNullOrWhiteSpace(action))
        {
            payload[AutomationPayloadKeys.Action] = action;
        }

        if (value is not null)
        {
            payload[AutomationPayloadKeys.Value] = value;
        }

        if (force)
        {
            payload[AutomationPayloadKeys.Force] = true;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.InvokeUiElement, payload, "UI invoke acknowledgement was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<CallToolResult> SendStructuredAsync(
        PipeClient pipeClient,
        AutomationCommandKind kind,
        Dictionary<string, object?> payload,
        string missingMessage,
        CancellationToken cancellationToken)
    {
        var response = await pipeClient.SendCommandAsync(kind, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
        return McpToolResultFactory.FromStructuredResponseKeepingFailureData(response, "Data", missingMessage);
    }
}
