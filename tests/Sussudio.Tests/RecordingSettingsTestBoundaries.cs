namespace Sussudio.Models;

// Passive data boundaries for executing the production recording-selection model
// and controller without the WinUI/native capture assembly. Runtime application
// and queue tests separately load the real CaptureService and coordinator.
public enum RecordingFormat { H264Mp4, HevcMp4, Av1Mp4 }
public enum VideoQuality { Auto, Low, Medium, High, SuperHigh, Custom }
public enum NvencPreset { Auto, P1, P2, P3, P4, P5, P6, P7, Fast, Slow }
public enum SplitEncodeMode { Auto, Disabled, TwoWay, ThreeWay, ForcedOn }

public sealed class CaptureSettings
{
    public RecordingFormat Format { get; set; }
    public VideoQuality Quality { get; set; }
    public double CustomBitrateMbps { get; set; }
    public NvencPreset NvencPreset { get; set; }
    public SplitEncodeMode SplitEncodeMode { get; set; }
    public bool AudioEnabled { get; set; }
    public int Width { get; set; }
}
