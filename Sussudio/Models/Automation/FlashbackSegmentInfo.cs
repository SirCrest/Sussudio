using System;
using System.Collections.Generic;

namespace Sussudio.Models;

public sealed class FlashbackSegmentInfo
{
    public string Path { get; init; } = "";
    public int SequenceNumber { get; init; }
    public long StartPtsMs { get; init; }
    public long EndPtsMs { get; init; }
    public long SizeBytes { get; init; }
    public bool IsActive { get; init; }
}
