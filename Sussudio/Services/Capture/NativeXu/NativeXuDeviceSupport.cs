using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

internal static class NativeXuDeviceSupport
{
    public static readonly Guid ExtensionUnitGuid = new("961073C7-49F7-44F2-AB42-E940405940C2");

    public const int DefaultTransportGateTimeoutMs = 500;

    private const ushort Elgato4kXVendorId = 0x0FD9;
    private const ushort Elgato4kXProductIdOriginal = 0x009B;
    private const ushort Elgato4kXProductIdRevision = 0x009C;
    private const ushort Elgato4kXProductIdAudioMode = 0x009D;

    private static readonly SemaphoreSlim TransportGate = new(1, 1);

    public static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken = default)
        => await TryAcquireTransportGateAsync(DefaultTransportGateTimeoutMs, cancellationToken).ConfigureAwait(false);

    public static async Task<bool> TryAcquireTransportGateAsync(
        int timeoutMs,
        CancellationToken cancellationToken = default)
        => await TransportGate.WaitAsync(timeoutMs, cancellationToken).ConfigureAwait(false);

    public static void ReleaseTransportGate() => TransportGate.Release();

    public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfaces(
        ushort vendorId,
        ushort productId,
        CaptureDevice? device)
        => EnumerateSelectedInterfacePath(device?.NativeXuInterfacePath);

    public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfacePath(
        string? selectedInterfacePath)
    {
        if (!string.IsNullOrWhiteSpace(selectedInterfacePath))
        {
            return new[] { new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty) };
        }

        return Array.Empty<KsExtensionUnitNative.KsInterfacePath>();
    }

    public static bool HasSelectedInterface(CaptureDevice? device, string operation)
    {
        if (!string.IsNullOrWhiteSpace(device?.NativeXuInterfacePath))
        {
            return true;
        }

        Logger.Log($"NATIVEXU_{operation}_FAILED stage=missing_selected_interface");
        return false;
    }

    public static bool TryGetSupported4kXIds(
        CaptureDevice? device,
        out ushort vendorId,
        out ushort productId)
    {
        vendorId = 0;
        productId = 0;

        return device != null &&
               !string.IsNullOrWhiteSpace(device.Id) &&
               TryParseVendorProductIds(device.Id, out vendorId, out productId) &&
               IsSupported4kXDevice(vendorId, productId);
    }

    public static bool TryParseVendorProductIds(string deviceId, out ushort vendorId, out ushort productId)
    {
        vendorId = 0;
        productId = 0;
        return TryParseHexToken(deviceId, "vid_", out vendorId) &&
               TryParseHexToken(deviceId, "pid_", out productId);
    }

    public static bool IsSupported4kXDevice(ushort vendorId, ushort productId)
        => vendorId == Elgato4kXVendorId &&
           (productId == Elgato4kXProductIdOriginal ||
            productId == Elgato4kXProductIdRevision ||
            productId == Elgato4kXProductIdAudioMode);

    private static bool TryParseHexToken(string value, string token, out ushort result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokenIndex = value.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (tokenIndex < 0 || tokenIndex + token.Length + 4 > value.Length)
        {
            return false;
        }

        return ushort.TryParse(
            value.AsSpan(tokenIndex + token.Length, 4),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out result);
    }
}
