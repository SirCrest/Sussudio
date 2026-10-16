using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sussudio.Services.Automation;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Wire contract between the app's agent-sense DTOs and the ssctl formatters that render them (docs/architecture/
// performance-telemetry.md sections 2.9 and 2.10). The formatters read the pipe JSON by property name, so the way this
// breaks is silent: rename or add a DTO field and ssctl prints "-" or drops a line, with no error anywhere. These tests
// serialize the REAL DTOs with the server's serializer settings and run them through the real ssctl assembly.
//
// Failure modes, written before the tests:
//   1. A DTO field the formatter reads is renamed or retyped: the value silently renders as "-" (every asserted value
//      below is distinctive so a "-" cannot pass).
//   2. A failed response (not-found, ambiguous, hidden, no-audio-rendered, activation-failed) is rendered from its
//      message only, losing the candidates, element state, analysis or failure stage the agent needs to recover.
//   3. A response without Data (the validation error thrown before any result exists) crashes the formatter instead of
//      printing the message and error code.
//   4. Optional blocks that are null on the wire (analysis, latency, element, candidates) crash the formatter.
//   5. A truncated tree is rendered as if it were complete.
//   6. The tree loses its nesting, so a child looks like a sibling.
public sealed class SenseFormatterContractsTests
{
    private static readonly JsonSerializerOptions PipeOptions = CreatePipeOptions();

