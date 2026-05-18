using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;
using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

var deviceNameFilter = args.Length > 0 ? args[0] : "4K X";
if (args.Length > 0 && string.Equals(args[0], "rtk-i2c", StringComparison.OrdinalIgnoreCase))
{
    var rtkArgs = args.Skip(1).ToArray();
    if (rtkArgs.Any(arg =>
        arg.StartsWith("--device=", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "--device", StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("rtk-i2c cannot accept a device filter because RTK_IO selects by name, not by native XU path.");
        Console.Error.WriteLine("Disconnect other supported devices so the locator can prove the selection is unambiguous.");
        return 1;
    }

    var dev = NativeXuProbeDeviceLocator.Find(null);
    if (dev == null)
    {
        return 1;
    }

    return RtkI2cProbe.Run(rtkArgs, dev);
}
if (args.Length > 0 && string.Equals(args[0], "service", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeServiceProbe.RunServiceControlProbeAsync(args.Skip(1).ToArray());
}
if (args.Length > 0 && string.Equals(args[0], "dump-s3", StringComparison.OrdinalIgnoreCase))
{
    // Dump XU selector 3 raw bytes to hex file for diffing
    var outPath = args.Length > 1 ? args[1] : "s3-dump.hex";
    var dev = NativeXuProbeDeviceLocator.Find("4K X");
    if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
    Console.WriteLine($"Device: {dev.Name}");

    var service = new NativeXuAudioControlService();
    var snapshot = await service.ReadPayloadSnapshotAsync(dev, CancellationToken.None).ConfigureAwait(false);
    if (snapshot == null)
    {
        Console.Error.WriteLine("Failed to read selector 3");
        return 1;
    }

    var raw = snapshot.RawPayload;
    Console.WriteLine($"Selector 3: {raw.Length} bytes");
    var hex = BitConverter.ToString(raw).Replace("-", "");
    File.WriteAllText(outPath, hex);
    Console.WriteLine($"Written to {outPath}");
    // Also print first 256 bytes for quick view
    for (int i = 0; i < Math.Min(raw.Length, 256); i += 16)
    {
        var line = string.Join(" ", Enumerable.Range(i, Math.Min(16, raw.Length - i)).Select(j => raw[j].ToString("X2")));
        Console.WriteLine($"  {i:X4}: {line}");
    }
    return 0;
}
if (args.Length > 0 && string.Equals(args[0], "i2c-probe", StringComparison.OrdinalIgnoreCase))
{
    // Probe I2C AT commands through XU selectors
    // Tests whether rtk_sendI2CATCommand uses the same XU path with different framing
    // Shim captured I2C AT format: [00 4A type(01=SET|02=GET) 00 opcode value_bytes...]
    var dev = NativeXuProbeDeviceLocator.Find("4K X");
    if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
    Console.WriteLine($"Device: {dev.Name} Id: {dev.Id}");

    // Parse vendor/product IDs from device ID string
    if (!NativeXuDeviceSupport.TryGetSupported4kXIds(dev, out var vid, out var pid))
    {
        Console.Error.WriteLine("Cannot parse vendor/product IDs");
        return 1;
    }

    var xuGuid = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
    var interfaces = GetSelectedKsInterfaces(dev);
    Console.WriteLine($"Found {interfaces.Count} KS interfaces");

    foreach (var ksIf in interfaces)
    {
        Console.WriteLine($"\n=== Interface: {ksIf.Path} ===");
        using var handle = KsExtensionUnitNative.TryOpen(ksIf.Path, out var openErr);
        if (handle == null)
        {
            Console.WriteLine($"  Open failed: win32={openErr}");
            continue;
        }

        if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out var topoErr))
        {
            Console.WriteLine($"  Topology read failed: {topoErr}");
            continue;
        }

        var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
        Console.WriteLine($"  Nodes: {string.Join(", ", nodeList.Select(n => $"{n.NodeId}(devSpec={n.IsDevSpecific})"))}");

        foreach (var node in nodeList.Where(n => n.IsDevSpecific))
        {
            Console.WriteLine($"\n  --- Node {node.NodeId} ---");

            // Phase 1: Scan all selectors 1-15 with GET to see what's available
            Console.WriteLine("  Scanning selectors 1-15:");
            for (int sel = 1; sel <= 15; sel++)
            {
                if (KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, sel, 64, out var getData, out var getBytes, out var getWin32))
                {
                    Console.WriteLine($"    Sel {sel,2}: GET OK ({getBytes} bytes) {BitConverter.ToString(getData, 0, Math.Min(getBytes, 32))}");
                }
                else
                {
                    Console.WriteLine($"    Sel {sel,2}: GET failed win32={getWin32}");
                }
            }

            // Phase 2: Send I2C AT GET frames through selector 1/2 (same path as regular AT)
            // The hypothesis: wrap I2C frame in the AT trigger/payload protocol
            var testFrames = new (string label, byte[] frame)[] {
                // Raw I2C AT GET frames from shim capture
                ("I2C GET 0x09 param=0x42", new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }),
                ("I2C GET 0x04 param=0x0E", new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }),
                ("I2C GET 0x03 param=0xA0", new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x03, 0xA0 }),
            };

            Console.WriteLine("\n  Sending raw I2C AT frames via S2(trigger)+S1(payload):");
            foreach (var (label, frame) in testFrames)
            {
                Console.WriteLine($"\n    {label}: {BitConverter.ToString(frame)}");

                // Trigger: send frame length to selector 2
                var trigger = new byte[] { (byte)(frame.Length & 0xFF), (byte)((frame.Length >> 8) & 0xFF) };
                if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, 2, trigger, out var trigWin32))
                {
                    Console.WriteLine($"      Trigger(S2) failed: win32={trigWin32}");
                    continue;
                }

                // Payload: send I2C frame to selector 1
                if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, 1, frame, out var sendWin32))
                {
                    Console.WriteLine($"      Send(S1) failed: win32={sendWin32}");
                    continue;
                }

                // Read response length from selector 2
                if (KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, 2, 2, out var lenData, out var lenBytes, out var lenWin32))
                {
                    int respLen = lenBytes >= 2 ? BitConverter.ToUInt16(lenData, 0) : 0;
                    Console.WriteLine($"      Response length(S2): {respLen}");

                    if (respLen > 0 && respLen < 1024)
                    {
                        if (KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, 1, respLen, out var respData, out var respBytes, out _))
                        {
                            Console.WriteLine($"      Response(S1) ({respBytes} bytes): {BitConverter.ToString(respData, 0, Math.Min(respBytes, 64))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"      No response or invalid length");
                    }
                }
                else
                {
                    Console.WriteLine($"      GetLength(S2) failed: win32={lenWin32}");
                }
            }

            // Phase 3: Try I2C AT frames on OTHER selectors (3-10)
            // rtk_sendI2CATCommand might use a completely different selector pair
            Console.WriteLine("\n  Testing I2C AT frame on alternate selectors:");
            var probeFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }; // I2C GET 0x09
            for (int sel = 3; sel <= 10; sel++)
            {
                // Try SET on this selector
                if (KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, sel, probeFrame, out var setWin32))
                {
                    Console.WriteLine($"    Sel {sel}: SET OK! Reading back...");
                    if (KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, sel, 64, out var resp, out var respB, out _))
                    {
                        Console.WriteLine($"    Sel {sel}: GET ({respB} bytes): {BitConverter.ToString(resp, 0, Math.Min(respB, 32))}");
                    }
                }
                else
                {
                    Console.WriteLine($"    Sel {sel}: SET failed win32={setWin32}");
                }
            }

            // Phase 4: Try wrapping I2C frame inside a standard AT frame envelope
            // Maybe rtk_sendI2CATCommand wraps [00 4A ...] inside [A1 len 00 00 cmd LRC]
            Console.WriteLine("\n  Testing I2C frame wrapped in AT envelope:");
            // Wrap as AT command with opcode 0x1B (I2C SET) and 0x1C (I2C GET)
            foreach (var (i2cLabel, i2cOpcode) in new[] { ("I2C_GET(0x1C)", 0x1C), ("I2C_SET(0x1B)", 0x1B) })
            {
                // Build AT frame: [A1, len, 00, 00, opcode(4B LE), i2c_payload..., LRC]
                var i2cPayload = new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }; // I2C GET opcode 0x09
                var atFrame = BuildAtFrameWithPayload(i2cOpcode, i2cPayload);
                Console.WriteLine($"    {i2cLabel}: AT frame = {BitConverter.ToString(atFrame)}");

                var trig = new byte[] { (byte)(atFrame.Length & 0xFF), (byte)((atFrame.Length >> 8) & 0xFF) };
                if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, 2, trig, out var tw))
                {
                    Console.WriteLine($"      Trigger failed: win32={tw}");
                    continue;
                }
                if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, 1, atFrame, out var sw))
                {
                    Console.WriteLine($"      Send failed: win32={sw}");
                    continue;
                }
                if (KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, 2, 2, out var ld, out var lb, out _))
                {
                    int rl = lb >= 2 ? BitConverter.ToUInt16(ld, 0) : 0;
                    Console.WriteLine($"      Response length: {rl}");
                    if (rl > 0 && rl < 1024)
                    {
                        if (KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, 1, rl, out var rd, out var rb, out _))
                        {
                            Console.WriteLine($"      Response ({rb} bytes): {BitConverter.ToString(rd, 0, Math.Min(rb, 64))}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"      GetLength failed");
                }
            }
        }
    }

    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "i2c-cmd", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeI2cCommands.RunAsync(args);
}
if (args.Length > 0 && string.Equals(args[0], "i2c-switch", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeI2cSwitch.RunAsync(args);
}
if (args.Length > 0 && string.Equals(args[0], "at-read", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeAtCommands.RunAtReadAsync(args);
}
if (args.Length > 0 && string.Equals(args[0], "at-write", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeAtCommands.RunAtWriteAsync(args);
}
if (args.Length > 0 && string.Equals(args[0], "at-set-input", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeAtCommands.RunAtSetInputAsync(args);
}

var device = NativeXuProbeDeviceLocator.Find(deviceNameFilter);

if (device == null)
{
    Console.Error.WriteLine("No capture device found.");
    return 1;
}

Console.WriteLine($"Device: {device.Name}");
Console.WriteLine($"Id: {device.Id}");

if (args.Any(arg => string.Equals(arg, "--service-smoke", StringComparison.OrdinalIgnoreCase)))
{
    return await NativeXuProbeServiceProbe.RunServiceSmokeAsync(device);
}

return await NativeXuProbeDefaultExperiment.RunAsync(device);
