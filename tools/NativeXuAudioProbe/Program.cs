using System.Globalization;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Devices;
using Sussudio.Services.Telemetry;

const int CmdAudioFormat = 0x04;
const int CmdAudioSamplingRate = 0x06;
const int CmdAudioSetAdcVolumeGain = 0x0A;
const int CmdAudioGetAdcVolumeGain = 0x0B;
const int CmdAudioSetHdmiDprxVolumeGain = 0x0C;
const int CmdAudioGetHdmiDprxVolumeGain = 0x0D;
const int CmdAudioSetUacVolumeGain = 0x10;
const int CmdAudioGetUacVolumeGain = 0x11;
const int CmdAudioSetUacOut2MixerSource = 0x26;
const int CmdAudioGetUacOut2MixerSource = 0x27;
const int CmdAudioSetDacHpMixerSource = 0x28;
const int CmdAudioGetDacHpMixerSource = 0x29;
const int CmdAudioSetI2sOutMixerSource = 0x2A;
const int CmdAudioGetI2sOutMixerSource = 0x2B;
const int CmdAudioSetUacOut1Mute = 0x2C;
const int CmdAudioGetUacOut1Mute = 0x2D;
const int CmdAudioSetUacOut2Mute = 0x2E;
const int CmdAudioGetUacOut2Mute = 0x2F;
const int CmdAudioSetDacHpMute = 0x30;
const int CmdAudioGetDacHpMute = 0x31;
const int CmdAudioSetI2sOutMute = 0x32;
const int CmdAudioGetI2sOutMute = 0x33;
const int CmdSetInputSource = 0x34;
const int CmdInputSource = 0x35;
const int CmdAudioSetAdcOnOff = 0x08;
const int CmdAudioSetDacHpOnOff = 0x09;
const int CmdAudioGetAdcOnOff = 0x74;
const int CmdAudioGetDacHpOnOff = 0x75;
const int CmdGetAuxInVolume = 0x7F;
const int CmdSetAuxInVolume = 0x80;
const int CmdGetAuxOutVolume = 0x81;
const int CmdSetAuxOutVolume = 0x82;

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
    return await RunServiceControlProbeAsync(args.Skip(1).ToArray());
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
    if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(dev, out var vid, out var pid))
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

static async Task<byte[]?> SendI2cAtGetAsync(CaptureDevice device, byte[] i2cFrame)
{
    // Wrap I2C frame inside AT command with opcode 0x1C (I2C GET)
    return await SendI2cViaAtAsync(device, 0x1C, i2cFrame);
}

static async Task<bool> SendI2cAtSetAsync(CaptureDevice device, byte[] i2cFrame)
{
    // Wrap I2C frame inside AT command with opcode 0x1B (I2C SET)
    var resp = await SendI2cViaAtAsync(device, 0x1B, i2cFrame);
    return resp != null; // any response = success
}

static async Task<byte[]?> SendI2cViaAtAsync(CaptureDevice device, int atOpcode, byte[] i2cPayload)
{
    // Send an AT command with the given opcode and I2C payload, then read the response
    // This uses the write frame format but expects a response (like the i2c-probe confirmed)
    if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out var vid, out var pid))
        return null;

    var interfaces = GetSelectedKsInterfaces(device);
    var xuGuid = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");

    foreach (var ksIf in interfaces)
    {
        using var handle = KsExtensionUnitNative.TryOpen(ksIf.Path, out _);
        if (handle == null) continue;

        if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
            continue;

        foreach (var node in (nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>()).Where(n => n.IsDevSpecific))
        {
            var atFrame = BuildAtFrameWithPayload(atOpcode, i2cPayload);

            // Trigger: send frame length to selector 2
            var trigger = new byte[] { (byte)(atFrame.Length & 0xFF), (byte)((atFrame.Length >> 8) & 0xFF) };
            if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, 2, trigger, out _))
                continue;

            // Payload: send AT frame to selector 1
            if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, 1, atFrame, out _))
                continue;

            // Read response length from selector 2
            if (!KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, 2, 2, out var lenData, out var lenBytes, out _))
                continue;

            int respLen = lenBytes >= 2 ? BitConverter.ToUInt16(lenData, 0) : 0;
            if (respLen <= 0 || respLen > 1024)
                return Array.Empty<byte>(); // Command accepted but no meaningful response

            // Read response from selector 1
            if (!KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, 1, respLen, out var respData, out var respBytes, out _))
                continue;

            // Strip AT envelope: [A1 len 00 00 cmd(4B) payload... LRC]
            // len = number of bytes from offset 2 to end of payload (before LRC)
            // So actual data = frame[4 .. 2+len-1], length = len - 2
            if (respBytes >= 5 && respData[0] == 0xA1)
            {
                int envLen = respData[1]; // bytes after offset 1, before LRC
                int payloadLen = envLen - 2; // subtract the 00-00 prefix
                if (payloadLen > 0 && 4 + payloadLen <= respBytes)
                {
                    var result = new byte[payloadLen];
                    Array.Copy(respData, 4, result, 0, payloadLen);
                    return result;
                }
            }

            // Return raw if not standard AT envelope
            var raw = new byte[respBytes];
            Array.Copy(respData, raw, respBytes);
            return raw;
        }
    }
    return null;
}

static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> GetSelectedKsInterfaces(CaptureDevice device)
{
    if (string.IsNullOrWhiteSpace(device.NativeXuInterfacePath))
    {
        Console.Error.WriteLine("Selected device has no native XU interface path.");
        return Array.Empty<KsExtensionUnitNative.KsInterfacePath>();
    }

    return new[] { new KsExtensionUnitNative.KsInterfacePath(device.NativeXuInterfacePath, Guid.Empty) };
}

