using System.Globalization;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

static partial class NativeXuProbeI2cCommands
{
    public static async Task<int> RunAsync(string[] args)
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
            return await RunSelectorProbeAsync(dev);
        }

        if (subCmd == "verify")
        {
            return await RunVerifyAsync(dev);
        }

        if (subCmd == "high-sel")
        {
            return await RunHighSelectorProbeAsync(dev);
        }

        if (subCmd == "topology")
        {
            return RunTopologyProbe(dev);
        }

        Console.Error.WriteLine("Usage: i2c-cmd get|set|scan|sel-probe|verify|high-sel|topology");
        return 1;
    }
}
