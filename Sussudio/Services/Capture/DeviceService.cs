using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Enumerates capture/audio devices and builds the format lists shown by the UI.
// It is the boundary between slow native discovery and view-model option state,
// so it owns cache hydration, capability filtering, and friendly labels.
public partial class DeviceService
{
    private const int FormatProbeConcurrency = 2;
    private const string PreferredNativeXuInterfaceFragment = "{65e8773d-8f56-11d0-a3b9-00a0c9223196}";
    private readonly SemaphoreSlim _formatProbeGate = new(FormatProbeConcurrency, FormatProbeConcurrency);

    private static readonly string[] PreferredDeviceNames =
    {
        "Game Capture Neo",
        "HD60 S+",
        "HD60 X",
        "4K60 Pro",
        "4K X",
        "4K S",
    };

    private static readonly string[] CaptureKeywords =
    {
        "elgato",
        "capture",
        "hdmi",
        "4k",
        "stream",
        "usb video"
    };

    private static readonly string[] ModelHints =
    {
        "4k x",
        "4k s",
        "4k60",
        "hd60 s+",
        "hd60",
        "neo",
        "pro"
    };

    private static readonly Regex TokenizeRegex = new("[A-Za-z0-9\\+]+", RegexOptions.Compiled);

    public string LastDiscoverySummary { get; private set; } = "No discovery run yet";
    public event EventHandler<DeviceFormatProbeCompletedEventArgs>? FormatProbeCompleted;