static byte[] BuildAtFrameWithPayload(int cmdCode, byte[] payload)
{
    // AT frame format: [0xA1, totalLen, 0x00, 0x00, cmd(4B LE), payload..., LRC]
    // totalLen = 4 (cmd bytes) + payload.Length
    var dataLen = 4 + payload.Length;
    var frame = new byte[5 + dataLen]; // header(A1, len, 00, 00) + cmd(4B) + payload + LRC
    frame[0] = 0xA1;
    frame[1] = (byte)dataLen;
    frame[2] = 0x00;
    frame[3] = 0x00;
    frame[4] = (byte)(cmdCode & 0xFF);
    frame[5] = (byte)((cmdCode >> 8) & 0xFF);
    frame[6] = (byte)((cmdCode >> 16) & 0xFF);
    frame[7] = (byte)((cmdCode >> 24) & 0xFF);
    Array.Copy(payload, 0, frame, 8, payload.Length);
    // LRC: two's complement of sum of all preceding bytes
    byte sum = 0;
    for (int i = 0; i < frame.Length - 1; i++) sum += frame[i];
    frame[^1] = unchecked((byte)(~sum + 1));
    return frame;
}
if (args.Length > 0 && string.Equals(args[0], "i2c-cmd", StringComparison.OrdinalIgnoreCase))
{
    // Send I2C AT commands via the AT envelope (opcode 0x1C=GET, 0x1B=SET)
    // Usage: i2c-cmd get <i2c_opcode_hex> <param_hex>
    //        i2c-cmd set <i2c_opcode_hex> <value_byte_hex>
    //        i2c-cmd scan  (scan I2C opcodes 0x00-0x20)
    if (args.Length < 2) { Console.Error.WriteLine("Usage: i2c-cmd get|set|scan ..."); return 1; }
    var dev = NativeXuProbeDeviceLocator.Find("4K X");
    if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
    Console.WriteLine($"Device: {dev.Name}");

    var subCmd = args[1].ToLowerInvariant();

    if (subCmd == "scan")
    {
        // Scan I2C opcodes to find which ones return data
        int start = args.Length > 2 ? int.Parse(args[2].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber) : 0x00;
        int end = args.Length > 3 ? int.Parse(args[3].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber) : 0x20;
        Console.WriteLine($"Scanning I2C AT opcodes 0x{start:X2} - 0x{end:X2}...");
        for (int op = start; op <= end; op++)
        {
            // I2C GET: [00 4A 02 00 opcode 00]
            var i2cFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)op, 0x00 };
            var resp = await SendI2cAtGetAsync(dev, i2cFrame);
            if (resp != null)
            {
                Console.WriteLine($"  I2C 0x{op:X2}: {BitConverter.ToString(resp)} ({resp.Length} bytes) int32={BitConverter.ToInt32(resp.Length >= 4 ? resp[..4] : resp.Concat(new byte[4 - resp.Length]).ToArray(), 0)}");
            }
            else
            {
                Console.WriteLine($"  I2C 0x{op:X2}: (no response)");
            }
        }
        return 0;
    }

    if (subCmd == "get" && args.Length >= 4)
    {
        int i2cOp = int.Parse(args[2].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber);
        byte param = byte.Parse(args[3].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber);
        var i2cFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)i2cOp, param };
        Console.WriteLine($"I2C GET opcode=0x{i2cOp:X2} param=0x{param:X2}");
        var resp = await SendI2cAtGetAsync(dev, i2cFrame);
        Console.WriteLine(resp != null
            ? $"  Response: {BitConverter.ToString(resp)} ({resp.Length} bytes)"
            : "  No response");
        return 0;
    }

    if (subCmd == "set" && args.Length >= 4)
    {
        int i2cOp = int.Parse(args[2].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber);
        byte value = byte.Parse(args[3].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber);
        var i2cFrame = new byte[] { 0x00, 0x4A, 0x01, 0x00, (byte)i2cOp, value };
        Console.WriteLine($"I2C SET opcode=0x{i2cOp:X2} value=0x{value:X2}");
        var ok = await SendI2cAtSetAsync(dev, i2cFrame);
        Console.WriteLine($"  Result: {(ok ? "OK" : "failed")}");
        return 0;
    }

    if (subCmd == "sel-probe")
    {
        // Probe XU selectors with various buffer sizes to find I2C AT transport
        if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(dev, out var vid2, out var pid2))
        {
            Console.Error.WriteLine("Cannot parse device IDs");
            return 1;
        }
        var xuGuid2 = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
        var ifaces = GetSelectedKsInterfaces(dev);
        var ksIf2 = ifaces.FirstOrDefault();
        if (ksIf2.Path == null) { Console.Error.WriteLine("No KS interface"); return 1; }
        using var h = KsExtensionUnitNative.TryOpen(ksIf2.Path, out _);
        if (h == null) { Console.Error.WriteLine("Cannot open"); return 1; }
        if (!KsExtensionUnitNative.TryReadTopologyNodes(h, out var ns, out _)) { Console.Error.WriteLine("No topology"); return 1; }
        var devNode = (ns ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>()).FirstOrDefault(n => n.IsDevSpecific);
        int nid = devNode.NodeId;
        Console.WriteLine($"Node: {nid}");

        // Probe selectors 1-30 with multiple buffer sizes
        foreach (int bufSize in new[] { 2, 8, 16, 32, 64, 128, 256, 512 })
        {
            Console.WriteLine($"\n--- Buffer size: {bufSize} ---");
            for (int sel = 1; sel <= 30; sel++)
            {
                if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, sel, bufSize, out var data, out var bytes, out var w32))
                {
                    var preview = BitConverter.ToString(data, 0, Math.Min(bytes, 16));
                    if (data.Take(bytes).Any(b => b != 0))
                        Console.WriteLine($"  Sel {sel,2}: GET OK ({bytes}B) {preview} ***HAS DATA***");
                    else
                        Console.WriteLine($"  Sel {sel,2}: GET OK ({bytes}B) all-zero");
                }
                else if (w32 != 1168) // skip NOT_FOUND
                {
                    Console.WriteLine($"  Sel {sel,2}: GET failed win32={w32}");
                }
            }
        }

        // Dump full selector 3 content
        Console.WriteLine("\n--- Full Selector 3 dump ---");
        if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var s3Data, out var s3Bytes, out _))
        {
            Console.WriteLine($"  {s3Bytes} bytes:");
            for (int i = 0; i < s3Bytes; i += 16)
            {
                var line = string.Join(" ", Enumerable.Range(i, Math.Min(16, s3Bytes - i)).Select(j => s3Data[j].ToString("X2")));
                var ascii = new string(Enumerable.Range(i, Math.Min(16, s3Bytes - i)).Select(j => s3Data[j] >= 0x20 && s3Data[j] < 0x7F ? (char)s3Data[j] : '.').ToArray());
                Console.WriteLine($"  {i:X4}: {line,-48} {ascii}");
            }
        }

        // Try selector 4 with large buffers
        Console.WriteLine("\n--- Selector 4 with large buffers ---");
        foreach (int bs in new[] { 512, 1024, 2048, 4096 })
        {
            if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 4, bs, out var s4dd, out var s4bb, out var s4w))
            {
                Console.WriteLine($"  GET({bs}B): {s4bb} bytes: {BitConverter.ToString(s4dd, 0, Math.Min(s4bb, 32))}");
                break;
            }
            else
            {
                Console.WriteLine($"  GET({bs}B): failed win32={s4w}");
            }
        }

        // Test I2C command/response flow via selector 3
        Console.WriteLine("\n--- I2C command/response test ---");
        var testI2c = new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }; // I2C GET opcode 0x09, param 0x42
        var padded = new byte[150];
        Array.Copy(testI2c, padded, testI2c.Length);

        // Read selector 3 and 4 BEFORE
        Console.WriteLine("  BEFORE:");
        if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var s3b, out var s3bLen, out _))
            Console.WriteLine($"    S3 ({s3bLen}B): {BitConverter.ToString(s3b, 0, Math.Min(s3bLen, 32))}");
        if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 4, 1024, out var s4b, out var s4bLen, out _))
            Console.WriteLine($"    S4 ({s4bLen}B): {BitConverter.ToString(s4b, 0, Math.Min(s4bLen, 32))}");

        // SET I2C GET command on selector 3
        Console.WriteLine($"  SET S3: I2C GET 0x09/42 (150B padded)");
        if (!KsExtensionUnitNative.TryXuSetViaOutput(h, nid, xuGuid2, 3, padded, out var setW32))
        {
            Console.WriteLine($"    SET failed: win32={setW32}");
        }
        else
        {
            Console.WriteLine("    SET OK");

            // Immediate readback
            Console.WriteLine("  AFTER (immediate):");
            if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var s3a, out var s3aLen, out _))
                Console.WriteLine($"    S3 ({s3aLen}B): {BitConverter.ToString(s3a, 0, Math.Min(s3aLen, 32))}");
            if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 4, 1024, out var s4a, out var s4aLen, out _))
                Console.WriteLine($"    S4 ({s4aLen}B): {BitConverter.ToString(s4a, 0, Math.Min(s4aLen, 32))}");

            // Wait and read again
            await Task.Delay(100);
            Console.WriteLine("  AFTER (100ms delay):");
            if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var s3d, out var s3dLen, out _))
                Console.WriteLine($"    S3 ({s3dLen}B): {BitConverter.ToString(s3d, 0, Math.Min(s3dLen, 32))}");
            if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 4, 1024, out var s4d2, out var s4dLen, out _))
                Console.WriteLine($"    S4 ({s4dLen}B): {BitConverter.ToString(s4d2, 0, Math.Min(s4dLen, 32))}");

            // Try a second I2C GET with different opcode
            Console.WriteLine("\n  --- Second test: I2C GET 0x04/0E ---");
            var testI2c2 = new byte[150];
            new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }.CopyTo(testI2c2, 0);
            if (KsExtensionUnitNative.TryXuSetViaOutput(h, nid, xuGuid2, 3, testI2c2, out _))
            {
                Console.WriteLine("    SET OK");
                await Task.Delay(50);
                if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var r3, out var r3l, out _))
                    Console.WriteLine($"    S3 ({r3l}B): {BitConverter.ToString(r3, 0, Math.Min(r3l, 32))}");
                if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 4, 1024, out var r4, out var r4l, out _))
                    Console.WriteLine($"    S4 ({r4l}B): {BitConverter.ToString(r4, 0, Math.Min(r4l, 32))}");
            }

            // Try I2C SET: opcode 0x04, value 0x01 (audio source = analog)
            Console.WriteLine("\n  --- I2C SET test: opcode 0x04 = 0x01 ---");
            var setI2c = new byte[150];
            new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01 }.CopyTo(setI2c, 0);
            if (KsExtensionUnitNative.TryXuSetViaOutput(h, nid, xuGuid2, 3, setI2c, out _))
            {
                Console.WriteLine("    SET OK");
                await Task.Delay(50);
                if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var rs3, out var rs3l, out _))
                    Console.WriteLine($"    S3 ({rs3l}B): {BitConverter.ToString(rs3, 0, Math.Min(rs3l, 32))}");

                // Readback via I2C GET to verify
                var verifyI2c = new byte[150];
                new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }.CopyTo(verifyI2c, 0);
                if (KsExtensionUnitNative.TryXuSetViaOutput(h, nid, xuGuid2, 3, verifyI2c, out _))
                {
                    await Task.Delay(50);
                    if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var vr3, out var vr3l, out _))
                        Console.WriteLine($"    Verify S3 ({vr3l}B): {BitConverter.ToString(vr3, 0, Math.Min(vr3l, 32))}");
                }

                // Restore: I2C SET opcode 0x04 = 0x00 (HDMI)
                var restoreI2c = new byte[150];
                new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x00 }.CopyTo(restoreI2c, 0);
                KsExtensionUnitNative.TryXuSetViaOutput(h, nid, xuGuid2, 3, restoreI2c, out _);
                Console.WriteLine("    Restored to HDMI (0x04=0x00)");
            }
            else
            {
                Console.WriteLine("    SET failed");
            }

            // Phase 5: Try TryXuSetViaInput (data in input buffer) on selector 3
            Console.WriteLine("\n  --- TryXuSetViaInput on selector 3 ---");
            // First, read S3 to see baseline
            if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var baseS3, out var baseS3l, out _))
                Console.WriteLine($"    Baseline S3: {BitConverter.ToString(baseS3, 0, Math.Min(baseS3l, 32))}");

            var i2cGetFrame = new byte[150];
            new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }.CopyTo(i2cGetFrame, 0);
            if (KsExtensionUnitNative.TryXuSetViaInput(h, nid, xuGuid2, 3, i2cGetFrame, out var inputSetW32))
            {
                Console.WriteLine("    SetViaInput OK!");
                await Task.Delay(100);
                if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var afterS3, out var afterS3l, out _))
                    Console.WriteLine($"    After S3: {BitConverter.ToString(afterS3, 0, Math.Min(afterS3l, 32))}");
            }
            else
            {
                Console.WriteLine($"    SetViaInput failed: win32={inputSetW32}");
            }

            // Phase 6: Try trigger on selectors 10-17 after writing I2C frame to S3
            Console.WriteLine("\n  --- Trigger test: write S3 then trigger on S10-17 ---");
            var trigFrame = new byte[150];
            new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }.CopyTo(trigFrame, 0);
            KsExtensionUnitNative.TryXuSetViaOutput(h, nid, xuGuid2, 3, trigFrame, out _);
            for (int tSel = 10; tSel <= 17; tSel++)
            {
                var trigData = new byte[] { 0x01, 0x00 }; // trigger = 1
                if (KsExtensionUnitNative.TryXuSetViaOutput(h, nid, xuGuid2, tSel, trigData, out var tW32))
                {
                    Console.WriteLine($"    Sel {tSel}: trigger SET OK!");
                    await Task.Delay(50);
                    if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var tR3, out var tR3l, out _))
                    {
                        var changed = !baseS3.Take(Math.Min(baseS3l, 32)).SequenceEqual(tR3.Take(Math.Min(tR3l, 32)));
                        Console.WriteLine($"    S3 after trigger: {BitConverter.ToString(tR3, 0, Math.Min(tR3l, 32))} {(changed ? "***CHANGED***" : "")}");
                    }
                }
                else
                {
                    Console.WriteLine($"    Sel {tSel}: trigger failed win32={tW32}");
                }
            }

            // Phase 7: Try SET on selector 4 with 525-byte buffer
            Console.WriteLine("\n  --- SET selector 4 (525B) ---");
            var s4frame = new byte[525];
            new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }.CopyTo(s4frame, 0);
            if (KsExtensionUnitNative.TryXuSetViaOutput(h, nid, xuGuid2, 4, s4frame, out var s4setW32))
            {
                Console.WriteLine($"    SET OK!");
                await Task.Delay(100);
                if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 4, 1024, out var s4resp, out var s4respL, out _))
                    Console.WriteLine($"    S4 after: {BitConverter.ToString(s4resp, 0, Math.Min(s4respL, 32))}");
                if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var s3after4, out var s3after4l, out _))
                    Console.WriteLine($"    S3 after S4 SET: {BitConverter.ToString(s3after4, 0, Math.Min(s3after4l, 32))}");
            }
            else
            {
                Console.WriteLine($"    SET via Output failed: win32={s4setW32}");
                // Try via input
                if (KsExtensionUnitNative.TryXuSetViaInput(h, nid, xuGuid2, 4, s4frame, out var s4inputW32))
                {
                    Console.WriteLine($"    SET via Input OK!");
                    await Task.Delay(100);
                    if (KsExtensionUnitNative.TryXuGetDirect(h, nid, xuGuid2, 3, 256, out var s3a4i, out var s3a4il, out _))
                        Console.WriteLine($"    S3 after: {BitConverter.ToString(s3a4i, 0, Math.Min(s3a4il, 32))}");
                }
                else
                {
                    Console.WriteLine($"    SET via Input failed: win32={s4inputW32}");
                }
            }
        }

        return 0;
    }

    if (subCmd == "verify")
    {
        // Verify the AT envelope I2C path actually works by doing SET+readback
        // Tests: read I2C 0x04 → SET I2C 0x04=1 → read I2C 0x04 → restore
        if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(dev, out var vid3, out var pid3))
        {
            Console.Error.WriteLine("Cannot parse device IDs");
            return 1;
        }
        var xuGuid3 = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
        var ifaces3 = GetSelectedKsInterfaces(dev);
        var ksIf3 = ifaces3.FirstOrDefault();
        using var h3 = KsExtensionUnitNative.TryOpen(ksIf3.Path, out _);
        if (h3 == null) { Console.Error.WriteLine("Cannot open"); return 1; }
        if (!KsExtensionUnitNative.TryReadTopologyNodes(h3, out var ns3, out _)) { Console.Error.WriteLine("No topology"); return 1; }
        var devNode3 = (ns3 ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>()).FirstOrDefault(n => n.IsDevSpecific);
        int nid3 = devNode3.NodeId;

        // Helper: send AT frame and get raw response
        byte[]? SendAtAndGetResponse(byte[] atFrame)
        {
            var trig = new byte[] { (byte)(atFrame.Length & 0xFF), (byte)((atFrame.Length >> 8) & 0xFF) };
            if (!KsExtensionUnitNative.TryXuSetViaOutput(h3, nid3, xuGuid3, 2, trig, out _)) return null;
            if (!KsExtensionUnitNative.TryXuSetViaOutput(h3, nid3, xuGuid3, 1, atFrame, out _)) return null;
            if (!KsExtensionUnitNative.TryXuGetDirect(h3, nid3, xuGuid3, 2, 2, out var ld, out var lb, out _)) return null;
            int rl = lb >= 2 ? BitConverter.ToUInt16(ld, 0) : 0;
            if (rl <= 0 || rl > 1024) return Array.Empty<byte>();
            if (!KsExtensionUnitNative.TryXuGetDirect(h3, nid3, xuGuid3, 1, rl, out var rd, out var rb, out _)) return null;
            return rd[..rb];
        }

        // Test 1: Read I2C opcodes 0x03, 0x04, 0x07, 0x09, 0x0E, 0x10 via AT 0x1C
        Console.WriteLine("--- I2C GET via AT envelope (opcode 0x1C) ---");
        foreach (int i2cOp in new[] { 0x03, 0x04, 0x07, 0x09, 0x0E, 0x0F, 0x10, 0x11 })
        {
            var i2cFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)i2cOp, 0x00 };
            var atFrame = BuildAtFrameWithPayload(0x1C, i2cFrame);
            var resp = SendAtAndGetResponse(atFrame);
            Console.WriteLine($"  I2C GET 0x{i2cOp:X2}: AT={BitConverter.ToString(atFrame)} → {(resp != null ? BitConverter.ToString(resp) : "(null)")}");
        }

        // Test 2: I2C SET via AT 0x1B: set opcode 0x04 to 0x01, read back, restore to 0x00
        Console.WriteLine("\n--- I2C SET/verify via AT envelope ---");
        // Read before
        var readBefore = SendAtAndGetResponse(BuildAtFrameWithPayload(0x1C, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x00 }));
        Console.WriteLine($"  BEFORE I2C GET 0x04: {(readBefore != null ? BitConverter.ToString(readBefore) : "(null)")}");

        // SET I2C 0x04 = 0x01 via AT 0x1B
        var setFrame = BuildAtFrameWithPayload(0x1B, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01 });
        Console.WriteLine($"  SET frame: {BitConverter.ToString(setFrame)}");
        var setResp = SendAtAndGetResponse(setFrame);
        Console.WriteLine($"  SET response: {(setResp != null ? BitConverter.ToString(setResp) : "(null)")}");

        await Task.Delay(200);

        // Read after
        var readAfter = SendAtAndGetResponse(BuildAtFrameWithPayload(0x1C, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x00 }));
        Console.WriteLine($"  AFTER I2C GET 0x04: {(readAfter != null ? BitConverter.ToString(readAfter) : "(null)")}");

        // Check if value changed
        var beforeHex = readBefore != null ? BitConverter.ToString(readBefore) : "";
        var afterHex = readAfter != null ? BitConverter.ToString(readAfter) : "";
        Console.WriteLine(beforeHex != afterHex ? "  *** VALUE CHANGED! I2C SET via AT envelope WORKS! ***" : "  Value unchanged - SET may not have been dispatched");

        // Restore: SET I2C 0x04 = 0x00
        var restoreFrame = BuildAtFrameWithPayload(0x1B, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x00 });
        var restoreResp = SendAtAndGetResponse(restoreFrame);
        Console.WriteLine($"  RESTORE response: {(restoreResp != null ? BitConverter.ToString(restoreResp) : "(null)")}");
        await Task.Delay(200);
        var readRestored = SendAtAndGetResponse(BuildAtFrameWithPayload(0x1C, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x00 }));
        Console.WriteLine($"  RESTORED I2C GET 0x04: {(readRestored != null ? BitConverter.ToString(readRestored) : "(null)")}");

        // Test 3: Also check if regular AT 0x35 (InputSource) changed
        var atInputBefore = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, 0x35, "InputSource");
        Console.WriteLine($"\n  UVC AT 0x35 InputSource: {(atInputBefore != null ? BitConverter.ToString(atInputBefore) : "(null)")}");

        return 0;
    }

    if (subCmd == "high-sel")
    {
        // Probe XU selectors 18-35, focusing on 0x1B(27) and 0x1C(28)
        // which are the a1 values in rtk_sendI2CATCommand
        if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(dev, out var vidH, out var pidH))
        {
            Console.Error.WriteLine("Cannot parse device IDs");
            return 1;
        }
        var xuGuidH = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
        var ifacesH = GetSelectedKsInterfaces(dev);
        var ksIfH = ifacesH.FirstOrDefault();
        using var hH = KsExtensionUnitNative.TryOpen(ksIfH.Path, out _);
        if (hH == null) { Console.Error.WriteLine("Cannot open"); return 1; }
        if (!KsExtensionUnitNative.TryReadTopologyNodes(hH, out var nsH, out _)) { Console.Error.WriteLine("No topology"); return 1; }
        var devNodeH = (nsH ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>()).FirstOrDefault(n => n.IsDevSpecific);
        int nidH = devNodeH.NodeId;

        // Probe selectors 18-40 with multiple buffer sizes
        Console.WriteLine("--- Probing selectors 18-40 ---");
        foreach (int bufSize in new[] { 2, 8, 64, 256, 1024 })
        {
            Console.WriteLine($"\n  Buffer: {bufSize}");
            for (int sel = 18; sel <= 40; sel++)
            {
                if (KsExtensionUnitNative.TryXuGetDirect(hH, nidH, xuGuidH, sel, bufSize, out var gd, out var gb, out var gw))
                {
                    var hasData = gd.Take(gb).Any(b => b != 0);
                    Console.WriteLine($"    Sel {sel,2} (0x{sel:X2}): GET OK ({gb}B) {(hasData ? BitConverter.ToString(gd, 0, Math.Min(gb, 16)) + " ***DATA***" : "all-zero")}");
                }
                else if (gw == 122) // INSUFFICIENT_BUFFER — selector exists but needs bigger buffer
                {
                    Console.WriteLine($"    Sel {sel,2} (0x{sel:X2}): BUFFER TOO SMALL (needs >{bufSize}B)");
                }
                else if (gw != 1168) // skip NOT_FOUND
                {
                    Console.WriteLine($"    Sel {sel,2} (0x{sel:X2}): GET failed win32={gw}");
                }
            }
        }

        // Focused test on selectors 0x1B(27) and 0x1C(28)
        Console.WriteLine("\n--- Focused test: selectors 0x1B(27) and 0x1C(28) ---");
        foreach (int sel in new[] { 0x1B, 0x1C })
        {
            Console.WriteLine($"\n  Selector 0x{sel:X2} ({sel}):");
            // Try SET with I2C frame
            var i2cFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 };
            foreach (int padSize in new[] { 6, 150, 525 })
            {
                var padded = new byte[padSize];
                Array.Copy(i2cFrame, padded, Math.Min(i2cFrame.Length, padSize));
                if (KsExtensionUnitNative.TryXuSetViaOutput(hH, nidH, xuGuidH, sel, padded, out var sw))
                {
                    Console.WriteLine($"    SET({padSize}B) OK!");
                    // Read back from multiple selectors
                    foreach (int rSel in new[] { sel, 3, 4 })
                    {
                        foreach (int rBuf in new[] { 256, 1024 })
                        {
                            if (KsExtensionUnitNative.TryXuGetDirect(hH, nidH, xuGuidH, rSel, rBuf, out var rd, out var rb, out _))
                            {
                                Console.WriteLine($"      GET sel={rSel} ({rb}B): {BitConverter.ToString(rd, 0, Math.Min(rb, 32))}");
                                break;
                            }
                        }
                    }
                    break;
                }
                else
                {
                    Console.WriteLine($"    SET({padSize}B) failed win32={sw}");
                }
            }
        }

        // Try ALL topology nodes (not just devSpec) for I2C AT
        Console.WriteLine("\n--- All topology nodes: I2C AT on non-devSpec nodes ---");
        foreach (var node in nsH ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>())
        {
            Console.WriteLine($"\n  Node {node.NodeId} (devSpec={node.IsDevSpecific}):");
            // Try AT S1/S2 protocol with I2C frame
            var atFrame = BuildAtFrameWithPayload(0x1C, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 });
            var trig = new byte[] { (byte)(atFrame.Length & 0xFF), (byte)((atFrame.Length >> 8) & 0xFF) };
            if (KsExtensionUnitNative.TryXuSetViaOutput(hH, node.NodeId, xuGuidH, 2, trig, out _) &&
                KsExtensionUnitNative.TryXuSetViaOutput(hH, node.NodeId, xuGuidH, 1, atFrame, out _))
            {
                if (KsExtensionUnitNative.TryXuGetDirect(hH, node.NodeId, xuGuidH, 2, 2, out var ld, out var lb, out _))
                {
                    int rl = lb >= 2 ? BitConverter.ToUInt16(ld, 0) : 0;
                    Console.WriteLine($"    Response length: {rl}");
                    if (rl > 0 && rl < 1024)
                    {
                        if (KsExtensionUnitNative.TryXuGetDirect(hH, node.NodeId, xuGuidH, 1, rl, out var rd, out var rb, out _))
                            Console.WriteLine($"    Response: {BitConverter.ToString(rd, 0, Math.Min(rb, 32))}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"    AT S1/S2 not available on this node");
            }
        }

        return 0;
    }

    if (subCmd == "topology")
    {
        // Dump full topology with node type GUIDs and test each as a property set
        if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(dev, out var vidT, out var pidT))
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

    Console.Error.WriteLine("Usage: i2c-cmd get|set|scan|sel-probe|verify|high-sel|topology");
    return 1;
}
if (args.Length > 0 && string.Equals(args[0], "i2c-switch", StringComparison.OrdinalIgnoreCase))
{
    // Replay the complete audio switching sequence captured from the shim
    // Usage: i2c-switch <hdmi|analog>
    var target = args.Length > 1 ? args[1].ToLowerInvariant() : "analog";
    var dev = NativeXuProbeDeviceLocator.Find("4K X");
    if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
    Console.WriteLine($"Device: {dev.Name}");
    Console.WriteLine($"Target: {target}");

    // Read current state via I2C GET opcodes from the shim capture
    Console.WriteLine("\n--- Current I2C AT state ---");
    foreach (var (label, op, param) in new[] {
        ("DacHpOnOff(0x09)", 0x09, 0x42),
        ("Opcode(0x03)", 0x03, 0xA0),
        ("Opcode(0x04)", 0x04, 0x0E),
        ("Opcode(0x07)", 0x07, 0x00),
        ("Opcode(0x0E)", 0x0E, 0x00),
        ("Opcode(0x0F)", 0x0F, 0x00),
        ("Opcode(0x10)", 0x10, 0x00),
        ("Opcode(0x11)", 0x11, 0x00),
    })
    {
        var frame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)op, (byte)param };
        var resp = await SendI2cAtGetAsync(dev, frame);
        Console.WriteLine($"  {label}: {(resp != null ? BitConverter.ToString(resp) : "(null)")}");
    }

    // Also read relevant UVC AT opcodes
    Console.WriteLine("\n--- Current UVC AT state ---");
    foreach (var (label, op) in new[] {
        ("InputSource(0x35)", 0x35),
        ("AudioFormat(0x04)", 0x04),
        ("AdcOnOff(0x74)", 0x74),
        ("DacHpOnOff(0x75)", 0x75),
        ("AT_0x5B", 0x5B),
        ("AT_0x52", 0x52),
        ("AT_0x51", 0x51),
    })
    {
        var raw = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, op, label);
        Console.WriteLine($"  {label}: {(raw != null ? BitConverter.ToString(raw) : "(null)")}");
    }

    // Now replay the SET sequence from the shim capture
    // The shim captured this exact sequence for Analog mode:
    // 1. I2C GET 0x09/42 → 01
    // 2. I2C SET 0x04 = 01
    // 3. I2C GET 0x03/A0 → 01
    // 4. I2C SET 0x0E = 01
    // 5. I2C SET 0x10 = 01
    // 6. UVC AT SET 0x5B = [00 05 00 00]
    // 7. More I2C SET/GET...
    Console.WriteLine($"\n--- Sending audio switch sequence ({target}) ---");

    // Step 1: Read DacHpOnOff
    var dacState = await SendI2cAtGetAsync(dev, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 });
    Console.WriteLine($"  1. I2C GET 0x09/42 = {(dacState != null ? BitConverter.ToString(dacState) : "(null)")}");

    // Step 2: I2C SET 0x04 = value (from shim: 01 for analog)
    byte audioSourceValue = (byte)(target == "analog" ? 0x01 : 0x00);
    var set04 = await SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, audioSourceValue });
    Console.WriteLine($"  2. I2C SET 0x04 = 0x{audioSourceValue:X2}: {(set04 ? "OK" : "failed")}");

    // Step 3: Read 0x03
    var state03 = await SendI2cAtGetAsync(dev, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x03, 0xA0 });
    Console.WriteLine($"  3. I2C GET 0x03/A0 = {(state03 != null ? BitConverter.ToString(state03) : "(null)")}");

    // Step 4: I2C SET 0x0E = 01
    var set0E = await SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x0E, 0x01 });
    Console.WriteLine($"  4. I2C SET 0x0E = 01: {(set0E ? "OK" : "failed")}");

    // Step 5: I2C SET 0x10 = 01
    var set10 = await SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x10, 0x01 });
    Console.WriteLine($"  5. I2C SET 0x10 = 01: {(set10 ? "OK" : "failed")}");

    // Step 6: UVC AT SET 0x5B = [00 05 00 00] (commit/trigger)
    var set5B = await NativeXuAtCommandProvider.SendNamedSetCommandPublicAsync(
        dev, 0x5B, new byte[] { 0x00, 0x05, 0x00, 0x00 }, "AT_0x5B_commit");
    Console.WriteLine($"  6. UVC AT SET 0x5B = 00-05-00-00: {(set5B ? "OK" : "failed")}");

    await Task.Delay(500);

    // Read final state
    Console.WriteLine("\n--- Final I2C AT state ---");
    foreach (var (label, op, param) in new[] {
        ("DacHpOnOff(0x09)", 0x09, 0x42),
        ("Opcode(0x04)", 0x04, 0x0E),
        ("Opcode(0x0E)", 0x0E, 0x00),
        ("Opcode(0x10)", 0x10, 0x00),
    })
    {
        var frame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)op, (byte)param };
        var resp = await SendI2cAtGetAsync(dev, frame);
        Console.WriteLine($"  {label}: {(resp != null ? BitConverter.ToString(resp) : "(null)")}");
    }

    Console.WriteLine("\n--- Final UVC AT state ---");
    foreach (var (label, op) in new[] {
        ("InputSource(0x35)", 0x35),
        ("AudioFormat(0x04)", 0x04),
    })
    {
        var raw = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, op, label);
        Console.WriteLine($"  {label}: {(raw != null ? BitConverter.ToString(raw) : "(null)")}");
    }

    return 0;
}
if (args.Length > 0 && string.Equals(args[0], "at-read", StringComparison.OrdinalIgnoreCase))
{
    // Read a single AT opcode and print raw response bytes
    // Usage: at-read <opcode_hex> [opcode2_hex ...]
    var dev = NativeXuProbeDeviceLocator.Find("4K X");
    if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
    Console.WriteLine($"Device: {dev.Name}");

    for (int ai = 1; ai < args.Length; ai++)
    {
        var opcodeStr = args[ai].TrimStart('0').TrimStart('x', 'X');
        if (opcodeStr.StartsWith("x", StringComparison.OrdinalIgnoreCase)) opcodeStr = opcodeStr[1..];
        if (!int.TryParse(args[ai].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out int opcode))
        {
            Console.Error.WriteLine($"Invalid opcode: {args[ai]}");
            continue;
        }
        var raw = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, opcode, $"0x{opcode:X2}");
        if (raw != null)
            Console.WriteLine($"  AT 0x{opcode:X2}: {BitConverter.ToString(raw)} ({raw.Length} bytes) int32={BitConverter.ToInt32(raw.Length >= 4 ? raw[..4] : raw.Concat(new byte[4 - raw.Length]).ToArray(), 0)}");
        else
            Console.WriteLine($"  AT 0x{opcode:X2}: (null/failed)");
    }
    return 0;
}
if (args.Length > 0 && string.Equals(args[0], "at-write", StringComparison.OrdinalIgnoreCase))
{
    // Write a value to an AT opcode: at-write <opcode_hex> <value_int>
    // Then read back using the next opcode (opcode+1) as the GET pair
    var dev = NativeXuProbeDeviceLocator.Find("4K X");
    if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
    Console.WriteLine($"Device: {dev.Name}");

    if (args.Length < 3) { Console.Error.WriteLine("Usage: at-write <opcode_hex> <value_int> [--read-back <get_opcode_hex>]"); return 1; }
    int.TryParse(args[1].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out int setOpcode);
    int.TryParse(args[2], out int value);
    int getOpcode = setOpcode + 1; // default: GET = SET + 1
    for (int ai = 3; ai < args.Length - 1; ai++)
    {
        if (args[ai] == "--read-back")
            int.TryParse(args[ai + 1].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out getOpcode);
    }

    // Read before
    var before = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, getOpcode, $"GET 0x{getOpcode:X2}");
    Console.WriteLine($"BEFORE: AT 0x{getOpcode:X2} = {(before != null ? BitConverter.ToString(before) : "(null)")}");

    // Write
    Console.WriteLine($"WRITING: AT 0x{setOpcode:X2} value={value} (bytes: {BitConverter.ToString(BitConverter.GetBytes(value))})");
    var ok = await NativeXuAtCommandProvider.SendNamedSetCommandPublicAsync(dev, setOpcode, BitConverter.GetBytes(value), $"SET 0x{setOpcode:X2}={value}");
    Console.WriteLine($"Result: {ok}");

    await Task.Delay(500);

    // Read after
    var after = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, getOpcode, $"GET 0x{getOpcode:X2}");
    Console.WriteLine($"AFTER: AT 0x{getOpcode:X2} = {(after != null ? BitConverter.ToString(after) : "(null)")}");

    return ok ? 0 : 1;
}
if (args.Length > 0 && string.Equals(args[0], "at-set-input", StringComparison.OrdinalIgnoreCase))
{
    // Pure AT-only SetInputSource: at-set-input <0=HDMI|1=Analog> [--no-restore]
    var atVal = args.Length > 1 ? int.Parse(args[1]) : 0;
    var noRestore = args.Any(a => a == "--no-restore");
    var dev = NativeXuProbeDeviceLocator.Find("4K X");
    if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
    Console.WriteLine($"Device: {dev.Name}");
    var beforeInput = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, CmdInputSource, "InputSource");
    Console.WriteLine($"Before: InputSource={FormatRaw(beforeInput)}");
    Console.WriteLine($"Sending AT SetInputSource(0x34) = {atVal} (1 byte)...");
    var ok = await NativeXuAtCommandProvider.SetInputSourceAsync(dev, atVal);
    Console.WriteLine($"Result: {ok}");
    await Task.Delay(500);
    var afterInput = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, CmdInputSource, "InputSource");
    Console.WriteLine($"After: InputSource={FormatRaw(afterInput)}");
    if (!noRestore && beforeInput?.Length > 0)
    {
        Console.WriteLine($"Restoring to {beforeInput[0]}...");
        await NativeXuAtCommandProvider.SetInputSourceAsync(dev, beforeInput[0]);
        await Task.Delay(300);
        var restored = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, CmdInputSource, "InputSource");
        Console.WriteLine($"Restored: InputSource={FormatRaw(restored)}");
    }
    return ok ? 0 : 1;
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
    return await RunServiceSmokeAsync(device);
}

