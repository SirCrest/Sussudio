using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

static partial class NativeXuProbeI2cCommands
{
    public static int RunTopologyProbe(CaptureDevice dev)
    {
        // Dump full topology with node type GUIDs and test each as a property set
        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(dev, out var vidT, out var pidT))
        {
            Console.Error.WriteLine("Cannot parse device IDs");
            return 1;
        }
        var ifacesT = GetSelectedKsInterfaces(dev);
        foreach (var ksIfT in ifacesT)
        {
            Console.WriteLine($"\n=== Interface: {ksIfT.Path} ===");
            using var hT = KsExtensionUnitNative.TryOpen(ksIfT.Path, out _);
            if (hT == null) continue;
            if (!KsExtensionUnitNative.TryReadTopologyNodes(hT, out var nsT, out _)) continue;

            foreach (var node in nsT ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>())
            {
                Console.WriteLine($"\n  Node {node.NodeId}: type={node.NodeType} devSpec={node.IsDevSpecific}");

                // Try using the node's own type GUID as a property set
                Console.WriteLine($"    Testing with own GUID as property set:");
                for (int sel = 1; sel <= 5; sel++)
                {
                    foreach (int bufSz in new[] { 256, 1024 })
                    {
                        if (KsExtensionUnitNative.TryXuGetDirect(hT, node.NodeId, node.NodeType, sel, bufSz, out var gd, out var gb, out var gw))
                        {
                            var hasData = gd.Take(gb).Any(b => b != 0);
                            Console.WriteLine($"      Sel {sel} ({bufSz}B): OK ({gb}B) {(hasData ? BitConverter.ToString(gd, 0, Math.Min(gb, 16)) + " ***DATA***" : "all-zero")}");
                            break;
                        }
                        else if (gw == 122) // needs bigger buffer
                        {
                            continue;
                        }
                        else
                        {
                            if (bufSz == 256) // only print once per selector
                                Console.WriteLine($"      Sel {sel}: failed win32={gw}");
                            break;
                        }
                    }
                }

                // Also try the XU GUID on this node
                var xuGuidT = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
                if (node.NodeType != xuGuidT)
                {
                    Console.WriteLine($"    Testing with XU GUID on this node:");
                    for (int sel = 1; sel <= 3; sel++)
                    {
                        if (KsExtensionUnitNative.TryXuGetDirect(hT, node.NodeId, xuGuidT, sel, 256, out var gd2, out var gb2, out var gw2))
                        {
                            var hasData = gd2.Take(gb2).Any(b => b != 0);
                            Console.WriteLine($"      Sel {sel}: OK ({gb2}B) {(hasData ? BitConverter.ToString(gd2, 0, Math.Min(gb2, 16)) : "all-zero")}");
                        }
                        else
                        {
                            Console.WriteLine($"      Sel {sel}: failed win32={gw2}");
                        }
                    }
                }
            }
        }
        return 0;
    }
}
