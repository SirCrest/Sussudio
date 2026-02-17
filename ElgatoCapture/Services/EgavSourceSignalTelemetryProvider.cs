using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Microsoft.Win32.SafeHandles;

namespace ElgatoCapture.Services;

public sealed class EgavSourceSignalTelemetryProvider : ISourceSignalTelemetryProvider
{
    private const string EgavLibraryName = "EGAVDeviceSupport";
    private const string EgavDllFileName = "EGAVDeviceSupport.dll";
    private const string Unsupported4kXPidToken = "VID_0FD9&PID_009B";
    private const string DefaultStudioPackageDir = @"C:\Program Files\WindowsApps\Elgato.Studio_1.0.5.895_x64__g54w8ztgkx496";
    private static readonly object ResolverGate = new();
    private static readonly SemaphoreSlim EgavCallGate = new(1, 1);
    private static string? s_preferredDllDirectory;
    private static string? s_cachedLoadableDllDirectory;

    private readonly string? _configuredDllDirectory;

    static EgavSourceSignalTelemetryProvider()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(EgavSourceSignalTelemetryProvider).Assembly, ResolveDllImport);
        }
        catch (InvalidOperationException)
        {
            // Resolver already configured.
        }
    }

    public EgavSourceSignalTelemetryProvider(string? configuredDllDirectory = null)
    {
        _configuredDllDirectory = configuredDllDirectory;
    }

    public Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable("device-unavailable"));
        }
        if (IsKnownUnsupportedDevice(device) && !ShouldAllowUnsupportedEgav())
        {
            return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable(
                "egav-device-unsupported",
                Unsupported4kXPidToken));
        }

        var gateTimeoutMs = GetIntFromEnv("ELGATOCAPTURE_EGAV_GATE_TIMEOUT_MS", 750, 100, 10000);
        var gateAcquired = false;

        var initializeResult = -100;
        var openResult = -2;
        int? signalStatusResult = null;
        int? isVideoHdrResult = null;
        EgavDeviceHandle? deviceHandle = null;
        bool isDeviceOpen = false;
        bool? isVideoHdr = null;
        EgavSignalStatus? signalStatus = null;
        string? openPath = null;

        try
        {
            gateAcquired = EgavCallGate.Wait(gateTimeoutMs, cancellationToken);
            if (!gateAcquired)
            {
                return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable(
                    "egav-native-busy",
                    $"{gateTimeoutMs}ms"));
            }

            var loadableDllDirectory = ResolveLoadableDllDirectory(_configuredDllDirectory);
            if (string.IsNullOrWhiteSpace(loadableDllDirectory))
            {
                return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable("egav-dll-not-found"));
            }

            using var pathScope = new ProcessPathScope(loadableDllDirectory);
            SetPreferredDllDirectory(loadableDllDirectory);

            using var initParams = new EgavInitializeParamsHandle();
            ConfigureInitializeParams(initParams, loadableDllDirectory);
            initializeResult = Native.EGAVDS_Initialize(initParams.AsHandleRef());
            if (!IsOk(initializeResult))
            {
                return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable(
                    "egav-initialize-failed",
                    ToResultName(initializeResult)));
            }

            deviceHandle = new EgavDeviceHandle();
            foreach (var candidate in BuildOpenCandidates(device))
            {
                cancellationToken.ThrowIfCancellationRequested();
                openResult = SafeCall(
                    () => Native.OpenDevice(candidate, deviceHandle.AsHandleRef()),
                    fallback: -100);
                isDeviceOpen = SafeIsDeviceOpen(deviceHandle);
                if (IsOk(openResult) && isDeviceOpen)
                {
                    openPath = candidate;
                    break;
                }
            }

            if (!isDeviceOpen || !IsOk(openResult))
            {
                return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable(
                    "egav-open-failed",
                    ToResultName(openResult)));
            }

            using var signalHandle = new EgavSignalStatusHandle();
            signalStatusResult = SafeCall(
                () => Native.GetSignalStatus(deviceHandle.AsHandleRef(), signalHandle.AsHandleRef()),
                fallback: -100);
            if (IsOk(signalStatusResult))
            {
                signalStatus = new EgavSignalStatus(
                    Native.EGAVDS_SIGNAL_STATUS_hasSignal_get(signalHandle.AsHandleRef()),
                    Native.EGAVDS_SIGNAL_STATUS_isInterlaced_get(signalHandle.AsHandleRef()),
                    Native.EGAVDS_SIGNAL_STATUS_isHDCPProtected_get(signalHandle.AsHandleRef()),
                    Native.EGAVDS_SIGNAL_STATUS_width_get(signalHandle.AsHandleRef()),
                    Native.EGAVDS_SIGNAL_STATUS_height_get(signalHandle.AsHandleRef()),
                    Native.EGAVDS_SIGNAL_STATUS_frameRate_get(signalHandle.AsHandleRef()));
            }

            var hdrValue = false;
            isVideoHdrResult = SafeCall(
                () => Native.IsVideoHDR(deviceHandle.AsHandleRef(), ref hdrValue),
                fallback: -100);
            if (IsOk(isVideoHdrResult))
            {
                isVideoHdr = hdrValue;
            }

            var availability = ResolveAvailability(signalStatusResult, signalStatus);
            var confidence = ResolveConfidence(signalStatus, isVideoHdrResult);
            var frameRateArg = InferFrameRateRational(signalStatus?.FrameRate);

            return Task.FromResult(new SourceSignalTelemetrySnapshot
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Availability = availability,
                Origin = SourceTelemetryOrigin.Egav,
                OriginDetail = !string.IsNullOrWhiteSpace(openPath) ? $"EGAV:{openPath}" : "EGAV",
                Confidence = confidence,
                Width = signalStatus?.Width,
                Height = signalStatus?.Height,
                FrameRateExact = signalStatus?.FrameRate,
                FrameRateArg = frameRateArg,
                IsHdr = isVideoHdr,
                DiagnosticSummary = BuildDiagnosticSummary(signalStatus, openPath),
                EgavInitializeResultName = ToResultName(initializeResult),
                EgavOpenResultName = ToResultName(openResult),
                EgavSignalStatusResultName = ToResultName(signalStatusResult),
                EgavIsVideoHdrResultName = ToResultName(isVideoHdrResult),
                AudioInputAvailability = SourceAudioInputAvailability.Unavailable,
                AudioInputOrigin = "not-implemented"
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable(
                "egav-exception",
                $"{ex.GetType().Name}: {ex.Message}"));
        }
        finally
        {
            try
            {
                if (deviceHandle != null && isDeviceOpen)
                {
                    Native.CloseDevice(deviceHandle.AsHandleRef());
                }
            }
            catch
            {
                // Best effort cleanup.
            }
            finally
            {
                deviceHandle?.Dispose();
            }

            if (IsOk(initializeResult))
            {
                try
                {
                    Native.EGAVDS_Deinitialize();
                }
                catch
                {
                    // Best effort cleanup.
                }
            }

            SetPreferredDllDirectory(null);
            if (gateAcquired)
            {
                EgavCallGate.Release();
            }
        }
    }

    private static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
    }

    private static bool IsKnownUnsupportedDevice(CaptureDevice device)
    {
        return !string.IsNullOrWhiteSpace(device.Id) &&
               device.Id.Contains(Unsupported4kXPidToken, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldAllowUnsupportedEgav()
    {
        var raw = Environment.GetEnvironmentVariable("ELGATOCAPTURE_EGAV_FORCE_UNSUPPORTED");
        return string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static SourceTelemetryAvailability ResolveAvailability(int? signalStatusResult, EgavSignalStatus? signalStatus)
    {
        if (!IsOk(signalStatusResult))
        {
            return SourceTelemetryAvailability.Unavailable;
        }

        if (signalStatus == null)
        {
            return SourceTelemetryAvailability.Inconclusive;
        }

        if (!signalStatus.HasSignal)
        {
            return SourceTelemetryAvailability.Inconclusive;
        }

        if (signalStatus.Width <= 0 || signalStatus.Height <= 0 || signalStatus.FrameRate <= 0)
        {
            return SourceTelemetryAvailability.Inconclusive;
        }

        return SourceTelemetryAvailability.Available;
    }

    private static SourceTelemetryConfidence ResolveConfidence(EgavSignalStatus? signalStatus, int? isVideoHdrResult)
    {
        if (signalStatus == null || signalStatus.Width <= 0 || signalStatus.Height <= 0 || signalStatus.FrameRate <= 0)
        {
            return SourceTelemetryConfidence.Unknown;
        }

        if (IsOk(isVideoHdrResult))
        {
            return SourceTelemetryConfidence.High;
        }

        return SourceTelemetryConfidence.Medium;
    }

    private static string BuildDiagnosticSummary(EgavSignalStatus? signalStatus, string? openPath)
    {
        if (signalStatus == null)
        {
            return "egav:no-signal-status";
        }

        var signalToken = signalStatus.HasSignal ? "signal" : "no-signal";
        return $"egav:{signalToken}:{signalStatus.Width}x{signalStatus.Height}@{signalStatus.FrameRate:0.###}:{openPath ?? "unknown-path"}";
    }

    private static string? ResolveDllDirectory(string? configuredDllDirectory)
    {
        static string? NormalizeConfiguredPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
            if (File.Exists(expanded))
            {
                if (!string.Equals(Path.GetFileName(expanded), EgavDllFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return Path.GetDirectoryName(Path.GetFullPath(expanded));
            }

            if (Directory.Exists(expanded) && File.Exists(Path.Combine(expanded, EgavDllFileName)))
            {
                return Path.GetFullPath(expanded);
            }

            return null;
        }

        var envPath = Environment.GetEnvironmentVariable("ELGATOCAPTURE_EGAV_DLL_DIR");
        var configured = NormalizeConfiguredPath(configuredDllDirectory)
            ?? NormalizeConfiguredPath(envPath ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (File.Exists(Path.Combine(AppContext.BaseDirectory, EgavDllFileName)))
        {
            return AppContext.BaseDirectory;
        }

        if (File.Exists(Path.Combine(DefaultStudioPackageDir, EgavDllFileName)))
        {
            return DefaultStudioPackageDir;
        }

        try
        {
            const string windowsApps = @"C:\Program Files\WindowsApps";
            if (Directory.Exists(windowsApps))
            {
                var candidate = Directory.EnumerateDirectories(windowsApps, "Elgato.Studio_*_x64__*")
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(path => File.Exists(Path.Combine(path, EgavDllFileName)));
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

    private static string? ResolveLoadableDllDirectory(string? configuredDllDirectory)
    {
        lock (ResolverGate)
        {
            if (!string.IsNullOrWhiteSpace(s_cachedLoadableDllDirectory) &&
                File.Exists(Path.Combine(s_cachedLoadableDllDirectory, EgavDllFileName)))
            {
                return s_cachedLoadableDllDirectory;
            }
        }

        var resolved = ResolveDllDirectory(configuredDllDirectory);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return null;
        }

        var loadable = PrepareDllDirectoryForLoading(resolved);
        lock (ResolverGate)
        {
            s_cachedLoadableDllDirectory = loadable;
        }

        return loadable;
    }

    private static string PrepareDllDirectoryForLoading(string resolvedDirectory)
    {
        if (!resolvedDirectory.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase))
        {
            return resolvedDirectory;
        }

        var packageName = Path.GetFileName(resolvedDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var stageDirectory = Path.Combine(RuntimePaths.GetRepoTempRoot(), "egav", "runtime", packageName);
        Directory.CreateDirectory(stageDirectory);
        CopyRequiredRuntimeDll(resolvedDirectory, stageDirectory, EgavDllFileName);
        CopyRequiredRuntimeDll(resolvedDirectory, stageDirectory, "RTICE_SDK_x64.dll");
        CopyRequiredRuntimeDll(resolvedDirectory, stageDirectory, "RTK_IO_x64.dll");
        return stageDirectory;
    }

    private static void CopyRequiredRuntimeDll(string sourceDirectory, string destinationDirectory, string fileName)
    {
        var sourcePath = Path.Combine(sourceDirectory, fileName);
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException($"Required runtime DLL '{sourcePath}' was not found.");
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

    private static void ConfigureInitializeParams(EgavInitializeParamsHandle initParams, string dllDirectory)
    {
        var root = Path.Combine(RuntimePaths.GetRepoTempRoot(), "egav");
        var appDataDir = Path.Combine(root, "appdata");
        var tempDir = Path.Combine(root, "temp");
        var logDir = Path.Combine(root, "logs");
        Directory.CreateDirectory(appDataDir);
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(logDir);
        Native.EGAVDS_INITIALIZE_PARAMS_appDataDirectoryPath_set(initParams.AsHandleRef(), appDataDir);
        Native.EGAVDS_INITIALIZE_PARAMS_tmpDirectoryPath_set(initParams.AsHandleRef(), tempDir);
        Native.EGAVDS_INITIALIZE_PARAMS_logDirectoryPath_set(initParams.AsHandleRef(), logDir);
        Native.EGAVDS_INITIALIZE_PARAMS_logFilename_set(initParams.AsHandleRef(), "egav-elgatocapture.log");
        Native.EGAVDS_INITIALIZE_PARAMS_firmwareDirectoryPath_set(initParams.AsHandleRef(), dllDirectory);
        Native.EGAVDS_INITIALIZE_PARAMS_edidDirectoryPath_set(initParams.AsHandleRef(), dllDirectory);
        Native.EGAVDS_INITIALIZE_PARAMS_companyNameShort_set(initParams.AsHandleRef(), "Elgato");
        Native.EGAVDS_INITIALIZE_PARAMS_companyNameLong_set(initParams.AsHandleRef(), "Elgato/Corsair");
        Native.EGAVDS_INITIALIZE_PARAMS_productNameShort_set(initParams.AsHandleRef(), "Capture");
        Native.EGAVDS_INITIALIZE_PARAMS_productNameLong_set(initParams.AsHandleRef(), "Elgato Capture");
        Native.EGAVDS_INITIALIZE_PARAMS_productClientName_set(initParams.AsHandleRef(), "elgatocapture");
        Native.EGAVDS_INITIALIZE_PARAMS_isDebug_set(initParams.AsHandleRef(), false);
    }

    private static IReadOnlyList<string> BuildOpenCandidates(CaptureDevice device)
    {
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();

        static void AddIfUseful(HashSet<string> seen, List<string> output, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var trimmed = value.Trim();
                if (seen.Add(trimmed))
                {
                    output.Add(trimmed);
                }
            }
        }

        AddIfUseful(dedupe, candidates, device.Id);
        AddIfUseful(dedupe, candidates, TryNormalizeToInstanceId(device.Id));
        return candidates;
    }

    private static string? TryNormalizeToInstanceId(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        var normalized = deviceId.Trim();
        if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            normalized = normalized[4..];
        }

        var categoryIndex = normalized.IndexOf("#{", StringComparison.Ordinal);
        if (categoryIndex >= 0)
        {
            normalized = normalized[..categoryIndex];
        }

        normalized = normalized.Replace('#', '\\');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int SafeCall(Func<int> call, int fallback)
    {
        try
        {
            return call();
        }
        catch
        {
            return fallback;
        }
    }

    private static bool SafeIsDeviceOpen(EgavDeviceHandle handle)
    {
        try
        {
            return Native.IsDeviceOpen(handle.AsHandleRef());
        }
        catch
        {
            return false;
        }
    }

    private static void SetPreferredDllDirectory(string? directory)
    {
        lock (ResolverGate)
        {
            s_preferredDllDirectory = directory;
        }
    }

    private static string ToResultName(int code)
        => code switch
        {
            0 => "EGAVDS_OK",
            -2 => "EGAVDS_ErrInvalidState",
            -3 => "EGAVDS_ErrNotAvailable",
            -4 => "EGAVDS_ErrInvalidParameter",
            -5 => "EGAVDS_ErrNotSupported",
            -6 => "EGAVDS_ErrIncompatibleEdid",
            -100 => "EGAVDS_ErrUnknown",
            _ => $"Unknown({code})"
        };

    private static string? ToResultName(int? code)
        => code.HasValue ? ToResultName(code.Value) : null;

    private static bool IsOk(int code)
        => code == 0;

    private static bool IsOk(int? code)
        => code.HasValue && code.Value == 0;

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

    private sealed record EgavSignalStatus(
        bool HasSignal,
        bool IsInterlaced,
        bool IsHdcpProtected,
        int Width,
        int Height,
        double FrameRate);

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

    private abstract class EgavSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected EgavSafeHandle() : base(true)
        {
        }

        public HandleRef AsHandleRef()
            => new(this, handle);
    }

    private sealed class EgavInitializeParamsHandle : EgavSafeHandle
    {
        public EgavInitializeParamsHandle() => SetHandle(Native.new_EGAVDS_INITIALIZE_PARAMS());
        protected override bool ReleaseHandle() { Native.delete_EGAVDS_INITIALIZE_PARAMS(AsHandleRef()); return true; }
    }

    private sealed class EgavDeviceHandle : EgavSafeHandle
    {
        public EgavDeviceHandle() => SetHandle(Native.new_EGAVDS_DEVICE_HANDLE());
        protected override bool ReleaseHandle() { Native.delete_EGAVDS_DEVICE_HANDLE(AsHandleRef()); return true; }
    }

    private sealed class EgavSignalStatusHandle : EgavSafeHandle
    {
        public EgavSignalStatusHandle() => SetHandle(Native.new_EGAVDS_SIGNAL_STATUS());
        protected override bool ReleaseHandle() { Native.delete_EGAVDS_SIGNAL_STATUS(AsHandleRef()); return true; }
    }

    private static nint ResolveDllImport(string libraryName, Assembly _, DllImportSearchPath? __)
    {
        if (!string.Equals(libraryName, EgavLibraryName, StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        string? preferred;
        lock (ResolverGate)
        {
            preferred = s_preferredDllDirectory;
        }

        if (string.IsNullOrWhiteSpace(preferred))
        {
            return IntPtr.Zero;
        }

        var candidate = Path.Combine(preferred, EgavDllFileName);
        return NativeLibrary.TryLoad(candidate, out var handle) ? handle : IntPtr.Zero;
    }

    private static class Native
    {
        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_new_EGAVDS_INITIALIZE_PARAMS___")]
        public static extern nint new_EGAVDS_INITIALIZE_PARAMS();

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_delete_EGAVDS_INITIALIZE_PARAMS___")]
        public static extern void delete_EGAVDS_INITIALIZE_PARAMS(HandleRef initializeParams);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_appDataDirectoryPath_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_appDataDirectoryPath_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_tmpDirectoryPath_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_tmpDirectoryPath_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_logDirectoryPath_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_logDirectoryPath_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_logFilename_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_logFilename_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_firmwareDirectoryPath_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_firmwareDirectoryPath_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_edidDirectoryPath_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_edidDirectoryPath_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_companyNameShort_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_companyNameShort_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_companyNameLong_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_companyNameLong_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productNameShort_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_productNameShort_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productNameLong_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_productNameLong_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productClientName_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_productClientName_set(HandleRef initParams, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_isDebug_set___")]
        public static extern void EGAVDS_INITIALIZE_PARAMS_isDebug_set(HandleRef initParams, bool value);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_Initialize___")]
        public static extern int EGAVDS_Initialize(HandleRef initParams);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_Deinitialize___")]
        public static extern void EGAVDS_Deinitialize();

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_new_EGAVDS_DEVICE_HANDLE___")]
        public static extern nint new_EGAVDS_DEVICE_HANDLE();

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_delete_EGAVDS_DEVICE_HANDLE___")]
        public static extern void delete_EGAVDS_DEVICE_HANDLE(HandleRef deviceHandle);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_OpenDevice___")]
        public static extern int OpenDevice([MarshalAs(UnmanagedType.LPUTF8Str)] string devicePath, HandleRef outDeviceHandle);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_CloseDevice___")]
        public static extern void CloseDevice(HandleRef handle);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_IsDeviceOpen___")]
        public static extern bool IsDeviceOpen(HandleRef handle);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_new_EGAVDS_SIGNAL_STATUS___")]
        public static extern nint new_EGAVDS_SIGNAL_STATUS();

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_delete_EGAVDS_SIGNAL_STATUS___")]
        public static extern void delete_EGAVDS_SIGNAL_STATUS(HandleRef signalStatus);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetSignalStatus___")]
        public static extern int GetSignalStatus(HandleRef handle, HandleRef outSignalStatus);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_SIGNAL_STATUS_hasSignal_get___")]
        public static extern bool EGAVDS_SIGNAL_STATUS_hasSignal_get(HandleRef signalStatus);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_SIGNAL_STATUS_isInterlaced_get___")]
        public static extern bool EGAVDS_SIGNAL_STATUS_isInterlaced_get(HandleRef signalStatus);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_SIGNAL_STATUS_isHDCPProtected_get___")]
        public static extern bool EGAVDS_SIGNAL_STATUS_isHDCPProtected_get(HandleRef signalStatus);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_SIGNAL_STATUS_width_get___")]
        public static extern int EGAVDS_SIGNAL_STATUS_width_get(HandleRef signalStatus);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_SIGNAL_STATUS_height_get___")]
        public static extern int EGAVDS_SIGNAL_STATUS_height_get(HandleRef signalStatus);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_SIGNAL_STATUS_frameRate_get___")]
        public static extern double EGAVDS_SIGNAL_STATUS_frameRate_get(HandleRef signalStatus);

        [DllImport(EgavLibraryName, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_IsVideoHDR___")]
        public static extern int IsVideoHDR(HandleRef handle, ref bool outIsHdr);
    }
}
