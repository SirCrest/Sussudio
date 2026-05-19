using static KsAudioNodeProbeNative;

var holdMode = args.Any(a => a.StartsWith("--set-hold", StringComparison.OrdinalIgnoreCase));
var selector = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "VID_0FD9&PID_009D";
if (!TryParseVidPid(selector, out var vendorId, out var productId))
{
    Console.Error.WriteLine($"Could not parse VID/PID from '{selector}'.");
    return 1;
}

var interfaces = EnumerateKsInterfaces(vendorId, productId);
var audioInterface = interfaces.FirstOrDefault(path =>
    path.Contains("&mi_02#", StringComparison.OrdinalIgnoreCase) &&
    path.Contains("{65e8773d-8f56-11d0-a3b9-00a0c9223196}", StringComparison.OrdinalIgnoreCase));

if (audioInterface == null)
{
    Console.Error.WriteLine("Could not find the MI_02 audio KS interface.");
    return 1;
}

Console.WriteLine($"Audio KS path: {audioInterface}");

using var handle = TryOpen(audioInterface, out var openError);
if (handle == null)
{
    Console.Error.WriteLine($"Failed to open audio KS interface: {DescribeWin32(openError)}");
    return 1;
}

if (holdMode)
{
    return KsAudioNodeProbeScanWorkflows.RunSetAndHold(handle);
}

KsAudioNodeProbeScanWorkflows.RunFullProbe(handle);
return 0;