var baselineSnapshot = await new NativeXuAtCommandProvider().ReadAsync(device);
PrintSnapshot("Baseline snapshot", baselineSnapshot);

var getterSpecs = new[]
{
    new GetterSpec("AudioFormat", CmdAudioFormat, ValueKind.Byte),
    new GetterSpec("AudioSamplingRate", CmdAudioSamplingRate, ValueKind.Byte),
    new GetterSpec("CurrentInputSource", CmdInputSource, ValueKind.Byte),
    new GetterSpec("AdcOnOff", CmdAudioGetAdcOnOff, ValueKind.Byte),
    new GetterSpec("DacHpOnOff", CmdAudioGetDacHpOnOff, ValueKind.Byte),
    new GetterSpec("AdcVolumeGain", CmdAudioGetAdcVolumeGain, ValueKind.Int16),
    new GetterSpec("HdmiDprxVolumeGain", CmdAudioGetHdmiDprxVolumeGain, ValueKind.Int16),
    new GetterSpec("UacVolumeGain", CmdAudioGetUacVolumeGain, ValueKind.Int16),
    new GetterSpec("AuxInVolume", CmdGetAuxInVolume, ValueKind.Int16),
    new GetterSpec("AuxOutVolume", CmdGetAuxOutVolume, ValueKind.Int16),
    new GetterSpec("UacOut2MixerSource", CmdAudioGetUacOut2MixerSource, ValueKind.Int16),
    new GetterSpec("DacHpMixerSource", CmdAudioGetDacHpMixerSource, ValueKind.Int16),
    new GetterSpec("I2sOutMixerSource", CmdAudioGetI2sOutMixerSource, ValueKind.Int16),
    new GetterSpec("UacOut1Mute", CmdAudioGetUacOut1Mute, ValueKind.Byte),
    new GetterSpec("UacOut2Mute", CmdAudioGetUacOut2Mute, ValueKind.Byte),
    new GetterSpec("DacHpMute", CmdAudioGetDacHpMute, ValueKind.Byte),
    new GetterSpec("I2sOutMute", CmdAudioGetI2sOutMute, ValueKind.Int32),
};

