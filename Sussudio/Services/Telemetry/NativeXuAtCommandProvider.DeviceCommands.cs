using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
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

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            !IsSupported4kXDevice(vendorId, productId))
        {
            return false;
        }

        if (!HasSelectedNativeXuInterface(device, "SET"))
        {
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await CallGate.WaitAsync(GateTimeoutMs, cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return false;
            }

            var interfaces = EnumerateKsInterfaces(vendorId, productId, device);
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
                CallGate.Release();
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

    private static bool ExecuteGainChange(
        SafeFileHandle handle,
        int nodeId,
        byte gainByte,
        bool persistFlash,
        CancellationToken cancellationToken)
    {
        // Compute 3-zone codec register values
        ComputeGainRegisters(gainByte, out var pga, out var digAtt, out var outGain);

        Logger.Log($"NATIVEXU_SET_GAIN regs gain=0x{gainByte:X2} PGA=0x{pga:X2} DigAtt=0x{digAtt:X2} OutGain=0x{outGain:X2} flash={persistFlash}");

        // I2C codec register writes (immediate hardware effect)
        // Write 6 registers: 0x0A, 0x0B (PGA), 0x0E, 0x0F (OutGain), 0x0C, 0x0D (DigAtt)
        var i2cWrites = new (byte reg, byte val)[]
        {
            (0x0A, pga),
            (0x0B, pga),
            (0x0E, outGain),
            (0x0F, outGain),
            (0x0C, digAtt),
            (0x0D, digAtt),
        };

        foreach (var (reg, val) in i2cWrites)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!SendSelector4Command(
                    handle,
                    nodeId,
                    CmdI2cWrite,
                    new byte[] { 0x00, 0x4A, 0x02, 0x00, reg, val },
                    cancellationToken))
            {
                Logger.Log($"NATIVEXU_SET_GAIN FAILED stage=i2c_write_r{reg:X2}");
                return false;
            }
        }

        if (!persistFlash)
        {
            return true;
        }

        // Flash persistence (deferred during rapid slider changes)
        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashGetCustomerProprietary, Array.Empty<byte>(), cancellationToken))
        {
            Logger.Log("NATIVEXU_SET_GAIN FAILED stage=flash_read");
            return false;
        }

        var flashData = new byte[32];
        flashData[0] = 0x01; // Analog
        flashData[1] = 0x80;
        flashData[2] = gainByte;
        flashData[3] = 0xAA;
        flashData[4] = 0x55;

        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashSetCustomerProprietary, flashData, cancellationToken))
        {
            Logger.Log("NATIVEXU_SET_GAIN FAILED stage=flash_write");
            return false;
        }

        return true;
    }

    internal static void ComputeGainRegisters(byte gainByte, out byte pga, out byte digAtt, out byte outGain)
    {
        if (gainByte < 0x80)
        {
            // Zone 1: Digital attenuation only
            pga = 0x00;
            digAtt = (byte)(0x80 + gainByte);
            outGain = 0x00;
        }
        else if (gainByte < 0x98)
        {
            // Zone 2: PGA gain ramp (0-23)
            pga = (byte)(gainByte - 0x80);
            digAtt = 0x00;
            outGain = 0x00;
        }
        else if (gainByte < 0xB0)
        {
            // Zone 3: PGA maxed + output gain ramp (0-23)
            pga = 0x18;
            digAtt = 0x00;
            outGain = (byte)(gainByte - 0x98);
        }
        else
        {
            // Zone 4: All stages maxed
            pga = 0x18;
            digAtt = 0x00;
            outGain = 0x18;
        }
    }

    private static bool ExecuteAudioSwitch(
        SafeFileHandle handle,
        int nodeId,
        bool analog,
        byte gainByte,
        string sourceLabel,
        CancellationToken cancellationToken)
    {
        // Phase 1: Flash persistence (selector 1 AT commands)
        // 1a. GPIO prep
        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdGpioSetParam, new byte[] { 0x00, 0x05, 0x00 }, cancellationToken))
        {
            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=gpio");
            return false;
        }

        // 1b. Flash read current state
        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashGetCustomerProprietary, Array.Empty<byte>(), cancellationToken))
        {
            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=flash_read");
            return false;
        }

        // 1c. Flash write new source (preserve current gain byte)
        var flashData = new byte[32];
        flashData[0] = analog ? (byte)0x01 : (byte)0x00;
        flashData[1] = 0x80;
        flashData[2] = gainByte;
        flashData[3] = 0xAA;
        flashData[4] = 0x55;

        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashSetCustomerProprietary, flashData, cancellationToken))
        {
            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=flash_write");
            return false;
        }

        Logger.Log($"NATIVEXU_SWITCH_AUDIO flash_ok source={sourceLabel}");

        // Phase 2: I2C codec register writes (selector 4)
        // 14-command sequence to audio codec at I2C address 0x4A
        // Commands 8-11 differ between HDMI and Analog
        byte reg0E = analog ? (byte)0x18 : (byte)0x98;
        byte reg0F = analog ? (byte)0x18 : (byte)0x98;
        byte reg10 = analog ? (byte)0x80 : (byte)0x00;
        byte reg11 = analog ? (byte)0x80 : (byte)0x00;

        var i2cCommands = new (int cmd, byte[] data)[]
        {
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }),       //  0: page select
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01, 0x00 }), //  1: read status
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x03, 0xA0 }),       //  2: mixer config
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x0E, 0x01, 0x00 }), //  3: read reg 0E
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x10, 0x01, 0x00 }), //  4: read reg 10
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01, 0x00 }), //  5: read status
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }),       //  6: unmute/prep
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x07, 0x00 }),       //  7: mixer enable
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x0E, reg0E }),      //  8: input select
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x0F, reg0F }),      //  9: input select
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x10, reg10 }),      // 10: input select
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x11, reg11 }),      // 11: input select
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }),       // 12: finalize
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x07, 0x00 }),       // 13: finalize
        };

        for (var i = 0; i < i2cCommands.Length; i++)
        {
            var (cmd, data) = i2cCommands[i];
            cancellationToken.ThrowIfCancellationRequested();
            if (!SendSelector4Command(handle, nodeId, cmd, data, cancellationToken))
            {
                Logger.Log($"NATIVEXU_SWITCH_AUDIO FAILED stage=i2c_{i} cmd=0x{cmd:X2}");
                return false;
            }
        }

        Logger.Log($"NATIVEXU_SWITCH_AUDIO i2c_ok source={sourceLabel} commands=14");
        return true;
    }

    /// <summary>
    /// Sends an AT command directly on selector 4 with a 525-byte padded payload.
    /// Used for I2C register read/write commands to the audio codec.
    /// </summary>
    private static bool SendSelector4Command(
        SafeFileHandle handle,
        int nodeId,
        int cmdCode,
        byte[] inputData,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var atFrame = BuildAtWriteFrame(cmdCode, inputData);
        var payload = new byte[I2cPayloadSize];
        Array.Copy(atFrame, 0, payload, 0, atFrame.Length);
        // Rest is already zero-padded

        cancellationToken.ThrowIfCancellationRequested();
        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, I2cSelector, payload, out var win32))
        {
            Logger.Log($"NATIVEXU_SEL4_FAILED cmd=0x{cmdCode:X2} win32={FormatWin32Code(win32)}");
            return false;
        }

        return true;
    }

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

    public static async Task<byte[]?> ReadAtCommandAsync(
        CaptureDevice? device,
        int cmdCode,
        string label,
        CancellationToken cancellationToken = default)
    {
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return null;
        }

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            !IsSupported4kXDevice(vendorId, productId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(device.NativeXuInterfacePath))
        {
            return null;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await CallGate.WaitAsync(GateTimeoutMs, cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return null;
            }

            var interfaces = EnumerateKsInterfaces(vendorId, productId, device);
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

                    var result = SendAtCommand(handle, node.NodeId, label, cmdCode);
                    if (result.Success)
                    {
                        return result.Response;
                    }
                }
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_GET_EXCEPTION cmd=0x{cmdCode:X2} type={ex.GetType().Name} msg={ex.Message}");
            return null;
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
