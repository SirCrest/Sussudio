using System.Globalization;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

static class NativeXuProbeServiceProbe
{
    public static async Task<int> RunServiceControlProbeAsync(string[] args)
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

    public static async Task<int> RunServiceSmokeAsync(CaptureDevice device)
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

    private static Task<NativeXuAudioControlService.DeviceAudioControlState> ReadServiceStateAsync(
        NativeXuAudioControlService service,
        CaptureDevice device)
        => service.ReadStateAsync(device, CancellationToken.None);

    private static void PrintServiceState(string title, NativeXuAudioControlService.DeviceAudioControlState state)
    {
        Console.WriteLine();
        Console.WriteLine($"== {title} service state ==");
        Console.WriteLine($"IsSupported: {state.IsSupported}");
        Console.WriteLine($"InterfacePath: {state.InterfacePath ?? "(null)"}");
        Console.WriteLine($"Mode: {state.Mode ?? "(null)"}");
        Console.WriteLine($"AnalogGainPercent: {state.AnalogGainPercent?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}");
        Console.WriteLine($"RawGainValue: {state.RawGainValue?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}");
    }

    private static async Task PrintServicePayloadSnapshotAsync(NativeXuAudioControlService service, CaptureDevice device)
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

    private static async Task PrintServiceStateAsync(NativeXuAudioControlService service, CaptureDevice device, string label)
    {
        var result = await ReadServiceStateAsync(service, device).ConfigureAwait(false);
        PrintServiceState(label, result);
    }

    private static string FormatVendorProduct(ushort? vendorId, ushort? productId)
        => vendorId.HasValue && productId.HasValue
            ? $"VID_0x{vendorId.Value:X4} PID_0x{productId.Value:X4}"
            : "(unknown)";
}
