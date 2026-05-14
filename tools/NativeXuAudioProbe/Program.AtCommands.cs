using System.Globalization;
using Sussudio.Services.Telemetry;
using static NativeXuProbeCommands;
using static NativeXuProbeFormatting;

static class NativeXuProbeAtCommands
{
    public static async Task<int> RunAtReadAsync(string[] args)
    {
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null)
        {
            Console.Error.WriteLine("No device");
            return 1;
        }

        Console.WriteLine($"Device: {dev.Name}");

        for (var ai = 1; ai < args.Length; ai++)
        {
            if (!int.TryParse(args[ai].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out var opcode))
            {
                Console.Error.WriteLine($"Invalid opcode: {args[ai]}");
                continue;
            }

            var raw = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, opcode, $"0x{opcode:X2}");
            if (raw != null)
            {
                Console.WriteLine($"  AT 0x{opcode:X2}: {BitConverter.ToString(raw)} ({raw.Length} bytes) int32={BitConverter.ToInt32(raw.Length >= 4 ? raw[..4] : raw.Concat(new byte[4 - raw.Length]).ToArray(), 0)}");
            }
            else
            {
                Console.WriteLine($"  AT 0x{opcode:X2}: (null/failed)");
            }
        }

        return 0;
    }

    public static async Task<int> RunAtWriteAsync(string[] args)
    {
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null)
        {
            Console.Error.WriteLine("No device");
            return 1;
        }

        Console.WriteLine($"Device: {dev.Name}");

        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: at-write <opcode_hex> <value_int> [--read-back <get_opcode_hex>]");
            return 1;
        }

        int.TryParse(args[1].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out var setOpcode);
        int.TryParse(args[2], out var value);
        var getOpcode = setOpcode + 1;
        for (var ai = 3; ai < args.Length - 1; ai++)
        {
            if (args[ai] == "--read-back")
            {
                int.TryParse(args[ai + 1].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out getOpcode);
            }
        }

        var before = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, getOpcode, $"GET 0x{getOpcode:X2}");
        Console.WriteLine($"BEFORE: AT 0x{getOpcode:X2} = {(before != null ? BitConverter.ToString(before) : "(null)")}");

        Console.WriteLine($"WRITING: AT 0x{setOpcode:X2} value={value} (bytes: {BitConverter.ToString(BitConverter.GetBytes(value))})");
        var ok = await NativeXuAtCommandProvider.SendNamedSetCommandPublicAsync(dev, setOpcode, BitConverter.GetBytes(value), $"SET 0x{setOpcode:X2}={value}");
        Console.WriteLine($"Result: {ok}");

        await Task.Delay(500);

        var after = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, getOpcode, $"GET 0x{getOpcode:X2}");
        Console.WriteLine($"AFTER: AT 0x{getOpcode:X2} = {(after != null ? BitConverter.ToString(after) : "(null)")}");

        return ok ? 0 : 1;
    }

    public static async Task<int> RunAtSetInputAsync(string[] args)
    {
        var atVal = args.Length > 1 ? int.Parse(args[1]) : 0;
        var noRestore = args.Any(a => a == "--no-restore");
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null)
        {
            Console.Error.WriteLine("No device");
            return 1;
        }

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
}
