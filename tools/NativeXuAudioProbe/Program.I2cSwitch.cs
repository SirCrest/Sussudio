using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

static class NativeXuProbeI2cSwitch
{
    public static async Task<int> RunAsync(string[] args)
    {
        var target = args.Length > 1 ? args[1].ToLowerInvariant() : "analog";
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null)
        {
            Console.Error.WriteLine("No device");
            return 1;
        }

        Console.WriteLine($"Device: {dev.Name}");
        Console.WriteLine($"Target: {target}");

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

        Console.WriteLine($"\n--- Sending audio switch sequence ({target}) ---");

        var dacState = await SendI2cAtGetAsync(dev, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 });
        Console.WriteLine($"  1. I2C GET 0x09/42 = {(dacState != null ? BitConverter.ToString(dacState) : "(null)")}");

        var audioSourceValue = (byte)(target == "analog" ? 0x01 : 0x00);
        var set04 = await SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, audioSourceValue });
        Console.WriteLine($"  2. I2C SET 0x04 = 0x{audioSourceValue:X2}: {(set04 ? "OK" : "failed")}");

        var state03 = await SendI2cAtGetAsync(dev, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x03, 0xA0 });
        Console.WriteLine($"  3. I2C GET 0x03/A0 = {(state03 != null ? BitConverter.ToString(state03) : "(null)")}");

        var set0E = await SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x0E, 0x01 });
        Console.WriteLine($"  4. I2C SET 0x0E = 01: {(set0E ? "OK" : "failed")}");

        var set10 = await SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x10, 0x01 });
        Console.WriteLine($"  5. I2C SET 0x10 = 01: {(set10 ? "OK" : "failed")}");

        var set5B = await NativeXuAtCommandProvider.SendNamedSetCommandPublicAsync(
            dev, 0x5B, new byte[] { 0x00, 0x05, 0x00, 0x00 }, "AT_0x5B_commit");
        Console.WriteLine($"  6. UVC AT SET 0x5B = 00-05-00-00: {(set5B ? "OK" : "failed")}");

        await Task.Delay(500);

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
}
