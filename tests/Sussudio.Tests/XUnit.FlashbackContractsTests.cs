using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.IO;

namespace Sussudio.Tests
{

public sealed class FlashbackModelsTests
{
    private enum SetterExpectation
    {
        InitOnly,
        None
    }

    private enum NullabilityExpectation
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private sealed record PropertySpec(
        string Name,
        Type Type,
        SetterExpectation Setter,
        NullabilityExpectation Nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation ElementNullability = NullabilityExpectation.NotApplicable,
        bool IsRequired = false);

    private static PropertySpec Property(
        string name,
        Type type,
        SetterExpectation setter,
        NullabilityExpectation nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation elementNullability = NullabilityExpectation.NotApplicable,
        bool isRequired = false)
        => new(name, type, setter, nullability, elementNullability, isRequired);

    private static PropertySpec String(string name, SetterExpectation setter, NullabilityExpectation nullability)
        => Property(name, typeof(string), setter, nullability);

    private static PropertySpec RequiredString(string name, SetterExpectation setter)
        => Property(name, typeof(string), setter, NullabilityExpectation.NotNull, isRequired: true);

    private static PropertySpec RequiredProperty(string name, Type type, SetterExpectation setter)
        => Property(name, type, setter, isRequired: true);

    private static void AssertDeclaredProperties(Type type, IReadOnlyCollection<PropertySpec> expectedProperties)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var actualNames = type.GetProperties(flags)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expectedNames = expectedProperties
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedNames, actualNames);

