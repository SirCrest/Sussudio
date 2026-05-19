using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    /// <summary>
    /// Sends an AT SET command to the Realtek chip. Opens its own KS handle.
    /// </summary>
    public static async Task<bool> SendAtSetCommandAsync(
        CaptureDevice? device,
        int cmdCode,
        byte[] inputData,
        CancellationToken cancellationToken = default)
    {
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return false;
        }

        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId))
        {
            return false;
        }

        if (!NativeXuDeviceSupport.HasSelectedInterface(device, "SET"))
        {
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return false;
            }

            var interfaces = NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device);
            foreach (var ksInterface in interfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();
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

                    var result = SendAtSetCommand(handle, node.NodeId, cmdCode, inputData, cancellationToken);
                    if (result)
                    {
                        Logger.Log($"NATIVEXU_SET_OK cmd=0x{cmdCode:X2} inputLen={inputData.Length}");
                        return true;
                    }
                }
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_SET_EXCEPTION cmd=0x{cmdCode:X2} type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
        finally
        {
            if (gateAcquired)
            {
                NativeXuDeviceSupport.ReleaseTransportGate();
            }
        }
    }

    public static Task<bool> SetInputSourceAsync(
        CaptureDevice? device,
        int source,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetInputSource,
            new byte[] { (byte)source },
            $"InputSource source={source}",
            ct);

    public static Task<bool> SetAdcOnOffAsync(
        CaptureDevice? device,
        bool on,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetAdcOnOff,
            BitConverter.GetBytes(on ? 1 : 0),
            $"AdcOnOff on={on}",
            ct);

    public static Task<bool> SetAdcVolumeGainAsync(
        CaptureDevice? device,
        int gain,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetAdcVolumeGain,
            BitConverter.GetBytes(gain),
            $"AdcVolumeGain gain={gain}",
            ct);

    public static Task<bool> SetHdr2SdrOnOffAsync(
        CaptureDevice? device,
        bool on,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetHdr2Sdr,
            new byte[] { on ? (byte)1 : (byte)0 },
            $"Hdr2Sdr on={on}",
            ct);

    public static Task<bool> SetLedLightAsync(
        CaptureDevice? device,
        int value,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetLedLight,
            BitConverter.GetBytes(value),
            $"LedLight value={value}",
            ct);

    public static Task<bool> SetDacHpOnOffAsync(
        CaptureDevice? device,
        bool on,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetDacHpOnOff,
            BitConverter.GetBytes(on ? 1 : 0),
            $"DacHpOnOff on={on}",
            ct);

    public static Task<bool> SetDacHpMuteAsync(
        CaptureDevice? device,
        bool mute,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            0x30,
            new byte[] { mute ? (byte)1 : (byte)0 },
            $"DacHpMute mute={mute}",
            ct);

    public static Task<bool> SetHpOutGainAsync(
        CaptureDevice? device,
        int gain,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetHpOutGain,
            BitConverter.GetBytes((short)gain),
            $"HpOutGain gain={gain}",
            ct);

    // Public wrapper for probe tools
    public static Task<bool> SendNamedSetCommandPublicAsync(
        CaptureDevice? device,
        int cmdCode,
        byte[] inputData,
        string operation,
        CancellationToken cancellationToken = default)
        => SendNamedSetCommandAsync(device, cmdCode, inputData, operation, cancellationToken);

    private static async Task<bool> SendNamedSetCommandAsync(
        CaptureDevice? device,
        int cmdCode,
        byte[] inputData,
        string operation,
        CancellationToken cancellationToken)
    {
        Logger.Log(
            $"NATIVEXU_SET_REQUEST op='{operation}' cmd=0x{cmdCode:X2} " +
            $"inputLen={inputData.Length} input={GetHexPreview(inputData, inputData.Length, inputData.Length)}");
        var success = await SendAtSetCommandAsync(device, cmdCode, inputData, cancellationToken).ConfigureAwait(false);
        Logger.Log($"NATIVEXU_SET_RESULT op='{operation}' cmd=0x{cmdCode:X2} success={success}");
        return success;
    }

}
