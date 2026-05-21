using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    public bool EncodingFailed => Volatile.Read(ref _encodingFailure) != null;
    public string? EncodingFailureType => Volatile.Read(ref _encodingFailure)?.GetType().Name;
    public string? EncodingFailureMessage => Volatile.Read(ref _encodingFailure)?.Message;

    public bool AudioEnabled => Volatile.Read(ref _audioEnabled);

    public bool MicrophoneEnabled => Volatile.Read(ref _microphoneEnabled);

    /// <summary>
    /// Registers a callback invoked when the encoding loop encounters a fatal error.
    /// This lets the owning CaptureService surface the failure rather than going silently dead.
    /// </summary>
    public void SetFatalErrorCallback(Action<Exception>? callback) => _onFatalError = callback;

    public string? CodecName => _sessionContext?.CodecName;
    public uint TargetBitRate => _sessionContext?.BitRate ?? 0;
    public int EncoderWidth => _width;
    public int EncoderHeight => _height;
    public double EncoderFrameRate => _sessionContext?.FrameRate ?? 0;
    public int? EncoderFrameRateNumerator => _sessionContext?.FrameRateNumerator;
    public int? EncoderFrameRateDenominator => _sessionContext?.FrameRateDenominator;

    /// <summary>
    /// True when this sink was started with a P010 (HDR) session context.
    /// Used by CaptureService to detect pixel-format drift between UVC re-negotiations.
    /// </summary>
    public bool? IsP010 => _sessionContext?.IsP010;

    internal Task EncodingCompletionTask => _encodingTask ?? Task.CompletedTask;
}
