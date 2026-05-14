using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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

    public string LastDiscoverySummary { get; private set; } = "No discovery run yet";
    public event EventHandler<DeviceFormatProbeCompletedEventArgs>? FormatProbeCompleted;

    public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(bool waitForFormatProbes = true)
    {
        var discoveryStopwatch = Stopwatch.StartNew();
        var discovered = new ObservableCollection<CaptureDevice>();

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
            return discovered;
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

        return discovered;
    }

    private sealed record DeviceCandidate(
        string SourceName,
        CaptureDevice Device,
        bool HasEnumeratedFormats,
        bool Include,
        bool PreferredByName,
        bool LikelyByCapability,
        bool LikelyByName);

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
