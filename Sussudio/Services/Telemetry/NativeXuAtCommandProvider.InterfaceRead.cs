using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private NodeReadAttempt TryReadInterface(
        KsExtensionUnitNative.KsInterfacePath ksInterface,
        CancellationToken cancellationToken)
    {
        try
        {
            using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out var openErrorCode);
            if (handle is null)
            {
                var detail = DescribeWin32Detail(ksInterface.Path, openErrorCode);
                Logger.Log($"NATIVEXU_OPEN_FAILED path='{ksInterface.Path}' detail='{detail}'");
                return new NodeReadAttempt(null, false, "nativexu-open-failed", detail);
            }

            if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out var topologyError))
            {
                var detail = $"{ksInterface.Path}: {topologyError ?? "unknown"}";
                Logger.Log($"NATIVEXU_TOPOLOGY_FAILED path='{ksInterface.Path}' error='{topologyError ?? "unknown"}'");
                return new NodeReadAttempt(null, false, "nativexu-topology-read-failed", detail);
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
                $"NATIVEXU_TOPOLOGY path='{ksInterface.Path}' nodeCount={nodeList.Count} " +
                $"devSpecificNodes=[{string.Join(",", devSpecificIds)}]");

            var candidateNodes = devSpecificIds.Count > 0
                ? nodeList.Where(node => node.IsDevSpecific)
                : nodeList.AsEnumerable();

            string? unavailableReason = null;
            string? unavailableDetail = null;

            foreach (var node in candidateNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attempt = TryReadRolling(handle, node.NodeId, ksInterface.Path, cancellationToken);
                if (attempt.Snapshot != null)
                {
                    return attempt;
                }

                if (attempt.UnsupportedNode)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(attempt.UnavailableReason))
                {
                    unavailableReason = attempt.UnavailableReason;
                    unavailableDetail = attempt.UnavailableDetail;
                }
            }

            return new NodeReadAttempt(null, false, unavailableReason, unavailableDetail);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var detail = $"{ksInterface.Path}: {ex.GetType().Name}: {ex.Message}";
            Logger.Log($"NATIVEXU_INTERFACE_EXCEPTION path='{ksInterface.Path}' type={ex.GetType().Name} message={ex.Message}");
            return new NodeReadAttempt(null, false, "nativexu-interface-exception", detail);
        }
    }

    private static NodeReadAttempt CreateUnavailableNodeResult(string interfacePath, string reason)
        => new(null, false, reason, interfacePath);

    private static NodeReadAttempt HandleFailedCommand(string reason, string interfacePath, AtCommandResult result)
    {
        if (IsUnsupportedNodeFailure(result.Win32Code))
        {
            return new NodeReadAttempt(null, true, null, null);
        }

        var detail = DescribeCommandFailure(interfacePath, result);
        return new NodeReadAttempt(null, false, reason, detail);
    }

    private static bool IsUnsupportedNodeFailure(int? win32Code)
        => win32Code is KsExtensionUnitNative.ErrorNotFound
            or KsExtensionUnitNative.ErrorSetNotFound
            or KsExtensionUnitNative.ErrorInvalidParameter
            or KsExtensionUnitNative.ErrorInvalidFunction;

    private static string DescribeCommandFailure(string interfacePath, AtCommandResult result)
        => $"{interfacePath}: {result.Name}:{result.FailureStage ?? "unknown"} win32={FormatWin32Code(result.Win32Code)}";

    private static string DescribeWin32Detail(string path, int? win32Code)
    {
        if (!win32Code.HasValue)
        {
            return $"{path}: unknown";
        }

        return $"{path}: win32={win32Code.Value} ({new Win32Exception(win32Code.Value).Message})";
    }
}