    public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(bool waitForFormatProbes = true)
    {
        var discovery = await EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes).ConfigureAwait(false);
        return discovery.CaptureDevices;
    }

    public async Task<DeviceDiscoveryResult> EnumerateCaptureDeviceDiscoveryAsync(bool waitForFormatProbes = true)
    {
        var discoveryStopwatch = Stopwatch.StartNew();
        var discovered = new ObservableCollection<CaptureDevice>();
        var noAudioDevices = Array.Empty<AudioInputDevice>();

        List<MfDeviceEnumerator.MfVideoDeviceInfo> videoDevices;
        List<AudioInputDevice> audioDevices;
        try
        {
            var videoTask = MfDeviceEnumerator.EnumerateVideoDevicesAsync();
            var audioTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();
            await Task.WhenAll(videoTask, audioTask).ConfigureAwait(false);
            videoDevices = videoTask.Result;
            audioDevices = audioTask.Result;
        }
        catch (Exception ex)
        {
            LastDiscoverySummary = $"Video devices: enumeration failed ({ex.GetType().Name}: {ex.Message})";
            Logger.Log($"Device discovery failed while querying MF/WASAPI enumerators: {ex}");
            return new DeviceDiscoveryResult(discovered, noAudioDevices);
        }

        if (videoDevices.Count == 0)
        {
            Logger.Log("Device discovery returned zero video devices from MFEnumDeviceSources.");
        }

        var evaluated = new List<DeviceCandidate>();
        foreach (var videoDevice in videoDevices)
        {
            var captureDevice = new CaptureDevice
            {
                Id = videoDevice.SymbolicLink,
                Name = videoDevice.Name,
                NativeXuInterfacePath = ResolveNativeXuInterfacePath(videoDevice.SymbolicLink)
            };

            var hasEnumeratedFormats = false;
            if (waitForFormatProbes)
            {
                hasEnumeratedFormats = await QuerySupportedFormatsAsync(captureDevice);
            }
            else
            {
                TryLoadFormatCache(captureDevice);
            }

            var preferredByName = PreferredDeviceNames.Any(name =>
                captureDevice.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            var likelyByName = CaptureKeywords.Any(keyword =>
                captureDevice.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            var likelyByCapability = LooksLikeHighBandwidthCapture(captureDevice);

            var include = !waitForFormatProbes || preferredByName || likelyByName || likelyByCapability || !hasEnumeratedFormats;

            evaluated.Add(new DeviceCandidate(
                captureDevice.Name,
                captureDevice,
                hasEnumeratedFormats,
                include,
                preferredByName,
                likelyByCapability,
                likelyByName));
        }

        var selected = evaluated.Where(x => x.Include).ToList();
        if (selected.Count == 0)
        {
            Logger.Log("Capability-first filtering found no strong candidates; falling back to all video devices");
            selected = evaluated;
        }

        foreach (var candidate in selected.OrderByDescending(GetDevicePriority))
        {
            AttachBestAudioDevice(candidate.SourceName, candidate.Device, audioDevices);
            discovered.Add(candidate.Device);
        }

        var filteredOut = Math.Max(0, evaluated.Count - selected.Count);
        discoveryStopwatch.Stop();
        LastDiscoverySummary =
            $"Video devices: total={videoDevices.Count}, accepted={discovered.Count}, filtered={filteredOut}, audio inputs={audioDevices.Count}, " +
            $"first-list-ms={discoveryStopwatch.ElapsedMilliseconds}, format-probes={(waitForFormatProbes ? "inline" : "background")}";
        Logger.Log($"Device discovery summary: {LastDiscoverySummary}");

        return new DeviceDiscoveryResult(discovered, audioDevices);
    }

    private sealed record DeviceCandidate(
        string SourceName,
        CaptureDevice Device,
        bool HasEnumeratedFormats,
        bool Include,
        bool PreferredByName,
        bool LikelyByCapability,
        bool LikelyByName);

    private static int GetDevicePriority(DeviceCandidate candidate)
    {
        var maxPixelCount = candidate.Device.SupportedFormats
            .Select(f => (long)f.Width * f.Height)
            .DefaultIfEmpty(0)
            .Max();
        var maxFrameRate = candidate.Device.SupportedFormats
            .Select(f => f.FrameRate)
            .DefaultIfEmpty(0)
            .Max();

        var priority = 0;
        if (candidate.PreferredByName) priority += 400;
        if (candidate.LikelyByCapability) priority += 200;
        if (candidate.LikelyByName) priority += 100;
        if (candidate.HasEnumeratedFormats) priority += 50;
        priority += (int)Math.Min(maxFrameRate, 120);
        priority += (int)Math.Min(maxPixelCount / 500_000, 40);
        return priority;
    }

    private static bool LooksLikeHighBandwidthCapture(CaptureDevice device)
    {
        foreach (var format in device.SupportedFormats)
        {
            if ((format.Width >= 1920 && format.FrameRate >= 50) ||
                (format.Width >= 2560 && format.FrameRate >= 30) ||
                (format.Width >= 3840 && format.FrameRate >= 24))
            {
                return true;
            }
        }

        return false;
    }

    private static void AttachBestAudioDevice(
        string videoDeviceName,
        CaptureDevice captureDevice,
        IReadOnlyList<AudioInputDevice> audioDevices)
    {
        var bestMatch = audioDevices
            .Select(audioDevice => new
            {
                Device = audioDevice,
                Score = ScoreAudioAssociation(videoDeviceName, audioDevice.Name)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (bestMatch == null || bestMatch.Score <= 0)
        {
            Logger.Log($"No associated audio device found for {captureDevice.Name}");
            return;
        }

        captureDevice.AudioDeviceId = bestMatch.Device.Id;
        captureDevice.AudioDeviceName = bestMatch.Device.Name;
        Logger.Log($"Associated audio device for {captureDevice.Name}: {bestMatch.Device.Name} (score={bestMatch.Score})");
    }

    private static int ScoreAudioAssociation(string videoDeviceName, string audioDeviceName)
    {
        var score = 0;

        var videoTokens = Tokenize(videoDeviceName);
        var audioTokens = Tokenize(audioDeviceName);
        var overlap = videoTokens.Intersect(audioTokens).Count();
        score += overlap * 20;

        if (videoDeviceName.Contains("Elgato", StringComparison.OrdinalIgnoreCase) &&
            audioDeviceName.Contains("Elgato", StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }

        var videoModel = GetModelHint(videoDeviceName);
        var audioModel = GetModelHint(audioDeviceName);
        if (!string.IsNullOrEmpty(videoModel) &&
            string.Equals(videoModel, audioModel, StringComparison.OrdinalIgnoreCase))
        {
            score += 50;
        }

        return score;
    }

    private static string? GetModelHint(string deviceName)
    {
        foreach (var modelHint in ModelHints)
        {
            if (deviceName.Contains(modelHint, StringComparison.OrdinalIgnoreCase))
            {
                return modelHint;
            }
        }

        return null;
    }

    private static HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in TokenizeRegex.Matches(text))
        {
            var token = match.Value.Trim();
            if (token.Length >= 2)
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }

    private static string? ResolveNativeXuInterfacePath(string deviceId)
    {
        var probeDevice = new CaptureDevice { Id = deviceId };
        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(probeDevice, out var vendorId, out var productId))
        {
            return null;
        }

        try
        {
            var interfaces = KsExtensionUnitNative.EnumerateKsInterfaces(vendorId, productId);
            if (interfaces.Count == 0)
            {
                return null;
            }

            var deviceInstanceKey = GetDeviceInstanceKey(deviceId);
            var sameDeviceInterfaces = interfaces
                .Where(ksInterface => string.Equals(
                    GetDeviceInstanceKey(ksInterface.Path),
                    deviceInstanceKey,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (sameDeviceInterfaces.Length == 0)
            {
                Logger.Log($"Native XU interface resolution found no matching interface for device '{deviceId}'");
                return null;
            }

            return sameDeviceInterfaces
                .Select(ksInterface => ksInterface.Path)
                .OrderByDescending(path =>
                    path.IndexOf(PreferredNativeXuInterfaceFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.Log($"Native XU interface resolution failed for device '{deviceId}': {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static string GetDeviceInstanceKey(string interfacePath)
    {
        var categoryStart = interfacePath.LastIndexOf("#{", StringComparison.Ordinal);
        return categoryStart > 0
            ? interfacePath[..categoryStart]
            : interfacePath;
    }

    public sealed record DeviceDiscoveryResult(
        ObservableCollection<CaptureDevice> CaptureDevices,
        IReadOnlyList<AudioInputDevice> AudioInputDevices);

    public sealed record DeviceFormatProbeCompletedEventArgs(
        string DeviceId,
        string DeviceName,
        IReadOnlyList<MediaFormat> Formats,
        bool IsHdrCapable,
        bool HasEnumeratedFormats,
        long RequestId,
        string? Error)
    {
        public bool Succeeded => string.IsNullOrWhiteSpace(Error);
    }
}