    private static JsonSerializerOptions CreatePipeOptions()
    {
        // The same settings NamedPipeAutomationServer serializes responses with.
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string Format(string formatterMethod, object response)
    {
        var assembly = ToolFormatterTestAssembly.Load(global::Program.SsctlAssemblyRelativePath);
        var type = assembly.GetType("Sussudio.Tools.Ssctl.SenseFormatters")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.SenseFormatters type not found.");
        var method = type.GetMethod(formatterMethod, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"SenseFormatters.{formatterMethod} not found.");
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, PipeOptions));
        return (string)method.Invoke(null, new object[] { document.RootElement.Clone() })!;
    }

    private static object Success(string message, object? data)
        => new { Success = true, Message = message, ErrorCode = (string?)null, Data = data };

    private static object Failure(string message, string errorCode, object? data)
        => new { Success = false, Message = message, ErrorCode = errorCode, Data = data };

    private static UiNode Node(
        string path,
        string type,
        string? name = null,
        string? automationId = null,
        string? automationName = null,
        string? text = null,
        bool isEnabled = true,
        string? disabledReason = null,
        bool isVisible = true,
        string? hiddenReason = null,
        UiBounds? bounds = null,
        string[]? patterns = null,
        string? toggleState = null,
        UiRange? range = null,
        string? value = null,
        string? selectedItem = null,
        int? selectedIndex = null,
        string[]? items = null,
        string? expandState = null,
        UiAccelerator[]? accelerators = null,
        UiNode[]? children = null)
        => new(
            path, type, name, automationId, automationName, text, isEnabled, disabledReason, isVisible, hiddenReason, bounds,
            Interactive: patterns is { Length: > 0 }, patterns, toggleState, range, value, ValueReadOnly: null,
            selectedItem, selectedIndex, items, ItemCount: items?.Length, expandState, accelerators, children);

    [Fact]
    public void UiTree_RendersEveryFieldTheFormatterReads_AndKeepsNesting()
    {
        var tree = new UiTreeResult(
            UiTreeStatus.Ok,
            "Tree of 'MainRoot': 6 nodes.",
            Root: "MainRoot",
            MaxDepth: 7,
            InteractiveOnly: true,
            VisitedElements: 61,
            EmittedNodes: 6,
            MaxNodes: UiTreeLimits.MaxNodes,
            Truncated: true,
            TruncationReason: "node cap 1500 reached",
            OpenPopups: 1,
            WindowWidth: 1280,
            WindowHeight: 720,
            Roots: new[]
            {
                Node("Grid", "Grid", name: "MainRoot", children: new[]
                {
                    Node("Grid/Button[0]", "Button", name: "RecordButton", automationId: "Record.Toggle", automationName: "Start recording",
                        text: "Record", bounds: new UiBounds(10, 20, 100, 30), patterns: new[] { "Invoke" },
                        accelerators: new[] { new UiAccelerator("R", "Control") }),
                    Node("Grid/ToggleSwitch[1]", "ToggleSwitch", name: "PreviewToggle", patterns: new[] { "Toggle" }, toggleState: "On"),
                    Node("Grid/Slider[2]", "Slider", name: "VolumeSlider", patterns: new[] { "RangeValue" },
                        range: new UiRange(42, 0, 100, IsReadOnly: false)),
                    Node("Grid/ComboBox[3]", "ComboBox", name: "ResolutionCombo", patterns: new[] { "Selection", "ExpandCollapse" },
                        selectedItem: "Beta", selectedIndex: 1, items: new[] { "Alpha", "Beta", "Gamma" }, expandState: "Collapsed"),
                    Node("Grid/Button[4]", "Button", isEnabled: false, disabledReason: "parent is disabled",
                        isVisible: false, hiddenReason: "Collapsed on Panel", patterns: new[] { "Invoke" }),
                }),
            });

        var output = Format("FormatUiTree", Success(tree.Message, tree));

        Assert.Contains("== UI tree (ok): Tree of 'MainRoot': 6 nodes. ==", output, StringComparison.Ordinal);
        Assert.Contains("!! TRUNCATED: node cap 1500 reached", output, StringComparison.Ordinal);
        Assert.Contains("window 1280 x 720 DIP", output, StringComparison.Ordinal);
        Assert.Contains("maxDepth 7", output, StringComparison.Ordinal);
        Assert.Contains("#RecordButton id=Record.Toggle name \"Start recording\" text \"Record\" [Invoke]", output, StringComparison.Ordinal);
        Assert.Contains("accel Control+R", output, StringComparison.Ordinal);
        Assert.Contains("@10,20 100x30", output, StringComparison.Ordinal);
        Assert.Contains("#PreviewToggle [Toggle] toggle=On", output, StringComparison.Ordinal);
        Assert.Contains("range=42.00 [0.00..100.00]", output, StringComparison.Ordinal);
        Assert.Contains("selected #1 \"Beta\" items(3): Alpha | Beta | Gamma expand=Collapsed", output, StringComparison.Ordinal);
        Assert.Contains("DISABLED(parent is disabled)", output, StringComparison.Ordinal);
        Assert.Contains("HIDDEN(Collapsed on Panel)", output, StringComparison.Ordinal);
        Assert.Contains("path=Grid/Button[4]", output, StringComparison.Ordinal);
        Assert.DoesNotContain("path=Grid/Button[0]", output, StringComparison.Ordinal);

        var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        Assert.Contains(lines, line => line.StartsWith("Grid #MainRoot", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("  Button #RecordButton", StringComparison.Ordinal));
    }

    [Fact]
    public void UiTree_CompleteResult_DoesNotClaimTruncation()
    {
        var tree = new UiTreeResult(
            UiTreeStatus.Ok, "ok", null, 10, true, 3, 1, UiTreeLimits.MaxNodes, Truncated: false, null, 0, 800, 600,
            new[] { Node("Grid", "Grid") });

        var output = Format("FormatUiTree", Success(tree.Message, tree));

        Assert.DoesNotContain("TRUNCATED", output, StringComparison.Ordinal);
    }

    [Fact]
    public void UiTree_FailedRootLookup_StillPrintsTheStatusAndMessage()
    {
        var tree = new UiTreeResult(
            UiTreeStatus.RootNotFound, "No element matches root 'Nope'.", "Nope", 10, true, 0, 0, UiTreeLimits.MaxNodes,
            false, null, 0, 800, 600, Array.Empty<UiNode>());

        var output = Format("FormatUiTree", Failure(tree.Message, "InvalidRequest", tree));

        Assert.Contains("(root-not-found): No element matches root 'Nope'.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void UiInvoke_AmbiguousFailure_RendersCandidatesWithPathsAndStates()
    {
        var result = new UiInvokeResult(
            UiActionStatus.Ambiguous,
            "'Save' matches 5 elements; pass one of the paths.",
            "Save",
            MatchedBy: "text",
            Action: null,
            Via: null,
            Overridden: null,
            Element: null,
            Candidates: new[]
            {
                new UiCandidateInfo("Grid/Button[2]", "Button", "SaveButton", null, null, "Save", true, true),
                new UiCandidateInfo("Flyout/Button[0]", "Button", null, "Flyout.Save", "Save settings", "Save", false, false),
            },
            CandidateTotal: 5);

        var output = Format("FormatUiInvoke", Failure(result.Message, "InvalidRequest", result));

        Assert.StartsWith("ambiguous: 'Save' matches 5 elements; pass one of the paths.", output, StringComparison.Ordinal);
        Assert.Contains("candidates (2 of 5):", output, StringComparison.Ordinal);
        Assert.Contains("Grid/Button[2]  [Button] #SaveButton text \"Save\"", output, StringComparison.Ordinal);
        Assert.Contains("Flyout/Button[0]  [Button] id=Flyout.Save name \"Save settings\" text \"Save\" HIDDEN DISABLED", output, StringComparison.Ordinal);
    }

    [Fact]
    public void UiInvoke_Success_ReportsMechanismAndTheReReadElement()
    {
        var result = new UiInvokeResult(
            UiActionStatus.Invoked,
            "Toggled 'PreviewToggle'.",
            "PreviewToggle",
            MatchedBy: "x:Name",
            Action: "toggle",
            Via: "peer:Toggle",
            Overridden: new[] { "hidden: Collapsed on Panel" },
            Element: Node("Grid/ToggleSwitch[1]", "ToggleSwitch", name: "PreviewToggle", patterns: new[] { "Toggle" }, toggleState: "Off"),
            Candidates: null,
            CandidateTotal: null);

        var output = Format("FormatUiInvoke", Success(result.Message, result));

        Assert.StartsWith("invoked: Toggled 'PreviewToggle'.", output, StringComparison.Ordinal);
        Assert.Contains("target \"PreviewToggle\" matched by x:Name, action toggle, via peer:Toggle", output, StringComparison.Ordinal);
        Assert.Contains("force overrode hidden: Collapsed on Panel", output, StringComparison.Ordinal);
        Assert.Contains("element after the action:", output, StringComparison.Ordinal);
        Assert.Contains("ToggleSwitch #PreviewToggle [Toggle] toggle=Off", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("FormatUiTree")]
    [InlineData("FormatUiInvoke")]
    [InlineData("FormatAudioListen")]
    public void ResponseWithoutData_PrintsMessageAndErrorCode(string formatter)
    {
        var output = Format(formatter, Failure("seconds must be between 0.5 and 30 (got 300).", "InvalidRequest", null));

        Assert.Contains("seconds must be between 0.5 and 30 (got 300).", output, StringComparison.Ordinal);
        Assert.Contains("ErrorCode: InvalidRequest", output, StringComparison.Ordinal);
    }

    [Fact]
    public void AudioListen_Captured_RendersLevelsDefectsLatencyWavAndDiagnostics()
    {
        var result = new AudioOutputProbeResult(
            "captured",
            "Captured 5.0 s of this app's audio output.",
            RequestedSeconds: 5,
            CapturedSeconds: 4.98,
            ProcessId: 31337,
            Source: "process-loopback",
            WavPath: @"C:\artifacts\out.wav",
            WavBytes: 1_920_044,
            Analysis: new AudioAnalysis(
                SampleRate: 48_000, TotalFrames: 239_040, DurationSeconds: 4.98,
                LeftRmsDbfs: -20.4, RightRmsDbfs: -21.6, LeftPeakDbfs: -6.1, RightPeakDbfs: -7.3,
                ChannelBalanceDb: 1.2, BlockCount: 498, SilentBlockPercent: 3,
                ClippedSamples: 12, ClipEvents: 2, Clicks: 2, ClickTimesMs: new[] { 120.0, 340.0 },
                Dropouts: 1, LongestDropoutMs: 2.5, DropoutTimesMs: new[] { 900.0 }, NonFiniteSamples: 0,
                Packets: 498, DiscontinuityPackets: 1, SilentPackets: 4, TimestampErrorPackets: 2, MaxPacketGapMs: 11.5,
                Verdict: "defects", Findings: new[] { "2 click(s) detected", "1 dropout(s) detected" }),
            Latency: new AudioLatencyResult(
                "ok", null, LatencyMs: 23.5, CoarseLatencyMs: 23.0, Resolution: "sample", Confidence: 0.91,
                SecondPeakRatio: 0.2, GainDifferenceDb: -0.4, InputPeakDbfs: -6.0, OutputPeakDbfs: -6.4,
                OverlapSeconds: 4.5, InputTimelineSpreadMs: 0.1, Scope: "capture delivery to mixer (excludes device latencies)"),
            Diagnostics: new AudioProbeDiagnostics(
                LoopbackPackets: 498, LoopbackFrames: 239_040, ActivationMs: 41.7, FirstPacketMs: 12.3, WaitTimeouts: 3,
                BufferTruncated: true, TapFrames: 240_000, TapPackets: 500, TapRejectedPackets: 3),
            FailureStage: null,
            FailureHResult: null);

        var output = Format("FormatAudioListen", Success(result.Message, result));

        Assert.StartsWith("Audio output capture: captured", output, StringComparison.Ordinal);
        Assert.Contains("requested 5.0 s, captured 4.98 s, pid 31337", output, StringComparison.Ordinal);
        Assert.Contains("source: process-loopback", output, StringComparison.Ordinal);
        Assert.Contains("verdict: defects", output, StringComparison.Ordinal);
        Assert.Contains("L rms -20.4 / peak -6.1 dBFS, R rms -21.6 / peak -7.3 dBFS, balance 1.2 dB, silent blocks 3%", output, StringComparison.Ordinal);
        Assert.Contains("clipping 12 samples in 2 events", output, StringComparison.Ordinal);
        Assert.Contains("clicks 2 at ms 120, 340", output, StringComparison.Ordinal);
        Assert.Contains("dropouts 1 (longest 2.5 ms) at ms 900", output, StringComparison.Ordinal);
        Assert.Contains("discontinuities 1, silent 4, timestamp errors 2, max gap 11.5 ms", output, StringComparison.Ordinal);
        Assert.Contains("- 2 click(s) detected", output, StringComparison.Ordinal);
        Assert.Contains("latency (capture delivery -> mixer): 23.50 ms [sample], confidence 0.91, gain difference -0.4 dB, overlap 4.5 s", output, StringComparison.Ordinal);
        Assert.Contains("latency scope: capture delivery to mixer (excludes device latencies)", output, StringComparison.Ordinal);
        Assert.Contains(@"wav: C:\artifacts\out.wav (1920044 bytes)", output, StringComparison.Ordinal);
        Assert.Contains("498 packets / 239040 frames, activation 41.7 ms, first packet 12.3 ms, wait timeouts 3, BUFFER TRUNCATED", output, StringComparison.Ordinal);
        Assert.Contains("tap: 240000 frames / 500 packets (3 rejected)", output, StringComparison.Ordinal);
    }

    [Fact]
    public void AudioListen_NoAudioRendered_ShowsTheAnalysisAndWhyLatencyIsUnavailable()
    {
        var result = new AudioOutputProbeResult(
            "no-audio-rendered",
            "The loopback delivered only silence; is the preview audio enabled?",
            RequestedSeconds: 2,
            CapturedSeconds: 2,
            ProcessId: 42,
            Source: "process-loopback",
            WavPath: null,
            WavBytes: 0,
            Analysis: new AudioAnalysis(
                48_000, 96_000, 2, -120, -120, -120, -120, 0, 200, 100, 0, 0, 0, Array.Empty<double>(), 0, 0, Array.Empty<double>(), 0,
                200, 0, 200, 0, 10, "silent", new[] { "The captured audio is digital silence." }),
            Latency: new AudioLatencyResult(
                "unavailable", "the tap recorded no packets", null, null, null, 0, null, null, -120, -120, 0, 0, "capture delivery to mixer"),
            Diagnostics: null,
            FailureStage: null,
            FailureHResult: null);

        var output = Format("FormatAudioListen", Failure(result.Message, "CommandFailed", result));

        Assert.StartsWith("Audio output capture: no-audio-rendered", output, StringComparison.Ordinal);
        Assert.Contains("verdict: silent", output, StringComparison.Ordinal);
        Assert.Contains("- The captured audio is digital silence.", output, StringComparison.Ordinal);
        Assert.Contains("latency: unavailable - the tap recorded no packets", output, StringComparison.Ordinal);
        Assert.DoesNotContain("wav:", output, StringComparison.Ordinal);
    }

    [Fact]
    public void AudioListen_ActivationFailure_ShowsStageAndHResultWithoutAnalysis()
    {
        var result = new AudioOutputProbeResult(
            "activation-failed",
            "Process loopback activation failed.",
            RequestedSeconds: 5,
            CapturedSeconds: 0,
            ProcessId: 42,
            Source: "process-loopback",
            WavPath: null,
            WavBytes: 0,
            Analysis: null,
            Latency: null,
            Diagnostics: null,
            FailureStage: "ActivateAudioInterfaceAsync",
            FailureHResult: "0x80070005");

        var output = Format("FormatAudioListen", Failure(result.Message, "CommandFailed", result));

        Assert.StartsWith("Audio output capture: activation-failed", output, StringComparison.Ordinal);
        Assert.Contains("failure: stage ActivateAudioInterfaceAsync, HRESULT 0x80070005", output, StringComparison.Ordinal);
        Assert.DoesNotContain("verdict:", output, StringComparison.Ordinal);
    }
}
