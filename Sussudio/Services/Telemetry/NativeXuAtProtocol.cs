using System;

namespace Sussudio.Services.Telemetry;

/// <summary>
/// Pure byte-level framing and HDMI InfoFrame decoding used by the Native XU provider.
/// </summary>
internal static class NativeXuAtProtocol
{
    private const int AtFrameHeaderSize = 4;
    private const int AtFrameLrcSize = 1;

    internal readonly record struct HdrMetadataInfo(bool HasMetadata, byte? Eotf, bool? IsHdr);

    internal readonly record struct AviInfoFrameInfo(
        bool HasData,
        string? ColorSpace,
        string? Colorimetry,
        string? Quantization)
    {
        internal static AviInfoFrameInfo Empty => new(false, null, null, null);
    }

    internal static byte[] BuildAtReadFrame(int cmdCode)
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

    internal static byte[] BuildAtWriteFrame(int cmdCode, byte[] inputData)
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

    internal static byte[] StripAtFrameEnvelope(byte[] responseFrame, int frameLength)
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

    internal static AviInfoFrameInfo DecodeAviInfoFrame(byte[] buffer)
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

    internal static HdrMetadataInfo DecodeHdrMetadata(byte[] buffer)
    {
        const int InfoFrameTypeOffset = 0;
        const int InfoFrameLengthOffset = 2;
        const int HdrStaticMetadataInfoFrameType = 0x87;
        const int HdrStaticMetadataChecksumOffset = 3;
        const int HdrStaticMetadataDataStartOffset = HdrStaticMetadataChecksumOffset + 1;
        const int HdrStaticMetadataEotfOffset = HdrStaticMetadataDataStartOffset;

        if (buffer.Length <= HdrStaticMetadataEotfOffset ||
            !HasNonZeroData(buffer) ||
            buffer[InfoFrameTypeOffset] != HdrStaticMetadataInfoFrameType ||
            buffer[InfoFrameLengthOffset] < 1)
        {
            return new HdrMetadataInfo(false, null, null);
        }

        var eotf = buffer[HdrStaticMetadataEotfOffset];
        var isHdr = eotf switch
        {
            2 or 3 => true,
            0 or 1 => false,
            _ => (bool?)null
        };
        return new HdrMetadataInfo(true, eotf, isHdr);
    }

    private static bool HasNonZeroData(byte[] buffer)
        => buffer.AsSpan().IndexOfAnyExcept((byte)0) >= 0;

    private static byte ComputeLrc(ReadOnlySpan<byte> data)
    {
        byte sum = 0;
        foreach (var value in data)
        {
            sum = (byte)(sum + value);
        }

        return (byte)(~sum + 1);
    }
}
