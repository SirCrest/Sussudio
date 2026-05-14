using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration()
    {
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");

        // 57 MB/s safety rate = 57 * 1024 * 1024 = 59768832 bytes/sec
        const long safetyBytesPerSecond = 57L * 1024 * 1024;

        var options = RuntimeHelpers.GetUninitializedObject(optionsType);

        // 5 minutes
        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
        var maxBytes = (long)GetPropertyValue(options, "MaxDiskBytes")!;
        AssertEqual((long)(300.0 * safetyBytesPerSecond), maxBytes, "MaxDiskBytes for 5 minutes");

        // 1 minute
        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(1));
        var oneMinBytes = (long)GetPropertyValue(options, "MaxDiskBytes")!;
        AssertEqual((long)(60.0 * safetyBytesPerSecond), oneMinBytes, "MaxDiskBytes for 1 minute");

        // Linear scaling: 5 min = 5 × 1 min
        AssertEqual(maxBytes, oneMinBytes * 5, "MaxDiskBytes linear scaling");

        SetPropertyBackingField(options, "BufferDuration", TimeSpan.Zero);
        AssertEqual(0L, (long)GetPropertyValue(options, "MaxDiskBytes")!, "MaxDiskBytes for zero duration");

        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromTicks(-1));
        AssertEqual(0L, (long)GetPropertyValue(options, "MaxDiskBytes")!, "MaxDiskBytes for negative duration");

        SetPropertyBackingField(options, "BufferDuration", TimeSpan.MaxValue);
        AssertEqual(long.MaxValue, (long)GetPropertyValue(options, "MaxDiskBytes")!, "MaxDiskBytes saturates huge duration");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_InitializeClearsRecordingPts()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_init_pts_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            var manager = Activator.CreateInstance(managerType, new[] { options })
                ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
            using var disposableManager = manager as IDisposable;

            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "session-a" });
            managerType.GetMethod("UpdateLatestPts")!.Invoke(manager, new object[] { TimeSpan.FromSeconds(10) });
            managerType.GetMethod("PauseEviction")!.Invoke(manager, null);
            managerType.GetMethod("UpdateLatestPts")!.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) });
            managerType.GetMethod("ResumeEviction")!.Invoke(manager, null);

            AssertEqual(TimeSpan.FromSeconds(10), (TimeSpan)GetPropertyValue(manager, "RecordingStartPts")!, "RecordingStartPts before reinitialize");
            AssertEqual(TimeSpan.FromSeconds(20), (TimeSpan)GetPropertyValue(manager, "RecordingEndPts")!, "RecordingEndPts before reinitialize");

            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "session-b" });
            AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "RecordingStartPts")!, "RecordingStartPts resets on Initialize");
            AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "RecordingEndPts")!, "RecordingEndPts resets on Initialize");

            var activePath = (string)managerType.GetMethod("AcquireSegmentPath", Type.EmptyTypes)!.Invoke(manager, null)!;
            File.WriteAllBytes(activePath, new byte[] { 1, 2, 3, 4 });
            var segmentInfo = (System.Collections.IEnumerable)managerType.GetMethod("GetSegmentInfoList")!.Invoke(manager, null)!;
            var activeInfo = segmentInfo.Cast<object>().Single(info => (bool)GetPropertyValue(info, "IsActive")!);
            AssertEqual(0L, (long)GetPropertyValue(activeInfo, "StartPtsMs")!, "Active segment start PTS resets on Initialize");
            AssertEqual(0L, (long)GetPropertyValue(activeInfo, "EndPtsMs")!, "Active segment end PTS resets on Initialize");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

}
