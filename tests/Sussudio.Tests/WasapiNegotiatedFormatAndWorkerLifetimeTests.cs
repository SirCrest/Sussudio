using System;
using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class WasapiNegotiatedFormatAndWorkerLifetimeTests
{
    private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly Assembly _assembly = SussudioAssembly.Load();

    [Fact]
    public void ExtensiblePcm24In32RetainsContainerWidthAndChannelMask()
    {
        var format = new TestWaveFormatExtensible
        {
            Format = new TestWaveFormatEx
            {
                FormatTag = 0xFFFE,
                Channels = 6,
                SamplesPerSecond = 96_000,
                AverageBytesPerSecond = 96_000 * 24,
                BlockAlign = 24,
                BitsPerSample = 32,
                ExtraSize = 22
            },
            ValidBitsPerSample = 24,
            ChannelMask = 0x3F,
            SubFormat = new Guid("00000001-0000-0010-8000-00AA00389B71")
        };

        var parsed = ParseFormat(format);
        Assert.Equal(96_000, ReadIntProperty(parsed, "SampleRate"));
        Assert.Equal(6, ReadIntProperty(parsed, "Channels"));
        Assert.Equal(32, ReadIntProperty(parsed, "ContainerBitsPerSample"));
        Assert.Equal(24, ReadIntProperty(parsed, "ValidBitsPerSample"));
        Assert.Equal(24, ReadIntProperty(parsed, "BlockAlign"));
        Assert.Equal(0x3Fu, ReadUIntProperty(parsed, "ChannelMask"));
        Assert.Equal("Pcm24", ReadProperty(parsed, "SampleType").ToString());
    }

    [Fact]
    public void MalformedUncompressedBlockAlignmentIsRejected()
    {
        var format = new TestWaveFormatEx
        {
            FormatTag = 0x0003,
            Channels = 2,
            SamplesPerSecond = 48_000,
            AverageBytesPerSecond = 48_000 * 4,
            BlockAlign = 4,
            BitsPerSample = 32,
            ExtraSize = 0
        };

        var exception = Assert.Throws<TargetInvocationException>(() => ParseFormat(format));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("block alignment", exception.InnerException!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderResamplerCarriesPhaseAcrossChunkAndCallbackBoundaries()
    {
        var playbackType = RequireType("Sussudio.Services.Audio.WasapiAudioPlayback");
        var playback = Activator.CreateInstance(playbackType, nonPublic: true)!;
        var renderFormat = CreateAudioFormat(
            sampleRate: 96_000,
            channels: 2,
            containerBits: 32,
            validBits: 32,
            blockAlign: 8,
            channelMask: 0x3,
            sampleType: "Float32");

        SetField(playback, "_renderFormat", renderFormat);
        SetField(playback, "_initialized", 1);
        EnqueueStereoFrames(playback, 0f, 1f);
        EnqueueStereoFrames(playback, 0f, -1f);

        var expected = new[] { 0f, 0.5f, 1f, 0.5f, 0f, -0.5f };
        var readFrame = playbackType.GetMethod("TryReadResampledStereoFrame", InstanceFlags)!;
        foreach (var expectedSample in expected)
        {
            object?[] arguments = { 0f, 0f };
            Assert.True((bool)readFrame.Invoke(playback, arguments)!);
            Assert.Equal(expectedSample, (float)arguments[0]!, precision: 5);
            Assert.Equal(expectedSample, (float)arguments[1]!, precision: 5);
        }

        playbackType.GetMethod("Flush", InstanceFlags)!.Invoke(playback, null);
        playbackType.GetMethod("Dispose", InstanceFlags)!.Invoke(playback, null);
    }

    [Fact]
    public void CanonicalRenderFormatAloneUsesBulkCopyFastPath()
    {
        var playbackType = RequireType("Sussudio.Services.Audio.WasapiAudioPlayback");
        var isCanonical = playbackType.GetMethod("IsCanonicalRenderFormat", StaticFlags)!;
        var canonical = CreateAudioFormat(48_000, 2, 32, 32, 8, 0x3, "Float32");
        var differentRate = CreateAudioFormat(44_100, 2, 32, 32, 8, 0x3, "Float32");

        Assert.True((bool)isCanonical.Invoke(null, new[] { canonical })!);
        Assert.False((bool)isCanonical.Invoke(null, new[] { differentRate })!);
    }

    [Fact]
    public void RenderSampleEncodingCoversEveryNegotiatedContainer()
    {
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x3F }, EncodeSample("Float32", 32, 32, 0.5f));
        Assert.Equal(
            BitConverter.GetBytes(0.5d),
            EncodeSample("Float64", 64, 64, 0.5f));
        Assert.Equal(new byte[] { 0x00, 0x40 }, EncodeSample("Pcm16", 16, 16, 0.5f));
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0x7F }, EncodeSample("Pcm24", 24, 24, 1f));
        Assert.Equal(new byte[] { 0x00, 0xFF, 0xFF, 0x7F }, EncodeSample("Pcm24", 32, 24, 1f));
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, EncodeSample("Pcm32", 32, 32, 1f));
    }

    [Fact]
    public void QuarantineBlocksOnlyUntilTheTimedOutWorkerCompletes()
    {
        var quarantineType = RequireType("Sussudio.Services.Audio.WasapiWorkerQuarantine");
        var roleType = RequireType("Sussudio.Services.Audio.WasapiWorkerRole");
        var role = Enum.Parse(roleType, "Playback");
        var owner = new object();
        var exited = new TaskCompletionSource<bool>();
        var register = quarantineType.GetMethod("Register", StaticFlags)!;
        var throwIfBlocked = quarantineType.GetMethod("ThrowIfBlocked", StaticFlags)!;

        register.Invoke(null, new object[] { role, owner, exited.Task, "WASAPI_TEST_TIMEOUT" });
        var blocked = Assert.Throws<TargetInvocationException>(() => throwIfBlocked.Invoke(null, new[] { role }));
        Assert.IsType<InvalidOperationException>(blocked.InnerException);

        exited.SetResult(true);
        throwIfBlocked.Invoke(null, new[] { role });
    }

    [Fact]
    public void WorkerFinallyOwnsNativeReleaseAndTimeoutIsFiveSeconds()
    {
        var playbackSource = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Audio/WasapiAudioPlayback.cs");
        var captureSource = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Audio/WasapiAudioCapture.cs");

        Assert.Contains("WorkerExitTimeout = TimeSpan.FromSeconds(5)", playbackSource);
        Assert.Contains("WorkerExitTimeout = TimeSpan.FromSeconds(5)", captureSource);
        Assert.Contains("finally", playbackSource);
        Assert.Contains("finally", captureSource);
        Assert.Contains("ReleaseNativeResources();\n            _workerExited?.TrySetResult(true);", Normalize(playbackSource));
        Assert.Contains("ReleaseNativeResources();\n            _workerExited?.TrySetResult(true);", Normalize(captureSource));
        Assert.DoesNotContain("_audioClient?.Stop();\n        }\n\n        var thread", Normalize(playbackSource));
        Assert.DoesNotContain("_audioClient?.Stop();\n        }\n\n        var thread", Normalize(captureSource));
    }

    [Fact]
    public void QuarantinedWorker_RequestsRecoveryAndBoundedProcessClosure()
    {
        var quarantineSource = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Audio/WasapiWorkerQuarantine.cs");
        var windowSource = RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml.cs");

        Assert.Contains("var handler = EmergencyCloseRequested;", quarantineSource, StringComparison.Ordinal);
        Assert.Contains("handler?.Invoke(", quarantineSource, StringComparison.Ordinal);
        Assert.Contains("StopRecordingForEmergencyAsync()", windowSource, StringComparison.Ordinal);
        Assert.Contains("Task.Delay(TimeSpan.FromSeconds(10))", windowSource, StringComparison.Ordinal);
        Assert.Contains("MarkRecordingFinalizationUnresolved", windowSource, StringComparison.Ordinal);
        Assert.Contains("Environment.Exit(1);", windowSource, StringComparison.Ordinal);

        var windowControllerSource = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Controllers/Window/WindowControllers.cs");
        Assert.Contains("await _context.PrepareForCloseAsync();", windowControllerSource, StringComparison.Ordinal);
        Assert.Contains("if (_context.IsEmergencyClosePending())", windowControllerSource, StringComparison.Ordinal);

        var captureSource = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Audio/WasapiAudioCapture.cs");
        Assert.Contains("WASAPI capture wait failed", captureSource, StringComparison.Ordinal);
        Assert.Contains("OnCaptureFailed(new InvalidOperationException", captureSource, StringComparison.Ordinal);
    }

    private object ParseFormat<T>(T format) where T : struct
    {
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
        try
        {
            Marshal.StructureToPtr(format, pointer, fDeleteOld: false);
            var interopType = RequireType("Sussudio.Services.Audio.WasapiComInterop");
            return interopType.GetMethod("ReadAudioFormat", StaticFlags)!.Invoke(null, new object[] { pointer })!;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private object CreateAudioFormat(
        int sampleRate,
        int channels,
        int containerBits,
        int validBits,
        int blockAlign,
        uint channelMask,
        string sampleType)
    {
        var formatType = RequireType("Sussudio.Services.Audio.WasapiAudioFormat");
        var sampleTypeEnum = RequireType("Sussudio.Services.Audio.WasapiSampleType");
        return Activator.CreateInstance(
            formatType,
            sampleRate,
            channels,
            containerBits,
            validBits,
            blockAlign,
            channelMask,
            Enum.Parse(sampleTypeEnum, sampleType))!;
    }

    private static void EnqueueStereoFrames(object playback, params float[] monoSamples)
    {
        var byteLength = monoSamples.Length * 2 * sizeof(float);
        var buffer = ArrayPool<byte>.Shared.Rent(byteLength);
        var samples = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, byteLength));
        for (var index = 0; index < monoSamples.Length; index++)
        {
            samples[index * 2] = monoSamples[index];
            samples[index * 2 + 1] = monoSamples[index];
        }

        playback.GetType().GetMethod("EnqueuePooledSamples", InstanceFlags)!
            .Invoke(playback, new object[] { buffer, byteLength, 0L });
    }

    private byte[] EncodeSample(string sampleType, int containerBits, int validBits, float sample)
    {
        var playbackType = RequireType("Sussudio.Services.Audio.WasapiAudioPlayback");
        var playback = Activator.CreateInstance(playbackType, nonPublic: true)!;
        var bytesPerSample = containerBits / 8;
        var format = CreateAudioFormat(
            48_000,
            1,
            containerBits,
            validBits,
            bytesPerSample,
            0,
            sampleType);
        SetField(playback, "_renderFormat", format);

        var method = playbackType.GetMethod("WriteOutputSample", InstanceFlags)!;
        var writeSample = (WriteOutputSampleDelegate)method.CreateDelegate(
            typeof(WriteOutputSampleDelegate),
            playback);
        var output = new byte[bytesPerSample];
        writeSample(output, 0, sample);
        playbackType.GetMethod("Dispose", InstanceFlags)!.Invoke(playback, null);
        return output;
    }

    private Type RequireType(string name) => _assembly.GetType(name, throwOnError: true)!;

    private static object ReadProperty(object instance, string name) =>
        instance.GetType().GetProperty(name, InstanceFlags)!.GetValue(instance)!;

    private static int ReadIntProperty(object instance, string name) => (int)ReadProperty(instance, name);

    private static uint ReadUIntProperty(object instance, string name) => (uint)ReadProperty(instance, name);

    private static void SetField(object instance, string name, object value) =>
        instance.GetType().GetField(name, InstanceFlags)!.SetValue(instance, value);

    private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);

    private delegate void WriteOutputSampleDelegate(Span<byte> outputFrame, int channelIndex, float sample);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TestWaveFormatEx
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSecond;
        public uint AverageBytesPerSecond;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort ExtraSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TestWaveFormatExtensible
    {
        public TestWaveFormatEx Format;
        public ushort ValidBitsPerSample;
        public uint ChannelMask;
        public Guid SubFormat;
    }
}
