using Microsoft.Win32.SafeHandles;
using static KsAudioNodeProbeNative;

static class KsAudioNodeProbeScanWorkflows
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

    private static void RunExtendedNodeTests(SafeFileHandle handle)
    {
        Console.WriteLine();
        Console.WriteLine("== Extended node tests ==");
        for (var nodeId = 0; nodeId < 32; nodeId++)
        {
            for (var propId = 1; propId <= 20; propId++)
            {
                for (var ch = -1; ch <= 1; ch++)
                {
                    if (!TryAudioGetLong(handle, nodeId, propId, ch, out var val, out _))
                    {
                        continue;
                    }

                    var pName = PropertyNames.TryGetValue(propId, out var n) ? n : $"Property({propId})";
                    Console.WriteLine($"  Node {nodeId}, {pName}, ch={ch}: GET={val}");
                    RunExtendedSetTest(handle, nodeId, propId, ch, val);
                }
            }
        }
    }

    private static void RunExtendedSetTest(SafeFileHandle handle, int nodeId, int propId, int ch, int val)
    {
        if (propId == 12) // MUTE
        {
            var newVal = val == 0 ? 1 : 0;
            if (TryAudioSetLong(handle, nodeId, propId, ch, newVal, out var setErr))
            {
                TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                Console.WriteLine($"    SET {newVal} -> readback={afterSet}");
                TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
                Console.WriteLine($"    RESTORED to {val}");
            }
            else
            {
                Console.WriteLine($"    SET {newVal} FAILED: {DescribeWin32(setErr)}");
            }
        }
        else if (propId == 11) // MUX
        {
            foreach (var target in new[] { 0, 1, 2, 3 })
            {
                if (TryAudioSetLong(handle, nodeId, propId, ch, target, out var setErr))
                {
                    TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                    Console.WriteLine($"    MUX SET {target} -> readback={afterSet}");
                }
                else
                {
                    Console.WriteLine($"    MUX SET {target} FAILED: {DescribeWin32(setErr)}");
                }
            }

            TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
        }
        else if (propId == 4) // VOLUME
        {
            var testVal = val + 65536;
            if (TryAudioSetLong(handle, nodeId, propId, ch, testVal, out var setErr))
            {
                TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                Console.WriteLine($"    VOLUME SET {testVal} -> readback={afterSet}");
                TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
                Console.WriteLine($"    RESTORED to {val}");
            }
            else
            {
                Console.WriteLine($"    VOLUME SET {testVal} FAILED: {DescribeWin32(setErr)}");
            }
        }
        else if (propId == 13) // BASS (might be repurposed)
        {
            foreach (var target in new[] { 0, 1, 2, 3 })
            {
                if (target == val) continue;
                if (TryAudioSetLong(handle, nodeId, propId, ch, target, out var setErr))
                {
                    TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                    Console.WriteLine($"    BASS SET {target} -> readback={afterSet}");
                }
                else
                {
                    Console.WriteLine($"    BASS SET {target} FAILED: {DescribeWin32(setErr)}");
                }
            }

            TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
        }
    }

    private static void RunAdcVolumeProbe(SafeFileHandle handle)
    {
        Console.WriteLine("== ADC volume probe ==");
        foreach (var channel in new[] { -1, 0, 1 })
        {
            if (TryAudioGetLong(handle, nodeId: 0, propertyId: 4, channel, out var value, out var error))
            {
                Console.WriteLine($"Read volume channel={channel}: {value}");

                var target = value == 0 ? -6 * 65536 : value + 65536;
                if (TryAudioSetLong(handle, nodeId: 0, propertyId: 4, channel, target, out var setError))
                {
                    if (TryAudioGetLong(handle, nodeId: 0, propertyId: 4, channel, out var after, out error))
                    {
                        Console.WriteLine($"  Set volume -> {target}; readback={after}");
                    }
                    else
                    {
                        Console.WriteLine($"  Set volume -> {target}; readback failed: {DescribeWin32(error)}");
                    }

                    TryAudioSetLong(handle, nodeId: 0, propertyId: 4, channel, value, out _);
                }
                else
                {
                    Console.WriteLine($"  Set volume channel={channel} failed: {DescribeWin32(setError)}");
                }
            }
            else
            {
                Console.WriteLine($"Read volume channel={channel} failed: {DescribeWin32(error)}");
            }
        }
    }

    private static void RunMuxProbe(SafeFileHandle handle)
    {
        Console.WriteLine();
        Console.WriteLine("== Mux probe ==");
        if (TryAudioGetLong(handle, nodeId: 3, propertyId: 11, channel: 0, out var muxValue, out var muxError))
        {
            Console.WriteLine($"Read mux source: {muxValue}");
            foreach (var target in new[] { 0, 1, 2, 3 })
            {
                if (TryAudioSetLong(handle, nodeId: 3, propertyId: 11, channel: 0, target, out var setError))
                {
                    if (TryAudioGetLong(handle, nodeId: 3, propertyId: 11, channel: 0, out var after, out muxError))
                    {
                        Console.WriteLine($"  Set mux -> {target}; readback={after}");
                    }
                    else
                    {
                        Console.WriteLine($"  Set mux -> {target}; readback failed: {DescribeWin32(muxError)}");
                    }
                }
                else
                {
                    Console.WriteLine($"  Set mux -> {target} failed: {DescribeWin32(setError)}");
                }
            }

            TryAudioSetLong(handle, nodeId: 3, propertyId: 11, channel: 0, muxValue, out _);
        }
        else
        {
            Console.WriteLine($"Read mux source failed: {DescribeWin32(muxError)}");
        }
    }

    private static void RunMuteProbe(SafeFileHandle handle)
    {
        Console.WriteLine();
        Console.WriteLine("== Mute probe ==");
        if (TryAudioGetLong(handle, nodeId: 2, propertyId: 12, channel: 0, out var muteValue, out var muteError))
        {
            Console.WriteLine($"Read mute: {muteValue}");
            foreach (var target in new[] { 0, 1 })
            {
                if (TryAudioSetLong(handle, nodeId: 2, propertyId: 12, channel: 0, target, out var setError))
                {
                    if (TryAudioGetLong(handle, nodeId: 2, propertyId: 12, channel: 0, out var after, out muteError))
                    {
                        Console.WriteLine($"  Set mute -> {target}; readback={after}");
                    }
                    else
                    {
                        Console.WriteLine($"  Set mute -> {target}; readback failed: {DescribeWin32(muteError)}");
                    }
                }
                else
                {
                    Console.WriteLine($"  Set mute -> {target} failed: {DescribeWin32(setError)}");
                }
            }

            TryAudioSetLong(handle, nodeId: 2, propertyId: 12, channel: 0, muteValue, out _);
        }
        else
        {
            Console.WriteLine($"Read mute failed: {DescribeWin32(muteError)}");
        }
    }
}
