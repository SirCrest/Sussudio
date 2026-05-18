using System;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class DeviceService
{
    private const string PreferredNativeXuInterfaceFragment = "{65e8773d-8f56-11d0-a3b9-00a0c9223196}";

    private static string? ResolveNativeXuInterfacePath(string deviceId)
    {
        var probeDevice = new CaptureDevice { Id = deviceId };
        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(probeDevice, out var vendorId, out var productId))
        {
            return null;
        }

        try
        {
            var interfaces = KsExtensionUnitNative.EnumerateKsInterfaces(vendorId, productId);
            if (interfaces.Count == 0)
            {
                return null;
            }

            var deviceInstanceKey = GetDeviceInstanceKey(deviceId);
            var sameDeviceInterfaces = interfaces
                .Where(ksInterface => string.Equals(
                    GetDeviceInstanceKey(ksInterface.Path),
                    deviceInstanceKey,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (sameDeviceInterfaces.Length == 0)
            {
                Logger.Log($"Native XU interface resolution found no matching interface for device '{deviceId}'");
                return null;
            }

            return sameDeviceInterfaces
                .Select(ksInterface => ksInterface.Path)
                .OrderByDescending(path =>
                    path.IndexOf(PreferredNativeXuInterfaceFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.Log($"Native XU interface resolution failed for device '{deviceId}': {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static string GetDeviceInstanceKey(string interfacePath)
    {
        var categoryStart = interfacePath.LastIndexOf("#{", StringComparison.Ordinal);
        return categoryStart > 0
            ? interfacePath[..categoryStart]
            : interfacePath;
    }

}