var baselineReads = await ReadAllAsync(device, getterSpecs);
PrintReads("Baseline AT reads", baselineReads);

var experiments = new List<SetExperiment>();

experiments.AddRange(BuildShortExperiments(
    "Audio routing",
    new[]
    {
        new SetterSpec("SetUacOut2MixerSource", CmdAudioSetUacOut2MixerSource, CmdAudioGetUacOut2MixerSource),
        new SetterSpec("SetDacHpMixerSource", CmdAudioSetDacHpMixerSource, CmdAudioGetDacHpMixerSource),
        new SetterSpec("SetI2sOutMixerSource", CmdAudioSetI2sOutMixerSource, CmdAudioGetI2sOutMixerSource),
    },
    new short[] { 0, 1, 2, 3, 4, 8, 9 }));

experiments.AddRange(BuildIntExperiments(
    "Input source",
    new[]
    {
        new SetterSpec("SetInputSourceByte", CmdSetInputSource, CmdInputSource, PayloadWidth: 1),
        new SetterSpec("SetInputSourceShort", CmdSetInputSource, CmdInputSource, PayloadWidth: 2),
        new SetterSpec("SetInputSourceInt", CmdSetInputSource, CmdInputSource, PayloadWidth: 4),
    },
    new[] { 0, 1, 2, 3 }));

