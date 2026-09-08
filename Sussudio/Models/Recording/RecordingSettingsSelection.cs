using System;

namespace Sussudio.Models;

// A complete desired encoder selection. Queued requests must carry every field
// because an encoder-parameter request can supersede an earlier queued request.
internal readonly record struct RecordingSettingsSelection(
    RecordingFormat RequestedFormat,
    VideoQuality Quality,
    double CustomBitrateMbps,
    NvencPreset NvencPreset,
    SplitEncodeMode SplitEncodeMode)
{
    internal static RecordingSettingsSelection From(CaptureSettings settings)
        => new(settings.Format, settings.Quality, settings.CustomBitrateMbps, settings.NvencPreset, settings.SplitEncodeMode);

    internal bool Matches(CaptureSettings? settings)
        => settings != null &&
           RequestedFormat == settings.Format &&
           Quality == settings.Quality &&
           Math.Abs(CustomBitrateMbps - settings.CustomBitrateMbps) <= 0.01 &&
           NvencPreset == settings.NvencPreset &&
           SplitEncodeMode == settings.SplitEncodeMode;

    internal void ApplyTo(CaptureSettings settings)
    {
        settings.Format = RequestedFormat;
        settings.Quality = Quality;
        settings.CustomBitrateMbps = CustomBitrateMbps;
        settings.NvencPreset = NvencPreset;
        settings.SplitEncodeMode = SplitEncodeMode;
    }
}

internal enum RecordingSettingsChangeKind
{
    RecordingFormat,
    EncoderParameters
}

// These are managed application outcomes, not proof of encoded output. Failures
// and cancellation propagate through the task instead of a success-shaped result.
internal enum RecordingSettingsApplyDisposition
{
    Accepted,
    Deferred,
    Applied,
    Superseded
}