        foreach (var expected in expectedProperties)
        {
            var property = type.GetProperty(expected.Name, flags);
            Assert.NotNull(property);
            Assert.Equal(expected.Type, property!.PropertyType);
            Assert.Equal(expected.IsRequired, property.GetCustomAttribute<RequiredMemberAttribute>() != null);
            Assert.NotNull(property.GetMethod);
            Assert.True(property.GetMethod!.IsPublic);

            if (expected.Setter == SetterExpectation.None)
            {
                Assert.Null(property.SetMethod);
            }
            else
            {
                Assert.NotNull(property.SetMethod);
                Assert.True(property.SetMethod!.IsPublic);
                Assert.True(IsInitOnlySetter(property));
            }

            AssertNullability(property, expected);
        }
    }

    private static void AssertNullability(PropertyInfo property, PropertySpec expected)
    {
        if (expected.Nullability == NullabilityExpectation.NotApplicable)
        {
            return;
        }

        var nullability = new NullabilityInfoContext().Create(property);
        var expectedState = expected.Nullability == NullabilityExpectation.Nullable
            ? NullabilityState.Nullable
            : NullabilityState.NotNull;
        Assert.Equal(expectedState, nullability.ReadState);
        if (expected.Setter != SetterExpectation.None)
        {
            Assert.Equal(expectedState, nullability.WriteState);
        }

        if (expected.ElementNullability == NullabilityExpectation.NotApplicable)
        {
            return;
        }

        var elementNullability = property.PropertyType.IsArray
            ? nullability.ElementType
            : nullability.GenericTypeArguments.FirstOrDefault();
        Assert.NotNull(elementNullability);
        var expectedElementState = expected.ElementNullability == NullabilityExpectation.Nullable
            ? NullabilityState.Nullable
            : NullabilityState.NotNull;
        Assert.Equal(expectedElementState, elementNullability!.ReadState);
        Assert.Equal(expectedElementState, elementNullability.WriteState);
    }

    private static bool IsInitOnlySetter(PropertyInfo property)
        => property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    private static Type RequireType(Assembly asm, string name)
        => asm.GetType(name, throwOnError: true)!;

    private static object CreateInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)!;

    private static object? Get(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static T Get<T>(object instance, string propertyName)
        => (T)Get(instance, propertyName)!;

    private static void Set(object instance, string propertyName, object? value)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName, ReflectionFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
            ?? type.GetField($"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}", ReflectionFlags.Instance)
            ?? type.GetField(propertyName, ReflectionFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static int Count(object value)
    {
        if (value is ICollection collection)
        {
            return collection.Count;
        }

        return ((IEnumerable)value).Cast<object>().Count();
    }

    private static void AssertEnumValues(Type enumType, params (string Name, int Value)[] expectedValues)
    {
        Assert.Equal(expectedValues.Length, Enum.GetNames(enumType).Length);
        foreach (var (name, value) in expectedValues)
        {
            Assert.Equal(value, Convert.ToInt32(Enum.Parse(enumType, name)));
        }
    }

    [Fact]
    public void FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration()
    {
        var asm = SussudioAssembly.Load();
        var optionsType = RequireType(asm, "Sussudio.Models.FlashbackBufferOptions");
        var options = CreateInstance(optionsType);

        const long safetyBytesPerSecond = 57L * 1024L * 1024L;

        Set(options, "BufferDuration", TimeSpan.FromMinutes(5));
        var maxBytes = Get<long>(options, "MaxDiskBytes");
        Assert.Equal(300L * safetyBytesPerSecond, maxBytes);

        Set(options, "BufferDuration", TimeSpan.FromMinutes(1));
        var oneMinuteBytes = Get<long>(options, "MaxDiskBytes");
        Assert.Equal(60L * safetyBytesPerSecond, oneMinuteBytes);
        Assert.Equal(maxBytes, oneMinuteBytes * 5);

        Set(options, "BufferDuration", TimeSpan.Zero);
        Assert.Equal(0L, Get<long>(options, "MaxDiskBytes"));

        Set(options, "BufferDuration", TimeSpan.FromTicks(-1));
        Assert.Equal(0L, Get<long>(options, "MaxDiskBytes"));

        Set(options, "BufferDuration", TimeSpan.MaxValue);
        Assert.Equal(long.MaxValue, Get<long>(options, "MaxDiskBytes"));
    }

    [Fact]
    public void FlashbackModels_PreserveBufferSessionExportContracts()
    {
        var asm = SussudioAssembly.Load();
        var bufferOptionsType = RequireType(asm, "Sussudio.Models.FlashbackBufferOptions");
        var sessionContextType = RequireType(asm, "Sussudio.Models.FlashbackSessionContext");
        var playbackStateType = RequireType(asm, "Sussudio.Models.FlashbackPlaybackState");
        var exportProgressType = RequireType(asm, "Sussudio.Models.ExportProgress");
        var exportSegmentType = RequireType(asm, "Sussudio.Models.FlashbackExportSegment");
        var exportRequestType = RequireType(asm, "Sussudio.Models.FlashbackExportRequest");

        AssertEnumValues(playbackStateType, ("Disabled", 0), ("Buffering", 1), ("Live", 2), ("Scrubbing", 3), ("Playing", 4), ("Paused", 5));
        AssertDeclaredProperties(
            bufferOptionsType,
            new[]
            {
                Property("BufferDuration", typeof(TimeSpan), SetterExpectation.InitOnly),
                String("TempDirectory", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                Property("SegmentDuration", typeof(TimeSpan), SetterExpectation.InitOnly),
                Property("MaxDiskBytes", typeof(long), SetterExpectation.None)
            });
        AssertDeclaredProperties(
            sessionContextType,
            new[]
            {
                RequiredProperty("Width", typeof(int), SetterExpectation.InitOnly),
                RequiredProperty("Height", typeof(int), SetterExpectation.InitOnly),
                RequiredProperty("FrameRate", typeof(double), SetterExpectation.InitOnly),
                Property("FrameRateNumerator", typeof(int?), SetterExpectation.InitOnly),
                Property("FrameRateDenominator", typeof(int?), SetterExpectation.InitOnly),
                RequiredProperty("BitRate", typeof(uint), SetterExpectation.InitOnly),
                RequiredProperty("IsP010", typeof(bool), SetterExpectation.InitOnly),
                RequiredString("CodecName", SetterExpectation.InitOnly),
                String("NvencPreset", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                String("SplitEncodeMode", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                Property("HdrEnabled", typeof(bool), SetterExpectation.InitOnly),
                Property("IsFullRangeInput", typeof(bool), SetterExpectation.InitOnly),
                String("HdrMasterDisplayMetadata", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                Property("HdrMaxCll", typeof(int), SetterExpectation.InitOnly),
                Property("HdrMaxFall", typeof(int), SetterExpectation.InitOnly),
                Property("D3D11DevicePtr", typeof(IntPtr), SetterExpectation.InitOnly),
                Property("D3D11DeviceContextPtr", typeof(IntPtr), SetterExpectation.InitOnly),
                Property("AudioEnabled", typeof(bool), SetterExpectation.InitOnly),
                Property("MicrophoneEnabled", typeof(bool), SetterExpectation.InitOnly)
            });
        AssertDeclaredProperties(
            exportProgressType,
            new[]
            {
                Property("SegmentsProcessed", typeof(int), SetterExpectation.InitOnly),
                Property("TotalSegments", typeof(int), SetterExpectation.InitOnly),
                Property("Percent", typeof(double), SetterExpectation.InitOnly)
            });
        AssertDeclaredProperties(
            exportSegmentType,
            new[]
            {
                RequiredString("Path", SetterExpectation.InitOnly),
                Property("StartPts", typeof(TimeSpan?), SetterExpectation.InitOnly),
                Property("EndPts", typeof(TimeSpan?), SetterExpectation.InitOnly)
            });
        AssertDeclaredProperties(
            exportRequestType,
            new[]
            {
                Property(
                    "Segments",
                    typeof(IReadOnlyList<>).MakeGenericType(exportSegmentType),
                    SetterExpectation.InitOnly,
                    NullabilityExpectation.Nullable,
                    NullabilityExpectation.NotNull),
                Property(
                    "SegmentPaths",
                    typeof(IReadOnlyList<string>),
                    SetterExpectation.InitOnly,
                    NullabilityExpectation.Nullable,
                    NullabilityExpectation.NotNull),
                String("InputTsPath", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                RequiredProperty("InPoint", typeof(TimeSpan), SetterExpectation.InitOnly),
                RequiredProperty("OutPoint", typeof(TimeSpan), SetterExpectation.InitOnly),
                RequiredString("OutputPath", SetterExpectation.InitOnly),
                Property("FastStart", typeof(bool), SetterExpectation.InitOnly),
                Property("Force", typeof(bool), SetterExpectation.InitOnly),
                Property("AdaptiveThrottleDelayMsProvider", typeof(Func<int>), SetterExpectation.InitOnly, NullabilityExpectation.Nullable)
            });

        var bufferOptions = CreateInstance(bufferOptionsType);
        Assert.Equal(TimeSpan.FromMinutes(5), Get<TimeSpan>(bufferOptions, "BufferDuration"));
        Assert.Equal(TimeSpan.FromMinutes(10), Get<TimeSpan>(bufferOptions, "SegmentDuration"));
        Assert.Contains("Sussudio", Get<string>(bufferOptions, "TempDirectory"), StringComparison.Ordinal);
        Assert.Contains("Flashback", Get<string>(bufferOptions, "TempDirectory"), StringComparison.Ordinal);
        Assert.Equal(300L * 57L * 1024L * 1024L, Get<long>(bufferOptions, "MaxDiskBytes"));

        var sessionContext = CreateInstance(sessionContextType);
        Set(sessionContext, "Width", 3840);
        Set(sessionContext, "Height", 2160);
        Set(sessionContext, "FrameRate", 119.88d);
        Set(sessionContext, "FrameRateNumerator", 120000);
        Set(sessionContext, "FrameRateDenominator", 1001);
        Set(sessionContext, "BitRate", 150_000_000u);
        Set(sessionContext, "IsP010", true);
        Set(sessionContext, "CodecName", "hevc_nvenc");
        Set(sessionContext, "NvencPreset", "P5");
        Set(sessionContext, "SplitEncodeMode", "2-way");
        Set(sessionContext, "HdrEnabled", true);
        Set(sessionContext, "IsFullRangeInput", true);
        Set(sessionContext, "HdrMasterDisplayMetadata", "G(13250,34500)");
        Set(sessionContext, "HdrMaxCll", 1000);
        Set(sessionContext, "HdrMaxFall", 400);
        Set(sessionContext, "D3D11DevicePtr", new IntPtr(123));
        Set(sessionContext, "D3D11DeviceContextPtr", new IntPtr(456));
        Set(sessionContext, "AudioEnabled", true);
        Set(sessionContext, "MicrophoneEnabled", true);
        Assert.Equal(3840, Get<int>(sessionContext, "Width"));
        Assert.Equal("hevc_nvenc", Get<string>(sessionContext, "CodecName"));
        Assert.Equal("2-way", Get<string>(sessionContext, "SplitEncodeMode"));
        Assert.Equal(new IntPtr(456), Get<IntPtr>(sessionContext, "D3D11DeviceContextPtr"));

        var progress = Activator.CreateInstance(exportProgressType, 3, 10, 30d)!;
        Assert.Equal(3, Get<int>(progress, "SegmentsProcessed"));
        Assert.Equal(10, Get<int>(progress, "TotalSegments"));
        Assert.Equal(30d, Get<double>(progress, "Percent"));

        var exportSegment = CreateInstance(exportSegmentType);
        Set(exportSegment, "Path", "segment.mp4");
        Set(exportSegment, "StartPts", TimeSpan.FromSeconds(5));
        Set(exportSegment, "EndPts", TimeSpan.FromSeconds(15));
        Assert.Equal("segment.mp4", Get<string>(exportSegment, "Path"));
        Assert.Equal(TimeSpan.FromSeconds(5), Get<TimeSpan>(exportSegment, "StartPts"));
        Assert.Equal(TimeSpan.FromSeconds(15), Get<TimeSpan>(exportSegment, "EndPts"));

        var exportRequest = CreateInstance(exportRequestType);
        Assert.True(Get<bool>(exportRequest, "FastStart"));
        Assert.False(Get<bool>(exportRequest, "Force"));
        var exportSegments = Array.CreateInstance(exportSegmentType, 1);
        exportSegments.SetValue(exportSegment, 0);
        Set(exportRequest, "Segments", exportSegments);
        Set(exportRequest, "SegmentPaths", new[] { "a.ts", "b.ts" });
        Set(exportRequest, "InputTsPath", "single.ts");
        Set(exportRequest, "InPoint", TimeSpan.FromSeconds(2));
        Set(exportRequest, "OutPoint", TimeSpan.FromSeconds(12));
        Set(exportRequest, "OutputPath", "clip.mp4");
        Set(exportRequest, "FastStart", false);
        Assert.Equal(1, Count(Get(exportRequest, "Segments")!));
        Assert.Equal(2, Count(Get(exportRequest, "SegmentPaths")!));
        Assert.Equal("single.ts", Get<string>(exportRequest, "InputTsPath"));
        Assert.Equal(TimeSpan.FromSeconds(12), Get<TimeSpan>(exportRequest, "OutPoint"));
        Assert.False(Get<bool>(exportRequest, "FastStart"));
        Assert.Null(Get(exportRequest, "AdaptiveThrottleDelayMsProvider"));
    }

    [Fact]
    public void FlashbackPlaybackState_HasAllExpectedStates()
    {
        var enumType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.FlashbackPlaybackState");

        Assert.Equal(
            new[] { "Disabled", "Buffering", "Live", "Scrubbing", "Playing", "Paused" },
            Enum.GetNames(enumType));
    }
}

public sealed class FlashbackDecoderContractsTests
{
    public FlashbackDecoderContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackDecoderCalculatesNv12FrameBufferSizes()
        => global::Program.FlashbackDecoder_CalculateFrameBufferSize_Nv12();

    [Fact]
    public Task FlashbackDecoderCalculatesP010FrameBufferSizes()
        => global::Program.FlashbackDecoder_CalculateFrameBufferSize_P010();

    [Fact]
    public Task FlashbackDecoderValidationHelpersLiveWithRootLifecycle()
        => global::Program.FlashbackDecoder_ValidationHelpersLiveWithRootLifecycle();

    [Fact]
    public Task FlashbackDecoderLifetimeCleanupLivesWithRootLifecycle()
        => global::Program.FlashbackDecoder_LifetimeCleanupLivesWithRootLifecycle();

    [Fact]
    public Task FlashbackDecoderStateGuardsAndTimingLiveWithOwners()
        => global::Program.FlashbackDecoder_StateGuardsAndTimingLiveWithOwners();

    [Fact]
    public Task FlashbackDecoderOutputTypesLiveWithDecoderRoot()
        => global::Program.FlashbackDecoder_OutputTypesLiveWithDecoderRoot();

    [Fact]
    public Task FlashbackDecoderVideoSetupOwnsHardwareAndSoftwareSetup()
        => global::Program.FlashbackDecoder_VideoSetupOwnsHardwareAndSoftwareSetup();

    [Fact]
    public Task FlashbackDecoderPlaybackOwnsSeekingAndDecodeLoop()
        => global::Program.FlashbackDecoder_PlaybackOwnsSeekingAndDecodeLoop();

    [Fact]
    public Task FlashbackDecoderDecodeLoopLivesWithPlayback()
        => global::Program.FlashbackDecoder_DecodeLoopLivesWithPlayback();

    [Fact]
    public Task FlashbackDecoderDefaultsToClosedState()
        => global::Program.FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized();

    [Fact]
    public Task FlashbackDecoderDisposeBeforeInitializeIsSafe()
        => global::Program.FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow();

    [Fact]
    public Task FlashbackDecoderUnreferencesDiscardedAudioFrames()
        => global::Program.FlashbackDecoder_DiscardedAudioFramesAreUnreffed();

    [Fact]
    public Task FlashbackDecoderMjpegPlaybackUsesLowLatencySingleThreadDecode()
        => global::Program.FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode();

    [Fact]
    public Task FlashbackDecoderRejectsInvalidTimestamps()
        => global::Program.FlashbackDecoder_PtsConversionRejectsInvalidTimestamps();

    [Fact]
    public Task FlashbackDecoderInputStreamsAndFrameSizesAreBounded()
        => global::Program.FlashbackDecoder_InputStreamsAndFrameSizesAreBounded();

    [Fact]
    public Task FlashbackDecoderAudioOutputBuffersAreBounded()
        => global::Program.FlashbackDecoder_AudioOutputBuffersAreBounded();

    [Fact]
    public Task FlashbackDecoderAudioSetupLivesWithPlaybackPacketFeed()
        => global::Program.FlashbackDecoder_AudioSetupLivesWithPlaybackPacketFeed();

    [Fact]
    public Task FlashbackDecoderSoftwareFramePlanesAreValidated()
        => global::Program.FlashbackDecoder_SoftwareFramePlanesAreValidated();

    [Fact]
    public Task FlashbackDecoderD3D11FramesAreValidated()
        => global::Program.FlashbackDecoder_D3D11FramesAreValidated();

    [Fact]
    public Task FlashbackDecoderHeldFrameCleanupIsBestEffort()
        => global::Program.FlashbackDecoder_HeldFrameCleanupIsBestEffort();

    [Fact]
    public Task FlashbackDecoderDecodeLoopsObserveCancellation()
        => global::Program.FlashbackDecoder_DecodeLoopsObserveCancellation();

    [Fact]
    public Task FlashbackDecoderRejectsInitializeAfterDispose()
        => global::Program.FlashbackDecoder_RejectsInitializeAfterDispose();

    [Fact]
    public Task FlashbackDecoderClearsAudioCallbackOnDispose()
        => global::Program.FlashbackDecoder_ClearsAudioCallbackOnDispose();
}

public sealed class FlashbackEncoderSinkContractsTests
{
    public FlashbackEncoderSinkContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackEncoderResolvesFractionalFrameRates()
        => global::Program.FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates();

    [Fact]
    public Task FlashbackEncoderMapsCodecNames()
        => global::Program.FlashbackEncoderSink_MapCodecName_MapsFormats();

    [Fact]
    public Task FlashbackEncoderCountersDefaultToZero()
        => global::Program.FlashbackEncoderSink_CountersDefaultToZero();

    [Fact]
    public Task FlashbackEncoderBoundsHighResolutionCpuQueueCapacity()
        => global::Program.FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded();

    [Fact]
    public Task FlashbackExportThrottleRespondsToLiveQueuePressure()
        => global::Program.CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure();

    [Fact]
    public Task FlashbackEncoderForceRotateDrainRejectsVideoEnqueues()
        => global::Program.FlashbackEncoderSink_ForceRotateDrainingRejectsVideoAndGpuEnqueues();

    [Fact]
    public Task FlashbackEncoderStartFailureRollsBackStartedState()
        => global::Program.FlashbackEncoderSink_StartFailureRollsBackStartedState();

    [Fact]
    public Task FlashbackEncoderDisposeResetsGpuQueueDepth()
        => global::Program.FlashbackEncoderSink_DisposeResetsGpuQueueDepth();

    [Fact]
    public Task FlashbackEncoderPtsGuardsInvalidFrameRates()
        => global::Program.FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate();

    [Fact]
    public Task FlashbackEncoderSinkRestoresActiveSegmentAfterRotationFailure()
        => global::Program.FlashbackEncoderSink_RotateFailureRestoresActiveSegment();

    [Fact]
    public Task FlashbackEncoderSinkRegistersSegmentsOnCancellationAndRotationFailure()
        => global::Program.FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure();

    [Fact]
    public Task FlashbackEncoderSinkRejectsForceRotateAfterEncoderFailure()
        => global::Program.FlashbackEncoderSink_ForceRotateRejectsFailedEncoder();

    [Fact]
    public Task FlashbackEncoderSinkSkipsCompletedForceRotateRequests()
        => global::Program.FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest();

    [Fact]
    public Task FlashbackEncoderSinkLogsFatalSegmentRegistrationFailures()
        => global::Program.FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged();

    [Fact]
    public Task FlashbackEncoderSinkValidatesAudioPacketsBeforeRent()
        => global::Program.FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent();

    [Fact]
    public Task FlashbackEncoderSinkInterleavesAudioWithBoundedVideoBatches()
        => global::Program.FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches();

    [Fact]
    public Task FlashbackEncoderSinkEncodingThreadWorkLivesInEncodingLoop()
        => global::Program.FlashbackEncoderSink_EncodingThreadWorkLivesInEncodingLoop();

    [Fact]
    public Task FlashbackEncoderSinkQueueingOwnsInputsAndCleanup()
        => global::Program.FlashbackEncoderSink_QueueingOwnsInputsAndCleanup();

    [Fact]
    public Task FlashbackEncoderSinkStartupLivesInFocusedPartial()
        => global::Program.FlashbackEncoderSink_StartupLivesInFocusedPartial();

    [Fact]
    public Task FlashbackEncoderSinkRootOwnsConstructionAndRuntimeSurface()
        => global::Program.FlashbackEncoderSink_RootOwnsConstructionAndRuntimeSurface();

    [Fact]
    public Task FlashbackEncoderSinkForceRotateLivesWithEncodingLoop()
        => global::Program.FlashbackEncoderSink_ForceRotateLivesWithEncodingLoop();

    [Fact]
    public Task FlashbackEncoderSinkStopAndDisposeLifecyclesShareShutdownOwner()
        => global::Program.FlashbackEncoderSink_StopAndDisposeLifecyclesShareShutdownOwner();

    [Fact]
    public Task FlashbackEncoderSinkProducerInputsLiveInCohesivePartial()
        => global::Program.FlashbackEncoderSink_ProducerInputsLiveInCohesivePartial();

    [Fact]
    public Task FlashbackEncoderSinkRuntimeStateLivesWithRoot()
        => global::Program.FlashbackEncoderSink_RuntimeStateLivesWithRoot();

    [Fact]
    public Task FlashbackEncoderSinkRecordingLifecycleLivesWithRootRuntimeSurface()
        => global::Program.FlashbackEncoderSink_RecordingLifecycleLivesWithRootRuntimeSurface();

    [Fact]
    public Task FlashbackEncoderSinkOptionsHelpersLiveWithStartup()
        => global::Program.FlashbackEncoderSink_OptionsHelpersLiveWithStartup();
}

public sealed class FlashbackExporterContractsTests
{
    public FlashbackExporterContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackSuppressedExceptionsUseAppLogs()
        => global::Program.FlashbackSuppressedExceptionsUseAppLogs();

    [Fact]
    public Task FlashbackExporterCleanupIgnoresNonexistentDirectories()
        => global::Program.FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory();

    [Fact]
    public Task FlashbackExporterCleanupDeletesOrphanedTempFiles()
        => global::Program.FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles();

    [Fact]
    public Task FlashbackExporterDoesNotScanUserOutputDirectoryForOrphans()
        => global::Program.FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans();

    [Fact]
    public Task FlashbackExporterTaskWrappersDisposeLinkedCancellation()
        => global::Program.FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation();

    [Fact]
    public Task FlashbackExporterOwnershipIsSplitAcrossFocusedPartials()
        => global::Program.FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials();

    [Fact]
    public Task FlashbackExporterRejectsNullRequests()
        => global::Program.FlashbackExporter_RejectsNullRequests();

    [Fact]
    public Task FlashbackExporterFailsWhenInputFileIsMissing()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound();

    [Fact]
    public Task FlashbackExporterFailsWhenOutputPathIsEmpty()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty();

    [Fact]
    public Task FlashbackExporterFailsWhenNoSegmentPathsAreProvided()
        => global::Program.FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments();

    [Fact]
    public Task FlashbackExporterOutputPathValidationReturnsFailure()
        => global::Program.FlashbackExporter_OutputPathValidation_ReturnsFailure();

    [Fact]
    public Task FlashbackExportFailureClassifierMapsCommandFailures()
        => global::Program.FlashbackExportFailureClassifier_MapsCommandFailures();

    [Fact]
    public Task FlashbackExporterRejectsDirectoryOutputPaths()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory();

    [Fact]
    public Task FlashbackExporterRejectsInvalidExportRanges()
        => global::Program.FlashbackExporter_RejectsInvalidExportRanges();

    [Fact]
    public Task FlashbackRejectedExportDiagnosticsPreserveAttemptedRange()
        => global::Program.FlashbackExportRejectedDiagnostics_PreserveAttemptedRange();

    [Fact]
    public Task FlashbackExporterRejectsEmptySegmentPaths()
        => global::Program.FlashbackExporter_RejectsEmptySegmentPaths();

    [Fact]
    public Task FlashbackExporterRejectsDuplicateSegmentPaths()
        => global::Program.FlashbackExporter_RejectsDuplicateSegmentPaths();

    [Fact]
    public Task FlashbackExporterProgressCallbacksAreBestEffort()
        => global::Program.FlashbackExporter_ProgressCallbacksAreBestEffort();

    [Fact]
    public Task FlashbackExporterReleasesBufferedSegmentPacketsOnFailures()
        => global::Program.FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures();

    [Fact]
    public Task FlashbackExporterTimestampConversionsAreSaturating()
        => global::Program.FlashbackExporter_TimestampConversionsAreSaturating();

    [Fact]
    public Task FlashbackExporterInputStreamCountsAreBounded()
        => global::Program.FlashbackExporter_InputStreamCountsAreBounded();

    [Fact]
    public Task FlashbackExporterSegmentTemplateValidationGuardsMissingVideoStreams()
        => global::Program.FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream();

    [Fact]
    public Task FlashbackExporterFailsWhenRequestedSegmentsAreSkipped()
        => global::Program.FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped();

    [Fact]
    public Task FlashbackExporterReturnsCancellationResultWhileWaitingForExportLock()
        => global::Program.FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled();

    [Fact]
    public Task FlashbackExporterCancellationWinsBeforeValidation()
        => global::Program.FlashbackExporter_CancellationWinsBeforeValidation();

    [Fact]
    public Task FlashbackExporterFailsFastWhenSegmentFilesAreGone()
        => global::Program.FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone();

    [Fact]
    public Task FlashbackExporterDisposeTimeoutDoesNotTearDownActiveNativeState()
        => global::Program.FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState();

    [Fact]
    public Task FlashbackExporterRejectsOutputPathsThatOverwriteSourceSegments()
        => global::Program.FlashbackExporter_RejectsOutputPathThatOverwritesSource();

    [Fact]
    public Task FlashbackExporterInvalidTempOutputPreservesExistingExports()
        => global::Program.FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport();

    [Fact]
    public Task FlashbackExporterRefusesToOverwriteExistingDestinationWhenForceIsFalse()
        => global::Program.FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse();

    [Fact]
    public Task FlashbackExporterOverwritesExistingDestinationWhenForceIsTrue()
        => global::Program.FlashbackExporter_OverwritesWhenForceTrue();

    [Fact]
    public Task FlashbackExporterDeletesInvalidMovedFinalOutputs()
        => global::Program.FlashbackExporter_FinalValidationFailureDeletesMovedOutput();

    [Fact]
    public Task FlashbackExporterRejectsBlockedTempOutputPathsBeforeNativeExport()
        => global::Program.FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport();
}

public sealed class FlashbackPlaybackContractsTests
{
    public FlashbackPlaybackContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackPlaybackInitialStateIsLive()
        => global::Program.FlashbackPlaybackController_InitialState_IsLive();

    [Fact]
    public Task FlashbackPlaybackCommandsNoOpBeforeInitialize()
        => global::Program.FlashbackPlaybackController_CommandsNoOpBeforeInitialize();

    [Fact]
    public Task FlashbackPlaybackSuccessfulNoOpsClearStaleFailures()
        => global::Program.FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure();

    [Fact]
    public Task FlashbackPlaybackCoalescedCommandsClearStaleFailures()
        => global::Program.FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure();

    [Fact]
    public Task FlashbackPlaybackWorkerExitRearmsFutureCommands()
        => global::Program.FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart();

    [Fact]
    public Task FlashbackPlaybackCommandQueueAcceptsNewestControlWhenFull()
        => global::Program.FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull();

    [Fact]
    public Task FlashbackCommandPositionsClampBeforeFileLookup()
        => global::Program.FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup();

    [Fact]
    public Task FlashbackPlaybackTimestampArithmeticIsSaturating()
        => global::Program.FlashbackPlaybackController_TimestampArithmeticIsSaturating();

    [Fact]
    public Task FlashbackEndOfSegmentOpenFailuresSnapLive()
        => global::Program.FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive();

    [Fact]
    public Task FlashbackNormalPlaybackUsesTightNearLiveSnap()
        => global::Program.FlashbackPlaybackController_NormalPlaybackUsesTightNearLiveSnap();

    [Fact]
    public Task FlashbackSnapLiveClearsOpenFileIdentity()
        => global::Program.FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity();

    [Fact]
    public Task FlashbackPauseFromLiveDisplaysBufferedFrameBeforePaused()
        => global::Program.FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused();

    [Fact]
    public Task FlashbackPlaybackGuardsInvalidDecoderFrameRates()
        => global::Program.FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps();

    [Fact]
    public Task FlashbackPlaybackPtsCadenceTelemetryTracksMismatches()
        => global::Program.FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches();

    [Fact]
    public Task FlashbackNudgeOpensDecoderAfterPauseFromLive()
        => global::Program.FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused();

    [Fact]
    public Task FlashbackPlaybackReleasesDecodedFramesAfterSubmitFailures()
        => global::Program.FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames();

    [Fact]
    public Task FlashbackPlaybackGuardsFmp4ReopenRetries()
        => global::Program.FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded();

    [Fact]
    public Task FlashbackPlaybackInOutPointsDefaultToUnset()
        => global::Program.FlashbackPlaybackController_InOutPoints_DefaultToUnset();

    [Fact]
    public Task FlashbackPlaybackInOutPointsClearInvalidCounterpart()
        => global::Program.FlashbackPlaybackController_InOutPoints_ClearInvalidCounterpart();

    [Fact]
    public Task FlashbackPlaybackInOutPointSettersNormalizeMarkers()
        => global::Program.FlashbackPlaybackController_InOutPointSettersNormalizeMarkers();

    [Fact]
    public Task FlashbackPlaybackInOutPointChangesStopAfterDispose()
        => global::Program.FlashbackPlaybackController_InOutPointChangesStopAfterDispose();

    [Fact]
    public Task FlashbackPlaybackClampPositionBoundsMarkersToBufferedDuration()
        => global::Program.FlashbackPlaybackController_ClampPosition_BoundsMarkersToBufferedDuration();

    [Fact]
    public Task FlashbackScrubCoalescingDoesNotRequeueControlCommands()
        => global::Program.FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands();

    [Fact]
    public Task FlashbackSeekSlotsPreserveControlCommandBarriers()
        => global::Program.FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers();

    [Fact]
    public Task FlashbackSeekSlotsPreserveSlotStateAfterRejectedBarriers()
        => global::Program.FlashbackPlaybackController_SeekSlots_PreserveSlotStateAfterRejectedBarriers();

    [Fact]
    public Task FlashbackPlaybackTransitionsUseBestEffortAudioPreviewGuards()
        => global::Program.FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards();

    [Fact]
    public Task FlashbackPlaybackMetricResetClearsDecodeTimings()
        => global::Program.FlashbackPlaybackController_ResetClearsDecodeMetrics();
}
}

static partial class Program
{
    internal static Task FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var method = sinkType.GetMethod("ResolveFrameRateParts", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFrameRateParts not found.");

        // "60000/1001" â†’ (60000, 1001)
        var result1 = method.Invoke(null, new object[] { "60000/1001" });
        var (num1, den1) = GetTupleValues(result1!);
        AssertEqual(60000, num1, "60000/1001 numerator");
        AssertEqual(1001, den1, "60000/1001 denominator");

        // "30/1" â†’ (30, 1)
        var result2 = method.Invoke(null, new object[] { "30/1" });
        var (num2, den2) = GetTupleValues(result2!);
        AssertEqual(30, num2, "30/1 numerator");
        AssertEqual(1, den2, "30/1 denominator");

        // null â†’ (null, null)
        var result3 = method.Invoke(null, new object?[] { null });
        var (num3, den3) = GetNullableTupleValues(result3!);
        if (num3 != null)
            throw new InvalidOperationException($"Expected null numerator for null input, got {num3}");

        // Empty string â†’ (null, null)
        var result4 = method.Invoke(null, new object[] { "" });
        var (num4, den4) = GetNullableTupleValues(result4!);
        if (num4 != null)
            throw new InvalidOperationException($"Expected null numerator for empty input, got {num4}");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_MapCodecName_MapsFormats()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var method = sinkType.GetMethod("MapCodecName", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MapCodecName not found.");

        var formatType = RequireType("Sussudio.Models.RecordingFormat");

        var hevc = method.Invoke(null, new[] { Enum.Parse(formatType, "HevcMp4") })?.ToString();
        AssertContains(hevc ?? "", "hevc");

        var h264 = method.Invoke(null, new[] { Enum.Parse(formatType, "H264Mp4") })?.ToString();
        AssertContains(h264 ?? "", "264");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_CountersDefaultToZero()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var ctor = sinkType.GetConstructor(new[] { optionsType })
            ?? throw new InvalidOperationException("FlashbackEncoderSink(FlashbackBufferOptions) constructor not found.");
        var sink = ctor.Invoke(new object?[] { null })!;

        AssertEqual(0L, GetLongProperty(sink, "DroppedVideoFrames"), "DroppedVideoFrames");
        AssertEqual(0L, GetLongProperty(sink, "EncodedVideoFrames"), "EncodedVideoFrames");
        AssertEqual(0L, GetLongProperty(sink, "AudioSamplesReceived"), "AudioSamplesReceived");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var contextType = RequireType("Sussudio.Models.FlashbackSessionContext");
        var resolve = sinkType.GetMethod("ResolveVideoQueueCapacity", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveVideoQueueCapacity not found.");

        var fourKContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(fourKContext, "Width", 3840);
        SetPropertyBackingField(fourKContext, "Height", 2160);
        var normalContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(normalContext, "Width", 1920);
        SetPropertyBackingField(normalContext, "Height", 1080);

        AssertEqual(128, (int)resolve.Invoke(null, new[] { fourKContext, false })!, "4K CPU Flashback queue capacity");
        AssertEqual(180, (int)resolve.Invoke(null, new[] { fourKContext, true })!, "4K GPU Flashback queue capacity");
        AssertEqual(180, (int)resolve.Invoke(null, new[] { normalContext, false })!, "1080p CPU Flashback queue capacity");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_StartFailureRollsBackStartedState()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();
        var startupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupRollbackText = startupText;

        var startCatchBlock = ExtractTextBetween(
            startupText,
            "catch (Exception ex)\n        {",
            "            throw;\n        }");
        var rollbackBlock = ExtractTextBetween(
            startupRollbackText,
            "private void RollBackStartFailure(Exception ex, string? startupGeneratedSegmentPath)\n    {",
            "\n    private static int ResolveVideoQueueCapacity");

        AssertContains(sourceText, "ValidateSessionContext(context);");
        AssertContains(sourceText, "if (ptsBaseOffset < TimeSpan.Zero)\n        {\n            throw new ArgumentOutOfRangeException(nameof(ptsBaseOffset), \"PTS base offset must not be negative.\");\n        }");
        AssertOccursBefore(sourceText, "ValidateSessionContext(context);", "_started = true;");
        AssertOccursBefore(sourceText, "PTS base offset must not be negative.", "_started = true;");
        AssertContains(sourceText, "private static void ValidateSessionContext(FlashbackSessionContext context)");
        AssertContains(sourceText, "Flashback session width must be positive.");
        AssertContains(sourceText, "Flashback session height must be positive.");
        AssertContains(sourceText, "Flashback session codec name is required.");
        AssertContains(sourceText, "if (_started || _encodingTask is { IsCompleted: false })");
        AssertContains(startCatchBlock, "RollBackStartFailure(ex, startupGeneratedSegmentPath);");
        AssertContains(rollbackBlock, "Logger.Log($\"FLASHBACK_SINK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(rollbackBlock, "lock (_sync)\n        {\n            _started = false;\n        }");
        AssertEqual(1, rollbackBlock.Split("_started = false;", StringSplitOptions.None).Length - 1, "Start failure rollback clears started state once");
        AssertOccursBefore(sourceText, "_started = false;", "    public bool IsForceRotateActive =>");
        AssertContains(rollbackBlock, "_tsFilePath = null;\n        _recordingOutputPath = string.Empty;\n        _segmentStartPts = TimeSpan.Zero;\n        _segmentDuration = TimeSpan.Zero;\n        _ptsBaseOffset = TimeSpan.Zero;\n        Interlocked.Exchange(ref _segmentStartBytes, 0);");
        AssertContains(sourceText, "var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);");
        AssertContains(sourceText, "startupGeneratedSegmentPath = tsPath;");
        AssertContains(rollbackBlock, "DisposeEncoderBestEffort(\"start_fail\");");
        AssertContains(rollbackBlock, "else if (startupGeneratedSegmentPath != null)\n        {\n            _bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);\n        }");
        AssertOccursBefore(rollbackBlock, "DisposeEncoderBestEffort(\"start_fail\");", "_bufferManager.PurgeAllSegments();");
        AssertOccursBefore(rollbackBlock, "DisposeEncoderBestEffort(\"start_fail\");", "_bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(_encoder.NextVideoPts / frameRate)");
        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(_encoder.NextVideoPts / finalFrameRate)");
        AssertDoesNotContain(sourceText, "ptsBaseOffset.TotalSeconds * context.FrameRate");
        AssertContains(sourceText, "var sessionFrameRate = ResolveSessionFrameRate(context.FrameRate);");
        AssertContains(sourceText, "var sessionContext = context with { FrameRate = sessionFrameRate };");
        AssertContains(sourceText, "_encoder.Initialize(CreateOptions(sessionContext, tsPath));");
        AssertContains(sourceText, "_bufferManager.EncodeFrameRate = sessionFrameRate;");
        AssertContains(sourceText, "private const double FallbackSessionFrameRate = 30.0;");
        AssertContains(sourceText, "private const double MaxSessionFrameRate = 1000.0;");
        AssertContains(sourceText, "var currentPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var finalPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var crashPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var pts = ResolveEncoderPts();");
        AssertContains(sourceText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(sourceText, "var frameRate = ResolveSessionFrameRate(_sessionContext?.FrameRate ?? 30.0);");
        AssertContains(sourceText, "if (!double.IsFinite(seconds) || seconds <= 0)");
        AssertContains(sourceText, "if (!double.IsFinite(frameRate) || frameRate <= 0)\n        {\n            return FallbackSessionFrameRate;\n        }");
        AssertContains(sourceText, "return Math.Min(frameRate, MaxSessionFrameRate);");
        AssertContains(sourceText, "private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0 || fps > MaxSessionFrameRate)");
        AssertContains(sourceText, "FrameRateNumerator = frameRateNumerator,");
        AssertContains(sourceText, "FrameRateDenominator = frameRateDenominator,");
        AssertContains(sourceText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(sourceText, "private static long NonNegativeByteDelta(long currentBytes, long startBytes)");
        AssertContains(sourceText, "private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)");
        AssertContains(sourceText, "private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(\n        FlashbackBufferManager bufferManager,\n        string operation)");
        AssertContains(sourceText, "return bufferManager.ResumeEviction();");
        AssertContains(sourceText, "FLASHBACK_SINK_EVICTION_RESUME_WARN");
        AssertContains(sourceText, "return (bufferManager.RecordingStartPts, bufferManager.RecordingEndPts);");
        AssertContains(sourceText, "var finalSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(sourceText, "var crashSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(sourceText, "var segmentBytes = NonNegativeByteDelta(result.PreviousTotalBytes, Interlocked.Read(ref _segmentStartBytes));");
        AssertDoesNotContain(sourceText, "_encoder.TotalBytesWritten - Interlocked.Read(ref _segmentStartBytes)");
        AssertDoesNotContain(sourceText, "result.PreviousTotalBytes - Interlocked.Read(ref _segmentStartBytes)");
        AssertDoesNotContain(sourceText, "LastRecordingEndPts - LastRecordingStartPts");

        return Task.CompletedTask;
    }



    internal static Task FlashbackEncoderSink_ForceRotateDrainingRejectsVideoAndGpuEnqueues()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var ctor = sinkType.GetConstructor(new[] { optionsType })
            ?? throw new InvalidOperationException("FlashbackEncoderSink(FlashbackBufferOptions) constructor not found.");
        var sink = ctor.Invoke(new object?[] { null })!;
        var rejectReason = sinkType.GetMethod("GetVideoEnqueueRejectReason", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetVideoEnqueueRejectReason not found.");

        try
        {
            SetPrivateField(sink, "_started", true);
            SetPrivateField(sink, "_forceRotateDraining", true);

            AssertEqual("force_rotate_draining", rejectReason.Invoke(sink, new object[] { false }) as string, "Force-rotate draining rejects CPU video");
            AssertEqual("force_rotate_draining", rejectReason.Invoke(sink, new object[] { true }) as string, "Force-rotate draining rejects GPU video");

            SetPrivateField(sink, "_forceRotateDraining", false);
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { false }) as string, "CPU video accepted after force-rotate drain clears");
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { true }) as string, "GPU video accepted after force-rotate drain clears");
        }
        finally
        {
            (sink as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_DisposeResetsGpuQueueDepth()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);");
        AssertContains(sourceText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(sourceText, "Interlocked.Exchange(ref queueDepth, 0);");
        AssertContains(sourceText, "var timeoutFailure = new TimeoutException(\"Flashback encode drain timed out while stopping.\");");
        AssertContains(sourceText, "_encodingFailure ??= timeoutFailure;");
        AssertContains(sourceText, "_encodingFailure ??= ex;");
        AssertContains(sourceText, "CancelEncodingCts(\"dispose\");");
        AssertContains(sourceText, "CancelEncodingCts(\"stop_timeout\");");
        AssertContains(sourceText, "private void CancelEncodingCts(string operation)");
        AssertContains(sourceText, "FLASHBACK_SINK_CANCEL_WARN");
        AssertContains(sourceText, "DisposeCtsBestEffort(_cts, \"start_fail\");");
        AssertContains(sourceText, "DisposeCtsBestEffort(_cts, \"finalize_dispose\");");
        AssertContains(sourceText, "DisposeWorkAvailableBestEffort(\"finalize_dispose\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"start_fail\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"finalize_dispose\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"encoding_loop_fatal\");");
        AssertContains(sourceText, "FLASHBACK_SINK_CTS_DISPOSE_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_WORK_SIGNAL_DISPOSE_WARN");
        AssertContains(sourceText, "private void SignalWork(string operation)");
        AssertContains(sourceText, "FLASHBACK_SINK_WORK_SIGNAL_SKIPPED");
        AssertContains(sourceText, "SignalWork(\"force_rotate_idle\");");
        AssertContains(sourceText, "SignalWork(\"force_rotate_request\");");
        AssertEqual(1, sourceText.Split("_workAvailable.Set();", StringSplitOptions.None).Length - 1, "All work-signal wakeups go through SignalWork");
        AssertContains(sourceText, "FLASHBACK_SINK_ENCODER_DISPOSE_WARN");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_SINK_BUFFER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_RECORDING_FAIL type={failure.GetType().Name} error='{failure.Message}'\");");
        AssertContains(sourceText, "ReturnVideoPacketBestEffort(packet);");
        AssertContains(sourceText, "ReleaseGpuTextureBestEffort(packet.Texture);");
        AssertContains(sourceText, "FLASHBACK_SINK_RETURN_VIDEO_PACKET_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_RELEASE_GPU_PACKET_WARN");
        AssertContains(sourceText, "public long VideoQueueRejectedFrames => Interlocked.Read(ref _videoQueueRejectedFrames);");
        AssertContains(sourceText, "public string? LastVideoQueueRejectReason => Volatile.Read(ref _lastVideoQueueRejectReason);");
        AssertContains(sourceText, "public long GpuQueueRejectedFrames => Interlocked.Read(ref _gpuQueueRejectedFrames);");
        AssertContains(sourceText, "public string? LastGpuQueueRejectReason => Volatile.Read(ref _lastGpuQueueRejectReason);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _videoQueueRejectedFrames, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _lastVideoQueueRejectReason, null);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _gpuQueueRejectedFrames, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _lastGpuQueueRejectReason, null);");
        AssertContains(sourceText, "private const double ForceRotateQueueGuardRatio = 0.65;");
        AssertContains(sourceText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(sourceText, "private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)");
        AssertContains(sourceText, "private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)");
        AssertContains(sourceText, "return \"force_rotate_draining\";");
        AssertDoesNotContain(sourceText, "return \"force_rotate_queue_guard\";");
        AssertContains(sourceText, "private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)");
        AssertContains(sourceText, "queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio)");
        AssertContains(sourceText, "return \"cancelled\";");
        AssertContains(sourceText, "return \"disposed\";");
        AssertContains(sourceText, "return \"not_started\";");
        AssertContains(sourceText, "return \"queue_null\";");
        AssertContains(sourceText, "return \"invalid_expected_size\";");
        AssertContains(sourceText, "return dataIsEmpty ? \"data_empty\" : null;");
        AssertContains(sourceText, "return texture == IntPtr.Zero ? \"null_texture\" : null;");
        AssertContains(sourceText, "TrackGpuQueueRejected(\"invalid_subresource\");");
        AssertContains(sourceText, "? $\"encoding_failed:{failure.GetType().Name}\"");
        AssertContains(sourceText, "private void TrackVideoQueueRejected(string reason)");
        AssertContains(sourceText, "private void TrackGpuQueueRejected(string reason)");
        AssertContains(sourceText, "FLASHBACK_SINK_VIDEO_QUEUE_REJECT");
        AssertContains(sourceText, "FLASHBACK_SINK_GPU_QUEUE_REJECT");
        AssertContains(sourceText, "total == 1 || total % 30 == 0");
        AssertContains(sourceText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(sourceText, "var depth = Interlocked.Increment(ref _videoQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "AtomicMax.Update(ref _videoQueueMaxDepth, depth);");
        AssertContains(sourceText, "DecrementQueueDepth(ref _videoQueueDepth, \"video_write_failed\");");
        AssertContains(sourceText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(sourceText, "var depth = Interlocked.Increment(ref _gpuQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "AtomicMax.Update(ref _gpuQueueMaxDepth, depth);");
        AssertContains(sourceText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu_write_failed\");");
        AssertContains(sourceText, "private static bool TryWriteAudioPacket(");
        AssertContains(sourceText, "Interlocked.Increment(ref queueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "DecrementQueueDepth(ref queueDepth, $\"{queueName}_write_failed\");");
        AssertContains(sourceText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(sourceText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio_after_evict\")");
        AssertContains(sourceText, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertContains(sourceText, "var current = Volatile.Read(ref target);");
        AssertContains(sourceText, "if (current <= 0)");
        AssertContains(sourceText, "if (Interlocked.CompareExchange(ref target, current - 1, current) == current)");
        AssertContains(sourceText, "FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertContains(sourceText, "DecrementQueueDepth(ref _videoQueueDepth, \"video\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _audioQueueDepth, \"audio\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _microphoneQueueDepth, \"microphone\");");
        AssertDoesNotContain(sourceText, "private bool WaitForBackpressureRetryCancellation()");
        AssertDoesNotContain(sourceText, "=> WaitForCancellation(TimeSpan.FromMilliseconds(1));");
        AssertContains(sourceText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(sourceText, "return cts.Token.WaitHandle.WaitOne(timeout);");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            return true;\n        }");
        AssertDoesNotContain(sourceText, "if (WaitForBackpressureRetryCancellation())");
        AssertContains(sourceText, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(sourceText, "TrackGpuQueueRejected(\"queue_full\");");
        AssertDoesNotContain(sourceText, "FLASHBACK_SINK_VIDEO_BACKPRESSURE_DROP");
        AssertDoesNotContain(sourceText, "FLASHBACK_SINK_GPU_BACKPRESSURE_DROP");
        AssertDoesNotContain(sourceText, "FailEncoding(overloadFailure);");
        AssertDoesNotContain(sourceText, "Flashback recording video queue overloaded after");
        AssertDoesNotContain(sourceText, "Flashback GPU recording queue overloaded after");
        AssertContains(sourceText, "if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))\n            {\n                return false;\n            }");
        AssertDoesNotContain(sourceText, "var depth = Interlocked.Decrement(ref target);");
        AssertDoesNotContain(sourceText, "Interlocked.Exchange(ref target, 0);\n        Logger.Log($\"FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _videoQueueDepth)");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _gpuQueueDepth)");
        AssertDoesNotContain(sourceText, "AtomicMax.Update(ref _videoQueueMaxDepth, Interlocked.Increment(ref _videoQueueDepth))");
        AssertDoesNotContain(sourceText, "AtomicMax.Update(ref _gpuQueueMaxDepth, Interlocked.Increment(ref _gpuQueueDepth))");
        AssertDoesNotContain(sourceText, "queue.Writer.TryWrite(packet))\n        {\n            Interlocked.Increment(ref queueDepth);");
        AssertDoesNotContain(sourceText, "Marshal.Release(packet.Texture);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "private const int AudioInputBlockAlignBytes = 2 * sizeof(float);");
        AssertContains(sourceText, "private const int MaxAudioPacketBytes = 4 * 1024 * 1024;");
        AssertContains(sourceText, "if (!TryValidateAudioPacketLength(samples.Length, \"audio\"))");
        AssertContains(sourceText, "if (!TryValidateAudioPacketLength(samples.Length, \"microphone\"))");
        AssertContains(sourceText, "private static bool TryValidateAudioPacketLength(int byteLength, string source)");
        AssertContains(sourceText, "if (byteLength <= 0 || byteLength > MaxAudioPacketBytes)");
        AssertContains(sourceText, "FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=size");
        AssertContains(sourceText, "if (byteLength % AudioInputBlockAlignBytes != 0)");
        AssertContains(sourceText, "FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=alignment");
        AssertContains(sourceText, "return byteLength > 0 ? byteLength / AudioInputBlockAlignBytes : 0;");
        AssertDoesNotContain(sourceText, "const int inputBlockAlign = 2 * sizeof(float);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "private const int VideoDrainBatchLimit = 24;");
        AssertContains(sourceText, "private const int GpuDrainBatchLimit = 16;");
        AssertContains(sourceText, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(sourceText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(sourceText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets)");
        AssertContains(sourceText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets && reader.TryRead(out var packet))");

        var loopBlock = ExtractTextBetween(
            sourceText,
            "private void EncodingLoop(CancellationToken cancellationToken)",
            "    private bool DrainVideoPackets");
        AssertOccursBefore(loopBlock, "DrainAudioPackets(audioQueue.Reader)", "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertOccursBefore(loopBlock, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)", "// Audio AGAIN");
        var secondAudioDrainBlock = ExtractTextBetween(
            loopBlock,
            "// Audio AGAIN",
            "// Handle force-rotate requests");
        AssertContains(secondAudioDrainBlock, "DrainAudioPackets(audioQueue.Reader)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RotateFailureRestoresActiveSegment()
    {
        var sinkText = ReadFlashbackEncoderSinkSource();
        var bufferText = ReadFlashbackBufferManagerSource();

        var rotateBlock = ExtractTextBetween(
            sinkText,
            "private bool RotateSegment(TimeSpan currentPts)",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateBlock, "string? completedPath = null;");
        AssertContains(rotateBlock, "string? newPath = null;");
        AssertContains(rotateBlock, "var encoderRotated = false;");
        AssertContains(rotateBlock, "completedPath = _tsFilePath;");
        AssertContains(rotateBlock, "var completedStartPts = _segmentStartPts;");
        AssertContains(rotateBlock, "newPath = _bufferManager.GenerateSegmentPath();");
        AssertContains(rotateBlock, "encoderRotated = true;");
        AssertOccursBefore(rotateBlock, "encoderRotated = true;", "_tsFilePath = newPath;");
        AssertOccursBefore(rotateBlock, "_tsFilePath = newPath;", "_bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, segmentBytes);");
        AssertContains(rotateBlock, "if (newPath != null && !encoderRotated)\n            {\n                _bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);\n            }");

        var abandonBlock = ExtractTextBetween(
            bufferText,
            "public void AbandonGeneratedSegmentPath",
            "    public void OnSegmentCompleted");
        AssertContains(abandonBlock, "if (IsSameSegmentPath(_activeSegmentPath, generatedPath))");
        AssertContains(abandonBlock, "_activeSegmentPath = restoreActivePath;");
        AssertContains(abandonBlock, "_nextSegmentIndex--;");
        AssertContains(abandonBlock, "if (!IsSameSegmentPath(generatedPath, restoreActivePath))");
        AssertContains(abandonBlock, "TryDeleteFile(generatedPath);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var cancelBlock = ExtractTextBetween(
            sourceText,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_ENCODING_LOOP_FATAL");
        AssertContains(cancelBlock, "Logger.Log(\"FLASHBACK_SINK_ENCODING_LOOP_CANCELLED\");");
        AssertContains(cancelBlock, "CompletePendingForceRotateWithEmptyResult();");
        AssertContains(cancelBlock, "var cancelPts = ResolveEncoderPts();");
        AssertContains(cancelBlock, "if (cancelPts > _segmentStartPts)");
        AssertContains(cancelBlock, "var cancelSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(cancelBlock, "_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);");
        AssertContains(cancelBlock, "FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_SEGMENT_REGISTERED");
        AssertContains(cancelBlock, "FLASHBACK_SINK_CANCELLED_SEGMENT_REGISTER_FAIL");
        AssertContains(cancelBlock, "ReturnAllRemainingQueuedBuffers();");
        AssertOccursBefore(cancelBlock, "_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);", "ReturnAllRemainingQueuedBuffers();");

        var rotateFailureBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            if (newPath != null && !encoderRotated)",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateFailureBlock, "Interlocked.Increment(ref _segmentRotationFailures);");
        AssertContains(rotateFailureBlock, "var failPts = ResolveEncoderPts();");
        AssertContains(rotateFailureBlock, "if (failPts > _segmentStartPts)");
        AssertContains(rotateFailureBlock, "var failSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(rotateFailureBlock, "_bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);");
        AssertContains(rotateFailureBlock, "FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED");
        AssertContains(rotateFailureBlock, "FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTER_FAIL");
        AssertContains(rotateFailureBlock, "_segmentStartPts = currentPts;");
        AssertOccursBefore(rotateFailureBlock, "_bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);", "_segmentStartPts = currentPts;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ForceRotateRejectsFailedEncoder()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public FlashbackForceRotateResult ForceRotateForExport",
            "    private bool TryCancelForceRotate");
        AssertContains(forceRotateBlock, "CancellationToken cancellationToken = default");
        AssertContains(forceRotateBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(forceRotateBlock, "if (inPoint < TimeSpan.Zero || outPoint <= inPoint)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE", "var request = new ForceRotateRequest();");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_INACTIVE");
        AssertContains(forceRotateBlock, "if (_encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.Failed();");
        AssertContains(forceRotateBlock, "var request = new ForceRotateRequest();");
        AssertContains(forceRotateBlock, "if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK", "_forceRotateRequest = request;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs").Replace("\r\n", "\n");
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n");
        var forceRotateText = loopText;

        var loopBlock = ExtractTextBetween(
            loopText,
            "if (Volatile.Read(ref _forceRotateRequested))",
            "                if (videoQueue.Reader.Completion.IsCompleted");
        var executionBlock = ExtractTextBetween(
            forceRotateText,
            "private bool ProcessPendingForceRotate(",
            "    private bool TryCancelForceRotate");

        AssertContains(sourceText, "private sealed class ForceRotateRequest");
        AssertContains(forceRotateText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateText, "private sealed class ForceRotateRequest");
        AssertContains(forceRotateText, "public bool TryBeginCommit()\n            => Interlocked.CompareExchange(ref _state, StateCommitting, StatePending) == StatePending;");
        AssertContains(forceRotateText, "public bool TryCancel()");
        AssertContains(forceRotateText, "public void Complete(IReadOnlyList<string> paths)");
        AssertContains(forceRotateText, "private bool TryCancelForceRotate(ForceRotateRequest request)");
        AssertContains(forceRotateText, "private void CompletePendingForceRotateWithEmptyResult()");
        AssertContains(forceRotateText, "private static bool ShouldAbortForceRotateDrain(");
        AssertDoesNotContain(rootText, "private sealed class ForceRotateRequest");
        AssertDoesNotContain(rootText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.ForceRotate.cs")),
            "FlashbackEncoderSink.ForceRotate.cs folded into FlashbackEncoderSink.EncodingLoop.cs");
        AssertContains(loopBlock, "if (ProcessPendingForceRotate(videoQueue, audioQueue, microphoneQueue, gpuQueue))");
        AssertContains(loopBlock, "madeProgress = true;\n                        continue;");
        AssertContains(executionBlock, "localRequest = _forceRotateRequest;\n            _forceRotateRequest = null;");
        AssertContains(executionBlock, "if (localRequest == null)\n            {\n                Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request\");\n                return true;\n            }");
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request", "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(executionBlock, "if (localRequest.IsCompleted)\n            {\n                Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed\");\n                return true;\n            }");
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed", "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(executionBlock, "var forceRotateDrainAborted = ShouldAbortForceRotateDrain(localRequest, \"before_drain\", inFlightCount);");
        AssertContains(sourceText, "private const int AudioDrainBatchLimit = 128;");
        AssertContains(executionBlock, "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(executionBlock, "while (DrainMicrophonePackets(microphoneQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(executionBlock, "while (DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit))");
        AssertContains(executionBlock, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))");
        AssertDoesNotContain(executionBlock, "while (DrainGpuPackets(gpuQueue.Reader))");
        AssertDoesNotContain(executionBlock, "while (DrainVideoPackets(videoQueue.Reader))");
        AssertContains(executionBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"audio\", inFlightCount))");
        AssertContains(executionBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"microphone\", inFlightCount))");
        AssertContains(executionBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"gpu\", inFlightCount))");
        AssertContains(executionBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"video\", inFlightCount))");
        AssertContains(executionBlock, "if (forceRotateDrainAborted)\n            {\n                return true;\n            }");
        AssertOccursBefore(executionBlock, "if (forceRotateDrainAborted)\n            {\n                return true;\n            }", "var currentPts = ResolveEncoderPts();");
        AssertContains(executionBlock, "if (localRequest.IsCompleted)\n            {\n                Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain\");\n                return true;\n            }");
        AssertOccursBefore(executionBlock, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))", "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain");
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain", "var currentPts = ResolveEncoderPts();");
        AssertContains(executionBlock, "if (!localRequest.TryBeginCommit())\n                {\n                    Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate\");\n                    return true;\n                }");
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate", "if (!RotateSegment(currentPts))");
        AssertContains(sourceText, "private static bool ShouldAbortForceRotateDrain(");
        AssertContains(sourceText, "if (!request.IsCompleted)");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_SINK_FORCE_ROTATE_ABORT_DRAIN phase={phase} in_flight_rounds={inFlightRounds}\");");
        AssertContains(sourceText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets && reader.TryRead(out var packet))");
        AssertContains(executionBlock, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_FORCE_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}\");\n            localRequest?.CompleteEmpty();\n            throw;\n        }");
        AssertOccursBefore(executionBlock, "localRequest?.CompleteEmpty();\n            throw;", "finally\n        {\n            lock (_videoQueueSync)");
        AssertContains(executionBlock, "finally\n        {\n            lock (_videoQueueSync)\n            {\n                Volatile.Write(ref _forceRotateDraining, false);\n            }\n        }");

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public FlashbackForceRotateResult ForceRotateForExport",
            "    private bool TryCancelForceRotate");
        AssertContains(forceRotateBlock, "if (!request.Task.Wait(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken))");
        AssertContains(forceRotateBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED");
        AssertContains(forceRotateBlock, "var cancelled = TryCancelForceRotate(request);");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED");
        AssertContains(sourceText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateBlock, "if (request.Task.Wait(TimeSpan.FromMilliseconds(ForceRotateCommittedGraceMs)))\n                    {\n                        return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());\n                    }");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED_PENDING");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.CommittedPending();");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED_COMMITTED");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.CanceledBeforeCommit();");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());");
        AssertDoesNotContain(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED_COMMITTED\");\n                _ = request.Task.GetAwaiter().GetResult();");
        AssertDoesNotContain(forceRotateBlock, "return request.Task.Result;");
        AssertDoesNotContain(sourceText, "_forceRotateTcs");
        AssertDoesNotContain(sourceText, "localTcs.Task.IsCompleted");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var fatalBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_ENCODING_LOOP_FATAL",
            "            ReturnAllRemainingQueuedBuffers();");
        AssertContains(fatalBlock, "catch (Exception segmentEx)");
        AssertContains(fatalBlock, "FLASHBACK_SINK_FATAL_SEGMENT_REGISTER_FAIL");
        AssertContains(fatalBlock, "Preserve the original fatal error.");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_StartupLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupText = rootText;
        var startupQueuesText = startupText;
        var startupRollbackText = startupText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(startupText, "public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)");
        AssertContains(startupText, "ValidateSessionContext(context);");
        AssertContains(startupText, "var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);");
        AssertContains(startupText, "InitializeStartupQueues(sessionContext);");
        AssertContains(startupText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(startupText, "RollBackStartFailure(ex, startupGeneratedSegmentPath);");

        AssertContains(startupQueuesText, "private void InitializeStartupQueues(FlashbackSessionContext sessionContext)");
        AssertContains(startupQueuesText, "Channel.CreateBounded<GpuFramePacket>");
        AssertContains(startupQueuesText, "Channel.CreateBounded<VideoFramePacket>");
        AssertContains(startupQueuesText, "Channel.CreateBounded<AudioSamplePacket>");
        AssertContains(startupQueuesText, "FLASHBACK_SINK_WARN_CPU_ENCODING");
        AssertContains(startupQueuesText, "FLASHBACK_SINK_GPU_QUEUE_INIT");

        AssertContains(startupRollbackText, "private void RollBackStartFailure(Exception ex, string? startupGeneratedSegmentPath)");
        AssertContains(startupRollbackText, "FLASHBACK_SINK_START_FAIL");
        AssertContains(startupRollbackText, "CompleteWriter(_videoQueue);");
        AssertContains(startupRollbackText, "DisposeCtsBestEffort(_cts, \"start_fail\");");
        AssertContains(startupRollbackText, "DisposeEncoderBestEffort(\"start_fail\");");
        AssertContains(startupRollbackText, "_bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Startup.cs")),
            "FlashbackEncoderSink startup folded into the root lifetime owner");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "startup queue allocation");
        AssertContains(docsText, "start-failure rollback");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RootOwnsConstructionAndRuntimeSurface()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupPolicyText = rootText;
        var diagnosticsResetText = startupPolicyText;
        var runtimeStateText = rootText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(rootText, "public FlashbackEncoderSink(FlashbackBufferOptions? options = null)");
        AssertContains(rootText, "public FlashbackEncoderSink(FlashbackBufferManager bufferManager)");
        AssertContains(rootText, "private static int ResolveVideoQueueCapacity");
        AssertContains(rootText, "private void ResetEncodingCounters()");

        AssertContains(startupPolicyText, "private static int ResolveVideoQueueCapacity(FlashbackSessionContext context, bool useHardwareFrames)");
        AssertContains(startupPolicyText, "private static bool IsHighResolutionFrame(FlashbackSessionContext context)");
        AssertContains(startupPolicyText, "private static double ResolveSessionFrameRate(double frameRate)");
        AssertContains(startupPolicyText, "private static void ValidateSessionContext(FlashbackSessionContext context)");

        AssertContains(diagnosticsResetText, "private void ResetEncodingCounters()");
        AssertContains(diagnosticsResetText, "ResetVideoDiagnostics();");
        AssertContains(diagnosticsResetText, "private void ResetVideoDiagnostics()");
        AssertContains(diagnosticsResetText, "Interlocked.Exchange(ref _segmentStartBytes, 0);");

        AssertContains(runtimeStateText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(runtimeStateText, "private static long NonNegativeByteDelta(long currentBytes, long startBytes)");
        AssertContains(runtimeStateText, "private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)");
        AssertContains(runtimeStateText, "private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(");
        AssertContains(runtimeStateText, "FLASHBACK_SINK_EVICTION_RESUME_WARN");

        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "session validation");
        AssertContains(docsText, "startup metric/counter reset");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "public runtime counters");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_EncodingThreadWorkLivesInEncodingLoop()
    {
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n");
        var packetDrainText = loopText;
        var encodingProgressText = loopText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(loopText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertContains(loopText, "DrainAudioPackets(audioQueue.Reader)");
        AssertContains(loopText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(loopText, "var finalPts = ResolveEncoderPts();");

        AssertContains(packetDrainText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "MfSourceReaderVideoCapture.GetFrameSizeBytes");
        AssertContains(packetDrainText, "OnVideoFrameEncoded();");
        AssertContains(packetDrainText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");

        AssertContains(encodingProgressText, "private void OnVideoFrameEncoded()");
        AssertContains(encodingProgressText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(encodingProgressText, "private bool RotateSegment(TimeSpan currentPts)");
        AssertContains(encodingProgressText, "_bufferManager.UpdateLatestPts(pts);");
        AssertContains(encodingProgressText, "FrameEncoded?.Invoke(this, encoded);");
        AssertContains(encodingProgressText, "FLASHBACK_SINK_ROTATE");
        AssertContains(encodingProgressText, "FLASHBACK_SINK_ROTATE_FAIL");
        AssertContains(docsText, "FlashbackEncoderSink.EncodingLoop.cs");
        AssertContains(docsText, "bounded video/GPU/audio/microphone packet drains");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.PacketDrain.cs")),
            "FlashbackEncoderSink.PacketDrain.cs folded into FlashbackEncoderSink.EncodingLoop.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.EncodingProgress.cs")),
            "FlashbackEncoderSink.EncodingProgress.cs folded into FlashbackEncoderSink.EncodingLoop.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_QueueingOwnsInputsAndCleanup()
    {
        var queueingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs")
            .Replace("\r\n", "\n");
        var inputsText = queueingText;
        var queueCleanupText = queueingText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(inputsText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(inputsText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(inputsText, "TryWriteVideoPacket(queue, packet)");
        AssertContains(inputsText, "TryWriteGpuPacket(queue, packet)");
        AssertContains(inputsText, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(inputsText, "TrackGpuQueueRejected(\"queue_full\");");
        AssertContains(inputsText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(inputsText, "private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)");
        AssertContains(inputsText, "private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)");
        AssertContains(inputsText, "return \"force_rotate_draining\";");
        AssertContains(inputsText, "? $\"encoding_failed:{failure.GetType().Name}\"");
        AssertContains(inputsText, "return dataIsEmpty ? \"data_empty\" : null;");
        AssertContains(inputsText, "return texture == IntPtr.Zero ? \"null_texture\" : null;");
        AssertContains(inputsText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(inputsText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(inputsText, "AtomicMax.Update(ref _videoQueueMaxDepth, depth);");
        AssertContains(inputsText, "AtomicMax.Update(ref _gpuQueueMaxDepth, depth);");
        AssertContains(inputsText, "DecrementQueueDepth(ref _videoQueueDepth, \"video_write_failed\");");
        AssertContains(inputsText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu_write_failed\");");
        AssertContains(inputsText, "private void TrackVideoQueueRejected(string reason)");
        AssertContains(inputsText, "private void TrackGpuQueueRejected(string reason)");
        AssertContains(inputsText, "FLASHBACK_SINK_VIDEO_QUEUE_REJECT");
        AssertContains(inputsText, "FLASHBACK_SINK_GPU_QUEUE_REJECT");
        AssertContains(inputsText, "total == 1 || total % 30 == 0");
        AssertContains(inputsText, "private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)");
        AssertContains(inputsText, "queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio)");
        AssertContains(inputsText, "private bool TryEnqueueAudioPacket(");
        AssertContains(inputsText, "Volatile.Read(ref _forceRotateDraining)");
        AssertContains(inputsText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(inputsText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio_after_evict\")");
        AssertContains(inputsText, "FLASHBACK_SINK_AUDIO_EVICT_PTS");
        AssertContains(inputsText, "private static bool TryWriteAudioPacket(");
        AssertContains(inputsText, "DecrementQueueDepth(ref queueDepth, $\"{queueName}_write_failed\");");

        AssertContains(queueCleanupText, "private void ReturnAllRemainingQueuedBuffers()");
        AssertContains(queueCleanupText, "private void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "ReturnVideoPacketBestEffort(packet);");
        AssertContains(queueCleanupText, "_videoLatencyTracker.ClearEnqueueTicksUnderLock();");
        AssertContains(queueCleanupText, "ReturnBuffer(packet.Buffer);");
        AssertContains(queueCleanupText, "ReleaseGpuTextureBestEffort(packet.Texture);");
        AssertContains(queueCleanupText, "Interlocked.Exchange(ref queueDepth, 0);");

        AssertContains(queueingText, "private void ReturnAllRemainingQueuedBuffers()");
        AssertContains(queueingText, "private void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueingText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertContains(queueingText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueingText, "private void CompleteWriter<TPacket>(Channel<TPacket>? channel)");
        AssertContains(queueingText, "private void SignalWork(string operation)");
        AssertContains(queueingText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(queueingText, "private void FailEncoding(Exception ex)");
        AssertContains(queueingText, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertDoesNotContain(queueingText, "private void ResetVideoDiagnostics()");

        AssertContains(docsText, "FlashbackEncoderSink.Queueing.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackEncoderSink.VideoQueueSubmission.Guards.cs",
            "FlashbackEncoderSink.VideoQueueSubmission.Writers.cs",
            "FlashbackEncoderSink.VideoQueueSubmission.Rejections.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackEncoderSink.Queueing.cs");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.AudioQueueSubmission.cs")),
            "FlashbackEncoderSink.AudioQueueSubmission.cs folded into FlashbackEncoderSink.Queueing.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.VideoQueueSubmission.cs")),
            "FlashbackEncoderSink.VideoQueueSubmission.cs folded into FlashbackEncoderSink.Queueing.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Inputs.cs")),
            "FlashbackEncoderSink.Inputs.cs folded into FlashbackEncoderSink.Queueing.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Queues.cs")),
            "FlashbackEncoderSink.Queues.cs folded into FlashbackEncoderSink.Queueing.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ForceRotateLivesWithEncodingLoop()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var forceRotateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(forceRotateText, "public bool IsForceRotateActive =>");
        AssertContains(forceRotateText, "public bool IsForceRotateRequested =>");
        AssertContains(forceRotateText, "public bool IsForceRotateDraining =>");
        AssertContains(forceRotateText, "public bool WaitForForceRotateIdle(TimeSpan timeout)");
        AssertContains(forceRotateText, "private bool _forceRotateRequested;");
        AssertContains(forceRotateText, "private volatile ForceRotateRequest? _forceRotateRequest;");
        AssertContains(forceRotateText, "private TimeSpan _forceRotateInPoint;");
        AssertContains(forceRotateText, "private TimeSpan _forceRotateOutPoint;");
        AssertContains(forceRotateText, "private bool _forceRotateDraining;");
        AssertContains(forceRotateText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertContains(forceRotateText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateText, "var request = new ForceRotateRequest();");
        AssertContains(forceRotateText, "TryCancelForceRotate(request)");
        AssertContains(forceRotateText, "private bool TryCancelForceRotate(ForceRotateRequest request)");
        AssertContains(forceRotateText, "private void CompletePendingForceRotateWithEmptyResult()");
        AssertContains(forceRotateText, "private static bool ShouldAbortForceRotateDrain(");
        AssertContains(forceRotateText, "private sealed class ForceRotateRequest");
        AssertContains(forceRotateText, "public bool TryBeginCommit()");
        AssertContains(forceRotateText, "public bool TryCancel()");
        AssertContains(forceRotateText, "public void Complete(IReadOnlyList<string> paths)");
        AssertContains(forceRotateText, "private bool ProcessPendingForceRotate(");
        AssertContains(forceRotateText, "Volatile.Write(ref _forceRotateDraining, true);");
        AssertContains(forceRotateText, "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(forceRotateText, "while (DrainMicrophonePackets(microphoneQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(forceRotateText, "while (DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit))");
        AssertContains(forceRotateText, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))");
        AssertContains(forceRotateText, "if (!localRequest.TryBeginCommit())");
        AssertContains(forceRotateText, "if (!RotateSegment(currentPts))");
        AssertContains(forceRotateText, "localRequest.Complete(_bufferManager.GetValidSegmentPaths(localIn, localOut));");
        AssertDoesNotContain(rootText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertDoesNotContain(rootText, "public bool IsForceRotateActive =>");
        AssertDoesNotContain(rootText, "public bool WaitForForceRotateIdle(TimeSpan timeout)");
        AssertDoesNotContain(rootText, "private bool _forceRotateRequested;");
        AssertDoesNotContain(rootText, "private TimeSpan _forceRotateInPoint;");
        AssertDoesNotContain(rootText, "private TimeSpan _forceRotateOutPoint;");
        AssertDoesNotContain(rootText, "private bool _forceRotateDraining;");
        AssertDoesNotContain(rootText, "private sealed class ForceRotateRequest");
        AssertDoesNotContain(rootText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(docsText, "FlashbackEncoderSink.EncodingLoop.cs");
        AssertDoesNotContain(docsText, "FlashbackEncoderSink.ForceRotate.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.ForceRotate.cs")),
            "FlashbackEncoderSink.ForceRotate.cs folded into FlashbackEncoderSink.EncodingLoop.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackEncoderSink.ForceRotateRequests.cs",
            "FlashbackEncoderSink.ForceRotateExecution.cs",
            "FlashbackEncoderSink.ForceRotateLifecycle.cs",
            "FlashbackEncoderSink.ForceRotateRequest.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackEncoderSink.EncodingLoop.cs");
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_StopAndDisposeLifecyclesShareShutdownOwner()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var lifetimeText = rootText;

        AssertContains(lifetimeText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertContains(lifetimeText, "private async Task<FinalizeResult> StopCoreAsync(CancellationToken cancellationToken)");
        AssertContains(lifetimeText, "FLASHBACK_SINK_STOP_DRAIN_TIMEOUT");
        AssertContains(lifetimeText, "FLASHBACK_SINK_STOP_FAIL");
        AssertContains(lifetimeText, "public void Dispose()");
        AssertContains(lifetimeText, "public async ValueTask DisposeAsync()");
        AssertContains(lifetimeText, "private void ScheduleDeferredDisposeCleanup(Task encodingTask)");
        AssertContains(lifetimeText, "private void FinalizeDisposeCore()");
        AssertContains(lifetimeText, "private void CancelEncodingCts(string operation)");
        AssertContains(lifetimeText, "private void DisposeEncoderBestEffort(string operation)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.DisposeLifecycle.cs")),
            "FlashbackEncoderSink stop/dispose lifecycle folded into FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Lifetime.cs")),
            "FlashbackEncoderSink.Lifetime.cs folded into FlashbackEncoderSink.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ProducerInputsLiveInCohesivePartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var inputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(inputsText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertContains(inputsText, "bool IRawVideoFrameLeaseTryEncoder.TryEnqueueRawVideoFrame(PooledVideoFrameLease frame)");
        AssertContains(inputsText, "public bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)");
        AssertContains(inputsText, "MfSourceReaderVideoCapture.GetFrameSizeBytes");
        AssertContains(inputsText, "Marshal.AddRef(d3d11Texture2D);");
        AssertContains(inputsText, "TrackVideoQueueRejected(rejectReason);");
        AssertContains(inputsText, "TrackGpuQueueRejected(rejectReason);");
        AssertContains(inputsText, "public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(inputsText, "public void EnqueueMicrophoneSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(inputsText, "public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(inputsText, "public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(inputsText, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(inputsText, "TryValidateAudioPacketLength(samples.Length, \"audio\")");
        AssertContains(inputsText, "TryValidateAudioPacketLength(samples.Length, \"microphone\")");
        AssertContains(inputsText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(inputsText, "private bool TryEnqueueAudioPacket(");
        AssertContains(inputsText, "private void TrackVideoQueueRejected(string reason)");

        AssertDoesNotContain(rootText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertDoesNotContain(rootText, "public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(docsText, "FlashbackEncoderSink.Queueing.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Inputs.Video.cs")),
            "FlashbackEncoderSink video producer inputs folded into FlashbackEncoderSink.Queueing.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Inputs.Audio.cs")),
            "FlashbackEncoderSink audio producer inputs folded into FlashbackEncoderSink.Queueing.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RuntimeStateLivesWithRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var runtimeStateText = rootText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(runtimeStateText, "public event EventHandler<long>? FrameEncoded;");
        AssertContains(runtimeStateText, "public long DroppedVideoFrames =>");
        AssertContains(runtimeStateText, "public long VideoFramesSubmittedToEncoder =>");
        AssertContains(runtimeStateText, "public long SegmentRotationFailures =>");
        AssertContains(runtimeStateText, "public int VideoQueueCount =>");
        AssertContains(runtimeStateText, "public long VideoQueueRejectedFrames =>");
        AssertContains(runtimeStateText, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics");
        AssertContains(runtimeStateText, "public long GpuQueueRejectedFrames =>");
        AssertContains(runtimeStateText, "public bool EncodingFailed =>");
        AssertContains(runtimeStateText, "public void SetFatalErrorCallback(Action<Exception>? callback)");
        AssertContains(runtimeStateText, "public string? CodecName =>");
        AssertContains(runtimeStateText, "public bool? IsP010 =>");
        AssertContains(runtimeStateText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(runtimeStateText, "private static long NonNegativeByteDelta(long currentBytes, long startBytes)");
        AssertContains(runtimeStateText, "private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)");
        AssertContains(runtimeStateText, "private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(");
        AssertContains(runtimeStateText, "internal Task EncodingCompletionTask =>");

        AssertContains(rootText, "public event EventHandler<long>? FrameEncoded;");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "queue telemetry");
        foreach (var removedFile in new[]
        {
            "FlashbackEncoderSink.RuntimeState.cs",
            "FlashbackEncoderSink.RuntimeState.Counters.cs",
            "FlashbackEncoderSink.RuntimeState.QueueMetrics.cs",
            "FlashbackEncoderSink.RuntimeState.Status.cs",
            "FlashbackEncoderSink.RecordingAccounting.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackEncoderSink.cs");
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RecordingLifecycleLivesWithRootRuntimeSurface()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(rootText, "public TimeSpan LastRecordingStartPts { get; private set; }");
        AssertContains(rootText, "public TimeSpan LastRecordingEndPts { get; private set; }");
        AssertContains(rootText, "public bool IsRecordingActive =>");
        AssertContains(rootText, "public bool CanBeginRecording");
        AssertContains(rootText, "!_bufferManager.IsSessionPreservedForRecovery");
        AssertContains(rootText, "Task IRecordingSink.StartAsync(RecordingContext context, CancellationToken cancellationToken)");
        AssertContains(rootText, "public void BeginRecording(string outputPath)");
        AssertContains(rootText, "Cannot begin recording: flashback export rotation is still draining.");
        AssertContains(rootText, "_bufferManager.PauseEviction();");
        AssertContains(rootText, "public void CancelRecordingStartRollback(string reason)");
        AssertContains(rootText, "ResumeEvictionBestEffort(_bufferManager, \"recording_start_rollback\")");
        AssertContains(rootText, "public async Task<FinalizeResult> EndRecordingAsync(CancellationToken cancellationToken)");
        AssertContains(rootText, "FLASHBACK_RECORDING_END_REJECTED");
        AssertContains(rootText, "FLASHBACK_RECORDING_FAIL");
        AssertContains(rootText, "ResumeEvictionBestEffort(_bufferManager, \"recording_end\")");
        AssertContains(rootText, "FLASHBACK_RECORDING_READY");

        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "recording PTS boundary state");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Recording.cs")),
            "FlashbackEncoderSink.Recording.cs folded into FlashbackEncoderSink.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackEncoderSink.Recording.State.cs",
            "FlashbackEncoderSink.Recording.Start.cs",
            "FlashbackEncoderSink.Recording.End.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackEncoderSink.cs");
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_OptionsHelpersLiveWithStartup()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupText = rootText;
        var optionsText = startupText;
        var sessionContextText = optionsText;
        var queuesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs")
            .Replace("\r\n", "\n");
        var packetBuffersText = queuesText;
        var packetTypesText = packetBuffersText;
        var inputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(optionsText, "private static LibAvEncoderOptions CreateOptions(FlashbackSessionContext context, string outputPath)");
        AssertContains(optionsText, "internal static string GetSegmentExtension(string codecName)");
        AssertContains(optionsText, "private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)");
        AssertContains(optionsText, "private static FlashbackSessionContext CreateSessionContext(RecordingContext context)");
        AssertContains(optionsText, "private static (int? Numerator, int? Denominator) ResolveFrameRateParts(string frameRateArg)");
        AssertContains(optionsText, "private static string MapCodecName(RecordingFormat format)");
        AssertDoesNotContain(optionsText, "private readonly record struct VideoFramePacket");
        AssertDoesNotContain(optionsText, "private static byte[] GetBuffer");

        AssertContains(sessionContextText, "private static FlashbackSessionContext CreateSessionContext(RecordingContext context)");
        AssertContains(sessionContextText, "private static (int? Numerator, int? Denominator) ResolveFrameRateParts(string frameRateArg)");
        AssertContains(sessionContextText, "private static string MapCodecName(RecordingFormat format)");
        AssertContains(sessionContextText, "SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode)");

        AssertContains(startupText, "private static string CreateSessionId()");
        AssertContains(startupText, "DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()");

        AssertContains(packetBuffersText, "private static byte[] GetBuffer(int size)");
        AssertContains(packetBuffersText, "private static void ReturnBuffer(byte[] buffer)");
        AssertContains(packetBuffersText, "private static void ReturnVideoPacket(VideoFramePacket packet)");
        AssertContains(packetBuffersText, "private static void ReturnVideoPacketBestEffort(VideoFramePacket packet)");
        AssertContains(packetBuffersText, "private static void ReleaseGpuTextureBestEffort(IntPtr texture)");
        AssertContains(packetBuffersText, "ArrayPool<byte>.Shared.Rent(size)");
        AssertContains(packetBuffersText, "Marshal.Release(texture);");

        AssertContains(packetTypesText, "private readonly record struct VideoFramePacket");
        AssertContains(packetTypesText, "private enum VideoEnqueueResult");
        AssertContains(packetTypesText, "private readonly record struct AudioSamplePacket");
        AssertContains(packetTypesText, "private readonly record struct GpuFramePacket");

        AssertContains(inputsText, "private static long GetSampleCount(int byteLength)");
        AssertContains(inputsText, "private static bool TryValidateAudioPacketLength(int byteLength, string source)");
        AssertContains(rootText, "private static FlashbackSessionContext CreateSessionContext");
        AssertDoesNotContain(rootText, "private static byte[] GetBuffer");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "recording-to-Flashback session mapping");
        AssertContains(docsText, "generated session ID formatting");
        AssertContains(docsText, "FlashbackEncoderSink.Queueing.cs");
        AssertContains(docsText, "packet DTOs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Options.cs")),
            "FlashbackEncoderSink.Options.cs folded into FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.PacketBuffers.cs")),
            "FlashbackEncoderSink.PacketBuffers.cs folded into FlashbackEncoderSink.Queueing.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var commandType = controllerType.GetNestedType("PlaybackCommand", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = controllerType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var commandChannelField = controllerType.GetField("_commandChannel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandChannel not found.");
        var queueCapacityProperty = controllerType.GetProperty("CommandQueueCapacityCommands", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CommandQueueCapacityCommands not found.");
        var commandsDroppedProperty = controllerType.GetProperty("CommandsDropped", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CommandsDropped not found.");
        var pendingCommandsProperty = controllerType.GetProperty("PendingCommands", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("PendingCommands not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;
        var controller = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController construction failed.");
        using var disposableController = controller as IDisposable;

        var playKind = Enum.Parse(commandKindType, "Play");
        var goLiveKind = Enum.Parse(commandKindType, "GoLive");
        var capacity = (int)queueCapacityProperty.GetValue(controller)!;

        for (var i = 0; i < capacity; i++)
        {
            var playCommand = Activator.CreateInstance(commandType)
                ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
            SetPropertyOrBackingField(playCommand, "Kind", playKind);
            AssertEqual(true, (bool)sendCommand.Invoke(controller, new[] { playCommand })!, $"Play command {i} enqueues");
        }

        AssertEqual(capacity, (int)pendingCommandsProperty.GetValue(controller)!, "Queue starts full");

        var goLiveCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand GoLive construction failed.");
        SetPropertyOrBackingField(goLiveCommand, "Kind", goLiveKind);
        AssertEqual(true, (bool)sendCommand.Invoke(controller, new[] { goLiveCommand })!, "Newest GoLive command is accepted when queue is full");
        AssertEqual(capacity, (int)pendingCommandsProperty.GetValue(controller)!, "Drop-oldest accounting keeps pending bounded at capacity");

        var channel = commandChannelField.GetValue(controller)
            ?? throw new InvalidOperationException("Command channel missing.");
        var sawGoLive = false;
        while (TryReadQueuedPlaybackCommand(channel, commandType, out var command) && command != null)
        {
            if (GetPropertyValue(command, "Kind")?.ToString() == "GoLive")
            {
                sawGoLive = true;
            }
        }

        AssertEqual(true, sawGoLive, "Full command queue preserves the newest GoLive command");
        AssertEqual(true, (long)commandsDroppedProperty.GetValue(controller)! > 0, "Dropped-command diagnostics record the evicted older command");

        return Task.CompletedTask;

        static bool TryReadQueuedPlaybackCommand(object channel, Type commandType, out object? command)
        {
            var reader = channel.GetType().GetProperty("Reader")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel reader missing.");
            var tryRead = reader.GetType().GetMethod(
                    "TryRead",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { commandType.MakeByRefType() },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryRead not found.");
            object?[] args = { null };
            var result = (bool)tryRead.Invoke(reader, args)!;
            command = args[0];
            return result;
        }
    }


    internal static Task FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var commandQueueText = rootText;

        var seekBlock = ExtractTextBetween(
            sourceText,
            "private void HandleSeekCommand(",
            "    private void HandleBeginScrubCommand(");

        AssertContains(seekBlock, "commandChannel.Reader.TryPeek(out var newerSeek) &&\n               newerSeek.Kind == CommandKind.Seek");
        AssertContains(seekBlock, "TrackCommandDequeued(newerSeek);");
        AssertContains(seekBlock, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertContains(seekBlock, "newerSeek = ResolveSeekCommandPosition(newerSeek);");
        AssertContains(seekBlock, "FLASHBACK_PLAYBACK_SEEK");

        var beginScrubMethod = ExtractTextBetween(
            sourceText,
            "public bool BeginScrub(TimeSpan position)",
            "    public bool Seek(TimeSpan position)");
        var seekMethod = ExtractTextBetween(
            sourceText,
            "public bool Seek(TimeSpan position)",
            "    private bool SendUpdateScrubCommand");
        var updateScrubBlock = ExtractTextBetween(
            sourceText,
            "private void HandleUpdateScrubCommand(",
            "    private void HandleEndScrubCommand(");
        var updateScrubMethod = ExtractTextBetween(
            sourceText,
            "public bool UpdateScrub(TimeSpan position)",
            "    public bool EndScrub()");
        var drainAbandonedCommands = ExtractTextBetween(
            sourceText,
            "private void DrainAbandonedCommandsOnThreadExit(Channel<PlaybackCommand> commandChannel)",
            "    private static void CompleteCommandChannelForThreadExit");

        AssertContains(commandQueueText, "private long _latestScrubUpdateTicks;");
        AssertContains(commandQueueText, "private sealed class SeekIntentSlot");
        AssertContains(commandQueueText, "private sealed class ScrubUpdateIntentSlot");
        AssertContains(sourceText, "public SeekIntentSlot? SeekSlot { get; init; }");
        AssertContains(sourceText, "public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }");
        AssertContains(commandQueueText, "private readonly object _seekSlotSync = new();");
        AssertContains(commandQueueText, "private SeekIntentSlot? _queuedSeekSlot;");
        AssertContains(commandQueueText, "private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;");
        AssertContains(commandQueueText, "private long _scrubUpdatesCoalesced;");
        AssertContains(commandQueueText, "private long _seekCommandsCoalesced;");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.CommandQueue.cs")), "Flashback playback command queue stays folded into the root controller");
        AssertContains(sourceText, "public long SeekCommandsCoalesced => Interlocked.Read(ref _seekCommandsCoalesced);");
        AssertContains(sourceText, "public bool HasPositionOverride { get; init; }");
        AssertContains(sourceText, "public bool EndScrub() => EndScrubAt(null);");
        AssertContains(sourceText, "public bool EndScrubAt(TimeSpan position) => EndScrubAt((TimeSpan?)position);");
        AssertContains(sourceText, "private bool EndScrubAt(TimeSpan? position)");
        AssertContains(sourceText, "return SendEndScrubCommand(position);");
        AssertContains(sourceText, "private bool SendEndScrubCommand(TimeSpan? position)");
        AssertContains(sourceText, "var commandTicks = position?.Ticks ??");
        AssertContains(sourceText, "_queuedScrubUpdateSlot?.LatestTicks ??");
        AssertContains(sourceText, "var commandPosition = TimeSpan.FromTicks(commandTicks);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Value.Ticks);");
        AssertContains(sourceText, "HasPositionOverride = position.HasValue");
        AssertContains(sourceText, "HasPositionOverride = command.HasPositionOverride");
        AssertContains(sourceText, "SeekSlot = command.SeekSlot");
        AssertContains(sourceText, "ScrubUpdateSlot = command.ScrubUpdateSlot");
        AssertContains(beginScrubMethod, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(seekMethod, "lock (_seekSlotSync)");
        AssertContains(seekMethod, "_queuedScrubUpdateSlot = null;");
        AssertContains(seekMethod, "if (_queuedSeekSlot is { } queuedSlot)");
        AssertContains(seekMethod, "queuedSlot.LatestTicks = position.Ticks;");
        AssertContains(seekMethod, "TrackCoalescedSeekCommand();");
        AssertContains(seekMethod, "ClearLastCommandFailure();");
        AssertContains(seekMethod, "return true;");
        AssertContains(seekMethod, "var slot = new SeekIntentSlot(position.Ticks);");
        AssertContains(seekMethod, "_queuedSeekSlot = slot;");
        AssertContains(seekMethod, "SeekSlot = slot");
        AssertContains(seekMethod, "ClearQueuedSeekSlotUnsafe(slot);");
        AssertContains(seekMethod, "return false;");
        AssertContains(sourceText, "private bool SendCommand(PlaybackCommand command)\n    {\n        lock (_seekSlotSync)\n        {\n            if (!SendCommandCore(command))\n            {\n                return false;\n            }\n\n            if (command.Kind != CommandKind.Seek)\n            {\n                _queuedSeekSlot = null;\n            }\n\n            if (command.Kind != CommandKind.UpdateScrub)\n            {\n                _queuedScrubUpdateSlot = null;\n            }\n\n            return true;\n        }\n    }");
        AssertContains(updateScrubMethod, "return SendUpdateScrubCommand(position);");
        AssertContains(sourceText, "private bool SendUpdateScrubCommand(TimeSpan position)");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(sourceText, "if (_queuedScrubUpdateSlot is { } queuedSlot)");
        AssertContains(sourceText, "queuedSlot.LatestTicks = position.Ticks;");
        AssertContains(sourceText, "ClearLastCommandFailure();");
        AssertContains(sourceText, "var slot = new ScrubUpdateIntentSlot(position.Ticks);");
        AssertContains(sourceText, "_queuedScrubUpdateSlot = slot;");
        AssertContains(sourceText, "ScrubUpdateSlot = slot");
        AssertContains(sourceText, "ClearQueuedScrubUpdateSlotUnsafe(slot);");
        AssertContains(updateScrubMethod, "if (!PlaybackThreadAlive) return RejectCommand(CommandKind.UpdateScrub, \"thread_not_running\", \"thread_not_running\", false, position);");
        AssertContains(sourceText, "TrackCoalescedScrubUpdate();");
        AssertContains(updateScrubBlock, "cmd = ResolveScrubUpdateCommandPosition(cmd);");
        AssertContains(updateScrubBlock, "commandChannel.Reader.TryPeek(out var newer) &&\n               newer.Kind == CommandKind.UpdateScrub");
        AssertContains(updateScrubBlock, "if (!commandChannel.Reader.TryRead(out newer))");
        AssertContains(updateScrubBlock, "TrackCommandDequeued(newer);");
        AssertContains(updateScrubBlock, "newer = ResolveScrubUpdateCommandPosition(newer);");
        AssertContains(updateScrubBlock, "cmd = newer;");
        AssertContains(updateScrubBlock, "if (ShouldYieldScrubUpdateToQueuedControl(commandChannel))");
        AssertContains(updateScrubBlock, "PlaybackPosition = cmd.Position;");
        AssertContains(updateScrubBlock, "MarkCommandNoOp(CommandKind.UpdateScrub, \"superseded_by_control\", cmd.Position);");
        AssertContains(updateScrubBlock, "FLASHBACK_PLAYBACK_SCRUB_UPDATE_NO_FILE");
        AssertContains(updateScrubBlock, "SafeResumePreviewSubmission(\"scrub_update_no_file\")");
        AssertContains(updateScrubBlock, "SetState(FlashbackPlaybackState.Live)");
        AssertContains(commandQueueText, "private static bool ShouldYieldScrubUpdateToQueuedControl(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.EndScrub or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(commandQueueText, "private static bool ShouldYieldSeekToQueuedPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(commandQueueText, "private static bool ShouldYieldPauseFromLiveToQueuedSeekOrPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.Seek or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(drainAbandonedCommands, "ClearQueuedCommandSlotsBarrier();");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.EndScrub, \"live_thread_not_running\", position);\n            return false;\n        }");
        var endScrubBlock = ExtractTextBetween(
            sourceText,
            "private void HandleEndScrubCommand(",
            "    private void HandleNudgeCommand(");
        AssertContains(endScrubBlock, "var endScrubPosition = ClampPosition(cmd.Position, frozenValidStart);");
        AssertContains(endScrubBlock, "PlaybackPosition = endScrubPosition;");
        AssertDoesNotContain(endScrubBlock, "TimeSpan.FromTicks(Interlocked.Read(ref _latestScrubUpdateTicks))");
        AssertContains(endScrubBlock, "var endScrubTarget = SaturatingAdd(endScrubPosition, frozenValidStart);");
        AssertDoesNotContain(endScrubBlock, "var endScrubTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(sourceText, "private bool RejectCommand(\n        CommandKind kind,\n        string failure,\n        string reason,\n        bool returnValue,\n        TimeSpan? position = null)");
        AssertContains(commandQueueText, "private bool RejectCommand(\n        CommandKind kind,\n        string failure,\n        string reason,\n        bool returnValue,\n        TimeSpan? position = null)");
        AssertContains(sourceText, "SetLastCommandFailure($\"{failure}:{kind}{detail}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_CMD_SKIP kind={kind} reason={reason}{detail}\");");
        AssertContains(sourceText, "private void SetNoFileFailure(CommandKind kind, TimeSpan position)");
        AssertContains(commandQueueText, "private void SetNoFileFailure(CommandKind kind, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"no_file:{kind}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(commandQueueText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(sourceText, "return $\" pos_ms={position.Value.TotalMilliseconds.ToString(\"0.###\", CultureInfo.InvariantCulture)}\";");
        AssertContains(sourceText, "return $\" delta_ms={delta.Value.TotalMilliseconds.ToString(\"0.###\", CultureInfo.InvariantCulture)}\";");
        AssertContains(sourceText, "private void SetLastCommandFailure(string failure)\n    {\n        Volatile.Write(ref _lastCommandFailure, failure);\n        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());\n    }");
        AssertContains(commandQueueText, "private void SetLastCommandFailure(string failure)");
        AssertContains(sourceText, "private void MarkCommandQueued(CommandKind kind)");
        AssertContains(sourceText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(commandQueueText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(sourceText, "private void ClearLastCommandFailure()\n    {\n        Volatile.Write(ref _lastCommandFailure, string.Empty);\n        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, 0);\n    }");
        AssertContains(commandQueueText, "private void ClearLastCommandFailure()");
        AssertContains(sourceText, "private void TrackCoalescedScrubUpdate()");
        AssertContains(sourceText, "Interlocked.Increment(ref _scrubUpdatesCoalesced);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SCRUB_COALESCED");
        var coalescedSeekMethod = ExtractTextBetween(
            sourceText,
            "private void TrackCoalescedSeekCommand()",
            "    private void TrackCommandDequeued");
        AssertContains(coalescedSeekMethod, "Interlocked.Increment(ref _seekCommandsCoalesced);");
        AssertContains(coalescedSeekMethod, "FLASHBACK_PLAYBACK_SEEK_COALESCED");
        AssertDoesNotContain(coalescedSeekMethod, "_commandsDropped");
        AssertContains(commandQueueText, "private PlaybackCommand ResolveSeekCommandPosition(PlaybackCommand command)");
        AssertContains(sourceText, "if (ReferenceEquals(_queuedSeekSlot, slot))\n            {\n                _queuedSeekSlot = null;\n            }");
        AssertContains(commandQueueText, "private PlaybackCommand ResolveScrubUpdateCommandPosition(PlaybackCommand command)");
        AssertContains(sourceText, "if (ReferenceEquals(_queuedScrubUpdateSlot, slot))\n            {\n                _queuedScrubUpdateSlot = null;\n            }");
        AssertContains(commandQueueText, "private void ClearQueuedCommandSlotsBarrier()");
        AssertDoesNotContain(updateScrubBlock, "SendCommand(newer)");
        AssertDoesNotContain(updateScrubBlock, "Non-scrub command consumed");

        return Task.CompletedTask;
    }


    internal static Task FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var commandType = controllerType.GetNestedType("PlaybackCommand", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = controllerType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var seekSlotType = controllerType.GetNestedType("SeekIntentSlot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SeekIntentSlot not found.");
        var scrubUpdateSlotType = controllerType.GetNestedType("ScrubUpdateIntentSlot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot not found.");
        var resolve = controllerType.GetMethod("ResolveSeekCommandPosition", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSeekCommandPosition not found.");
        var resolveScrub = controllerType.GetMethod("ResolveScrubUpdateCommandPosition", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveScrubUpdateCommandPosition not found.");
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");
        var sendEndScrub = controllerType.GetMethod("SendEndScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendEndScrubCommand not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var latestTicksField = seekSlotType.GetField("LatestTicks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SeekIntentSlot.LatestTicks not found.");
        var scrubLatestTicksField = scrubUpdateSlotType.GetField("LatestTicks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot.LatestTicks not found.");
        var queuedSeekSlotField = controllerType.GetField("_queuedSeekSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedSeekSlot not found.");
        var queuedScrubSlotField = controllerType.GetField("_queuedScrubUpdateSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedScrubUpdateSlot not found.");
        var commandChannelField = controllerType.GetField("_commandChannel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandChannel not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;
        var controller = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController construction failed.");
        using var disposableController = controller as IDisposable;

        var seekKind = Enum.Parse(commandKindType, "Seek");
        var updateScrubKind = Enum.Parse(commandKindType, "UpdateScrub");
        var playKind = Enum.Parse(commandKindType, "Play");
        var oneSecond = TimeSpan.FromSeconds(1);
        var twoSeconds = TimeSpan.FromSeconds(2);
        var threeSeconds = TimeSpan.FromSeconds(3);
        var fourSeconds = TimeSpan.FromSeconds(4);

        var slotA = Activator.CreateInstance(
                seekSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { oneSecond.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("SeekIntentSlot A construction failed.");
        var commandA = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand construction failed.");
        SetPropertyOrBackingField(commandA, "Kind", seekKind);
        SetPropertyOrBackingField(commandA, "Position", oneSecond);
        SetPropertyOrBackingField(commandA, "SeekSlot", slotA);

        queuedSeekSlotField.SetValue(controller, slotA);
        latestTicksField.SetValue(slotA, twoSeconds.Ticks);
        var resolvedCoalesced = resolve.Invoke(controller, new[] { commandA })
            ?? throw new InvalidOperationException("Resolve coalesced seek returned null.");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedCoalesced, "Position")!, "Coalesced seek slot resolves latest position");
        AssertEqual(null, queuedSeekSlotField.GetValue(controller), "Resolved active seek slot is cleared");

        latestTicksField.SetValue(slotA, oneSecond.Ticks);
        var slotB = Activator.CreateInstance(
                seekSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { threeSeconds.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("SeekIntentSlot B construction failed.");
        queuedSeekSlotField.SetValue(controller, slotB);
        var resolvedBarrier = resolve.Invoke(controller, new[] { commandA })
            ?? throw new InvalidOperationException("Resolve barrier seek returned null.");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedBarrier, "Position")!, "Older seek slot does not consume later barrier-separated target");
        if (!ReferenceEquals(slotB, queuedSeekSlotField.GetValue(controller)))
        {
            throw new InvalidOperationException("Later seek slot should remain queued after resolving older barrier-separated seek.");
        }

        var scrubSlotA = Activator.CreateInstance(
                scrubUpdateSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { oneSecond.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot A construction failed.");
        var updateCommandA = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand update construction failed.");
        SetPropertyOrBackingField(updateCommandA, "Kind", updateScrubKind);
        SetPropertyOrBackingField(updateCommandA, "Position", oneSecond);
        SetPropertyOrBackingField(updateCommandA, "ScrubUpdateSlot", scrubSlotA);

        queuedScrubSlotField.SetValue(controller, scrubSlotA);
        scrubLatestTicksField.SetValue(scrubSlotA, twoSeconds.Ticks);
        var resolvedScrubCoalesced = resolveScrub.Invoke(controller, new[] { updateCommandA })
            ?? throw new InvalidOperationException("Resolve coalesced scrub update returned null.");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedScrubCoalesced, "Position")!, "Coalesced scrub slot resolves latest position");
        AssertEqual(null, queuedScrubSlotField.GetValue(controller), "Resolved active scrub slot is cleared");

        scrubLatestTicksField.SetValue(scrubSlotA, oneSecond.Ticks);
        var scrubSlotB = Activator.CreateInstance(
                scrubUpdateSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { threeSeconds.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot B construction failed.");
        queuedScrubSlotField.SetValue(controller, scrubSlotB);
        var resolvedScrubBarrier = resolveScrub.Invoke(controller, new[] { updateCommandA })
            ?? throw new InvalidOperationException("Resolve barrier scrub update returned null.");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedScrubBarrier, "Position")!, "Older scrub slot does not consume later barrier-separated target");
        if (!ReferenceEquals(scrubSlotB, queuedScrubSlotField.GetValue(controller)))
        {
            throw new InvalidOperationException("Later scrub slot should remain queued after resolving older barrier-separated scrub update.");
        }

        var producerController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Producer FlashbackPlaybackController construction failed.");
        using var disposableProducerController = producerController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { oneSecond })!, "First producer seek enqueues");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { twoSeconds })!, "Adjacent producer seek coalesces");
        var playCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
        SetPropertyOrBackingField(playCommand, "Kind", playKind);
        AssertEqual(true, (bool)sendCommand.Invoke(producerController, new[] { playCommand })!, "Producer play barrier enqueues");
        AssertEqual(null, queuedSeekSlotField.GetValue(producerController), "Accepted non-seek barrier closes active seek slot before later seeks");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { threeSeconds })!, "Post-barrier producer seek enqueues new slot");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(producerController, new object[] { oneSecond })!, "Producer scrub update barrier enqueues");
        AssertEqual(null, queuedSeekSlotField.GetValue(producerController), "Accepted scrub update barrier closes active seek slot before later seeks");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { fourSeconds })!, "Post-scrub-barrier producer seek enqueues new slot");

        var channel = commandChannelField.GetValue(producerController)
            ?? throw new InvalidOperationException("Producer command channel missing.");
        var firstQueued = ReadQueuedPlaybackCommand(channel, commandType, "first queued command");
        var resolvedFirstQueued = resolve.Invoke(producerController, new[] { firstQueued })
            ?? throw new InvalidOperationException("Resolve first producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedFirstQueued, "Kind")?.ToString(), "First queued producer command kind");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedFirstQueued, "Position")!, "Adjacent producer seeks resolve to latest pre-barrier position");

        var secondQueued = ReadQueuedPlaybackCommand(channel, commandType, "second queued command");
        AssertEqual("Play", GetPropertyValue(secondQueued, "Kind")?.ToString(), "Second queued producer command is the barrier");

        var thirdQueued = ReadQueuedPlaybackCommand(channel, commandType, "third queued command");
        var resolvedThirdQueued = resolve.Invoke(producerController, new[] { thirdQueued })
            ?? throw new InvalidOperationException("Resolve third producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedThirdQueued, "Kind")?.ToString(), "Third queued producer command kind");
        AssertEqual(threeSeconds, (TimeSpan)GetPropertyValue(resolvedThirdQueued, "Position")!, "Post-barrier producer seek keeps its own position");

        var fourthQueued = ReadQueuedPlaybackCommand(channel, commandType, "fourth queued command");
        var resolvedFourthQueued = resolveScrub.Invoke(producerController, new[] { fourthQueued })
            ?? throw new InvalidOperationException("Resolve fourth producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedFourthQueued, "Kind")?.ToString(), "Fourth queued producer command is the scrub barrier");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedFourthQueued, "Position")!, "Scrub barrier command keeps its own position");

        var fifthQueued = ReadQueuedPlaybackCommand(channel, commandType, "fifth queued command");
        var resolvedFifthQueued = resolve.Invoke(producerController, new[] { fifthQueued })
            ?? throw new InvalidOperationException("Resolve fifth producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedFifthQueued, "Kind")?.ToString(), "Fifth queued producer command kind");
        AssertEqual(fourSeconds, (TimeSpan)GetPropertyValue(resolvedFifthQueued, "Position")!, "Post-scrub-barrier producer seek keeps its own position");
        AssertEqual(false, TryReadQueuedPlaybackCommand(channel, commandType, out _), "No extra producer commands are queued");

        var scrubProducerController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Scrub producer FlashbackPlaybackController construction failed.");
        using var disposableScrubProducerController = scrubProducerController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { oneSecond })!, "First producer scrub update enqueues");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { twoSeconds })!, "Adjacent producer scrub update coalesces");
        AssertEqual(true, (bool)sendEndScrub.Invoke(scrubProducerController, new object?[] { null })!, "Producer end scrub barrier enqueues");
        AssertEqual(null, queuedScrubSlotField.GetValue(scrubProducerController), "EndScrub closes active scrub slot before later updates");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { threeSeconds })!, "Post-barrier producer scrub update enqueues new slot");

        var scrubChannel = commandChannelField.GetValue(scrubProducerController)
            ?? throw new InvalidOperationException("Scrub producer command channel missing.");
        var firstScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "first queued scrub command");
        var resolvedFirstScrubQueued = resolveScrub.Invoke(scrubProducerController, new[] { firstScrubQueued })
            ?? throw new InvalidOperationException("Resolve first producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedFirstScrubQueued, "Kind")?.ToString(), "First queued producer scrub command kind");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedFirstScrubQueued, "Position")!, "Adjacent producer scrub updates resolve to latest pre-barrier position");

        var secondScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "second queued scrub command");
        AssertEqual("EndScrub", GetPropertyValue(secondScrubQueued, "Kind")?.ToString(), "Second queued producer scrub command is the barrier");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(secondScrubQueued, "Position")!, "EndScrub snapshots the latest pre-barrier scrub target");

        var thirdScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "third queued scrub command");
        var resolvedThirdScrubQueued = resolveScrub.Invoke(scrubProducerController, new[] { thirdScrubQueued })
            ?? throw new InvalidOperationException("Resolve third producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedThirdScrubQueued, "Kind")?.ToString(), "Third queued producer scrub command kind");
        AssertEqual(threeSeconds, (TimeSpan)GetPropertyValue(resolvedThirdScrubQueued, "Position")!, "Post-barrier producer scrub update keeps its own position");
        AssertEqual(false, TryReadQueuedPlaybackCommand(scrubChannel, commandType, out _), "No extra producer scrub commands are queued");

        return Task.CompletedTask;

        static object ReadQueuedPlaybackCommand(object channel, Type commandType, string label)
        {
            if (!TryReadQueuedPlaybackCommand(channel, commandType, out var command) || command is null)
            {
                throw new InvalidOperationException($"Expected {label}.");
            }

            return command;
        }

        static bool TryReadQueuedPlaybackCommand(object channel, Type commandType, out object? command)
        {
            var reader = channel.GetType().GetProperty("Reader")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel reader missing.");
            var tryRead = reader.GetType().GetMethod(
                    "TryRead",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { commandType.MakeByRefType() },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryRead not found.");
            object?[] args = { null };
            var result = (bool)tryRead.Invoke(reader, args)!;
            command = args[0];
            return result;
        }
    }


    internal static Task FlashbackPlaybackController_SeekSlots_PreserveSlotStateAfterRejectedBarriers()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var commandType = controllerType.GetNestedType("PlaybackCommand", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = controllerType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");
        var sendEndScrub = controllerType.GetMethod("SendEndScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendEndScrubCommand not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var queuedSeekSlotField = controllerType.GetField("_queuedSeekSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedSeekSlot not found.");
        var queuedScrubSlotField = controllerType.GetField("_queuedScrubUpdateSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedScrubUpdateSlot not found.");
        var commandChannelField = controllerType.GetField("_commandChannel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandChannel not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;

        var playKind = Enum.Parse(commandKindType, "Play");
        var playCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
        SetPropertyOrBackingField(playCommand, "Kind", playKind);
        var oneSecond = TimeSpan.FromSeconds(1);
        var twoSeconds = TimeSpan.FromSeconds(2);
        var failedBarrierController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed barrier FlashbackPlaybackController construction failed.");
        using var disposableFailedBarrierController = failedBarrierController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(failedBarrierController, new object[] { oneSecond })!, "Failed-barrier setup seek enqueues");
        var failedBarrierSlot = queuedSeekSlotField.GetValue(failedBarrierController)
            ?? throw new InvalidOperationException("Failed-barrier seek slot missing.");
        var failedBarrierChannel = commandChannelField.GetValue(failedBarrierController)
            ?? throw new InvalidOperationException("Failed-barrier command channel missing.");
        CompleteQueuedPlaybackCommands(failedBarrierChannel);
        AssertEqual(false, (bool)sendCommand.Invoke(failedBarrierController, new[] { playCommand })!, "Rejected play barrier reports failure");
        if (!ReferenceEquals(failedBarrierSlot, queuedSeekSlotField.GetValue(failedBarrierController)))
        {
            throw new InvalidOperationException("Rejected play barrier should preserve the active seek slot.");
        }

        var failedSeekController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed seek FlashbackPlaybackController construction failed.");
        using var disposableFailedSeekController = failedSeekController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedSeekController, new object[] { oneSecond })!, "Failed-seek setup scrub update enqueues");
        var failedSeekScrubSlot = queuedScrubSlotField.GetValue(failedSeekController)
            ?? throw new InvalidOperationException("Failed-seek scrub slot missing.");
        var failedSeekChannel = commandChannelField.GetValue(failedSeekController)
            ?? throw new InvalidOperationException("Failed-seek command channel missing.");
        CompleteQueuedPlaybackCommands(failedSeekChannel);
        AssertEqual(false, (bool)sendSeek.Invoke(failedSeekController, new object[] { twoSeconds })!, "Rejected seek barrier reports failure");
        if (!ReferenceEquals(failedSeekScrubSlot, queuedScrubSlotField.GetValue(failedSeekController)))
        {
            throw new InvalidOperationException("Rejected seek should preserve the active scrub slot.");
        }
        AssertEqual(null, queuedSeekSlotField.GetValue(failedSeekController), "Rejected seek clears only its own newly-created seek slot");

        var failedScrubUpdateController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed scrub update FlashbackPlaybackController construction failed.");
        using var disposableFailedScrubUpdateController = failedScrubUpdateController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(failedScrubUpdateController, new object[] { oneSecond })!, "Failed-scrub-update setup seek enqueues");
        var failedScrubUpdateSeekSlot = queuedSeekSlotField.GetValue(failedScrubUpdateController)
            ?? throw new InvalidOperationException("Failed-scrub-update seek slot missing.");
        var failedScrubUpdateChannel = commandChannelField.GetValue(failedScrubUpdateController)
            ?? throw new InvalidOperationException("Failed-scrub-update command channel missing.");
        CompleteQueuedPlaybackCommands(failedScrubUpdateChannel);
        AssertEqual(false, (bool)sendUpdateScrub.Invoke(failedScrubUpdateController, new object[] { twoSeconds })!, "Rejected scrub update barrier reports failure");
        if (!ReferenceEquals(failedScrubUpdateSeekSlot, queuedSeekSlotField.GetValue(failedScrubUpdateController)))
        {
            throw new InvalidOperationException("Rejected scrub update should preserve the active seek slot.");
        }
        AssertEqual(null, queuedScrubSlotField.GetValue(failedScrubUpdateController), "Rejected scrub update clears only its own newly-created scrub slot");

        var failedEndScrubController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed end scrub FlashbackPlaybackController construction failed.");
        using var disposableFailedEndScrubController = failedEndScrubController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedEndScrubController, new object[] { oneSecond })!, "Failed-end-scrub setup update enqueues");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedEndScrubController, new object[] { twoSeconds })!, "Failed-end-scrub setup update coalesces");
        var failedEndScrubSlot = queuedScrubSlotField.GetValue(failedEndScrubController)
            ?? throw new InvalidOperationException("Failed-end-scrub slot missing.");
        var failedEndScrubChannel = commandChannelField.GetValue(failedEndScrubController)
            ?? throw new InvalidOperationException("Failed-end-scrub command channel missing.");
        CompleteQueuedPlaybackCommands(failedEndScrubChannel);
        AssertEqual(false, (bool)sendEndScrub.Invoke(failedEndScrubController, new object?[] { null })!, "Rejected end scrub barrier reports failure");
        if (!ReferenceEquals(failedEndScrubSlot, queuedScrubSlotField.GetValue(failedEndScrubController)))
        {
            throw new InvalidOperationException("Rejected end scrub should preserve the active scrub slot.");
        }

        return Task.CompletedTask;

        static void CompleteQueuedPlaybackCommands(object channel)
        {
            var writer = channel.GetType().GetProperty("Writer")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel writer missing.");
            var tryComplete = writer.GetType().GetMethod(
                    "TryComplete",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { typeof(Exception) },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryComplete not found.");
            _ = (bool)tryComplete.Invoke(writer, new object?[] { null })!;
        }
    }
    private static string ReadFlashbackPlaybackControllerPlaybackSource()
        => ReadFlashbackPlaybackControllerSource();

    internal static Task FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var threadLifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs")
            .Replace("\r\n", "\n");
        var threadLoopText = ExtractTextBetween(
            threadLifecycleText,
            "private void PlaybackThreadEntry(",
            "    private bool EnsurePlaybackThread(");
        var threadCommandDispatchText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs")
            .Replace("\r\n", "\n");
        var threadSeekCommandsText = threadCommandDispatchText;
        var threadSeekScrubCommandsText = threadCommandDispatchText;
        var threadEndScrubCommandText = threadCommandDispatchText;
        var threadPlayCommandText = threadCommandDispatchText;
        var threadPauseCommandText = threadCommandDispatchText;
        var threadNudgeCommandText = threadCommandDispatchText;
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var commandQueueText = rootText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(threadLoopText, "private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)");
        AssertContains(commandQueueText, "private enum CommandKind");
        AssertContains(commandQueueText, "private readonly struct PlaybackCommand");
        AssertContains(commandQueueText, "public SeekIntentSlot? SeekSlot { get; init; }");
        AssertContains(commandQueueText, "public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }");
        AssertContains(rootText, "private enum CommandKind");
        AssertContains(rootText, "private readonly struct PlaybackCommand");
        AssertContains(threadLifecycleText, "[DllImport(\"winmm.dll\", ExactSpelling = true)]");
        AssertContains(threadLifecycleText, "private static extern uint timeBeginPeriod(uint uMilliseconds);");
        AssertContains(threadLifecycleText, "private static extern uint timeEndPeriod(uint uMilliseconds);");
        AssertContains(threadLoopText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_ENTER\");");
        AssertContains(threadCommandDispatchText, "private bool ExecutePlaybackCommand(");
        AssertContains(threadSeekCommandsText, "private void HandleSeekCommand(");
        AssertContains(threadSeekScrubCommandsText, "private void HandleBeginScrubCommand(");
        AssertContains(threadSeekScrubCommandsText, "private void HandleUpdateScrubCommand(");
        AssertContains(threadEndScrubCommandText, "private void HandleEndScrubCommand(");
        AssertContains(threadSeekCommandsText, "private void HandleBeginScrubCommand(");
        AssertContains(threadSeekCommandsText, "private void HandleUpdateScrubCommand(");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.ThreadSeekCommands.cs")), "Seek/scrub command handlers stay folded into ThreadCommands.cs");
        AssertContains(threadPlayCommandText, "private void HandlePlayCommand(");
        AssertContains(threadPlayCommandText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, seekTarget, \"play\", cts.Token);");
        AssertContains(threadPauseCommandText, "private void HandlePauseCommand(");
        AssertContains(threadCommandDispatchText, "private void HandleGoLiveCommand(");
        AssertContains(threadNudgeCommandText, "private void HandleNudgeCommand(");
        AssertContains(threadSeekCommandsText, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertContains(threadSeekScrubCommandsText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertContains(threadCommandDispatchText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");");
        AssertContains(agentMapText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(agentMapText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(agentMapText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(cleanupPlanText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(cleanupPlanText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(cleanupPlanText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(agentMapText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(cleanupPlanText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(threadCommandDispatchText, "HandleSeekCommand(ref cmd, commandChannel, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref frozenValidStart, ref pendingExactResumeTarget, ref frameDuration, prebufferedFrames, pacingStopwatch);");
        AssertContains(threadCommandDispatchText, "HandleGoLiveCommand(ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref pendingExactResumeTarget);");
        AssertContains(threadLoopText, "if (!ExecutePlaybackCommand(ref cmd, commandChannel, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref frozenValidStart, ref pendingExactResumeTarget, ref frameDuration, prebufferedFrames, pacingStopwatch))");
        AssertDoesNotContain(threadLoopText, "case CommandKind.Seek:");
        AssertDoesNotContain(threadLoopText, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertDoesNotContain(threadLoopText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertDoesNotContain(threadLoopText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");");
        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })\n            {\n                SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });\n            }");
        AssertContains(threadCommandDispatchText, "case CommandKind.Stop:\n                    HandleStopCommand(ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref pendingExactResumeTarget);\n                    return false;");
        AssertContains(threadCommandDispatchText, "private void HandleStopCommand(");
        AssertContains(threadCommandDispatchText, "isPlaying = false;\n        isScrubbing = false;\n        pendingExactResumeTarget = null;");
        AssertContains(threadCommandDispatchText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_stop\");\n        Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT\");");
        AssertDoesNotContain(threadLoopText, "isPlaying = false;\n                            isScrubbing = false;\n                            pendingExactResumeTarget = null;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_stop\");");
        AssertContains(sourceText, "private void RestoreLiveForPlaybackThreadExit(");
        AssertContains(sourceText, "Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        SetState(FlashbackPlaybackState.Live);");
        AssertDoesNotContain(sourceText, "_suppressAudioUntilPtsTicks");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.GoLive, \"live_thread_not_running\");\n            return false;\n        }");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.Nudge, \"live_thread_not_running\", delta: delta);\n            return false;\n        }");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_NOOP kind={kind} reason={reason}{FormatCommandDetail(position, delta)}");
        AssertContains(threadLifecycleText, "private bool EnsurePlaybackThread(CommandKind commandKind)");
        AssertContains(threadLifecycleText, "private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);");
        AssertContains(threadLifecycleText, "private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);");
        AssertContains(threadLifecycleText, "private const int CommandQueueCapacity = 256;");
        AssertContains(threadLifecycleText, "private readonly object _playbackThreadSync = new();");
        AssertContains(threadLifecycleText, "private Thread? _playbackThread;");
        AssertContains(threadLifecycleText, "private int _playbackThreadStarted;");
        AssertContains(threadLifecycleText, "private CancellationTokenSource? _playCts;");
        AssertContains(threadLifecycleText, "private Channel<PlaybackCommand> _commandChannel;");
        AssertDoesNotContain(rootText, "private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);");
        AssertDoesNotContain(rootText, "private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);");
        AssertDoesNotContain(rootText, "private const int CommandQueueCapacity = 256;");
        AssertDoesNotContain(rootText, "private readonly object _playbackThreadSync = new();");
        AssertDoesNotContain(rootText, "private Thread? _playbackThread;");
        AssertDoesNotContain(rootText, "private int _playbackThreadStarted;");
        AssertDoesNotContain(rootText, "private CancellationTokenSource? _playCts;");
        AssertDoesNotContain(rootText, "private Channel<PlaybackCommand> _commandChannel;");
        AssertContains(threadLifecycleText, "lock (_playbackThreadSync)");
        AssertContains(sourceText, "if (_disposedFlag != 0) return RejectCommand(commandKind, \"disposed\", \"disposed\", false);");
        AssertContains(threadLifecycleText, "if (Volatile.Read(ref _playbackThreadStarted) != 0)\n            {\n                if (_playbackThread is { IsAlive: true })");
        AssertContains(threadLifecycleText, "FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped");
        AssertContains(threadLifecycleText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped\");\n                DrainAbandonedCommandsOnThreadExit(_commandChannel);");
        AssertContains(threadLifecycleText, "DisposePlaybackCtsBestEffort(_playCts, \"recover_stale_thread\");");
        AssertContains(threadLifecycleText, "Volatile.Write(ref _playbackThreadStarted, 0);\n            }\n\n            if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)");
        AssertContains(threadLifecycleText, "private bool StopPlaybackThread(");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_UPDATE_SKIP reason=disposed");
        AssertContains(commandQueueText, "public int CommandQueueCapacityCommands => CommandQueueCapacity;");
        AssertContains(commandQueueText, "public long CommandsEnqueued => Interlocked.Read(ref _commandsEnqueued);");
        AssertContains(commandQueueText, "public long CommandsSkippedNotReady => Interlocked.Read(ref _commandsSkippedNotReady);");
        AssertContains(commandQueueText, "public string LastCommandFailure => Volatile.Read(ref _lastCommandFailure);");
        AssertContains(commandQueueText, "public bool PlaybackThreadAlive => _playbackThread is { IsAlive: true };");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.CommandTelemetry.cs")), "Flashback playback command telemetry stays folded into the root controller");
        AssertContains(commandQueueText, "private long _commandsEnqueued;");
        AssertContains(commandQueueText, "private long _commandsProcessed;");
        AssertContains(commandQueueText, "private long _commandsDropped;");
        AssertContains(commandQueueText, "private long _commandsSkippedNotReady;");
        AssertContains(commandQueueText, "private int _pendingCommands;");
        AssertContains(commandQueueText, "private int _maxPendingCommands;");
        AssertContains(commandQueueText, "private long _lastCommandQueueLatencyMs;");
        AssertContains(commandQueueText, "private long _maxCommandQueueLatencyMs;");
        AssertContains(commandQueueText, "private string _lastCommandFailure = string.Empty;");
        AssertContains(commandQueueText, "private bool IsReady => _initialized && _disposedFlag == 0;");
        AssertContains(commandQueueText, "private bool IsNotReady(CommandKind kind, TimeSpan? position = null)");
        AssertContains(commandQueueText, "private bool RejectCommand(");
        AssertContains(commandQueueText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(commandQueueText, "private void SetLastCommandFailure(string failure)");
        AssertContains(commandQueueText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(commandQueueText, "private int _activeCommandKind = -1;");
        AssertContains(commandQueueText, "private long _activeCommandStartedTimestamp;");
        AssertContains(rootText, "private long _commandsEnqueued;");
        AssertContains(rootText, "private int _pendingCommands;");
        AssertContains(rootText, "private string _lastCommandFailure = string.Empty;");
        AssertContains(rootText, "private int _activeCommandKind = -1;");
        AssertContains(sourceText, "private Channel<PlaybackCommand> _commandChannel;");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(threadLifecycleText, "private Channel<PlaybackCommand> CreateCommandChannel()");
        AssertContains(threadLifecycleText, "Channel.CreateBounded<PlaybackCommand>");
        AssertContains(threadLifecycleText, "new BoundedChannelOptions(CommandQueueCapacity)");
        AssertContains(threadLifecycleText, "FullMode = BoundedChannelFullMode.Wait");
        AssertContains(sourceText, "private bool IsCommandChannelOpenForDropRetry()");
        AssertContains(sourceText, "private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)");
        AssertContains(sourceText, "private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_DROP_OLD kind={droppedCommand.Kind}{detail} new_kind={newCommandKind} reason=channel_full");
        AssertContains(sourceText, "private void ClearQueuedCommandSlotForDroppedCommand(PlaybackCommand command)");
        AssertDoesNotContain(sourceText, "Channel.CreateUnbounded<PlaybackCommand>");
        AssertContains(threadLifecycleText, "catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(threadLifecycleText, "DisposePlaybackCtsBestEffort(_playCts, \"thread_start_fail\");");
        AssertContains(threadLifecycleText, "_playbackThread = null;\n                Interlocked.Exchange(ref _playbackThreadStarted, 0);");
        AssertContains(threadLifecycleText, "return RejectCommand(\n                    commandKind,\n                    $\"thread_start_failed:{ex.GetType().Name}:{ex.Message}\",\n                    $\"thread_start_failed type={ex.GetType().Name}\",\n                    false);");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");\n        return;");
        AssertContains(sourceText, "var commandChannel = _commandChannel;");
        AssertContains(sourceText, "_playbackThread = new Thread(() => PlaybackThreadEntry(threadCts, commandChannel))");
        AssertContains(sourceText, "private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK");
        AssertContains(sourceText, "SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY");
        AssertContains(threadLifecycleText, "private readonly string _playbackMmcssTask = Environment.GetEnvironmentVariable(\"SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK\") ?? \"Playback\";");
        AssertContains(threadLifecycleText, "private readonly int _playbackMmcssPriority = EnvironmentHelpers.GetIntFromEnv(\"SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY\", 1, -2, 2);");
        AssertDoesNotContain(rootText, "private readonly string _playbackMmcssTask");
        AssertDoesNotContain(rootText, "private readonly int _playbackMmcssPriority");
        AssertContains(sourceText, "using var mmcss = MmcssThreadRegistration.TryRegister(_playbackMmcssTask, _playbackMmcssPriority, message => Logger.Log(message));");
        AssertContains(sourceText, "var canRead = commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();");
        AssertContains(sourceText, "if (!canRead)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed\");\n                            isScrubbing = false;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"channel_closed\");");
        AssertContains(sourceText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_disposed\");\n                            return;\n                        }");
        AssertContains(sourceText, "if (_disposedFlag != 0)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT\");\n                            isScrubbing = false;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_disposed\");");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");");
        AssertContains(sourceText, "catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'\");\n            }");
        AssertContains(threadLoopText, "finally\n        {\n            CompletePlaybackThreadExit(prebufferedFrames, cts, commandChannel);\n        }");
        AssertContains(threadLifecycleText, "private void CompletePlaybackThreadExit(");
        AssertContains(threadLifecycleText, "ClearPrebufferedFrames(prebufferedFrames, \"thread_exit\");\n        timeEndPeriod(1);");
        AssertDoesNotContain(threadLoopText, "ClearPrebufferedFrames(prebufferedFrames, \"thread_exit\");\n            timeEndPeriod(1);");
        AssertContains(threadLifecycleText, "private bool StopPlaybackThread(TimeSpan timeout, string operation)");
        AssertContains(threadLifecycleText, "var threadExited = true;");
        AssertContains(threadLifecycleText, "if (ReferenceEquals(Thread.CurrentThread, thread))\n                {\n                    Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_JOIN_SKIP reason=self\");\n                    SetLastCommandFailure(\"thread_join_skipped:self\");\n                    threadExited = false;\n                }");
        AssertContains(sourceText, "private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);");
        AssertContains(sourceText, "private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);");
        AssertContains(threadLifecycleText, "Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT op={operation} timeout_ms={timeout.TotalMilliseconds:0}\");\n                    SetLastCommandFailure($\"thread_join_timeout:{operation}\");\n                    threadExited = false;");
        AssertContains(threadLifecycleText, "SetLastCommandFailure(\"thread_join_skipped:self\");");
        AssertContains(threadLifecycleText, "SetLastCommandFailure($\"thread_join_timeout:{operation}\");");
        AssertContains(threadLifecycleText, "FLASHBACK_PLAYBACK_STOP_THREAD_COMPLETE op={operation} duration_ms=");
        AssertContains(threadLifecycleText, "thread_was_alive={threadWasAlive} thread_exited={threadExited}");
        AssertContains(threadLifecycleText, "active_at_request={activeKindAtRequest} active_ms_at_request={activeElapsedMsAtRequest:0.###}");
        AssertContains(threadLifecycleText, "if (threadExited)\n            {\n                ApplyDeferredPreviewAttachAfterStopTimeout();\n                DisposePlaybackCtsBestEffort(_playCts, \"stop_thread\");");
        AssertContains(threadLifecycleText, "Interlocked.Exchange(ref _pendingCommands, 0);\n                ClearQueuedCommandSlotsBarrier();\n                Volatile.Write(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);");
        AssertContains(sourceText, "Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);");
        AssertContains(threadCommandDispatchText, "Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);");
        AssertContains(threadCommandDispatchText, "Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);");
        AssertContains(threadCommandDispatchText, "FLASHBACK_PLAYBACK_CMD_COMPLETE kind={cmd.Kind} duration_ms={commandElapsedMs:0.###}");
        AssertDoesNotContain(threadLoopText, "Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);");
        AssertContains(sourceText, "private static string FormatActiveCommandKind(int rawKind)");
        AssertContains(sourceText, "private double GetActiveCommandElapsedMs(long nowTimestamp)");
        AssertContains(sourceText, "if (cts.IsCancellationRequested)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_cancelled\");");
        AssertContains(sourceText, "PaceAndDecodeFrame(decoder, prebufferedFrames, commandChannel, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, \"end_scrub\", cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, \"play\", cts.Token)");
        AssertContains(sourceText, "TryDecodeNextVideoFrameWithMetrics(decoder, out var nudgeFrame, cts.Token)");
        AssertContains(sourceText, "CancellationToken cancellationToken)\n    {\n        try\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        var playbackFramesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackLoopText = playbackFramesText;
        AssertContains(playbackFramesText, "private bool TryReadNextPlaybackFrame(");
        AssertContains(playbackFramesText, "private void ClearPrebufferedFrames(");
        AssertContains(playbackFramesText, "private bool TryResolveAudioDriftFrameSkip(");
        AssertContains(playbackLoopText, "TryResolveAudioDriftFrameSkip(");
        AssertContains(sourceText, "while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "if (commandChannel.Reader.TryPeek(out _))\n            {\n                ReleaseHeldFrameBestEffort(videoFrame, \"av_sync_skip_command_pending\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_COMMAND_PENDING count={skipped}");
        AssertContains(sourceText, "const double FrameSkipThresholdMs = 500.0;");
        // Frame-skip catch-up loop must re-sync the audio clock each iteration so a
        // long catch-up burst does not extrapolate from a stale wall-time anchor.
        AssertContains(sourceText, "private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)");
        AssertContains(sourceText, "if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out var driftMs) ||\n            driftMs >= -FrameSkipThresholdMs)");
        AssertContains(sourceText, "if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out driftMs))\n            {\n                break;\n            }");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_EOS count={skipped}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_BUDGET count={skipped}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH");
        AssertContains(sourceText, "nextSegmentStart.Value - lastFrameAbsPts > TimeSpan.FromMilliseconds(250)");
        AssertContains(sourceText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            throw;\n        }\n        catch (Exception ex)\n        {\n            SnapToLiveOnError(decoder, ex, ref fileOpen);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(operation);");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");\n            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_cancelled\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FATAL type={ex.GetType().Name} error='{ex.Message}'\");\n            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_fatal\");");
        AssertContains(sourceText, "var decoderToDispose = decoder;\n            decoder = null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE was_open={wasOpen}");
        AssertContains(sourceText, "release_ms={releaseMs:0.###} close_ms={closeMs:0.###} dispose_ms={disposeMs:0.###} total_ms={totalMs:0.###}");
        AssertContains(sourceText, "fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(threadLifecycleText, "CompleteCommandChannelForThreadExit(commandChannel);\n        DrainAbandonedCommandsOnThreadExit(commandChannel);");
        AssertContains(threadLifecycleText, "private static void CompleteCommandChannelForThreadExit(Channel<PlaybackCommand> commandChannel)");
        AssertContains(threadLifecycleText, "commandChannel.Writer.TryComplete();");
        AssertContains(threadLifecycleText, "FLASHBACK_PLAYBACK_CHANNEL_COMPLETE_WARN");
        AssertContains(threadLifecycleText, "Interlocked.Add(ref _commandsDropped, abandoned);");
        AssertContains(threadLifecycleText, "if (string.IsNullOrEmpty(Volatile.Read(ref _lastCommandFailure)))\n            {\n                SetLastCommandFailure($\"abandoned_on_exit:{abandoned}\");\n            }");
        AssertContains(threadLifecycleText, "Interlocked.Exchange(ref _pendingCommands, 0);");
        AssertDoesNotContain(threadLoopText, "CompleteCommandChannelForThreadExit(commandChannel);\n            DrainAbandonedCommandsOnThreadExit(commandChannel);");
        AssertContains(sourceText, "var ownsPlaybackThread = ReferenceEquals(Thread.CurrentThread, _playbackThread);");
        AssertContains(threadLifecycleText, "var ownsPlaybackThread = ReferenceEquals(Thread.CurrentThread, _playbackThread);");
        AssertContains(sourceText, "var ownsCts = ReferenceEquals(cts, _playCts);");
        AssertContains(threadLifecycleText, "if (ownsPlaybackThread)\n        {\n            _playbackThread = null;\n        }");
        AssertContains(sourceText, "_playbackThread = null;");
        AssertContains(sourceText, "StopPlaybackThread(PlaybackThreadStopTimeout, \"dispose\");\n        _initialized = false;\n        Logger.Log(\"FLASHBACK_PLAYBACK_DISPOSED\");");
        AssertContains(sourceText, "if (_disposedFlag != 0 && command.Kind != CommandKind.Stop)\n        {\n            return RejectCommand(command.Kind, \"disposed\", \"disposed\", false);\n        }");
        AssertContains(threadLifecycleText, "if (ownsCts)\n        {\n            _playCts = null;\n        }\n        DisposePlaybackCtsBestEffort(cts, \"thread_exit\");");
        AssertContains(sourceText, "private static void DisposePlaybackCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CTS_DISPOSE_WARN");
        AssertContains(threadLifecycleText, "if (ownsPlaybackThread || ownsCts)\n        {\n            Volatile.Write(ref _playbackThreadStarted, 0);\n        }");
        AssertContains(sourceText, "Interlocked.Increment(ref _commandsEnqueued);\n        UpdateMaxPendingCommands(pending);\n        MarkCommandQueued(command.Kind);\n        return true;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DiscardedAudioFramesAreUnreffed()
    {
        var sourceText = ReadFlashbackDecoderSource();

        var audioDecodeBlock = ExtractTextBetween(
            sourceText,
            "private void DecodeAndDeliverAudioPacket",
            "private DecodedVideoFrame ConvertAndOutputVideoFrame()");
        AssertContains(audioDecodeBlock, "if (callback == null)\n            {\n                ffmpeg.av_frame_unref(_audioFrame);\n                continue; // Codec advanced, but no delivery during seek/scrub\n            }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)\n        {\n            _videoCodecCtx->thread_count = 1;\n        }");
        AssertOccursBefore(
            sourceText,
            "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)",
            "ThrowIfError(\n            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_PtsConversionRejectsInvalidTimestamps()
    {
        var sourceText = ReadFlashbackDecoderSource();
        var timestampFragmentPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Flashback",
            "FlashbackDecoder.Timestamps.cs");

        AssertEqual(false, File.Exists(timestampFragmentPath), "Flashback decoder timestamp helpers stay folded into caller owners");
        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_videoFrame), _videoTimeBase);");
        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_audioFrame), _audioTimeBase);");
        AssertContains(sourceText, "var streamTimestamp = ToStreamTimestamp(target, _videoTimeBase);");
        AssertContains(sourceText, "_formatCtx, _videoStreamIndex, streamTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);");
        AssertContains(sourceText, "var timestampUs = ToAvTimeBaseTimestamp(target);");
        AssertContains(sourceText, "_formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);");
        AssertContains(sourceText, "FLASHBACK_DECODER_SEEK_FALLBACK_OK");
        AssertContains(sourceText, "private static TimeSpan DecodePtsToTimeSpan(long pts, AVRational timeBase)");
        AssertContains(sourceText, "private static long ResolveBestEffortFrameTimestamp(AVFrame* frame)");
        AssertContains(sourceText, "frame->best_effort_timestamp != ffmpeg.AV_NOPTS_VALUE");
        AssertContains(sourceText, "return frame->best_effort_timestamp;");
        AssertContains(sourceText, "return frame->pts;");
        AssertContains(sourceText, "if (pts == ffmpeg.AV_NOPTS_VALUE || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(sourceText, "private static long ToAvTimeBaseTimestamp(TimeSpan value)");
        AssertContains(sourceText, "private static long ToStreamTimestamp(TimeSpan value, AVRational timeBase)");
        AssertContains(sourceText, "if (value <= TimeSpan.Zero || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "var timestamp = value.TotalSeconds * timeBase.den / timeBase.num;");
        AssertContains(sourceText, "if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(timestamp) || timestamp >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "private bool _suppressRecoverableSeekLogsForNextVideoFrame;");
        AssertContains(sourceText, "_suppressRecoverableSeekLogsForNextVideoFrame = true;");
        AssertContains(sourceText, "using var recoverableSeekLogScope = BeginRecoverableSeekLogSuppressionIfNeeded();");
        AssertContains(sourceText, "private IDisposable? BeginRecoverableSeekLogSuppressionIfNeeded()");
        AssertContains(sourceText, "return LibAvEncoder.SuppressRecoverableSeekFfmpegLogs();");
        AssertContains(sourceText, "_suppressRecoverableSeekLogsForNextVideoFrame = false;");
        AssertDoesNotContain(sourceText, "(long)(target.TotalSeconds * ffmpeg.AV_TIME_BASE)");
        AssertDoesNotContain(sourceText, "var seconds = (double)_videoFrame->pts * _videoTimeBase.num / _videoTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "var seconds = (double)_audioFrame->pts * _audioTimeBase.num / _audioTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "DecodePtsToTimeSpan(_videoFrame->pts, _videoTimeBase)");
        AssertDoesNotContain(sourceText, "DecodePtsToTimeSpan(_audioFrame->pts, _audioTimeBase)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_InputStreamsAndFrameSizesAreBounded()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(sourceText, "private const int MaxDecodedVideoDimension = 8192;");
        AssertContains(sourceText, "private const int MaxDecodedVideoFrameBytes = 512 * 1024 * 1024;");
        AssertContains(sourceText, "private const int MaxMpegTsProbeSizeBytes = 20 * 1024 * 1024;");
        AssertContains(sourceText, "private const int MaxMpegTsAnalyzeDurationUs = 5 * 1000 * 1000;");
        AssertContains(sourceText, "_formatCtx->probesize = MaxMpegTsProbeSizeBytes;");
        AssertContains(sourceText, "_formatCtx->max_analyze_duration = MaxMpegTsAnalyzeDurationUs;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_formatCtx, out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "if (!IsValidStreamIndex(_videoStreamIndex, streamCount))");
        AssertContains(sourceText, "if (_audioStreamIndex >= 0 && !IsValidStreamIndex(_audioStreamIndex, streamCount))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_stream_index");
        AssertContains(sourceText, "ValidateVideoDimensions(_videoWidth, _videoHeight);");
        AssertContains(sourceText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertContains(sourceText, "width > MaxDecodedVideoDimension");
        AssertContains(sourceText, "height > MaxDecodedVideoDimension");
        AssertContains(sourceText, "(width & 1) != 0");
        AssertContains(sourceText, "var pixels = (long)width * height;");
        AssertContains(sourceText, "if (bytes <= 0 || bytes > MaxDecodedVideoFrameBytes || bytes > int.MaxValue)");
        AssertDoesNotContain(sourceText, "return width * height * 2 + width * (height / 2) * 2;");
        AssertDoesNotContain(sourceText, "return width * height + width * (height / 2);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_AudioOutputBuffersAreBounded()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private const int MaxDecodedAudioFrameBytes = 16 * 1024 * 1024;");
        AssertContains(sourceText, "byte[]? result = null;\n        var returnResultToPool = true;");
        AssertContains(sourceText, "if (inputSamples <= 0)\n            {\n                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };\n            }");
        AssertContains(sourceText, "maxOutputSamples = ToBoundedAudioSampleCount((long)inputSamples * 2);");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(maxOutputSamples, out var outputBytesNeeded))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_output_size");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(outputSamplesProduced, out var validBytes) || validBytes > result.Length)");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_converted_size");
        AssertContains(sourceText, "returnResultToPool = false;");
        AssertContains(sourceText, "finally\n        {\n            ffmpeg.av_frame_unref(_audioFrame);\n            if (returnResultToPool && result is { Length: > 0 })");
        AssertContains(sourceText, "ArrayPool<byte>.Shared.Return(result);");
        AssertContains(sourceText, "private static int ToBoundedAudioSampleCount(long sampleCount)");
        AssertContains(sourceText, "private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)");
        AssertContains(sourceText, "var calculated = (long)sampleCount * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var outputBytesNeeded = maxOutputSamples * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var validBytes = outputSamplesProduced * OutputAudioChannels * sizeof(float);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_AudioSetupLivesWithPlaybackPacketFeed()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "private void InitializeAudioDecoder()");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler()");
        AssertContains(playbackText, "private void InitializeAudioDecoder()");
        AssertContains(playbackText, "private void InitializeAudioResampler()");
        AssertContains(playbackText, "private void DecodeAndDeliverAudioPacket(AVPacket* packet)");
        AssertContains(playbackText, "private DecodedAudioChunk ConvertAndOutputAudioFrame()");
        AssertContains(playbackText, "FLASHBACK_DECODER_AUDIO codec=");
        AssertContains(playbackText, "swr_alloc_set_opts2");
        AssertContains(playbackText, "DecodeAndDeliverAudioPacket(_packet);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.AudioOutput.cs")),
            "Flashback decoder audio output folded into playback packet feed owner");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_SoftwareFramePlanesAreValidated()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (actualFormat != AVPixelFormat.AV_PIX_FMT_NONE && actualFormat != _decodedPixelFormat)");
        AssertContains(sourceText, "if (!TryValidateSoftwareVideoFrame(_videoFrame, _decodedPixelFormat, _videoWidth, _videoHeight, _isHdr, out var frameFailure))");
        AssertContains(sourceText, "FLASHBACK_DECODER_VIDEO_WARN reason=invalid_software_frame");
        AssertContains(sourceText, "ffmpeg.av_frame_unref(_videoFrame);\n            return default;");
        var softwareOutputBlock = ExtractTextBetween(
            sourceText,
            "var outputSize = CalculateFrameBufferSize(_videoWidth, _videoHeight, _isHdr);",
            "    private void CopyFramePlanesToBuffer");
        AssertContains(softwareOutputBlock, "finally\n        {\n            ffmpeg.av_frame_unref(_videoFrame);\n        }");
        AssertOccursBefore(softwareOutputBlock, "CopyFramePlanesToBuffer((byte*)dataPtr, outputSize);", "finally\n        {\n            ffmpeg.av_frame_unref(_videoFrame);\n        }");
        AssertContains(sourceText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertContains(sourceText, "width_mismatch frame={frame->width} expected={width}");
        AssertContains(sourceText, "height_mismatch frame={frame->height} expected={height}");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P10LE");
        AssertContains(sourceText, "failure = $\"unsupported_format:{format}\";");
        AssertContains(sourceText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertContains(sourceText, "var plane = (uint)planeIndex;");
        AssertContains(sourceText, "failure = $\"plane_{planeIndex}_null\";");
        AssertContains(sourceText, "failure = $\"plane_{planeIndex}_linesize:{frame->linesize[plane]}<{minLineSize}\";");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_D3D11FramesAreValidated()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (!TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight, out var d3dFrameFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_DECODER_VIDEO_WARN reason=invalid_d3d11_frame detail='{d3dFrameFailure}' w={_videoWidth} h={_videoHeight}\");\n                ffmpeg.av_frame_free(&clonedFrame);\n                return default;");
        AssertContains(sourceText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(sourceText, "failure = \"texture_null\";");
        AssertContains(sourceText, "failure = $\"subresource_out_of_range:{subresource}\";");
        AssertOccursBefore(sourceText, "TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight", "var texturePtr = (IntPtr)clonedFrame->data[0];");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_HeldFrameCleanupIsBestEffort()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FLASHBACK_DECODER_RELEASE_HELD_FRAME_WARN");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_pendingVideoFrame, \"seek_keyframe_pending\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_replace_best\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_best_superseded\");");
        AssertContains(sourceText, "var bestFrameTransferred = false;");
        AssertContains(sourceText, "bestFrameTransferred = true;\n                        return true;");
        AssertContains(sourceText, "finally\n        {\n            if (!bestFrameTransferred && bestFrame != null)\n            {\n                ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_best_abandoned\");\n            }\n        }");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_pendingVideoFrame, \"close_pending\");");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DecodeLoopsObserveCancellation()
    {
        var sourceText = ReadFlashbackDecoderSource();
        var decodeLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "if (!SeekToKeyframe(target, cancellationToken))");
        AssertContains(sourceText, "if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))");
        AssertContains(sourceText, "if (!FeedNextVideoPacket(cancellationToken))");
        AssertContains(sourceText, "cancellationToken.ThrowIfCancellationRequested();");

        var seekToBlock = ExtractTextBetween(
            sourceText,
            "public bool SeekTo(TimeSpan target",
            "    /// <summary>\n    /// Decodes the next video frame.");
        AssertContains(seekToBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertOccursBefore(seekToBlock, "cancellationToken.ThrowIfCancellationRequested();\n                if (!TryDecodeNextVideoFrame", "if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))");

        AssertContains(decodeLoopText, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(decodeLoopText, "if (!FeedNextVideoPacket(cancellationToken))");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_RejectsInitializeAfterDispose()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var initialize = decoderType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.Initialize not found.");

        decoder.Dispose();

        try
        {
            initialize.Invoke(decoder, new object[] { IntPtr.Zero, IntPtr.Zero });
            throw new InvalidOperationException("Expected disposed decoder initialization to be rejected.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
        }

        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var d3d11Text = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(rootText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11.cs")),
            "Flashback decoder D3D11VA initialization lives with video decoder setup.");
        var initializeBlock = ExtractTextBetween(
            d3d11Text,
            "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)",
            "    private static AVPixelFormat GetFormatD3D11");
        AssertContains(initializeBlock, "ThrowIfDisposed();");
        AssertOccursBefore(initializeBlock, "ThrowIfDisposed();", "if (_initialized)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_ClearsAudioCallbackOnDispose()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var callbackProperty = decoderType.GetProperty("AudioChunkCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.AudioChunkCallback not found.");

        var callbackType = callbackProperty.PropertyType;
        var callbackParameter = Expression.Parameter(callbackType.GetGenericArguments()[0], "chunk");
        var callback = Expression.Lambda(callbackType, Expression.Empty(), callbackParameter).Compile();
        callbackProperty.SetValue(decoder, callback);

        decoder.Dispose();

        AssertEqual(null, callbackProperty.GetValue(decoder), "Disposed decoder clears audio callback");

        var sourceText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var disposeBlock = ExtractTextBetween(
            sourceText,
            "public void Dispose()",
            "// Free persistent D3D11VA device context");
        AssertContains(disposeBlock, "AudioChunkCallback = null;");
        AssertOccursBefore(disposeBlock, "AudioChunkCallback = null;", "CloseFileCore();");

        return Task.CompletedTask;
    }

    internal static Task FlashbackSuppressedExceptionsUseAppLogs()
    {
        var decoderText = ReadFlashbackDecoderSource();
        var d3d11Text = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs").Replace("\r\n", "\n");
        var d3d11DiscoveryText = d3d11Text;

        var openFileBlock = ExtractTextBetween(
            decoderText,
            "public void OpenFile(string filePath)",
            "    /// <summary>\n    /// Closes the currently open file");
        AssertContains(openFileBlock, "FLASHBACK_DECODER_OPEN_WARN");
        AssertContains(openFileBlock, "CloseFileCore();\n            throw;");
        AssertContains(decoderText, "var closedPath = _currentFilePath;\n        CloseFileCore();\n        Logger.Log($\"FLASHBACK_DECODER_CLOSE path='{closedPath}'\");");
        AssertContains(decoderText, "_currentPosition = TimeSpan.Zero;\n        _currentFilePath = null;\n        _needsConvert = false;");
        AssertDoesNotContain(openFileBlock, "System.Diagnostics.Trace.TraceWarning");
        AssertContains(decoderText, "FLASHBACK_DECODER_INIT d3d11va=false reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(d3d11Text, "var codec = FindD3D11VADecoder(codecPar->codec_id, out var codecName);");
        AssertContains(d3d11Text, "FLASHBACK_DECODER_D3D11VA_SKIP reason=no_d3d11_device_ctx_decoder");
        AssertContains(d3d11Text, "FLASHBACK_DECODER_D3D11VA_SKIP reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(d3d11Text, "private static string DescribeHardwareConfigs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11Discovery.cs")),
            "Flashback decoder D3D11VA discovery folded into video decoder setup owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11.cs")),
            "Flashback decoder D3D11VA setup folded into video decoder setup owner");
        AssertContains(d3d11DiscoveryText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(d3d11DiscoveryText, "ffmpeg.avcodec_find_decoder_by_name(preferredName)");
        AssertContains(d3d11DiscoveryText, "AVCodecID.AV_CODEC_ID_AV1 => \"av1\"");
        AssertContains(d3d11DiscoveryText, "FLASHBACK_DECODER_D3D11VA_SELECT source=preferred codec={codecName}");
        AssertContains(d3d11DiscoveryText, "FLASHBACK_DECODER_D3D11VA_CANDIDATE source={source} codec={codecName} configs=[{hardwareConfigSummary}] d3d11_device_ctx={hasD3D11DeviceConfig}");
        AssertContains(d3d11DiscoveryText, "private static string DescribeHardwareConfigs(AVCodec* codec, out bool hasD3D11DeviceConfig)");
        AssertContains(d3d11DiscoveryText, "ffmpeg.avcodec_get_hw_config(codec, i)");
        AssertContains(d3d11DiscoveryText, "pixelFormat == AVPixelFormat.AV_PIX_FMT_D3D11");
        AssertContains(d3d11DiscoveryText, "deviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA");
        AssertContains(d3d11DiscoveryText, "AvCodecHwConfigMethodHwDeviceCtx");
        AssertContains(d3d11DiscoveryText, "private static string FormatHardwareConfigMethods(int methods)");
        AssertContains(d3d11DiscoveryText, "private static string GetPixelFormatName(AVPixelFormat pixelFormat)");
        AssertContains(d3d11DiscoveryText, "private static string GetHardwareDeviceName(AVHWDeviceType deviceType)");

        return Task.CompletedTask;
    }



    // FlashbackDecoder: CalculateFrameBufferSize

    internal static Task FlashbackDecoder_CalculateFrameBufferSize_Nv12()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var method = decoderType.GetMethod("CalculateFrameBufferSize",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CalculateFrameBufferSize not found.");

        // NV12: width * height + width * (height / 2)
        var size1080 = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(1920 * 1080 + 1920 * (1080 / 2), size1080, "NV12 1080p buffer size");

        var size720 = (int)method.Invoke(null, new object[] { 1280, 720, false })!;
        AssertEqual(1280 * 720 + 1280 * (720 / 2), size720, "NV12 720p buffer size");

        var size4k = (int)method.Invoke(null, new object[] { 3840, 2160, false })!;
        AssertEqual(3840 * 2160 + 3840 * (2160 / 2), size4k, "NV12 4K buffer size");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_CalculateFrameBufferSize_P010()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var method = decoderType.GetMethod("CalculateFrameBufferSize",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CalculateFrameBufferSize not found.");

        // P010: width * height * 2 + width * (height / 2) * 2
        var size1080 = (int)method.Invoke(null, new object[] { 1920, 1080, true })!;
        AssertEqual(1920 * 1080 * 2 + 1920 * (1080 / 2) * 2, size1080, "P010 1080p buffer size");

        var size4k = (int)method.Invoke(null, new object[] { 3840, 2160, true })!;
        AssertEqual(3840 * 2160 * 2 + 3840 * (2160 / 2) * 2, size4k, "P010 4K buffer size");

        // P010 should be exactly 2x NV12
        var nv12Size = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(nv12Size * 2, size1080, "P010 is 2x NV12");

        return Task.CompletedTask;
    }

    // FlashbackDecoder: state guard properties

    internal static Task FlashbackDecoder_ValidationHelpersLiveWithRootLifecycle()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var videoOutputText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static int CalculateFrameBufferSize(int width, int height, bool isHdr)");
        AssertContains(rootText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertContains(rootText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertContains(rootText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertContains(rootText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(rootText, "private static bool TryGetInputStreamCount(AVFormatContext* formatCtx, out int streamCount, out string failureMessage)");
        AssertContains(rootText, "private static bool IsValidStreamIndex(int streamIndex, int streamCount)");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(videoOutputText, "private void CopyFramePlanesToBuffer(");
        AssertContains(videoOutputText, "private void ConvertYuv420pToNv12(");
        AssertContains(videoOutputText, "private void ConvertYuv420p10leToP010(");
        AssertContains(videoOutputText, "private static void InterleaveUvRow(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.VideoConversion.cs")),
            "FlashbackDecoder.VideoConversion.cs folded into FlashbackDecoder.VideoSetup.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.VideoOutput.cs")),
            "FlashbackDecoder.VideoOutput.cs folded into FlashbackDecoder.VideoSetup.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.Validation.cs")),
            "FlashbackDecoder validation helpers folded into decoder root");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_LifetimeCleanupLivesWithRootLifecycle()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private void CloseFileCore()");
        AssertContains(rootText, "internal static void ReleaseHeldFrame(DecodedVideoFrame frame)");
        AssertContains(rootText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.Lifetime.cs")),
            "FlashbackDecoder file-close cleanup lives with the root lifecycle owner");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_StateGuardsAndTimingLiveWithOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var decodeLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

        AssertContains(decodeLoopText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertContains(decodeLoopText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertContains(rootText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(rootText, "private static string GetErrorString(int errorCode)");
        AssertContains(rootText, "private static InvalidOperationException CreateException(string message)");
        AssertContains(rootText, "private void ThrowIfNotInitialized()");
        AssertContains(rootText, "private void ThrowIfNotOpen()");
        AssertContains(rootText, "private void ThrowIfDisposed()");
        AssertDoesNotContain(rootText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertDoesNotContain(rootText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertDoesNotContain(decodeLoopText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertDoesNotContain(decodeLoopText, "private void ThrowIfNotInitialized()");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_OutputTypesLiveWithDecoderRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.OutputTypes.cs")),
            "Flashback decoder output DTOs stay folded into the decoder root surface.");
        AssertContains(rootText, "internal readonly struct DecodedVideoFrame");
        AssertContains(rootText, "internal readonly struct DecodedAudioChunk");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_VideoSetupOwnsHardwareAndSoftwareSetup()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var videoSetupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs")
            .Replace("\r\n", "\n");

        AssertContains(videoSetupText, "private void InitializeVideoDecoder()");
        AssertContains(videoSetupText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        AssertContains(videoSetupText, "private bool TryInitializeD3D11VADecoder(AVCodecParameters* codecPar)");
        AssertContains(videoSetupText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(videoSetupText, "private void AllocateVideoOutputBuffers()");
        AssertContains(videoSetupText, "private DecodedVideoFrame ConvertAndOutputVideoFrame()");
        AssertContains(videoSetupText, "private void CopyFramePlanesToBuffer(");
        AssertContains(videoSetupText, "private void ConvertYuv420pToNv12(");
        AssertContains(videoSetupText, "private void ConvertYuv420p10leToP010(");
        AssertDoesNotContain(rootText, "private void InitializeVideoDecoder()");
        AssertDoesNotContain(rootText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        AssertDoesNotContain(rootText, "private void AllocateVideoOutputBuffers()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11.cs")),
            "FlashbackDecoder D3D11VA setup folded into video setup owner");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_PlaybackOwnsSeekingAndDecodeLoop()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

        AssertContains(playbackText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "FLASHBACK_DECODER_SEEK_FALLBACK_OK");
        AssertContains(playbackText, "FLASHBACK_DECODER_SEEK_CAP_HIT");
        AssertContains(playbackText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertContains(playbackText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertOccursBefore(
            playbackText,
            "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)",
            "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DecodeLoopLivesWithPlayback()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

        AssertContains(playbackText, "private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;");
        AssertContains(playbackText, "public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;");
        AssertContains(playbackText, "public readonly record struct PlaybackDecodePhaseTimings(");
        AssertContains(playbackText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "private void DecodeAndDeliverAudioPacket(AVPacket* packet)");
        AssertContains(playbackText, "private DecodedAudioChunk ConvertAndOutputAudioFrame()");
        AssertContains(playbackText, "private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)");
        AssertContains(playbackText, "ffmpeg.av_read_frame(_formatCtx, _packet)");
        AssertContains(playbackText, "DecodeAndDeliverAudioPacket(_packet);");
        AssertDoesNotContain(rootText, "private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;");
        AssertDoesNotContain(rootText, "public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;");
        AssertDoesNotContain(rootText, "public readonly record struct PlaybackDecodePhaseTimings(");
        AssertDoesNotContain(rootText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "private void DecodeAndDeliverAudioPacket(AVPacket* packet)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var decoder = Activator.CreateInstance(decoderType)!;

        var isOpenProp = decoderType.GetProperty("IsOpen",
            BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(isOpenProp, "FlashbackDecoder.IsOpen");
        AssertEqual(false, (bool)isOpenProp!.GetValue(decoder)!, "IsOpen default");

        return Task.CompletedTask;
    }

    // FlashbackDecoder: Dispose is safe when not initialized

    internal static Task FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var decoder = Activator.CreateInstance(decoderType)!;

        // Dispose via IDisposable
        if (decoder is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else
        {
            var disposeMethod = decoderType.GetMethod("Dispose",
                BindingFlags.Public | BindingFlags.Instance);
            disposeMethod?.Invoke(decoder, null);
        }

        return Task.CompletedTask;
    }
}