experiments.AddRange(BuildIntExperiments(
    "Audio on/off",
    new[]
    {
        new SetterSpec("SetAdcOnOff", CmdAudioSetAdcOnOff, CmdAudioGetAdcOnOff, PayloadWidth: 4),
        new SetterSpec("SetDacHpOnOff", CmdAudioSetDacHpOnOff, CmdAudioGetDacHpOnOff, PayloadWidth: 4),
    },
    new[] { 0, 1 }));

experiments.AddRange(BuildByteExperiments(
    "Audio mutes",
    new[]
    {
        new SetterSpec("SetUacOut1Mute", CmdAudioSetUacOut1Mute, CmdAudioGetUacOut1Mute, PayloadWidth: 1),
        new SetterSpec("SetUacOut2Mute", CmdAudioSetUacOut2Mute, CmdAudioGetUacOut2Mute, PayloadWidth: 1),
        new SetterSpec("SetDacHpMute", CmdAudioSetDacHpMute, CmdAudioGetDacHpMute, PayloadWidth: 1),
        new SetterSpec("SetI2sOutMute", CmdAudioSetI2sOutMute, CmdAudioGetI2sOutMute, PayloadWidth: 1),
    },
    new byte[] { 0, 1 }));

experiments.AddRange(BuildIntExperiments(
    "Audio gain",
    new[]
    {
        new SetterSpec("SetAdcVolumeGainByte", CmdAudioSetAdcVolumeGain, CmdAudioGetAdcVolumeGain, PayloadWidth: 1),
        new SetterSpec("SetAdcVolumeGainShort", CmdAudioSetAdcVolumeGain, CmdAudioGetAdcVolumeGain, PayloadWidth: 2),
        new SetterSpec("SetAdcVolumeGainInt", CmdAudioSetAdcVolumeGain, CmdAudioGetAdcVolumeGain, PayloadWidth: 4),
        new SetterSpec("SetHdmiDprxVolumeGain", CmdAudioSetHdmiDprxVolumeGain, CmdAudioGetHdmiDprxVolumeGain, PayloadWidth: 4),
        new SetterSpec("SetUacVolumeGain", CmdAudioSetUacVolumeGain, CmdAudioGetUacVolumeGain, PayloadWidth: 4),
        new SetterSpec("SetAuxInVolumeByte", CmdSetAuxInVolume, CmdGetAuxInVolume, PayloadWidth: 1),
        new SetterSpec("SetAuxInVolumeShort", CmdSetAuxInVolume, CmdGetAuxInVolume, PayloadWidth: 2),
        new SetterSpec("SetAuxInVolumeInt", CmdSetAuxInVolume, CmdGetAuxInVolume, PayloadWidth: 4),
        new SetterSpec("SetAuxOutVolumeByte", CmdSetAuxOutVolume, CmdGetAuxOutVolume, PayloadWidth: 1),
        new SetterSpec("SetAuxOutVolumeShort", CmdSetAuxOutVolume, CmdGetAuxOutVolume, PayloadWidth: 2),
        new SetterSpec("SetAuxOutVolumeInt", CmdSetAuxOutVolume, CmdGetAuxOutVolume, PayloadWidth: 4),
    },
    new[] { 0, 64, 128, 192, 255 }));

