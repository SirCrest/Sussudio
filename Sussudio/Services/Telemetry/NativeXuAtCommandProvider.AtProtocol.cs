using System;
using System.Globalization;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static AtCommandResult SendAtCommand(
        SafeFileHandle handle,
        int nodeId,
        string name,
        int cmdCode)
    {
        var requestFrame = BuildAtReadFrame(cmdCode);
        var triggerData = new byte[]
        {
            (byte)(requestFrame.Length & 0xFF),
            (byte)((requestFrame.Length >> 8) & 0xFF)
        };

        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtTriggerSelector, triggerData, out var triggerWin32))
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=trigger win32={FormatWin32Code(triggerWin32)}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), triggerWin32, "trigger");
        }

        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtPayloadSelector, requestFrame, out var sendWin32))
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=send win32={FormatWin32Code(sendWin32)}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), sendWin32, "send");
        }

        if (!KsExtensionUnitNative.TryXuGetDirect(
                handle,
                nodeId,
                XuGuid,
                AtTriggerSelector,
                2,
                out var lengthData,
                out var lengthBytes,
                out var lengthWin32))
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=getlength win32={FormatWin32Code(lengthWin32)}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), lengthWin32, "getlength");
        }

        var responseFrameLen = lengthBytes >= 2
            ? (int)BitConverter.ToUInt16(lengthData, 0)
            : 0;

        if (responseFrameLen <= 0 || responseFrameLen > MaxAtResponseFrameSize)
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=framelen len={responseFrameLen}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), null, "framelen");
        }

        if (!KsExtensionUnitNative.TryXuGetDirect(
                handle,
                nodeId,
                XuGuid,
                AtPayloadSelector,
                responseFrameLen,
                out var responseFrame,
                out var responseBytes,
                out var responseWin32))
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=getresponse win32={FormatWin32Code(responseWin32)}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), responseWin32, "getresponse");
        }

        var rawData = StripAtFrameEnvelope(responseFrame, responseBytes);
        Logger.Log(
            $"NATIVEXU_AT cmd={name} code=0x{cmdCode:X2} frameLen={responseFrameLen} " +
            $"rawBytes={rawData.Length} preview={GetHexPreview(rawData, rawData.Length, 32)}");
        return new AtCommandResult(name, cmdCode, true, rawData, null, null);
    }

    private static bool SendAtSetCommand(
        SafeFileHandle handle,
        int nodeId,
        int cmdCode,
        byte[] inputData,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requestFrame = BuildAtWriteFrame(cmdCode, inputData);
        var triggerData = new byte[]
        {
            (byte)(requestFrame.Length & 0xFF),
            (byte)((requestFrame.Length >> 8) & 0xFF)
        };

        cancellationToken.ThrowIfCancellationRequested();
        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtTriggerSelector, triggerData, out var triggerWin32))
        {
            Logger.Log($"NATIVEXU_SET_FAILED cmd=0x{cmdCode:X2} stage=trigger win32={FormatWin32Code(triggerWin32)}");
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtPayloadSelector, requestFrame, out var sendWin32))
        {
            Logger.Log($"NATIVEXU_SET_FAILED cmd=0x{cmdCode:X2} stage=send win32={FormatWin32Code(sendWin32)}");
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, XuGuid, AtTriggerSelector, 2, out _, out _, out _);
        return true;
    }

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

        cancellationToken.ThrowIfCancellationRequested();
        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, I2cSelector, payload, out var win32))
        {
            Logger.Log($"NATIVEXU_SEL4_FAILED cmd=0x{cmdCode:X2} win32={FormatWin32Code(win32)}");
            return false;
        }

        return true;
    }

    private static byte ComputeLrc(ReadOnlySpan<byte> data)
    {
        byte sum = 0;
        foreach (var value in data)
        {
            sum = (byte)(sum + value);
        }

        return (byte)(~sum + 1);
    }

    private static byte[] BuildAtReadFrame(int cmdCode)
    {
        var frame = new byte[9];
        frame[0] = 0xA1;
        frame[1] = 0x06;
        frame[4] = (byte)(cmdCode & 0xFF);
        frame[5] = (byte)((cmdCode >> 8) & 0xFF);
        frame[6] = (byte)((cmdCode >> 16) & 0xFF);
        frame[7] = (byte)((cmdCode >> 24) & 0xFF);
        frame[8] = ComputeLrc(frame.AsSpan(0, 8));
        return frame;
    }

    private static byte[] BuildAtWriteFrame(int cmdCode, byte[] inputData)
    {
        var dataLen = 4 + inputData.Length;
        var frameLen = AtFrameHeaderSize + dataLen + AtFrameLrcSize;
        var frame = new byte[frameLen];
        frame[0] = 0xA1;
        frame[1] = (byte)((dataLen + 2) & 0x7F);
        frame[4] = (byte)(cmdCode & 0xFF);
        frame[5] = (byte)((cmdCode >> 8) & 0xFF);
        frame[6] = (byte)((cmdCode >> 16) & 0xFF);
        frame[7] = (byte)((cmdCode >> 24) & 0xFF);
        if (inputData.Length > 0)
        {
            Array.Copy(inputData, 0, frame, 8, inputData.Length);
        }

        frame[frameLen - 1] = ComputeLrc(frame.AsSpan(0, frameLen - 1));
        return frame;
    }

    private static byte[] StripAtFrameEnvelope(byte[] responseFrame, int frameLength)
    {
        var effectiveLength = Math.Min(Math.Max(frameLength, 0), responseFrame.Length);
        if (effectiveLength <= AtFrameHeaderSize + AtFrameLrcSize)
        {
            return Array.Empty<byte>();
        }

        var dataLength = effectiveLength - AtFrameHeaderSize - AtFrameLrcSize;
        var result = new byte[dataLength];
        Array.Copy(responseFrame, AtFrameHeaderSize, result, 0, dataLength);
        return result;
    }

    private static string FormatWin32Code(int? win32Code)
        => win32Code.HasValue ? win32Code.Value.ToString(CultureInfo.InvariantCulture) : "unknown";

    private static string GetHexPreview(byte[] buffer, int bytesReturned, int maxBytes)
    {
        if (buffer.Length == 0 || bytesReturned <= 0)
        {
            return "empty";
        }

        var previewLength = Math.Min(Math.Min(bytesReturned, buffer.Length), maxBytes);
        return previewLength > 0
            ? Convert.ToHexString(buffer.AsSpan(0, previewLength))
            : "empty";
    }
}
