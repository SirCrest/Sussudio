using System.Runtime.InteropServices;
using System.Text.Json;
using FFmpeg.AutoGen;
using Xunit;

namespace Sussudio.Tests;

internal class HevcP010FactAttribute : FactAttribute
{
    public HevcP010FactAttribute() : this(HevcP010Capability.Current) { }

    internal HevcP010FactAttribute(HevcP010Capability.Result result)
    {
        if (result.Outcome == HevcP010Capability.Outcome.Unavailable)
            Skip = result.Reason;
    }
}

// The probe owns a child because FFmpeg bindings and its log callback are process-wide.
// Only native codec opening is gated; every later production failure remains a failure.
internal static class HevcP010Capability
{
    private static readonly Lazy<Result> Cached = new(ProbeInChild);
    internal static Result Current => Cached.Value;

    internal enum Outcome { Unexpected, Available, Unavailable }
    internal sealed record Result(Outcome Outcome, string Reason, int? NativeError = null, string[]? Diagnostics = null);

    internal static void RequireAvailable() => RequireAvailable(Current);

    internal static void RequireAvailable(Result result)
    {
        Assert.True(result.Outcome == Outcome.Available,
            $"HEVC/P010 capability probe: {result.Reason}; native error={result.NativeError}; " +
            string.Join(" | ", result.Diagnostics ?? Array.Empty<string>()));
    }

    private static Result ProbeInChild()
    {
        try
        {
            var child = RecordingNativeTestChild.RunAsync("capability", TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
            return child.TimedOut || child.ExitCode != 0 || !string.IsNullOrWhiteSpace(child.Errors)
                ? new(Outcome.Unexpected, child.FailureDetail)
                : ReadChildResult(child.ExitCode, child.Output, child.Errors);
        }
        catch (Exception error) { return new(Outcome.Unexpected, error.ToString()); }
    }

    internal static Result ReadChildResult(int exitCode, string stdout, string stderr)
    {
        if (exitCode != 0 || !string.IsNullOrWhiteSpace(stderr))
            return new(Outcome.Unexpected, $"Capability child exit={exitCode}; stdout={stdout}; stderr={stderr}");
        try
        {
            var result = JsonSerializer.Deserialize<Result>(stdout);
            return result != null && Enum.IsDefined(result.Outcome) && !string.IsNullOrWhiteSpace(result.Reason)
                ? result
                : new(Outcome.Unexpected, "Capability child returned an invalid result: " + stdout);
        }
        catch (JsonException error)
        {
            return new(Outcome.Unexpected, "Capability child returned invalid JSON: " + error.Message + "; stdout=" + stdout);
        }
    }

    internal static unsafe Result OpenCodec()
    {
        AVCodecContext* context = null;
        var diagnostics = new List<string>();
        string? callbackFailure = null;
        av_log_set_callback_callback callback = (avcl, level, format, arguments) =>
        {
            if (level > ffmpeg.AV_LOG_WARNING && !format.Contains("does not support NVENC", StringComparison.Ordinal)) return;
            try
            {
                var buffer = stackalloc byte[1024];
                var prefix = 0;
                var length = ffmpeg.av_log_format_line2(avcl, level, format, arguments, buffer, 1024, &prefix);
                lock (diagnostics)
                {
                    if (length >= 1024 || diagnostics.Count >= 128)
                        callbackFailure = "Native capability diagnostics exceeded their bounded buffer.";
                    else
                        diagnostics.Add((Marshal.PtrToStringUTF8((nint)buffer) ?? string.Empty).Trim());
                }
            }
            catch (Exception error)
            {
                lock (diagnostics) callbackFailure = error.ToString();
            }
        };
        ffmpeg.av_log_set_callback(callback);
        ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);
        var operation = "find hevc_nvenc";
        var nativeResult = 0;
        try
        {
            var codec = ffmpeg.avcodec_find_encoder_by_name("hevc_nvenc");
            if (codec == null) return new(Outcome.Unavailable, "HEVC/P010 unavailable: bundled FFmpeg has no hevc_nvenc encoder.");
            operation = "allocate codec context";
            context = ffmpeg.avcodec_alloc_context3(codec);
            if (context == null) throw new OutOfMemoryException("Native codec context allocation failed.");
            context->width = 256;
            context->height = 256;
            context->time_base = new AVRational { num = 1, den = 30 };
            context->framerate = new AVRational { num = 30, den = 1 };
            context->pix_fmt = AVPixelFormat.AV_PIX_FMT_P010LE;
            context->bit_rate = 2_000_000;
            context->gop_size = 30;
            context->max_b_frames = 0;
            context->color_primaries = AVColorPrimaries.AVCOL_PRI_BT2020;
            context->color_trc = AVColorTransferCharacteristic.AVCOL_TRC_SMPTE2084;
            context->colorspace = AVColorSpace.AVCOL_SPC_BT2020_NCL;
            operation = "set p4 preset";
            var optionResult = ffmpeg.av_opt_set(context->priv_data, "preset", "p4", 0);
            if (optionResult < 0) throw new InvalidOperationException($"Setting probe preset failed: {optionResult}");
            operation = "open 256x256 HEVC/P010 codec";
            nativeResult = ffmpeg.avcodec_open2(context, codec, null);
        }
        catch (Exception error)
        {
            return new(Outcome.Unexpected, operation + ": " + error, Diagnostics: diagnostics.ToArray());
        }
        finally
        {
            if (context != null) ffmpeg.avcodec_free_context(&context);
            // The child is done with native work; do not leave a callback pointing at a collected delegate.
            ffmpeg.av_log_set_callback(null);
            GC.KeepAlive(callback);
        }
        return callbackFailure == null
            ? ClassifyOpen(nativeResult, diagnostics.ToArray())
            : new(Outcome.Unexpected, callbackFailure, nativeResult, diagnostics.ToArray());
    }

