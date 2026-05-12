using System;
using System.Collections.Generic;

namespace Sussudio.Models;

public sealed class VideoSourceFormatEntry
{
    public string Subtype { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public double FrameRate { get; init; }
    public string Summary { get; init; } = string.Empty;
}

public sealed class VideoSourceProbeResult
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool SessionActive { get; init; }
    public string MemoryPreference { get; init; } = "Unknown";
    public string CurrentSubtype { get; init; } = "Unknown";
    public int CurrentWidth { get; init; }
    public int CurrentHeight { get; init; }
    public double CurrentFrameRate { get; init; }
    public bool P010Available { get; init; }
    public bool Nv12Available { get; init; }
    public IReadOnlyList<string> SupportedSubtypes { get; init; } = Array.Empty<string>();
    public int TotalFormatCount { get; init; }
    public IReadOnlyList<VideoSourceFormatEntry> Formats { get; init; } = Array.Empty<VideoSourceFormatEntry>();
}

public sealed class PreviewColorProbeResult
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool SessionActive { get; init; }
    public string RendererMode { get; init; } = "None";
    public string NegotiatedSubtype { get; init; } = "Unknown";
    public int SourceWidth { get; init; }
    public int SourceHeight { get; init; }
    public double SourceFrameRate { get; init; }

    // MF_MT_VIDEO_NOMINAL_RANGE: 0=Unknown, 1=Normal(0-255), 2=Wide(16-235)
    public int NominalRange { get; init; }
    public string NominalRangeLabel { get; init; } = "Unknown";

    // MF_MT_TRANSFER_FUNCTION: 1=Unknown, 6=BT709, 8=sRGB, 12=SMPTE2084(PQ), 16=HLG
    public int TransferFunction { get; init; }
    public string TransferFunctionLabel { get; init; } = "Unknown";

    // MF_MT_VIDEO_PRIMARIES: 1=Unknown, 2=BT709, 9=BT2020
    public int VideoPrimaries { get; init; }
    public string VideoPrimariesLabel { get; init; } = "Unknown";

    // MF_MT_YUV_MATRIX: 0=Unknown, 1=BT709, 2=BT601, 4=BT2020_non_const
    public int YuvMatrix { get; init; }
    public string YuvMatrixLabel { get; init; } = "Unknown";

    // Luma (Y plane) analysis from the preview adapter
    public int? LumaMin { get; init; }
    public int? LumaMax { get; init; }
    public double? LumaMean { get; init; }
    public int? LumaBelow16Count { get; init; }
    public int? LumaAbove235Count { get; init; }
    public int? LumaSampleCount { get; init; }

    // Raw MF properties dump (Guid → value)
    public IReadOnlyDictionary<string, string> FormatProperties { get; init; } = new Dictionary<string, string>();

    // D3D11 Video Processor color spaces (set when renderer is active)
    public string D3DInputColorSpace { get; init; } = "None";
    public string D3DOutputColorSpace { get; init; } = "None";
}