var results = new List<ExperimentResult>();
foreach (var experiment in experiments)
{
    Console.WriteLine();
    Console.WriteLine($"== {experiment.Group} :: {experiment.Setter.Name} -> {experiment.DisplayValue} ==");

    var before = await ReadAllAsync(device, getterSpecs);
    var writeOk = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, experiment.Setter.SetCmd, experiment.Payload);
    await Task.Delay(200);
    var after = await ReadAllAsync(device, getterSpecs);

    PrintDiff(before, after);
    results.Add(new ExperimentResult(experiment, writeOk, before, after));

    if (before.TryGetValue(experiment.Setter.ReadbackCmd, out var readbackBefore) &&
        readbackBefore.TypedValue is byte byteValue)
    {
        var restorePayload = BuildPayload(experiment.Setter.PayloadWidth, byteValue);
        var restored = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, experiment.Setter.SetCmd, restorePayload);
        await Task.Delay(150);
        Console.WriteLine($"Restore to {byteValue}: {(restored ? "ok" : "failed")}");
    }
    else if (before.TryGetValue(experiment.Setter.ReadbackCmd, out readbackBefore) &&
             readbackBefore.TypedValue is short shortValue)
    {
        var restorePayload = BuildPayload(experiment.Setter.PayloadWidth, shortValue);
        var restored = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, experiment.Setter.SetCmd, restorePayload);
        await Task.Delay(150);
        Console.WriteLine($"Restore to {shortValue}: {(restored ? "ok" : "failed")}");
    }
    else if (before.TryGetValue(experiment.Setter.ReadbackCmd, out readbackBefore) &&
             readbackBefore.TypedValue is int intValue)
    {
        var restorePayload = BuildPayload(experiment.Setter.PayloadWidth, intValue);
        var restored = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, experiment.Setter.SetCmd, restorePayload);
        await Task.Delay(150);
        Console.WriteLine($"Restore to {intValue}: {(restored ? "ok" : "failed")}");
    }
}

