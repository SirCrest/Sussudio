using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

static partial class NativeXuProbeI2cCommands
{
    public static async Task<int> RunSelectorProbeAsync(CaptureDevice dev)
    {
        // Probe XU selectors with various buffer sizes to find I2C AT transport
        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(dev, out var vid2, out var pid2))
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
}
