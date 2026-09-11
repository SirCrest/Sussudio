using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Runtime;

internal enum NativeSplitEncodeProbeOutcome
{
    Failed,
    Accepted,
    Unavailable
}

internal sealed record NativeSplitEncodeProbeResult
{
    public int ProtocolVersion { get; init; }
    public int Mode { get; init; }
    public string RuntimeRoot { get; init; } = string.Empty;
    public string RuntimeVersions { get; init; } = string.Empty;
    public string Codec { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public NativeSplitEncodeProbeOutcome Outcome { get; init; }
    public string Operation { get; init; } = string.Empty;
    public int? NativeErrorCode { get; init; }
    public string? Error { get; init; }
    public int PacketCount { get; init; }
    public long ElapsedMilliseconds { get; init; }
}

// Runs only in a supervised app child, before App, the instance mutex, or Logger.
// A successful fixture proves option acceptance, not the number of physical NVENC engines.
internal static class NativeFfmpegCapabilityProbe
{
    internal const string ChildFlag = "--native-ffmpeg-split-probe";
    internal const int ProtocolVersion = 1;
    internal const int ProbeWidth = 3840;
    internal const int ProbeHeight = 2160;
    private const string ProbeCodec = "hevc_nvenc";
    private const int MaximumResultCharacters = 16_384;

    internal static bool TryRunChildProcess(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length == 0 || !string.Equals(args[0], ChildFlag, StringComparison.Ordinal))
        {
            return false;
        }

        // Do not initialize native bindings or Logger while parsing normal app arguments.
        if (!TryParseArguments(args, out var runtimeRoot, out var mode, out var logRoot) || Directory.Exists(logRoot))
        {
            Console.Error.WriteLine("Invalid native FFmpeg probe arguments.");
            exitCode = 2;
            return true;
        }

        try
        {
            // RuntimePaths normally falls back when an override cannot be
            // created. A private probe must fail before that can reach app logs.
            Directory.CreateDirectory(logRoot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"Native FFmpeg probe log directory is unavailable: {ex.Message}");
            exitCode = 2;
            return true;
        }

        // Logger rotates its file on first use. A child must never use the live app's log.
        Environment.SetEnvironmentVariable("SUSSUDIO_LOG_ROOT", logRoot);
        var result = RunNativeTrial(runtimeRoot, mode);
        // This process owns its log and is already bounded by the supervisor.
        // Drain queued diagnostics before its background writer disappears.
        Logger.ShutdownAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
        Console.Out.WriteLine(JsonSerializer.Serialize(result));
        exitCode = result.Outcome == NativeSplitEncodeProbeOutcome.Failed ? 1 : 0;
        return true;
    }

    internal static string CreateArguments(string runtimeRoot, int mode, string logRoot)
    {
        if (mode is not (2 or 3))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return $"{ChildFlag} --protocol-version {ProtocolVersion} " +
            $"--runtime-root {QuoteDirectory(runtimeRoot)} --mode {mode} --log-root {QuoteDirectory(logRoot)}";
    }

    private static string QuoteDirectory(string path)
    {
        if (!Path.IsPathFullyQualified(path) || path.IndexOf('"') >= 0)
        {
            throw new ArgumentException("The probe requires an absolute directory path.", nameof(path));
        }

        var directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        // A drive/UNC root can retain a final backslash; double it before the closing quote.
        return '"' + directory + (directory.EndsWith('\\') ? "\\" : string.Empty) + '"';
    }

