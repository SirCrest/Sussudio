using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

public sealed class KsXuSourceSignalTelemetryProvider : ISourceSignalTelemetryProvider
{
    private static readonly Guid Elgato4kXXuGuid = new("961073C7-49F7-44F2-AB42-E940405940C2");
    private static readonly SemaphoreSlim KsXuCallGate = new(1, 1);

    private const int GateTimeoutMs = 500;
    private const int HdrDetectionSelector = 3;
    private const int HdrFingerprintPrefixLength = 24;
    private const int MaxXuBufferSize = 1024;
    private const ushort Elgato4kXVendorId = 0x0FD9;
    private const ushort Elgato4kXProductId = 0x009B;

    public async Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("device-unavailable");
        }

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            vendorId != Elgato4kXVendorId ||
            productId != Elgato4kXProductId)
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-device-unsupported");
        }

        var gateAcquired = false;
        SourceSignalTelemetrySnapshot? inconclusiveSnapshot = null;
        string? unavailableReason = null;
        string? unavailableDetail = null;

        try
        {
            gateAcquired = await KsXuCallGate.WaitAsync(GateTimeoutMs, cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-native-busy", $"{GateTimeoutMs}ms");
            }

            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> interfaces;
            try
            {
                interfaces = KsExtensionUnitNative.EnumerateKsInterfaces(vendorId, productId);
            }
            catch (Exception ex)
            {
                Logger.Log($"KSXU_ENUMERATE_FAILED type={ex.GetType().Name} message={ex.Message}");
                return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-enumerate-failed", ex.Message);
            }

            if (interfaces.Count == 0)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-interface-not-found");
            }

            foreach (var ksInterface in interfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out var openErrorCode);
                    if (handle is null)
                    {
                        unavailableReason = "ksxu-open-failed";
                        unavailableDetail = DescribeWin32Detail(ksInterface.Path, openErrorCode);
                        Logger.Log($"KSXU_OPEN_FAILED path='{ksInterface.Path}' detail='{unavailableDetail}'");
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out var topologyError))
                    {
                        unavailableReason = "ksxu-topology-read-failed";
                        unavailableDetail = $"{ksInterface.Path}: {topologyError ?? "unknown"}";
                        Logger.Log($"KSXU_TOPOLOGY_FAILED path='{ksInterface.Path}' error='{topologyError ?? "unknown"}'");
                        continue;
                    }

                    var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
                    var devSpecificIds = new List<int>();
                    foreach (var node in nodeList)
                    {
                        if (node.IsDevSpecific)
                        {
                            devSpecificIds.Add(node.NodeId);
                        }
                    }

                    Logger.Log(
                        $"KSXU_TOPOLOGY path='{ksInterface.Path}' nodeCount={nodeList.Count} " +
                        $"devSpecificNodes=[{string.Join(",", devSpecificIds)}]");

                    var candidateNodes = devSpecificIds.Count > 0
                        ? nodeList.Where(node => node.IsDevSpecific)
                        : nodeList.AsEnumerable();

                    foreach (var node in candidateNodes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var readSucceeded = KsExtensionUnitNative.TryReadPropertyValue(
                            handle,
                            node.NodeId,
                            Elgato4kXXuGuid,
                            HdrDetectionSelector,
                            MaxXuBufferSize,
                            out var bytesReturned,
                            out var valueHexPreview,
                            out var readWin32Code,
                            out var readError);

                        Logger.Log(
                            $"KSXU_READ_RESULT path='{ksInterface.Path}' node={node.NodeId} " +
                            $"selector={HdrDetectionSelector} succeeded={readSucceeded} " +
                            $"bytes={bytesReturned} win32={readWin32Code} " +
                            $"hex={valueHexPreview ?? "null"}");

                        if (readSucceeded)
                        {
                            foreach (var selector in new[] { 1, 2, 4 })
                            {
                                KsExtensionUnitNative.TryReadPropertyValue(
                                    handle,
                                    node.NodeId,
                                    Elgato4kXXuGuid,
                                    selector,
                                    MaxXuBufferSize,
                                    out var diagBytes,
                                    out var diagHex,
                                    out _,
                                    out _);
                                Logger.Log($"KSXU_DIAG sel={selector} bytes={diagBytes} hex={diagHex ?? "null"}");
                            }

                            return CreateSnapshot(ksInterface.Path, bytesReturned, valueHexPreview);
                        }

                        if (readWin32Code is 1168 or 1170 or 87 or 1)
                        {
                            continue;
                        }

                        var readDetail = string.IsNullOrWhiteSpace(readError)
                            ? DescribeWin32Detail(ksInterface.Path, readWin32Code)
                            : $"{ksInterface.Path}: {readError}";

                        if (IsTransientAccessFailure(readWin32Code))
                        {
                            unavailableReason = "ksxu-read-failed";
                            unavailableDetail = readDetail;
                            continue;
                        }

                        inconclusiveSnapshot ??= CreateSnapshot(ksInterface.Path, bytesReturned, valueHexPreview);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    unavailableReason = "ksxu-interface-exception";
                    unavailableDetail = $"{ksInterface.Path}: {ex.GetType().Name}: {ex.Message}";
                    Logger.Log($"KSXU_INTERFACE_EXCEPTION path='{ksInterface.Path}' type={ex.GetType().Name} message={ex.Message}");
                }
            }

            return inconclusiveSnapshot ??
                   SourceSignalTelemetrySnapshot.CreateUnavailable(
                       unavailableReason ?? "ksxu-read-failed",
                       unavailableDetail);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"KSXU_PROVIDER_EXCEPTION type={ex.GetType().Name} message={ex.Message}");
            return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-exception", $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (gateAcquired)
            {
                KsXuCallGate.Release();
            }
        }
    }

    private static SourceSignalTelemetrySnapshot CreateSnapshot(string interfacePath, int bytesReturned, string? valueHexPreview)
    {
        var prefixHex = GetPrefixHex(valueHexPreview, bytesReturned);
        var isHdr = ResolveHdrState(valueHexPreview, bytesReturned);
        var hdrLabel = isHdr switch
        {
            true => "on",
            false => "off",
            _ => "unknown"
        };

        return new SourceSignalTelemetrySnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Availability = SourceTelemetryAvailability.Available,
            Origin = SourceTelemetryOrigin.KsXu,
            OriginDetail = $"KsXu:{interfacePath}",
            Confidence = isHdr.HasValue ? SourceTelemetryConfidence.Medium : SourceTelemetryConfidence.Low,
            IsHdr = isHdr,
            DiagnosticSummary = $"ksxu:hdr={hdrLabel}:s3prefix={prefixHex}"
        };
    }

    private static bool? ResolveHdrState(string? valueHexPreview, int bytesReturned)
    {
        if (bytesReturned < HdrFingerprintPrefixLength)
        {
            return null;
        }

        var expectedPrefixLength = HdrFingerprintPrefixLength * 2;
        if (string.IsNullOrWhiteSpace(valueHexPreview) || valueHexPreview.Length < expectedPrefixLength)
        {
            return null;
        }

        for (var index = 0; index < expectedPrefixLength; index++)
        {
            if (valueHexPreview[index] != '0')
            {
                return false;
            }
        }

        return true;
    }

    private static string GetPrefixHex(string? valueHexPreview, int bytesReturned)
    {
        if (bytesReturned <= 0 || string.IsNullOrWhiteSpace(valueHexPreview))
        {
            return "none";
        }

        var prefixLength = Math.Min(bytesReturned, HdrFingerprintPrefixLength) * 2;
        prefixLength = Math.Min(prefixLength, valueHexPreview.Length);
        return prefixLength > 0 ? valueHexPreview[..prefixLength] : "none";
    }

    private static bool TryParseVendorProductIds(string deviceId, out ushort vendorId, out ushort productId)
    {
        vendorId = 0;
        productId = 0;
        return TryParseHexToken(deviceId, "vid_", out vendorId) &&
               TryParseHexToken(deviceId, "pid_", out productId);
    }

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

    private static bool IsTransientAccessFailure(int? win32Code)
        => win32Code is 5 or 32 or 170;

    private static string DescribeWin32Detail(string path, int? win32Code)
    {
        if (!win32Code.HasValue)
        {
            return $"{path}: unknown";
        }

        return $"{path}: win32={win32Code.Value} ({new Win32Exception(win32Code.Value).Message})";
    }
}
