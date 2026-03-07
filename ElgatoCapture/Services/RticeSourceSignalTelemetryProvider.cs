using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

public sealed class RticeSourceSignalTelemetryProvider : ISourceSignalTelemetryProvider
{
    private const string RticeDllFileName = "RTICE_SDK_x64.dll";
    private const string RtkIoDllFileName = "RTK_IO_x64.dll";
    private const string RticeDllDirectoryEnvVar = "ELGATOCAPTURE_RTICE_DLL_DIR";
    private const string DefaultStudioPackageDir = @"C:\Program Files\WindowsApps\Elgato.Studio_1.0.5.895_x64__g54w8ztgkx496";
    private const int UsbUvcPortType = 2;
    private const int DefaultGateTimeoutMs = 750;
    private const int DefaultBufferSize = 256;
    private const ushort Elgato4kXVendorId = 0x0FD9;
    private const ushort Elgato4kXProductId = 0x009B;

    private static readonly object NativeLoadGate = new();
    private static readonly SemaphoreSlim RticeCallGate = new(1, 1);
    private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap = new Dictionary<int, VicTiming>
    {
        [97] = new(3840, 2160, 60.0, false),
        [96] = new(3840, 2160, 50.0, false),
        [95] = new(3840, 2160, 30.0, false),
        [94] = new(3840, 2160, 25.0, false),
        [93] = new(3840, 2160, 24.0, false),
        [16] = new(1920, 1080, 60.0, false),
        [31] = new(1920, 1080, 50.0, false),
        [34] = new(1920, 1080, 30.0, false),
        [32] = new(1920, 1080, 24.0, false),
        [5] = new(1920, 1080, 60.0, true),
        [4] = new(1280, 720, 60.0, false),
        [19] = new(1280, 720, 50.0, false)
    };

    private static RticeNativeBindings? s_nativeBindings;
    private static string? s_cachedLoadableDllDirectory;