    private static bool TryParseArguments(string[] args, out string runtimeRoot, out int mode, out string logRoot)
    {
        runtimeRoot = string.Empty;
        logRoot = string.Empty;
        mode = 0;
        if (args.Length != 9 || args[1] != "--protocol-version" ||
            args[2] != ProtocolVersion.ToString(CultureInfo.InvariantCulture) ||
            args[3] != "--runtime-root" || args[5] != "--mode" || args[7] != "--log-root" ||
            !int.TryParse(args[6], NumberStyles.None, CultureInfo.InvariantCulture, out mode) || mode is not (2 or 3) ||
            !Path.IsPathFullyQualified(args[4]) || !Path.IsPathFullyQualified(args[8]))
        {
            return false;
        }

        try
        {
            runtimeRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[4]));
            logRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[8]));
            return !string.Equals(runtimeRoot, logRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    internal static bool ReadAcceptedResult(
        ProcessRunResult process,
        string runtimeRoot,
        string runtimeVersions,
        int mode)
    {
        if (process.GetOutputReadFailure() is { } readFailure)
        {
            throw new InvalidOperationException($"Native split probe output capture failed: {readFailure.Message}", readFailure);
        }

        if (!process.Started || process.TimedOut || !process.ExitConfirmed)
        {
            throw new InvalidOperationException(
                $"Native split probe did not complete: started={process.Started} timedOut={process.TimedOut} " +
                $"exitConfirmed={process.ExitConfirmed} pid={process.ProcessId}.", process.StartException);
        }

        NativeSplitEncodeProbeResult? result;
        try
        {
            if (process.StdOut.Length > MaximumResultCharacters)
            {
                throw new JsonException("Native probe result exceeded its size limit.");
            }

            result = JsonSerializer.Deserialize<NativeSplitEncodeProbeResult>(process.StdOut);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Native split probe returned an invalid result.", ex);
        }

        if (result == null || result.ProtocolVersion != ProtocolVersion || result.Mode != mode ||
            !string.Equals(result.RuntimeRoot, runtimeRoot, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(result.RuntimeVersions, runtimeVersions, StringComparison.Ordinal) ||
            result.Codec != ProbeCodec || result.Width != ProbeWidth || result.Height != ProbeHeight)
        {
            throw new InvalidOperationException("Native split probe result did not match the selected runtime and requested fixture.");
        }

        if (process.ExitCode == 0 && result.Outcome == NativeSplitEncodeProbeOutcome.Unavailable)
        {
            return false;
        }

        if (process.ExitCode != 0 || result.Outcome != NativeSplitEncodeProbeOutcome.Accepted || result.PacketCount <= 0)
        {
            throw new InvalidOperationException(
                $"Native split probe failed at {result.Operation}: {result.Error ?? "no encoded packet"} " +
                $"(nativeError={result.NativeErrorCode}, exitCode={process.ExitCode}).");
        }

        return true;
    }

    private static unsafe NativeSplitEncodeProbeResult RunNativeTrial(string runtimeRoot, int mode)
    {
        var timer = Stopwatch.StartNew();
        var result = new NativeSplitEncodeProbeResult
        {
            ProtocolVersion = ProtocolVersion,
            Mode = mode,
            RuntimeRoot = runtimeRoot,
            Codec = ProbeCodec,
            Width = ProbeWidth,
            Height = ProbeHeight
        };
        AVCodecContext* context = null;
        AVFrame* frame = null;
        AVPacket* packet = null;
        var operation = "initialize runtime";
        int? nativeError = null;
        try
        {
            FfmpegRuntimeInit.EnsureInitializedAtRoot(runtimeRoot);
            result = result with { RuntimeVersions = FfmpegRuntimeInit.GetInitializedRuntimeVersions() };
            operation = "avcodec_find_encoder_by_name";
            var codec = ffmpeg.avcodec_find_encoder_by_name(ProbeCodec);
            if (codec == null)
            {
                return result with { Outcome = NativeSplitEncodeProbeOutcome.Unavailable, Operation = operation, ElapsedMilliseconds = timer.ElapsedMilliseconds };
            }

            operation = "avcodec_alloc_context3";
            context = ffmpeg.avcodec_alloc_context3(codec);
            if (context == null)
            {
                throw new InvalidOperationException("Codec context allocation failed.");
            }

            operation = "av_opt_find(split_encode_mode)";
            if (ffmpeg.av_opt_find(context->priv_data, "split_encode_mode", null, 0, 0) == null)
            {
                return result with { Outcome = NativeSplitEncodeProbeOutcome.Unavailable, Operation = operation, ElapsedMilliseconds = timer.ElapsedMilliseconds };
            }

            context->width = ProbeWidth;
            context->height = ProbeHeight;
            context->time_base = new AVRational { num = 1, den = 60 };
            context->framerate = new AVRational { num = 60, den = 1 };
            context->pix_fmt = AVPixelFormat.AV_PIX_FMT_NV12;
            context->bit_rate = 10_000_000;
            context->gop_size = 60;
            context->max_b_frames = 0;
            Check(ffmpeg.av_opt_set(context->priv_data, "preset", "p4", 0), "av_opt_set(preset)");
            Check(ffmpeg.av_opt_set_int(context->priv_data, "delay", 0, 0), "av_opt_set_int(delay)");
            Check(ffmpeg.av_opt_set_int(context->priv_data, "split_encode_mode", mode, 0), "av_opt_set_int(split_encode_mode)");
            Check(ffmpeg.avcodec_open2(context, codec, null), "avcodec_open2");

            operation = "av_frame_alloc";
            frame = ffmpeg.av_frame_alloc();
            if (frame == null)
            {
                throw new InvalidOperationException("Frame allocation failed.");
            }

            frame->format = (int)AVPixelFormat.AV_PIX_FMT_NV12;
            frame->width = ProbeWidth;
            frame->height = ProbeHeight;
            frame->pts = 0;
            Check(ffmpeg.av_frame_get_buffer(frame, 32), "av_frame_get_buffer");
            for (var row = 0; row < ProbeHeight; row++)
            {
                new Span<byte>(frame->data[0] + row * frame->linesize[0], ProbeWidth).Fill(16);
            }
            for (var row = 0; row < ProbeHeight / 2; row++)
            {
                new Span<byte>(frame->data[1] + row * frame->linesize[1], ProbeWidth).Fill(128);
            }

            operation = "av_packet_alloc";
            packet = ffmpeg.av_packet_alloc();
            if (packet == null)
            {
                throw new InvalidOperationException("Packet allocation failed.");
            }

            var packetCount = 0;
            Send(context, packet, frame, ref packetCount);
            Drain(context, packet, ref packetCount);
            Send(context, packet, null, ref packetCount);
            var drainResult = Drain(context, packet, ref packetCount);
            if (drainResult != ffmpeg.AVERROR_EOF || packetCount == 0)
            {
                operation = "drain encoder";
                throw new InvalidOperationException("The encoder did not finish the one-frame trial with a packet.");
            }

            result = result with { Outcome = NativeSplitEncodeProbeOutcome.Accepted, PacketCount = packetCount, Operation = "completed" };

            void Send(AVCodecContext* codecContext, AVPacket* outputPacket, AVFrame* input, ref int packets)
            {
                var sendResult = ffmpeg.avcodec_send_frame(codecContext, input);
                if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Drain(codecContext, outputPacket, ref packets);
                    sendResult = ffmpeg.avcodec_send_frame(codecContext, input);
                }
                Check(sendResult, input == null ? "avcodec_send_frame(flush)" : "avcodec_send_frame");
            }

            int Drain(AVCodecContext* codecContext, AVPacket* outputPacket, ref int packets)
            {
                while (true)
                {
                    var receiveResult = ffmpeg.avcodec_receive_packet(codecContext, outputPacket);
                    if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
                    {
                        return receiveResult;
                    }
                    Check(receiveResult, "avcodec_receive_packet");
                    if (outputPacket->size > 0)
                    {
                        packets++;
                    }
                    ffmpeg.av_packet_unref(outputPacket);
                }
            }
        }
        catch (Exception ex)
        {
            result = result with
            {
                Outcome = NativeSplitEncodeProbeOutcome.Failed,
                Operation = operation,
                NativeErrorCode = nativeError,
                Error = ex.Message
            };
        }
        finally
        {
            // Missing/incompatible libraries can fail before any native call. Do
            // not resolve free functions unless their corresponding allocation ran.
            if (packet != null) ffmpeg.av_packet_free(&packet);
            if (frame != null) ffmpeg.av_frame_free(&frame);
            if (context != null) ffmpeg.avcodec_free_context(&context);
        }

        return result with { ElapsedMilliseconds = timer.ElapsedMilliseconds };

        void Check(int code, string currentOperation)
        {
            operation = currentOperation;
            if (code < 0)
            {
                nativeError = code;
                throw new InvalidOperationException($"{currentOperation} returned {code}.");
            }
        }
    }
}
