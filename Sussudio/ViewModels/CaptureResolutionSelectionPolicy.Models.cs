using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal sealed record CaptureResolutionSelectionRequest(
    IReadOnlyList<ResolutionOption> Options,
    IReadOnlyDictionary<string, List<MediaFormat>> ResolutionToFormats,
    SourceSignalTelemetrySnapshot SourceTelemetry,
    string? PreferredSelection,
    double PreviousFrameRate,
    bool IsHdrEnabled,
    bool AllowSourceAutoSelect,
    bool PendingSdrAutoSelectionForDeviceChange);

internal sealed record CaptureResolutionSelection(
    ResolutionOption? Selected,
    string? HdrHint,
    int? SdrAutoFriendlyFrameRateBucket);

internal sealed record HdrSupportHintRequest(
    IReadOnlyDictionary<string, List<MediaFormat>> ResolutionToFormats,
    string? ResolutionKey,
    bool IsHdrEnabled,
    double SelectedFrameRate);

internal sealed record HdrResolutionSelection(
    ResolutionOption? Selected,
    string? Hint);

internal sealed record SdrAutoResolutionSelection(
    ResolutionOption Selected,
    int SelectedFriendlyBucket);
