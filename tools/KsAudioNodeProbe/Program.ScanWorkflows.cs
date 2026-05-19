using Microsoft.Win32.SafeHandles;
using static KsAudioNodeProbeNative;

static partial class KsAudioNodeProbeScanWorkflows
{
    private static readonly Dictionary<int, string> PropertyNames = new()
    {
        { 1, "KSPROPERTY_AUDIO_LATENCY" },
        { 2, "KSPROPERTY_AUDIO_COPY_PROTECTION" },
        { 3, "KSPROPERTY_AUDIO_CHANNEL_CONFIG" },
        { 4, "KSPROPERTY_AUDIO_VOLUMELEVEL" },
        { 5, "KSPROPERTY_AUDIO_POSITION" },
        { 6, "KSPROPERTY_AUDIO_DYNAMIC_RANGE" },
        { 7, "KSPROPERTY_AUDIO_QUALITY" },
        { 8, "KSPROPERTY_AUDIO_SAMPLING_RATE" },
        { 9, "KSPROPERTY_AUDIO_DYNAMIC_SAMPLING_RATE" },
        { 10, "KSPROPERTY_AUDIO_MIX_LEVEL_TABLE" },
        { 11, "KSPROPERTY_AUDIO_MUX_SOURCE" },
        { 12, "KSPROPERTY_AUDIO_MUTE" },
        { 13, "KSPROPERTY_AUDIO_BASS" },
        { 14, "KSPROPERTY_AUDIO_MID" },
        { 15, "KSPROPERTY_AUDIO_TREBLE" },
        { 16, "KSPROPERTY_AUDIO_BASS_BOOST" },
        { 17, "KSPROPERTY_AUDIO_EQ_LEVEL" },
        { 18, "KSPROPERTY_AUDIO_NUM_EQ_BANDS" },
        { 19, "KSPROPERTY_AUDIO_EQ_BANDS" },
        { 20, "KSPROPERTY_AUDIO_AGC" },
    };

    public static int RunSetAndHold(SafeFileHandle handle)
    {
        Console.WriteLine("== SET-AND-HOLD MODE ==");
        var holdNodeId = 2;
        var holdPropId = 13; // BASS (repurposed as selector)
        var holdChannel = 0;

        if (TryAudioGetLong(handle, holdNodeId, holdPropId, holdChannel, out var currentVal, out var getErr))
        {
            Console.WriteLine($"Current: node={holdNodeId} prop={holdPropId} ch={holdChannel} value={currentVal}");
            var targetVal = currentVal == 0 ? 1 : 0;
            Console.WriteLine($"Setting to {targetVal}...");
            if (TryAudioSetLong(handle, holdNodeId, holdPropId, holdChannel, targetVal, out var setErr))
            {
                TryAudioGetLong(handle, holdNodeId, holdPropId, holdChannel, out var afterSet, out _);
                Console.WriteLine($"Readback after SET: {afterSet}");
                Console.WriteLine($"Holding for 10 seconds — check AT telemetry (AdcOnOff, InputSource, selector 3 payload)...");
                Thread.Sleep(10000);
                TryAudioGetLong(handle, holdNodeId, holdPropId, holdChannel, out var stillSet, out _);
                Console.WriteLine($"Value after hold: {stillSet}");
                Console.WriteLine($"Restoring to {currentVal}...");
                TryAudioSetLong(handle, holdNodeId, holdPropId, holdChannel, currentVal, out _);
                TryAudioGetLong(handle, holdNodeId, holdPropId, holdChannel, out var restored, out _);
                Console.WriteLine($"Restored: {restored}");
            }
            else
            {
                Console.WriteLine($"SET failed: {DescribeWin32(setErr)}");
            }
        }
        else
        {
            Console.WriteLine($"GET failed: {DescribeWin32(getErr)}");
        }

        return 0;
    }

    public static void RunFullProbe(SafeFileHandle handle)
    {
        EnumerateTopologyNodes(handle);
        RunBruteForceNodePropertyScan(handle);
        RunExtendedNodeTests(handle);
        RunAdcVolumeProbe(handle);
        RunMuxProbe(handle);
        RunMuteProbe(handle);
    }

    private static void EnumerateTopologyNodes(SafeFileHandle handle)
    {
        Console.WriteLine("== Topology node enumeration ==");
        var topologyNodeTypes = EnumerateTopologyNodeTypes(handle);
        if (topologyNodeTypes.Count == 0)
        {
            Console.WriteLine("No topology nodes found. Trying brute-force node scan...");
            return;
        }

        foreach (var (nodeId, nodeType) in topologyNodeTypes)
        {
            Console.WriteLine($"  Node {nodeId}: {DescribeNodeType(nodeType)}");
        }
    }

    private static void RunBruteForceNodePropertyScan(SafeFileHandle handle)
    {
        Console.WriteLine();
        Console.WriteLine("== Brute-force node/property scan ==");

        for (var nodeId = 0; nodeId < 32; nodeId++)
        {
            var hits = new List<string>();
            foreach (var (propId, propName) in PropertyNames)
            {
                if (TryAudioGetLong(handle, nodeId, propId, channel: 0, out var val, out _))
                {
                    hits.Add($"{propName}({propId})={val}");
                }
            }

            if (hits.Count > 0)
            {
                Console.WriteLine($"  Node {nodeId}: {string.Join(", ", hits)}");
            }
        }
    }

}
