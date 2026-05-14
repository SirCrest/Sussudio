using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Capture;

internal static partial class KsExtensionUnitNative
{
    internal static IReadOnlyList<KsInterfacePath> EnumerateKsInterfaces(ushort vendorId, ushort productId)
    {
        var token = $"vid_{vendorId:x4}&pid_{productId:x4}";
        var result = new List<KsInterfacePath>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categories = new[] { KsCategoryCapture, KsCategoryVideo };

        foreach (var category in categories)
        {
            var categoryGuid = category;
            var deviceInfoSet = SetupDiGetClassDevs(ref categoryGuid, null, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
            if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(-1))
            {
                continue;
            }

            try
            {
                var interfaceData = new SP_DEVICE_INTERFACE_DATA
                {
                    cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>()
                };

                for (uint index = 0; ; index++)
                {
                    interfaceData.cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>();
                    if (!SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref categoryGuid, index, ref interfaceData))
                    {
                        var error = Marshal.GetLastWin32Error();
                        if (error == ErrorNoMoreItems)
                        {
                            break;
                        }

                        continue;
                    }

                    var detail = new SP_DEVICE_INTERFACE_DETAIL_DATA
                    {
                        cbSize = IntPtr.Size == 8 ? 8 : 6,
                        DevicePath = string.Empty
                    };

                    if (!SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet,
                            ref interfaceData,
                            ref detail,
                            (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DETAIL_DATA>(),
                            out _,
                            IntPtr.Zero))
                    {
                        continue;
                    }

                    if (detail.DevicePath.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (!dedupe.Add(detail.DevicePath))
                    {
                        continue;
                    }

                    result.Add(new KsInterfacePath(detail.DevicePath, categoryGuid));
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        return result;
    }
}