Console.WriteLine();
Console.WriteLine("== Interesting changes ==");
foreach (var result in results.Where(r => r.HasAnyChange))
{
    Console.WriteLine($"{result.Experiment.Setter.Name} -> {result.Experiment.DisplayValue} (write {(result.WriteOk ? "ok" : "failed")})");
    foreach (var changed in result.ChangedValues)
    {
        Console.WriteLine($"  {changed.Label}: {changed.Before} -> {changed.After}");
    }
}

if (results.All(r => !r.HasAnyChange))
{
    Console.WriteLine("No getter-visible changes were observed from the current candidate payload set.");
}

await RunAnalogGainSequenceAsync(device);

var finalSnapshot = await new NativeXuAtCommandProvider().ReadAsync(device);
PrintSnapshot("Final snapshot", finalSnapshot);
return 0;

static IEnumerable<SetExperiment> BuildShortExperiments(string group, IReadOnlyList<SetterSpec> setters, IReadOnlyList<short> values)
{
    foreach (var setter in setters)
    {
        foreach (var value in values)
        {
            yield return new SetExperiment(group, setter, value.ToString(CultureInfo.InvariantCulture), BuildPayload(setter.PayloadWidth, value));
        }
    }
}

static IEnumerable<SetExperiment> BuildIntExperiments(string group, IReadOnlyList<SetterSpec> setters, IReadOnlyList<int> values)
{
    foreach (var setter in setters)
    {
        foreach (var value in values)
        {
            yield return new SetExperiment(group, setter, value.ToString(CultureInfo.InvariantCulture), BuildPayload(setter.PayloadWidth, value));
        }
    }
}

static IEnumerable<SetExperiment> BuildByteExperiments(string group, IReadOnlyList<SetterSpec> setters, IReadOnlyList<byte> values)
{
    foreach (var setter in setters)
    {
        foreach (var value in values)
        {
            yield return new SetExperiment(group, setter, value.ToString(CultureInfo.InvariantCulture), BuildPayload(setter.PayloadWidth, value));
        }
    }
}

static byte[] BuildPayload(int width, long value)
{
    return width switch
    {
        1 => new[] { unchecked((byte)value) },
        2 => BitConverter.GetBytes(unchecked((short)value)),
        4 => BitConverter.GetBytes(unchecked((int)value)),
        _ => throw new InvalidOperationException($"Unsupported payload width {width}.")
    };
}

static async Task RunAnalogGainSequenceAsync(CaptureDevice device)
{
    var provider = new NativeXuAtCommandProvider();
    Console.WriteLine();
    Console.WriteLine("== Analog gain sequence ==");

    var baselineInput = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdInputSource, "CurrentInputSource");
    var baselineAdcOn = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdAudioGetAdcOnOff, "AdcOnOff");
    var baselineAdcGain = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdAudioGetAdcVolumeGain, "AdcVolumeGain");
    Console.WriteLine($"Baseline: input={FormatRaw(baselineInput)} adcOn={FormatRaw(baselineAdcOn)} adcGain={FormatRaw(baselineAdcGain)}");

    var inputOk = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdSetInputSource, BuildPayload(1, 1));
    var adcOnOk = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdAudioSetAdcOnOff, BuildPayload(4, 1));
    await Task.Delay(200);
    var afterAdcOn = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdAudioGetAdcOnOff, "AdcOnOff");
    Console.WriteLine($"Set input=1 ok={inputOk}; set adc-on=1 ok={adcOnOk}; adcOnNow={FormatRaw(afterAdcOn)}");

    foreach (var width in new[] { 1, 2, 4 })
    {
        foreach (var value in new[] { 0, 64, 128, 192, 255 })
        {
            var ok = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdAudioSetAdcVolumeGain, BuildPayload(width, value));
            await Task.Delay(150);
            var gain = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdAudioGetAdcVolumeGain, "AdcVolumeGain");
            Console.WriteLine($"  width={width} value={value} ok={ok} gain={FormatRaw(gain)}");
        }
    }

    if (baselineAdcGain?.Length > 0)
    {
        var baselineAdcGainValue = BitConverter.ToInt32(PadToFourBytes(baselineAdcGain), 0);
        var restoredGain = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdAudioSetAdcVolumeGain, BuildPayload(4, baselineAdcGainValue));
        Console.WriteLine($"Restore adc gain to baseline={baselineAdcGainValue} ok={restoredGain}");
    }

    if (baselineAdcOn?.Length > 0)
    {
        var baselineAdcOnValue = baselineAdcOn[0];
        var restoredAdcOn = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdAudioSetAdcOnOff, BuildPayload(4, baselineAdcOnValue));
        Console.WriteLine($"Restore adc on/off to baseline={baselineAdcOnValue} ok={restoredAdcOn}");
    }

    if (baselineInput?.Length > 0)
    {
        var baselineInputValue = baselineInput[0];
        var restoredInput = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdSetInputSource, BuildPayload(1, baselineInputValue));
        Console.WriteLine($"Restore input source to baseline={baselineInputValue} ok={restoredInput}");
    }
}

static async Task<int> RunServiceControlProbeAsync(string[] args)
{
    var deviceNameFilter = "4K X";
    string? targetMode = null;
    double? targetGain = null;
    var dumpPayload = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--device" when i + 1 < args.Length:
                deviceNameFilter = args[++i];
                break;
            case "--mode" when i + 1 < args.Length:
                targetMode = args[++i];
                break;
            case "--gain" when i + 1 < args.Length && double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out var gain):
                targetGain = gain;
                break;
            case "--dump-payload":
                dumpPayload = true;
                break;
        }
    }

    var device = NativeXuProbeDeviceLocator.Find(deviceNameFilter);

    if (device == null)
    {
        Console.Error.WriteLine("No capture device found.");
        return 1;
    }

    var service = new NativeXuAudioControlService();

    if (dumpPayload)
    {
        await PrintServicePayloadSnapshotAsync(service, device).ConfigureAwait(false);
    }

    var initial = await ReadServiceStateAsync(service, device).ConfigureAwait(false);
    PrintServiceState("Initial", initial);

    if (!initial.IsSupported)
    {
        Console.Error.WriteLine("Service reports device audio control unsupported.");
        return 2;
    }

    if (!string.IsNullOrWhiteSpace(targetMode))
    {
        var applied = await service.SetAudioModeAsync(device, targetMode, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine($"Set mode '{targetMode}': {(applied ? "ok" : "failed")}");
    }

    if (targetGain.HasValue)
    {
        var applied = await service.SetAnalogGainPercentAsync(device, targetGain.Value, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine($"Set gain '{targetGain.Value:0}': {(applied ? "ok" : "failed")}");
    }

    var final = await ReadServiceStateAsync(service, device).ConfigureAwait(false);
    PrintServiceState("Final", final);
    return 0;
}

static byte[] PadToFourBytes(byte[] value)
{
    if (value.Length >= 4)
    {
        return value;
    }

    var padded = new byte[4];
    Array.Copy(value, padded, value.Length);
    return padded;
}

static async Task<Dictionary<int, AtReadResult>> ReadAllAsync(CaptureDevice device, IEnumerable<GetterSpec> specs)
{
    var results = new Dictionary<int, AtReadResult>();
    foreach (var spec in specs)
    {
        var payload = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, spec.Cmd, spec.Name);
        results[spec.Cmd] = Decode(spec, payload);
    }

    return results;
}