    internal static Result ClassifyOpen(int nativeResult, string[] diagnostics)
    {
        if (nativeResult >= 0) return new(Outcome.Available, "256x256 HEVC/P010 codec opened successfully.", nativeResult, diagnostics);
        if (nativeResult == ffmpeg.AVERROR(ffmpeg.ENOMEM) || nativeResult == ffmpeg.AVERROR(ffmpeg.EINVAL))
            return new(Outcome.Unexpected, "Native codec opening reported allocation failure or invalid arguments.", nativeResult, diagnostics);
        var messages = diagnostics.Where(message => !string.IsNullOrWhiteSpace(message)).ToArray();
        var reason = messages.FirstOrDefault(IsEnvironmentalReason);
        // A generic aggregate error can also follow invalid parameters or allocation failures.
        // Require a specific environment reason and reject any unexplained companion diagnostic.
        if (reason != null && messages.All(message => IsEnvironmentalReason(message) || IsEnvironmentalSummary(message)))
            return new(Outcome.Unavailable, "HEVC/P010 unavailable: " + reason, nativeResult, diagnostics);
        return new(Outcome.Unexpected, "Independent HEVC/P010 codec open failed unexpectedly.", nativeResult, diagnostics);
    }

    private static bool IsEnvironmentalReason(string message)
        => message is "Cannot load nvcuda.dll" or "Cannot load nvEncodeAPI64.dll" or
            "No CUDA capable devices found" or "Codec not supported" or "10 bit encode not supported" or
            "does not support NVENC"
        || message.StartsWith("Driver does not support the required nvenc API version.", StringComparison.Ordinal)
        || message.StartsWith("OpenEncodeSessionEx failed: unsupported device (", StringComparison.Ordinal)
        || message.StartsWith("OpenEncodeSessionEx failed: no encode device (", StringComparison.Ordinal)
        || message.Contains("CUDA_ERROR_NO_DEVICE:", StringComparison.Ordinal);

    private static bool IsEnvironmentalSummary(string message)
        => message == "No capable devices found"
        || message.StartsWith("The minimum required Nvidia driver for nvenc is ", StringComparison.Ordinal);
}
