using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

static partial class NativeXuProbeI2cCommands
{
    public static async Task<int> RunVerifyAsync(CaptureDevice dev)
    {
        // Verify the AT envelope I2C path actually works by doing SET+readback
        // Tests: read I2C 0x04 â†’ SET I2C 0x04=1 â†’ read I2C 0x04 â†’ restore
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
            Console.WriteLine($"  I2C GET 0x{i2cOp:X2}: AT={BitConverter.ToString(atFrame)} â†’ {(resp != null ? BitConverter.ToString(resp) : "(null)")}");
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
}