static AtReadResult Decode(GetterSpec spec, byte[]? payload)
{
    if (payload == null || payload.Length == 0)
    {
        return new AtReadResult(spec.Name, payload, "unavailable", null);
    }

    return spec.Kind switch
    {
        ValueKind.Byte => new AtReadResult(spec.Name, payload, payload[0].ToString(CultureInfo.InvariantCulture), payload[0]),
        ValueKind.Int16 when payload.Length >= 2 => new AtReadResult(spec.Name, payload, BitConverter.ToInt16(payload, 0).ToString(CultureInfo.InvariantCulture), BitConverter.ToInt16(payload, 0)),
        ValueKind.Int32 when payload.Length >= 4 => new AtReadResult(spec.Name, payload, BitConverter.ToInt32(payload, 0).ToString(CultureInfo.InvariantCulture), BitConverter.ToInt32(payload, 0)),
        _ => new AtReadResult(spec.Name, payload, BitConverter.ToString(payload), null)
    };
}

static void PrintReads(string title, IReadOnlyDictionary<int, AtReadResult> reads)
{
    Console.WriteLine();
    Console.WriteLine($"== {title} ==");
    foreach (var item in reads.Values)
    {
        Console.WriteLine($"{item.Label}: {item.DisplayValue} raw={FormatRaw(item.Payload)}");
    }
}

static void PrintDiff(IReadOnlyDictionary<int, AtReadResult> before, IReadOnlyDictionary<int, AtReadResult> after)
{
    foreach (var key in before.Keys.OrderBy(k => k))
    {
        var left = before[key];
        var right = after[key];
        if (left.DisplayValue != right.DisplayValue || !RawEqual(left.Payload, right.Payload))
        {
            Console.WriteLine($"  {left.Label}: {left.DisplayValue} -> {right.DisplayValue} ({FormatRaw(left.Payload)} -> {FormatRaw(right.Payload)})");
        }
    }
}

static bool RawEqual(byte[]? a, byte[]? b)
{
    if (ReferenceEquals(a, b))
    {
        return true;
    }

    if (a == null || b == null || a.Length != b.Length)
    {
        return false;
    }

    for (var i = 0; i < a.Length; i++)
    {
        if (a[i] != b[i])
        {
            return false;
        }
    }

    return true;
}

static string FormatRaw(byte[]? payload) => payload == null ? "null" : BitConverter.ToString(payload);

static void PrintSnapshot(string title, SourceSignalTelemetrySnapshot snapshot)
{
    Console.WriteLine();
    Console.WriteLine($"== {title} ==");
    Console.WriteLine($"Availability: {snapshot.Availability}");
    Console.WriteLine($"Origin: {snapshot.Origin} ({snapshot.Confidence})");
    Console.WriteLine($"Source video: {snapshot.Width}x{snapshot.Height} @ {snapshot.FrameRateExact:0.###}");
    Console.WriteLine($"HDR: {snapshot.IsHdr}");
    Console.WriteLine($"Video format: {snapshot.VideoFormat}");
    Console.WriteLine($"Audio format: {snapshot.AudioFormat}");
    Console.WriteLine($"Audio sample rate: {snapshot.AudioSampleRate}");
    Console.WriteLine($"Input source: {snapshot.InputSource}");
    Console.WriteLine($"Diagnostic: {snapshot.DiagnosticSummary}");
}

static async Task<int> RunServiceSmokeAsync(CaptureDevice device)
{
    var service = new NativeXuAudioControlService();

    await PrintServiceStateAsync(service, device, "Before");

    var setModeResult = await service.SetAudioModeAsync(device, "Analog", CancellationToken.None).ConfigureAwait(false);
    Console.WriteLine($"SetAudioModeAsync('Analog') => {setModeResult}");

    await PrintServiceStateAsync(service, device, "After mode");

    var setGainResult = await service.SetAnalogGainPercentAsync(device, 50d, CancellationToken.None).ConfigureAwait(false);
    Console.WriteLine($"SetAnalogGainPercentAsync(50) => {setGainResult}");

    await PrintServiceStateAsync(service, device, "After gain");
    return 0;
}

static Task<NativeXuAudioControlService.DeviceAudioControlState> ReadServiceStateAsync(
    NativeXuAudioControlService service,
    CaptureDevice device)
    => service.ReadStateAsync(device, CancellationToken.None);

static void PrintServiceState(string title, NativeXuAudioControlService.DeviceAudioControlState state)
{
    Console.WriteLine();
    Console.WriteLine($"== {title} service state ==");
    Console.WriteLine($"IsSupported: {state.IsSupported}");
    Console.WriteLine($"InterfacePath: {state.InterfacePath ?? "(null)"}");
    Console.WriteLine($"Mode: {state.Mode ?? "(null)"}");
    Console.WriteLine($"AnalogGainPercent: {state.AnalogGainPercent?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}");
    Console.WriteLine($"RawGainValue: {state.RawGainValue?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}");
}

static async Task PrintServicePayloadSnapshotAsync(NativeXuAudioControlService service, CaptureDevice device)
{
    var snapshot = await service.ReadPayloadSnapshotAsync(device, CancellationToken.None).ConfigureAwait(false);
    if (snapshot == null)
    {
        Console.WriteLine("Service payload snapshot: null");
        return;
    }

    Console.WriteLine("== Service payload snapshot ==");
    Console.WriteLine($"DeviceId: {snapshot.DeviceId ?? "(null)"}");
    Console.WriteLine($"DeviceName: {snapshot.DeviceName ?? "(null)"}");
    Console.WriteLine($"VendorProduct: {FormatVendorProduct(snapshot.VendorId, snapshot.ProductId)}");
    Console.WriteLine($"InterfacePath: {snapshot.InterfacePath}");
    Console.WriteLine($"NodeId: {snapshot.NodeId}");
    Console.WriteLine($"SelectorId: {snapshot.SelectorId}");
    Console.WriteLine($"TimestampUtc: {snapshot.TimestampUtc:O}");
    Console.WriteLine($"ControlByteIndexes: {string.Join(",", snapshot.ControlByteIndexes)}");
    Console.WriteLine($"VolatileByteIndexes: {string.Join(",", snapshot.VolatileByteIndexes)}");
    Console.WriteLine($"RawLength: {snapshot.RawPayload.Length}");
    Console.WriteLine($"RawHex: {BitConverter.ToString(snapshot.RawPayload).Replace("-", string.Empty)}");
    Console.WriteLine($"NormalizedLength: {snapshot.NormalizedPayload.Length}");
    Console.WriteLine($"NormalizedHex: {BitConverter.ToString(snapshot.NormalizedPayload).Replace("-", string.Empty)}");
}

static async Task PrintServiceStateAsync(NativeXuAudioControlService service, CaptureDevice device, string label)
{
    var result = await ReadServiceStateAsync(service, device).ConfigureAwait(false);
    PrintServiceState(label, result);
}

static string FormatVendorProduct(ushort? vendorId, ushort? productId)
    => vendorId.HasValue && productId.HasValue
        ? $"VID_0x{vendorId.Value:X4} PID_0x{productId.Value:X4}"
        : "(unknown)";

enum ValueKind
{
    Byte,
    Int16,
    Int32
}

sealed record GetterSpec(string Name, int Cmd, ValueKind Kind);

sealed record SetterSpec(string Name, int SetCmd, int ReadbackCmd, int PayloadWidth = 2);

sealed record SetExperiment(string Group, SetterSpec Setter, string DisplayValue, byte[] Payload);

sealed record AtReadResult(string Label, byte[]? Payload, string DisplayValue, object? TypedValue);

sealed record ChangedValue(string Label, string Before, string After);

sealed class ExperimentResult
{
    public ExperimentResult(SetExperiment experiment, bool writeOk, IReadOnlyDictionary<int, AtReadResult> before, IReadOnlyDictionary<int, AtReadResult> after)
    {
        Experiment = experiment;
        WriteOk = writeOk;
        ChangedValues = before.Keys
            .Where(key => before[key].DisplayValue != after[key].DisplayValue || !AreEqual(before[key].Payload, after[key].Payload))
            .Select(key => new ChangedValue(before[key].Label, before[key].DisplayValue, after[key].DisplayValue))
            .ToArray();
    }

    public SetExperiment Experiment { get; }
    public bool WriteOk { get; }
    public IReadOnlyList<ChangedValue> ChangedValues { get; }
    public bool HasAnyChange => ChangedValues.Count > 0;

    private static bool AreEqual(byte[]? a, byte[]? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }
}
