using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    /// <summary>
    /// Switches the audio input source (HDMI/Analog) using the same sequence
    /// that Elgato Studio uses: flash persistence + I2C codec register writes.
    /// This is seamless — no USB re-enumeration, no preview interruption.
    /// DO NOT use SetInputSourceAsync (AT 0x34) — it causes USB re-enumeration
    /// and can permanently corrupt firmware audio state.
    /// </summary>
    public static async Task<bool> SwitchAudioInputAsync(
        CaptureDevice? device,
        bool analog,
        byte gainByte = 0xFF,
        CancellationToken ct = default)
    {
        var sourceLabel = analog ? DeviceAudioMode.Analog : DeviceAudioMode.Hdmi;
        Logger.Log($"NATIVEXU_SWITCH_AUDIO begin source={sourceLabel} gain=0x{gainByte:X2}");

        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return false;
        }

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            !IsSupported4kXDevice(vendorId, productId))
        {
            return false;
        }

        if (!HasSelectedNativeXuInterface(device, "SWITCH_AUDIO"))
        {
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await CallGate.WaitAsync(GateTimeoutMs * 4, ct).ConfigureAwait(false);
            if (!gateAcquired)
            {
                Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=gate_timeout");
                return false;
            }

            var interfaces = EnumerateKsInterfaces(vendorId, productId, device);
            foreach (var ksInterface in interfaces)
            {
                ct.ThrowIfCancellationRequested();
                using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out _);
                if (handle is null)
                {
                    continue;
                }

                if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
                {
                    continue;
                }

                var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
                foreach (var node in nodeList)
                {
                    if (!node.IsDevSpecific)
                    {
                        continue;
                    }

                    var ok = ExecuteAudioSwitch(handle, node.NodeId, analog, gainByte, sourceLabel, ct);
                    if (ok)
                    {
                        Logger.Log($"NATIVEXU_SWITCH_AUDIO OK source={sourceLabel}");
                        return true;
                    }
                }
            }

            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=no_device");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_SWITCH_AUDIO EXCEPTION type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
        finally
        {
            if (gateAcquired)
            {
                CallGate.Release();
            }
        }
    }

    /// <summary>
    /// Sets the analog input gain via I2C codec register writes + flash persistence.
    /// The gain byte maps 0x00-0xFF (0-100%) to a 3-zone codec register configuration:
    ///   Zone 1 (0x00-0x7F): Digital attenuation only — regs 0x0C/0x0D = 0x80 + gain
    ///   Zone 2 (0x80-0x97): PGA gain — regs 0x0A/0x0B = gain - 0x80
    ///   Zone 3 (0x98-0xAF): PGA maxed + output gain — regs 0x0E/0x0F = gain - 0x98
    ///   Zone 4 (0xB0-0xFF): All stages maxed (PGA=0x18, OutGain=0x18)
    /// </summary>
    public static async Task<bool> SetAnalogGainAsync(
        CaptureDevice? device,
        byte gainByte,
        bool persistFlash = true,
        CancellationToken ct = default)
    {
        var logPct = (Math.Exp(4.0 * (gainByte / 255.0)) - 1.0) / (Math.Exp(4.0) - 1.0) * 100.0;
        Logger.Log($"NATIVEXU_SET_GAIN begin gain=0x{gainByte:X2} ({logPct:0}%) flash={persistFlash}");

        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return false;
        }

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            !IsSupported4kXDevice(vendorId, productId))
        {
            return false;
        }

        if (!HasSelectedNativeXuInterface(device, "SET_GAIN"))
        {
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await CallGate.WaitAsync(GateTimeoutMs * 4, ct).ConfigureAwait(false);
            if (!gateAcquired)
            {
                Logger.Log("NATIVEXU_SET_GAIN FAILED stage=gate_timeout");
                return false;
            }

            var interfaces = EnumerateKsInterfaces(vendorId, productId, device);
            foreach (var ksInterface in interfaces)
            {
                ct.ThrowIfCancellationRequested();
                using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out _);
                if (handle is null)
                {
                    continue;
                }

                if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
                {
                    continue;
                }

                var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
                foreach (var node in nodeList)
                {
                    if (!node.IsDevSpecific)
                    {
                        continue;
                    }

                    var ok = ExecuteGainChange(handle, node.NodeId, gainByte, persistFlash, ct);
                    if (ok)
                    {
                        Logger.Log($"NATIVEXU_SET_GAIN OK gain=0x{gainByte:X2} flash={persistFlash}");
                        return true;
                    }
                }
            }

            Logger.Log("NATIVEXU_SET_GAIN FAILED stage=no_device");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_SET_GAIN EXCEPTION type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
        finally
        {
            if (gateAcquired)
            {
                CallGate.Release();
            }
        }
    }
}
