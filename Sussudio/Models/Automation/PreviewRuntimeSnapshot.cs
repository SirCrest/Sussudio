using System;

namespace Sussudio.Models;

public sealed partial class PreviewRuntimeSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool IsPreviewing { get; init; }
    public bool GpuActive { get; init; }
    public bool PlaceholderVisible { get; init; }
    public bool GpuElementVisible { get; init; }
    public bool CpuElementVisible { get; init; }
    public bool RendererAttached { get; init; }
    public long FramesArrived { get; init; }
    public long FramesDisplayed { get; init; }
    public long FramesDropped { get; init; }
    public bool BlankSuspected { get; init; }
    public bool StallSuspected { get; init; }
}
