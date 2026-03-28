using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

internal static class Program
{
    private sealed record Options(
        string? DeviceNameFilter,
        uint? Width,
        uint? Height,
        double? Fps,
        int Frames,
        int TimeoutSeconds,
        bool ExpectHdr);

    private sealed record FrameCopyInfo(
        int Width,
        int Height,
        int YStrideBytes,
        int UVStrideBytes,
        int TightFrameBytes);

    private sealed class CaptureRunState
    {
        public int TargetFrames { get; init; }
        public int CapturedFrames;
        public int P010Frames;
        public int NonP010Frames;
        public int? FirstNonP010FrameIndex;
        public FrameCopyInfo? FirstFrameCopyInfo;
        public string? LastObservedPixelFormat;
        public Exception? TerminalError;
    }

    private static async Task<int> Main(string[] args)
    {
        Options options;
        try
        {
            options = ParseOptions(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            PrintUsage();
            return 2;
        }

        var repoRoot = ResolveRepoRoot();
        var artifactRoot = Path.Combine(
            repoRoot,
            "artifacts",
            "hdr-lab",
            DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(artifactRoot);
        var logPath = Path.Combine(artifactRoot, "hdr-lab.log");
        var rawPath = Path.Combine(artifactRoot, "capture_p010.raw");
        var sidecarPath = Path.Combine(artifactRoot, "capture_p010.json");

        var logBuilder = new StringBuilder();
        void Log(string message)
        {
            var line = $"{DateTime.UtcNow:O} {message}";
            Console.WriteLine(line);
            logBuilder.AppendLine(line);
        }

        Log($"artifactRoot={artifactRoot}");
        Log("mf_readwrite_disable_converters=true (intent) source=MediaCapture");
        Log($"expect_hdr={options.ExpectHdr} target_frames={options.Frames}");

        var mediaCapture = new MediaCapture();
        MediaFrameReader? frameReader = null;
        var runState = new CaptureRunState { TargetFrames = options.Frames };
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sync = new object();
        byte[]? frameBuffer = null;

        try
        {
            var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (videoDevices.Count == 0)
            {
                throw new InvalidOperationException("No video capture devices found.");
            }

            var selectedDevice = SelectDevice(videoDevices, options.DeviceNameFilter);
            Log($"selected_device_name={selectedDevice.Name}");
            Log($"selected_device_id={selectedDevice.Id}");

            var initSettings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = selectedDevice.Id,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly
            };
            await mediaCapture.InitializeAsync(initSettings);
            Log("mediacapture_initialized=true");

            var frameSource = mediaCapture.FrameSources.Values
                .FirstOrDefault(source => source.Info.SourceKind == MediaFrameSourceKind.Color);
            if (frameSource == null)
            {
                throw new InvalidOperationException("No color frame source is available.");
            }

            Log($"frame_source_id={frameSource.Info.Id}");
            Log($"frame_source_stream_type={frameSource.Info.MediaStreamType}");

            foreach (var format in frameSource.SupportedFormats)
            {
                Log($"format subtype={format.Subtype} size={format.VideoFormat.Width}x{format.VideoFormat.Height} fps={ToFps(format):0.###}");
            }

            var selectedFormat = ChooseP010Format(frameSource, options);
            if (selectedFormat == null)
            {
                throw new InvalidOperationException("No P010 format candidate matched requested constraints.");
            }

            await frameSource.SetFormatAsync(selectedFormat);
            Log($"selected_format_subtype={selectedFormat.Subtype}");
            Log($"selected_format_size={selectedFormat.VideoFormat.Width}x{selectedFormat.VideoFormat.Height}");
            Log($"selected_format_fps={ToFps(selectedFormat):0.###}");

            frameReader = await mediaCapture.CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.P010);
            if (frameReader.AcquisitionMode != MediaFrameReaderAcquisitionMode.Realtime)
            {
                frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            }

            using var fileStream = new FileStream(rawPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            frameReader.FrameArrived += (sender, _) =>
            {
                lock (sync)
                {
                    if (completion.Task.IsCompleted)
                    {
                        return;
                    }

                    try
                    {
                        using var frameReference = sender.TryAcquireLatestFrame();
                        if (frameReference?.VideoMediaFrame?.SoftwareBitmap == null)
                        {
                            return;
                        }

                        using var bitmap = frameReference.VideoMediaFrame.SoftwareBitmap;
                        runState.CapturedFrames++;
                        runState.LastObservedPixelFormat = bitmap.BitmapPixelFormat.ToString();

                        if (bitmap.BitmapPixelFormat != BitmapPixelFormat.P010)
                        {
                            runState.NonP010Frames++;
                            runState.FirstNonP010FrameIndex ??= runState.CapturedFrames;
                            Log($"frame_index={runState.CapturedFrames} observed_non_p010={bitmap.BitmapPixelFormat}");
                            if (options.ExpectHdr)
                            {
                                throw new InvalidOperationException(
                                    $"HDR mode requires P010, but observed {bitmap.BitmapPixelFormat} at frame {runState.CapturedFrames}.");
                            }

                            if (runState.CapturedFrames >= runState.TargetFrames)
                            {
                                completion.TrySetResult(true);
                            }

                            return;
                        }

                        runState.P010Frames++;
                        if (frameBuffer == null)
                        {
                            frameBuffer = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 3];
                        }

                        var copyInfo = CopyP010Frame(bitmap, frameBuffer);
                        runState.FirstFrameCopyInfo ??= copyInfo;
                        fileStream.Write(frameBuffer, 0, copyInfo.TightFrameBytes);

                        if (runState.CapturedFrames >= runState.TargetFrames)
                        {
                            completion.TrySetResult(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        runState.TerminalError = ex;
                        completion.TrySetException(ex);
                    }
                }
            };

            var startStatus = await frameReader.StartAsync();
            Log($"frame_reader_start_status={startStatus}");
            if (startStatus != MediaFrameReaderStartStatus.Success)
            {
                throw new InvalidOperationException($"Frame reader failed to start: {startStatus}");
            }

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(options.TimeoutSeconds));
            var completedTask = await Task.WhenAny(completion.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException(
                    $"Timed out after {options.TimeoutSeconds}s waiting for {options.Frames} frames (captured={runState.CapturedFrames}).");
            }

            await completion.Task;
            Log($"capture_complete=true captured={runState.CapturedFrames} p010={runState.P010Frames} non_p010={runState.NonP010Frames}");

            var sidecar = new
            {
                deviceName = selectedDevice.Name,
                deviceId = selectedDevice.Id,
                converterDisableIntent = true,
                expectedHdr = options.ExpectHdr,
                requestedFrames = options.Frames,
                capturedFrames = runState.CapturedFrames,
                p010Frames = runState.P010Frames,
                nonP010Frames = runState.NonP010Frames,
                firstNonP010FrameIndex = runState.FirstNonP010FrameIndex,
                lastObservedPixelFormat = runState.LastObservedPixelFormat,
                firstFrame = runState.FirstFrameCopyInfo,
                rawPath = rawPath
            };
            await File.WriteAllTextAsync(sidecarPath, JsonSerializer.Serialize(sidecar, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            Log($"raw_output={rawPath}");
            Log($"sidecar_output={sidecarPath}");
            Log("HDR_LAB_RESULT PASS");
            await File.WriteAllTextAsync(logPath, logBuilder.ToString());
            return 0;
        }
        catch (Exception ex)
        {
            Log($"HDR_LAB_RESULT FAIL reason={ex.Message}");
            await File.WriteAllTextAsync(logPath, logBuilder.ToString());
            return 1;
        }
        finally
        {
            if (frameReader != null)
            {
                try
                {
                    await frameReader.StopAsync();
                }
                catch
                {
                    // Best effort stop on teardown.
                }

                frameReader.Dispose();
            }

            mediaCapture.Dispose();
        }
    }

    private static DeviceInformation SelectDevice(DeviceInformationCollection devices, string? filter)
    {
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var matched = devices.FirstOrDefault(d =>
                d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                return matched;
            }

            throw new InvalidOperationException($"No video device matched filter '{filter}'.");
        }

        return devices[0];
    }

    private static MediaFrameFormat? ChooseP010Format(MediaFrameSource frameSource, Options options)
    {
        var p010Formats = frameSource.SupportedFormats
            .Where(format => string.Equals(format.Subtype, MediaEncodingSubtypes.P010, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (p010Formats.Count == 0)
        {
            return null;
        }

        MediaFrameFormat? exact = p010Formats.FirstOrDefault(format =>
            MatchesDimensions(format, options.Width, options.Height) &&
            MatchesFps(format, options.Fps));
        if (exact != null)
        {
            return exact;
        }

        MediaFrameFormat? sizeOnly = p010Formats.FirstOrDefault(format =>
            MatchesDimensions(format, options.Width, options.Height));
        if (sizeOnly != null)
        {
            return sizeOnly;
        }

        return p010Formats[0];
    }

    private static bool MatchesDimensions(MediaFrameFormat format, uint? width, uint? height)
    {
        if (!width.HasValue || !height.HasValue)
        {
            return true;
        }

        return format.VideoFormat.Width == width.Value &&
               format.VideoFormat.Height == height.Value;
    }

    private static bool MatchesFps(MediaFrameFormat format, double? fps)
    {
        if (!fps.HasValue)
        {
            return true;
        }

        var value = ToFps(format);
        return Math.Abs(value - fps.Value) < 0.05;
    }

    private static double ToFps(MediaFrameFormat format)
    {
        var rate = format.FrameRate;
        if (rate.Denominator == 0)
        {
            return 0;
        }

        return rate.Numerator / (double)rate.Denominator;
    }

    private static unsafe FrameCopyInfo CopyP010Frame(SoftwareBitmap frame, byte[] destination)
    {
        using var bitmapBuffer = frame.LockBuffer(BitmapBufferAccessMode.Read);
        using var reference = bitmapBuffer.CreateReference();

        byte* srcBytes;
        uint srcCapacity;
        reference.As<IMemoryBufferByteAccess>().GetBuffer(out srcBytes, out srcCapacity);

        var planeY = bitmapBuffer.GetPlaneDescription(0);
        var planeUV = bitmapBuffer.GetPlaneDescription(1);

        var width = frame.PixelWidth;
        var height = frame.PixelHeight;
        var yRowBytes = width * 2;
        var uvRowBytes = width * 2;
        var uvHeight = height / 2;
        var yBytes = yRowBytes * height;
        var uvBytes = uvRowBytes * uvHeight;
        var totalBytes = yBytes + uvBytes;
        if (destination.Length < totalBytes)
        {
            throw new ArgumentException("Destination buffer is too small for tight P010 frame copy.");
        }

        fixed (byte* dest = destination)
        {
            for (var row = 0; row < height; row++)
            {
                Buffer.MemoryCopy(
                    srcBytes + planeY.StartIndex + (row * planeY.Stride),
                    dest + (row * yRowBytes),
                    yRowBytes,
                    yRowBytes);
            }

            var uvDest = dest + yBytes;
            for (var row = 0; row < uvHeight; row++)
            {
                Buffer.MemoryCopy(
                    srcBytes + planeUV.StartIndex + (row * planeUV.Stride),
                    uvDest + (row * uvRowBytes),
                    uvRowBytes,
                    uvRowBytes);
            }
        }

        return new FrameCopyInfo(width, height, planeY.Stride, planeUV.Stride, totalBytes);
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ElgatoCapture.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (ElgatoCapture.slnx).");
    }

    private static Options ParseOptions(string[] args)
    {
        string? device = null;
        uint? width = null;
        uint? height = null;
        double? fps = null;
        var frames = 120;
        var timeout = 45;
        var expectHdr = true;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--device":
                    device = ReadValue(args, ref i, "--device");
                    break;
                case "--width":
                    width = uint.Parse(ReadValue(args, ref i, "--width"));
                    break;
                case "--height":
                    height = uint.Parse(ReadValue(args, ref i, "--height"));
                    break;
                case "--fps":
                    fps = double.Parse(ReadValue(args, ref i, "--fps"));
                    break;
                case "--frames":
                    frames = int.Parse(ReadValue(args, ref i, "--frames"));
                    break;
                case "--timeout":
                    timeout = int.Parse(ReadValue(args, ref i, "--timeout"));
                    break;
                case "--expect-hdr":
                    expectHdr = true;
                    break;
                case "--allow-sdr":
                    expectHdr = false;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        if (frames <= 0)
        {
            throw new ArgumentException("--frames must be > 0.");
        }

        if (timeout <= 0)
        {
            throw new ArgumentException("--timeout must be > 0.");
        }

        return new Options(device, width, height, fps, frames, timeout, expectHdr);
    }

    private static string ReadValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        index++;
        return args[index];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tests/ElgatoCapture.HdrLab -- [--device <name contains>] [--width <uint>] [--height <uint>] [--fps <double>] [--frames <int>] [--timeout <seconds>] [--expect-hdr|--allow-sdr]");
    }
}

[ComImport]
[Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal unsafe interface IMemoryBufferByteAccess
{
    void GetBuffer(out byte* buffer, out uint capacity);
}
