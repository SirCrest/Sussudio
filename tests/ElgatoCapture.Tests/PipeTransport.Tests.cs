using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task EcctlPipeTransport_ExposesAdvancedAutomationCommandIds()
    {
        var assemblyPath = Path.Combine("tools", "ecctl", "bin", "Debug", "net8.0", "ecctl.dll");
        var fullPath = Path.Combine(GetRepoRoot(), assemblyPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        var ecctlAssembly = LoadToolAssembly(assemblyPath);

        // Verify PipeTransport exposes expected command routing
        var transportType = ecctlAssembly.GetType("EcCtl.PipeTransport")
            ?? throw new InvalidOperationException("EcCtl.PipeTransport type not found.");
        var sendCommandAsync = transportType.GetMethod("SendCommandAsync", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("EcCtl.PipeTransport.SendCommandAsync not found.");

        var pipeName = $"ecctl-pipe-transport-{Guid.NewGuid():N}";
        var transport = Activator.CreateInstance(transportType, pipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for transport test.");
        var request = await CapturePipeRequestAsync(
            pipeName,
            async () =>
            {
                var task = sendCommandAsync.Invoke(
                    transport,
                    new object?[]
                    {
                        "SetPreviewVolume",
                        new Dictionary<string, object?> { ["previewVolumePercent"] = 55.5 },
                        null
                    }) as Task
                    ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(34, request.GetProperty("command").GetInt32(), "PipeTransport SetPreviewVolume command id");
        AssertEqual(55.5, request.GetProperty("payload").GetProperty("previewVolumePercent").GetDouble(), "PipeTransport preview volume payload");
    }
}