    public async Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("device-unavailable");
        }

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            vendorId != Elgato4kXVendorId ||
            productId != Elgato4kXProductId)
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("rtice-device-unsupported");
        }

        var gateAcquired = false;
        var initialized = false;
        var portOpened = false;

        try
        {
            var gateTimeoutMs = GetIntFromEnv("ELGATOCAPTURE_RTICE_GATE_TIMEOUT_MS", DefaultGateTimeoutMs, 100, 10000);
            gateAcquired = await RticeCallGate.WaitAsync(gateTimeoutMs, cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable("rtice-native-busy", $"{gateTimeoutMs}ms");
            }

            var bindings = EnsureBindingsLoaded();

            var initializeResult = bindings.Initialize();
            Logger.LogVerbose($"RTICE_INITIALIZE result={initializeResult}");
            if (initializeResult != 0)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable(
                    "rtice-initialize-failed",
                    $"result={initializeResult}");
            }

            initialized = true;

            var setPortResult = bindings.SetPort(UsbUvcPortType, string.Empty);
            Logger.LogVerbose($"RTICE_SET_PORT result={setPortResult} portType={UsbUvcPortType}");
            if (setPortResult != 0)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable(
                    "rtice-port-open-failed",
                    $"setPort={setPortResult}");
            }

            var openPortResult = bindings.OpenPort();
            Logger.LogVerbose($"RTICE_OPEN_PORT result={openPortResult}");
            if (openPortResult != 0)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable(
                    "rtice-port-open-failed",
                    $"openPort={openPortResult}");
            }

            portOpened = true;
            Logger.LogVerbose($"RTICE_PORT_STATE isOpen={bindings.IsOpen()}");

            var cable = InvokeIntCommand("AT_HdmiRX_Get_Cable_Connect", bindings.GetCableConnect);
            if (cable.IsSuccess && cable.Value == 0)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable("rtice-no-cable");
            }

            var hdr2Sdr = InvokeIntCommand("AT_Get_HDR2SDR_OnOff_Status", bindings.GetHdr2SdrOnOffStatus);
            var outputTiming = InvokeIntCommand("AT_UVC_Get_Output_Timing", bindings.GetUvcOutputTiming);
            var videoFormat = InvokeIntCommand("AT_UVC_Get_VideoFormat", bindings.GetUvcVideoFormat);
            var vfreq = InvokeIntCommand("AT_Get_HdmiRX_Video_Vfreq", bindings.GetHdmiVideoVfreq);
            var vicBuffer = InvokeBufferCommand("AT_HdmiRX_Get_VIC", bindings.GetVic);
            var aviInfo = InvokeBufferCommand("AT_Get_HdmiRX_AVI_Infoframe", bindings.GetAviInfoFrame);
            var hdrMetadata = InvokeBufferCommand("AT_System_Get_HDRmetadata_cmd", bindings.GetHdrMetadata);
            var systemInfo = InvokeBufferCommand("AT_Get_System_Info", bindings.GetSystemInfo);

            var vicCode = ExtractVicCode(vicBuffer.Buffer) ?? ExtractVicCodeFromAviInfoFrame(aviInfo.Buffer);
            var timing = vicCode.HasValue && VicTimingMap.TryGetValue(vicCode.Value, out var mappedTiming)
                ? mappedTiming
                : (VicTiming?)null;
            var frameRateExact = ResolveFrameRateExact(vfreq, timing);
            var hdrInfo = DecodeHdrMetadata(hdrMetadata.Buffer);
            var aviInfoFrame = DecodeAviInfoFrame(aviInfo.Buffer);
            var systemInfoString = DecodeCString(systemInfo.Buffer);

            if (!timing.HasValue && !frameRateExact.HasValue && !hdrInfo.HasMetadata && !aviInfoFrame.HasData)
            {
                Logger.Log("RTICE_SIGNAL_UNAVAILABLE reason=no-decodable-source-data");
                return SourceSignalTelemetrySnapshot.CreateUnavailable("rtice-no-signal-data");
            }

            Logger.Log(
                $"RTICE_DECODE vic={(vicCode.HasValue ? vicCode.Value.ToString(CultureInfo.InvariantCulture) : "none")} " +
                $"size={timing?.Width.ToString(CultureInfo.InvariantCulture) ?? "?"}x{timing?.Height.ToString(CultureInfo.InvariantCulture) ?? "?"} " +
                $"fps={(frameRateExact.HasValue ? frameRateExact.Value.ToString("0.###", CultureInfo.InvariantCulture) : "?")} " +
                $"hdr={BoolToToken(hdrInfo.IsHdr)} colorspace={aviInfoFrame.ColorSpace ?? "unknown"} " +
                $"colorimetry={aviInfoFrame.Colorimetry ?? "unknown"} firmware={systemInfoString ?? "unknown"}");

            return new SourceSignalTelemetrySnapshot
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Availability = SourceTelemetryAvailability.Available,
                Origin = SourceTelemetryOrigin.Rtice,
                OriginDetail = BuildOriginDetail(bindings.DllDirectory),
                Confidence = ResolveConfidence(vicCode.HasValue, hdrInfo, aviInfoFrame, frameRateExact),
                Width = timing?.Width,
                Height = timing?.Height,
                FrameRateExact = frameRateExact,
                FrameRateArg = InferFrameRateRational(frameRateExact),
                IsHdr = hdrInfo.IsHdr,
                DiagnosticSummary = BuildDiagnosticSummary(
                    vicCode,
                    timing,
                    frameRateExact,
                    hdrInfo,
                    aviInfoFrame,
                    hdr2Sdr,
                    outputTiming,
                    videoFormat,
                    systemInfoString),
                AudioInputAvailability = SourceAudioInputAvailability.Unavailable,
                AudioInputOrigin = "not-implemented"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException ex)
        {
            Logger.Log($"RTICE_DLL_NOT_FOUND detail={ex.Message}");
            return SourceSignalTelemetrySnapshot.CreateUnavailable("rtice-dll-not-found", ex.Message);
        }
        catch (DllNotFoundException ex)
        {
            Logger.Log($"RTICE_DLL_LOAD_FAILED type={ex.GetType().Name} message={ex.Message}");
            return SourceSignalTelemetrySnapshot.CreateUnavailable("rtice-dll-not-found", ex.Message);
        }
        catch (Exception ex)
        {
            Logger.Log($"RTICE_EXCEPTION type={ex.GetType().Name} message={ex.Message}");
            return SourceSignalTelemetrySnapshot.CreateUnavailable("rtice-exception", $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (portOpened)
            {
                try
                {
                    var closePortResult = s_nativeBindings?.ClosePort() ?? int.MinValue;
                    Logger.LogVerbose($"RTICE_CLOSE_PORT result={closePortResult}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"RTICE_CLOSE_PORT_FAILED type={ex.GetType().Name} message={ex.Message}");
                }
            }

            if (initialized)
            {
                try
                {
                    s_nativeBindings?.Uninitialize();
                    Logger.LogVerbose("RTICE_UNINITIALIZE result=void");
                }
                catch (Exception ex)
                {
                    Logger.Log($"RTICE_UNINITIALIZE_FAILED type={ex.GetType().Name} message={ex.Message}");
                }
            }

            if (gateAcquired)
            {
                RticeCallGate.Release();
            }
        }
    }

    private static RticeNativeBindings EnsureBindingsLoaded()
    {
        lock (NativeLoadGate)
        {
            if (s_nativeBindings != null)
            {
                return s_nativeBindings.Value;
            }

            var loadableDllDirectory = ResolveLoadableDllDirectory();
            if (string.IsNullOrWhiteSpace(loadableDllDirectory))
            {
                throw new FileNotFoundException("RTICE DLL directory could not be resolved.");
            }

            var rtkIoPath = Path.Combine(loadableDllDirectory, RtkIoDllFileName);
            var rticePath = Path.Combine(loadableDllDirectory, RticeDllFileName);
            nint rtkIoHandle = IntPtr.Zero;
            nint rticeHandle = IntPtr.Zero;

            try
            {
                using var pathScope = new ProcessPathScope(loadableDllDirectory);
                rtkIoHandle = NativeLibrary.Load(rtkIoPath);
                rticeHandle = NativeLibrary.Load(rticePath);
                var bindings = new RticeNativeBindings(
                    loadableDllDirectory,
                    rtkIoHandle,
                    rticeHandle,
                    GetRequiredExport<InitializeDelegate>(rticeHandle, "initialize"),
                    GetRequiredExport<UninitializeDelegate>(rticeHandle, "uninitialize"),
                    GetRequiredExport<SetPortDelegate>(rticeHandle, "setPort"),
                    GetRequiredExport<OpenPortDelegate>(rticeHandle, "openPort"),
                    GetRequiredExport<ClosePortDelegate>(rticeHandle, "closePort"),
                    GetRequiredExport<IsOpenDelegate>(rticeHandle, "isOpen"),
                    GetRequiredExport<IntOutDelegate>(rticeHandle, "AT_HdmiRX_Get_Cable_Connect"),
                    GetRequiredExport<IntOutDelegate>(rticeHandle, "AT_Get_HDR2SDR_OnOff_Status"),
                    GetRequiredExport<IntOutDelegate>(rticeHandle, "AT_UVC_Get_Output_Timing"),
                    GetRequiredExport<IntOutDelegate>(rticeHandle, "AT_UVC_Get_VideoFormat"),
                    GetRequiredExport<IntOutDelegate>(rticeHandle, "AT_Get_HdmiRX_Video_Vfreq"),
                    GetRequiredExport<BufferOutDelegate>(rticeHandle, "AT_HdmiRX_Get_VIC"),
                    GetRequiredExport<BufferOutDelegate>(rticeHandle, "AT_Get_HdmiRX_AVI_Infoframe"),
                    GetRequiredExport<BufferOutDelegate>(rticeHandle, "AT_System_Get_HDRmetadata_cmd"),
                    GetRequiredExport<BufferOutDelegate>(rticeHandle, "AT_Get_System_Info"));
                s_nativeBindings = bindings;

                Logger.Log(
                    $"RTICE_DLL_RESOLVED dir='{loadableDllDirectory}' rtice='{rticePath}' rtkIo='{rtkIoPath}'");
                return bindings;
            }
            catch
            {
                if (rticeHandle != IntPtr.Zero)
                {
                    NativeLibrary.Free(rticeHandle);
                }

                if (rtkIoHandle != IntPtr.Zero)
                {
                    NativeLibrary.Free(rtkIoHandle);
                }

                s_nativeBindings = null;
                throw;
            }
        }
    }

    private static string? ResolveLoadableDllDirectory()
    {
        lock (NativeLoadGate)
        {
            if (!string.IsNullOrWhiteSpace(s_cachedLoadableDllDirectory) &&
                HasRequiredDlls(s_cachedLoadableDllDirectory))
            {
                return s_cachedLoadableDllDirectory;
            }
        }

        var resolved = ResolveDllDirectory();
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return null;
        }

        var loadable = PrepareDllDirectoryForLoading(resolved);
        lock (NativeLoadGate)
        {
            s_cachedLoadableDllDirectory = loadable;
        }

        return loadable;
    }

    private static string? ResolveDllDirectory()
    {
        var envPath = NormalizeDllDirectory(Environment.GetEnvironmentVariable(RticeDllDirectoryEnvVar));
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        var baseDirectory = NormalizeDllDirectory(AppContext.BaseDirectory);
        if (!string.IsNullOrWhiteSpace(baseDirectory))
        {
            return baseDirectory;
        }

        var defaultPackage = NormalizeDllDirectory(DefaultStudioPackageDir);
        if (!string.IsNullOrWhiteSpace(defaultPackage))
        {
            return defaultPackage;
        }

        try
        {
            const string windowsApps = @"C:\Program Files\WindowsApps";
            if (Directory.Exists(windowsApps))
            {
                var candidate = Directory.EnumerateDirectories(windowsApps, "Elgato.Studio_*_x64__*")
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(HasRequiredDlls);
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
        }
        catch
        {
            // Best effort lookup.
        }

        try
        {
            var stagedRoot = Path.Combine(RuntimePaths.GetRepoTempRoot(), "egav", "runtime");
            if (Directory.Exists(stagedRoot))
            {
                var candidate = Directory.EnumerateDirectories(stagedRoot, "Elgato.Studio_*")
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(HasRequiredDlls);
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
        }
        catch
        {
            // Best effort lookup.
        }

        return null;
    }

    private static string PrepareDllDirectoryForLoading(string resolvedDirectory)
    {
        if (!resolvedDirectory.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase))
        {
            return resolvedDirectory;
        }

        var packageName = Path.GetFileName(
            resolvedDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var stageDirectory = Path.Combine(RuntimePaths.GetRepoTempRoot(), "egav", "runtime", packageName);
        Directory.CreateDirectory(stageDirectory);
        CopyRequiredRuntimeDll(resolvedDirectory, stageDirectory, RticeDllFileName);
        CopyRequiredRuntimeDll(resolvedDirectory, stageDirectory, RtkIoDllFileName);
        return stageDirectory;
    }

    private static void CopyRequiredRuntimeDll(string sourceDirectory, string destinationDirectory, string fileName)
    {
        var sourcePath = Path.Combine(sourceDirectory, fileName);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Required runtime DLL '{sourcePath}' was not found.", sourcePath);
        }

        var destinationPath = Path.Combine(destinationDirectory, fileName);
        if (File.Exists(destinationPath))
        {
            return;
        }

        try
        {
            File.Copy(sourcePath, destinationPath, overwrite: false);
        }
        catch (IOException) when (File.Exists(destinationPath))
        {
            // Another poll loop staged this file concurrently.
        }
    }

    private static string? NormalizeDllDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
        if (File.Exists(expanded))
        {
            if (!string.Equals(Path.GetFileName(expanded), RticeDllFileName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(expanded));
            return HasRequiredDlls(directory) ? directory : null;
        }

        if (Directory.Exists(expanded))
        {
            var fullPath = Path.GetFullPath(expanded);
            return HasRequiredDlls(fullPath) ? fullPath : null;
        }

        return null;
    }

    private static bool HasRequiredDlls(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        return File.Exists(Path.Combine(directory, RticeDllFileName)) &&
               File.Exists(Path.Combine(directory, RtkIoDllFileName));
    }

    private static TDelegate GetRequiredExport<TDelegate>(nint libraryHandle, string exportName)
        where TDelegate : Delegate
    {
        var export = NativeLibrary.GetExport(libraryHandle, exportName);
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(export);
    }

    private static RticeIntCommandResult InvokeIntCommand(string name, IntOutDelegate command)
    {
        var result = command(out var value);
        Logger.LogVerbose($"RTICE_AT_INT name={name} result={result} value={value}");
        return new RticeIntCommandResult(result, result == 0 ? value : null);
    }

    private static RticeBufferCommandResult InvokeBufferCommand(string name, BufferOutDelegate command)
    {
        var buffer = new byte[DefaultBufferSize];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var result = command(handle.AddrOfPinnedObject(), buffer.Length);
            Logger.LogVerbose($"RTICE_AT_BUFFER name={name} result={result} preview={GetHexPreview(buffer, 32)}");
            return new RticeBufferCommandResult(result, buffer);
        }
        finally
        {
            handle.Free();
        }
    }

    private static int? ExtractVicCode(byte[] buffer)
    {
        if (!HasNonZeroData(buffer))
        {
            return null;
        }

        return buffer[0] > 0 ? buffer[0] : null;
    }

    private static int? ExtractVicCodeFromAviInfoFrame(byte[] buffer)
    {
        if (buffer.Length <= 7 || !HasNonZeroData(buffer) || buffer[0] != 0x82)
        {
            return null;
        }

        return buffer[7] > 0 ? buffer[7] : null;
    }

    private static readonly double[] CanonicalFrameRates =
    {
        24000.0 / 1001.0,  // 23.976
        24.0,
        25.0,
        30000.0 / 1001.0,  // 29.97
        30.0,
        50.0,
        60000.0 / 1001.0,  // 59.94
        60.0,
        120000.0 / 1001.0, // 119.88
        120.0
    };

    private static double? ResolveFrameRateExact(RticeIntCommandResult vfreq, VicTiming? timing)
    {
        if (vfreq.IsSuccess && vfreq.Value.HasValue && vfreq.Value.Value > 0)
        {
            var raw = vfreq.Value.Value / 100.0;
            return SnapToCanonicalFrameRate(raw);
        }

        return timing?.NominalFrameRate;
    }

    private static double SnapToCanonicalFrameRate(double measured)
    {
        const double tolerance = 0.05;
        foreach (var canonical in CanonicalFrameRates)
        {
            if (Math.Abs(measured - canonical) <= tolerance)
            {
                return canonical;
            }
        }

        return measured;
    }

    private static SourceTelemetryConfidence ResolveConfidence(
        bool hasVicCode,
        HdrMetadataInfo hdrInfo,
        AviInfoFrameInfo aviInfoFrame,
        double? frameRateExact)
    {
        if (hasVicCode && hdrInfo.HasMetadata)
        {
            return SourceTelemetryConfidence.High;
        }

        if (hasVicCode)
        {
            return SourceTelemetryConfidence.Medium;
        }

        if (aviInfoFrame.HasData || hdrInfo.HasMetadata || frameRateExact.HasValue)
        {
            return SourceTelemetryConfidence.Low;
        }

        return SourceTelemetryConfidence.Unknown;
    }

    private static HdrMetadataInfo DecodeHdrMetadata(byte[] buffer)
    {
        if (buffer.Length < 4 || !HasNonZeroData(buffer) || buffer[0] != 0x87)
        {
            return new HdrMetadataInfo(false, null, null);
        }

        var eotf = buffer[3];
        var isHdr = eotf switch
        {
            2 or 3 => true,
            0 or 1 => false,
            _ => (bool?)null
        };
        return new HdrMetadataInfo(true, eotf, isHdr);
    }

    private static AviInfoFrameInfo DecodeAviInfoFrame(byte[] buffer)
    {
        if (buffer.Length < 8 || !HasNonZeroData(buffer) || buffer[0] != 0x82)
        {
            return AviInfoFrameInfo.Empty;
        }

        var db1 = buffer[4];
        var db2 = buffer[5];
        var db3 = buffer[6];

        var colorSpace = ((db1 >> 5) & 0x03) switch
        {
            0 => "RGB",
            1 => "YCbCr422",
            2 => "YCbCr444",
            3 => "YCbCr420",
            _ => null
        };

        var colorimetry = ((db2 >> 6) & 0x03) switch
        {
            0 => null,
            1 => "BT.601",
            2 => "BT.709",
            3 => ((db3 >> 4) & 0x07) switch
            {
                0 => "xvYCC601",
                1 => "xvYCC709",
                2 => "sYCC601",
                3 => "AdobeYCC601",
                4 => "AdobeRGB",
                5 => "BT.2020cYCC",
                6 => "BT.2020",
                7 => "Reserved",
                _ => null
            },
            _ => null
        };

        var quantization = ((db3 >> 2) & 0x03) switch
        {
            0 => "Default",
            1 => "Limited",
            2 => "Full",
            _ => "Reserved"
        };

        return new AviInfoFrameInfo(true, colorSpace, colorimetry, quantization);
    }

    private static string? DecodeCString(byte[] buffer)
    {
        if (!HasNonZeroData(buffer))
        {
            return null;
        }

        var terminatorIndex = Array.IndexOf(buffer, (byte)0);
        if (terminatorIndex < 0)
        {
            terminatorIndex = buffer.Length;
        }

        var decoded = Encoding.ASCII.GetString(buffer, 0, terminatorIndex).Trim();
        return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
    }

    private static string BuildOriginDetail(string dllDirectory)
    {
        var originDirectory = Path.GetFileName(
            dllDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(originDirectory) ? "Rtice" : $"Rtice:{originDirectory}";
    }

    private static string BuildDiagnosticSummary(
        int? vicCode,
        VicTiming? timing,
        double? frameRateExact,
        HdrMetadataInfo hdrInfo,
        AviInfoFrameInfo aviInfoFrame,
        RticeIntCommandResult hdr2Sdr,
        RticeIntCommandResult outputTiming,
        RticeIntCommandResult videoFormat,
        string? systemInfo)
    {
        var resolutionToken = timing.HasValue
            ? $"{timing.Value.Width}x{timing.Value.Height}{(timing.Value.IsInterlaced ? "i" : "p")}"
            : "unknown";
        var hdr2SdrToken = hdr2Sdr.IsSuccess && hdr2Sdr.Value.HasValue
            ? (hdr2Sdr.Value.Value == 1 ? "on" : "off")
            : "unknown";
        var outputTimingToken = outputTiming.IsSuccess && outputTiming.Value.HasValue && outputTiming.Value.Value > 0
            ? outputTiming.Value.Value.ToString(CultureInfo.InvariantCulture)
            : "unknown";
        var videoFormatToken = videoFormat.IsSuccess && videoFormat.Value.HasValue
            ? ResolveVideoFormat(videoFormat.Value.Value)
            : "unknown";

        return string.Join(
            ":",
            "rtice",
            $"vic={(vicCode.HasValue ? vicCode.Value.ToString(CultureInfo.InvariantCulture) : "unknown")}",
            resolutionToken,
            FormatFrameRate(frameRateExact),
            hdrInfo.IsHdr switch
            {
                true => "hdr",
                false => "sdr",
                _ => "unknown"
            },
            aviInfoFrame.ColorSpace ?? "unknown-space",
            aviInfoFrame.Colorimetry ?? "unknown-color",
            $"quant={aviInfoFrame.Quantization ?? "unknown"}",
            $"hdr2sdr={hdr2SdrToken}",
            $"uvcWidth={outputTimingToken}",
            $"uvcFormat={videoFormatToken}",
            $"eotf={(hdrInfo.Eotf.HasValue ? hdrInfo.Eotf.Value.ToString(CultureInfo.InvariantCulture) : "unknown")}",
            $"fw={systemInfo ?? "unknown"}");
    }

    private static string ResolveVideoFormat(int value)
        => value switch
        {
            1 => "NV12",
            _ => $"Unknown({value})"
        };

    private static string BoolToToken(bool? value)
        => value switch
        {
            true => "true",
            false => "false",
            _ => "unknown"
        };

    private static string FormatFrameRate(double? value)
        => value.HasValue && value.Value > 0
            ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
            : "unknown";

    private static bool HasNonZeroData(byte[] buffer)
        => buffer.AsSpan().IndexOfAnyExcept((byte)0) >= 0;

    private static string GetHexPreview(byte[] buffer, int maxBytes)
    {
        if (buffer.Length == 0)
        {
            return "empty";
        }

        var previewLength = Math.Min(maxBytes, buffer.Length);
        return Convert.ToHexString(buffer.AsSpan(0, previewLength));
    }

    private static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
    }

    private static string? InferFrameRateRational(double? frameRate)
    {
        if (!frameRate.HasValue || frameRate.Value <= 0)
        {
            return null;
        }

        var value = frameRate.Value;
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) <= 0.01 && rounded > 0)
        {
            return $"{(int)rounded}/1";
        }

        if (rounded > 0)
        {
            var ntscCandidate = rounded * 1000.0 / 1001.0;
            if (Math.Abs(value - ntscCandidate) <= 0.03)
            {
                return $"{(int)rounded * 1000}/1001";
            }
        }

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static bool TryParseVendorProductIds(string deviceId, out ushort vendorId, out ushort productId)
    {
        vendorId = 0;
        productId = 0;
        return TryParseHexToken(deviceId, "vid_", out vendorId) &&
               TryParseHexToken(deviceId, "pid_", out productId);
    }

    private static bool TryParseHexToken(string value, string token, out ushort result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokenIndex = value.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (tokenIndex < 0 || tokenIndex + token.Length + 4 > value.Length)
        {
            return false;
        }

        return ushort.TryParse(
            value.AsSpan(tokenIndex + token.Length, 4),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out result);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void UninitializeDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SetPortDelegate(int portType, [MarshalAs(UnmanagedType.LPStr)] string? portPath);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int OpenPortDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ClosePortDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int IsOpenDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int IntOutDelegate(out int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int BufferOutDelegate(IntPtr buffer, int size);

    private readonly record struct RticeNativeBindings(
        string DllDirectory,
        nint RtkIoHandle,
        nint RticeHandle,
        InitializeDelegate Initialize,
        UninitializeDelegate Uninitialize,
        SetPortDelegate SetPort,
        OpenPortDelegate OpenPort,
        ClosePortDelegate ClosePort,
        IsOpenDelegate IsOpen,
        IntOutDelegate GetCableConnect,
        IntOutDelegate GetHdr2SdrOnOffStatus,
        IntOutDelegate GetUvcOutputTiming,
        IntOutDelegate GetUvcVideoFormat,
        IntOutDelegate GetHdmiVideoVfreq,
        BufferOutDelegate GetVic,
        BufferOutDelegate GetAviInfoFrame,
        BufferOutDelegate GetHdrMetadata,
        BufferOutDelegate GetSystemInfo);

    private readonly record struct RticeIntCommandResult(int Result, int? Value)
    {
        public bool IsSuccess => Result == 0;
    }

    private readonly record struct RticeBufferCommandResult(int Result, byte[] Buffer);

    private readonly record struct VicTiming(int Width, int Height, double NominalFrameRate, bool IsInterlaced);

    private readonly record struct HdrMetadataInfo(bool HasMetadata, byte? Eotf, bool? IsHdr);

    private readonly record struct AviInfoFrameInfo(
        bool HasData,
        string? ColorSpace,
        string? Colorimetry,
        string? Quantization)
    {
        public static AviInfoFrameInfo Empty => new(false, null, null, null);
    }

    private sealed class ProcessPathScope : IDisposable
    {
        private readonly string? _originalPath;

        public ProcessPathScope(string prependDirectory)
        {
            _originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(prependDirectory))
            {
                return;
            }

            var alreadyPresent = _originalPath?
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(path => string.Equals(path, prependDirectory, StringComparison.OrdinalIgnoreCase))
                ?? false;
            if (alreadyPresent)
            {
                return;
            }

            var merged = string.IsNullOrWhiteSpace(_originalPath)
                ? prependDirectory
                : $"{prependDirectory};{_originalPath}";
            Environment.SetEnvironmentVariable("PATH", merged, EnvironmentVariableTarget.Process);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PATH", _originalPath, EnvironmentVariableTarget.Process);
        }
    }
}
