using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static bool ExecuteGainChange(
        SafeFileHandle handle,
        int nodeId,
        byte gainByte,
        bool persistFlash,
        CancellationToken cancellationToken)
    {
        ComputeGainRegisters(gainByte, out var pga, out var digAtt, out var outGain);

        Logger.Log($"NATIVEXU_SET_GAIN regs gain=0x{gainByte:X2} PGA=0x{pga:X2} DigAtt=0x{digAtt:X2} OutGain=0x{outGain:X2} flash={persistFlash}");

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

        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashGetCustomerProprietary, Array.Empty<byte>(), cancellationToken))
        {
            Logger.Log("NATIVEXU_SET_GAIN FAILED stage=flash_read");
            return false;
        }

        var flashData = new byte[32];
        flashData[0] = 0x01;
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
            pga = 0x00;
            digAtt = (byte)(0x80 + gainByte);
            outGain = 0x00;
        }
        else if (gainByte < 0x98)
        {
            pga = (byte)(gainByte - 0x80);
            digAtt = 0x00;
            outGain = 0x00;
        }
        else if (gainByte < 0xB0)
        {
            pga = 0x18;
            digAtt = 0x00;
            outGain = (byte)(gainByte - 0x98);
        }
        else
        {
            pga = 0x18;
            digAtt = 0x00;
            outGain = 0x18;
        }
    }
}
