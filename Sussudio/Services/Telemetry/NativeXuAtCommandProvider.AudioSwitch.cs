using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static bool ExecuteAudioSwitch(
        SafeFileHandle handle,
        int nodeId,
        bool analog,
        byte gainByte,
        string sourceLabel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdGpioSetParam, new byte[] { 0x00, 0x05, 0x00 }, cancellationToken))
        {
            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=gpio");
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashGetCustomerProprietary, Array.Empty<byte>(), cancellationToken))
        {
            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=flash_read");
            return false;
        }

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

        byte reg0E = analog ? (byte)0x18 : (byte)0x98;
        byte reg0F = analog ? (byte)0x18 : (byte)0x98;
        byte reg10 = analog ? (byte)0x80 : (byte)0x00;
        byte reg11 = analog ? (byte)0x80 : (byte)0x00;

        var i2cCommands = new (int cmd, byte[] data)[]
        {
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }),
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01, 0x00 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x03, 0xA0 }),
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x0E, 0x01, 0x00 }),
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x10, 0x01, 0x00 }),
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01, 0x00 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x07, 0x00 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x0E, reg0E }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x0F, reg0F }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x10, reg10 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x11, reg11 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x07, 0x00 }),
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
}
