using System.Threading.Tasks;

static partial class Program
{
    private static Task RtkI2cProbe_PreservesDirectDllCommandSurface()
    {
        // NativeXuAudioProbe is a separate tool assembly not loaded by the test harness,
        // so source-text verification is the only option for the P/Invoke surface contract.
        var probeText = ReadRepoFile("tools/NativeXuAudioProbe/RtkI2cProbe.cs");

        // Native DLL target
        AssertContains(probeText, "RTK_IO_x64.dll");

        // Critical P/Invoke declarations that define the hardware command interface
        AssertContains(probeText, "static extern long rtk_sendI2CATCommand(");
        AssertContains(probeText, "static extern long rtk_sendI2CATCommand_v2(");
        AssertContains(probeText, "static extern long rtk_sendATCommand(");

        return Task.CompletedTask;
    }
}
