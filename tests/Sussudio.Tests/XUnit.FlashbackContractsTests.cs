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
                Property("FreeDiskBytesProvider", typeof(Func<long>), SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
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
                String("InputPath", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
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
        Set(exportRequest, "InputPath", "single.ts");
        Set(exportRequest, "InPoint", TimeSpan.FromSeconds(2));
        Set(exportRequest, "OutPoint", TimeSpan.FromSeconds(12));
        Set(exportRequest, "OutputPath", "clip.mp4");
        Set(exportRequest, "FastStart", false);
        Assert.Equal(1, Count(Get(exportRequest, "Segments")!));
        Assert.Equal(2, Count(Get(exportRequest, "SegmentPaths")!));
        Assert.Equal("single.ts", Get<string>(exportRequest, "InputPath"));
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
    public Task FlashbackDecoderStateGuardsTimingAndErrorsLiveWithDecoderRoot()
        => global::Program.FlashbackDecoder_StateGuardsTimingAndErrorsLiveWithDecoderRoot();

    [Fact]
    public Task FlashbackDecoderOutputTypesLiveWithDecoderRoot()
        => global::Program.FlashbackDecoder_OutputTypesLiveWithDecoderRoot();

    [Fact]
    public Task FlashbackDecoderVideoSetupLivesWithDecoderRoot()
        => global::Program.FlashbackDecoder_VideoSetupLivesWithDecoderRoot();

    [Fact]
    public Task FlashbackDecoderPlaybackFlowLivesWithDecoderRoot()
        => global::Program.FlashbackDecoder_PlaybackFlowLivesWithDecoderRoot();

    [Fact]
    public Task FlashbackDecoderDecodeLoopLivesWithDecoderRoot()
        => global::Program.FlashbackDecoder_DecodeLoopLivesWithDecoderRoot();

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
    public Task FlashbackDecoderAudioSetupLivesWithDecoderRoot()
        => global::Program.FlashbackDecoder_AudioSetupLivesWithDecoderRoot();

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
    public Task FlashbackEncoderForceRotateDrainDoesNotRejectVideoByLifecycleState()
        => global::Program.FlashbackEncoderSink_ForceRotateDrainingDoesNotRejectVideoByLifecycleState();

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
    public Task FlashbackOutputTransactionCleanupIgnoresNonexistentDirectories()
        => global::Program.FlashbackOutputTransaction_CleanupOrphanedTempFiles_HandlesNonexistentDirectory();

    [Fact]
    public Task FlashbackOutputTransactionCleanupDeletesOrphanedTempFiles()
        => global::Program.FlashbackOutputTransaction_CleanupOrphanedTempFiles_DeletesTempFiles();

    [Fact]
    public Task FlashbackExporterDoesNotScanUserOutputDirectoryForOrphans()
        => global::Program.FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans();

    [Fact]
    public Task FlashbackExporterTaskWrappersDisposeLinkedCancellation()
        => global::Program.FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation();

    [Fact]
    public Task FlashbackExporterOwnsFfmpegExportLifecycle()
        => global::Program.FlashbackExporter_OwnsFfmpegExportLifecycle();

    [Fact]
    public Task FlashbackOutputTransactionOwnsFilesystemPublication()
        => global::Program.FlashbackExportOutputTransaction_OwnsFilesystemPublication();

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
    public Task FlashbackExporterPreservesLaterValidMicrophoneStreamTemplate()
        => global::Program.FlashbackExporter_PreservesLaterValidMicrophoneStreamTemplate();

    [Fact]
    public Task FlashbackExporterFailsWhenRequestedSegmentsAreSkipped()
        => global::Program.FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped();

    [Fact]
    public Task FlashbackExporterRetainsHiddenDecoderPreroll()
        => global::Program.FlashbackExporter_RetainsHiddenDecoderPreroll();

    [Fact]
    public Task FlashbackExporterPreservesTimelineAcrossDelayedSegmentPackets()
        => global::Program.FlashbackExporter_PreservesTimelineAcrossDelayedSegmentPackets();

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
    public Task FlashbackOutputTransactionRejectsEmptyTempAndPreservesExistingOutput()
        => global::Program.FlashbackOutputTransaction_EmptyTempDoesNotReplaceExistingOutput();

    [Fact]
    public Task FlashbackExporterRefusesToOverwriteExistingDestinationWhenForceIsFalse()
        => global::Program.FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse();

    [Fact]
    public Task FlashbackExporterRefusesToOverwriteExistingDestinationWhenForceIsTrue()
        => global::Program.FlashbackExporter_RefusesOverwriteWhenForceTrue();

    [Fact]
    public Task FlashbackOutputTransactionPreservesOutputAfterPostMoveValidationFailure()
        => global::Program.FlashbackOutputTransaction_PostMoveValidationFailurePreservesOutput();

    [Fact]
    public Task FlashbackOutputTransactionCreatesUniqueTempOutputPaths()
        => global::Program.FlashbackOutputTransaction_CreatesUniqueTempOutputPaths();

    [Fact]
    public Task FlashbackOutputTransactionReservationBlocksPathReplacement()
        => global::Program.FlashbackOutputTransaction_ReservationBlocksPathReplacement();

    [Fact]
    public Task FlashbackOutputTransactionPublishesWithLiveReservation()
        => global::Program.FlashbackExportOutputTransaction_PublishesWithLiveReservation();

    [Fact]
    public Task FlashbackOutputTransactionRefusesDestinationCreatedAfterReservation()
        => global::Program.FlashbackExportOutputTransaction_RefusesDestinationCreatedAfterReservation();

    [Fact]
    public Task FlashbackOutputTransactionDoesNotPublishOrDeleteAReplacedTempPath()
        => global::Program.FlashbackOutputTransaction_ReplacedTempPathIsNeverPublishedOrDeleted();

    [Fact]
    public Task FlashbackOutputTransactionAbandonDeletesOnlyItsUncommittedTemp()
        => global::Program.FlashbackOutputTransaction_AbandonDeletesOnlyItsUncommittedTemp();
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
    public Task FlashbackPlaybackTransitionsUseBestEffortAudioPreviewGuards()
        => global::Program.FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards();

    [Fact]
    public Task FlashbackPlaybackMetricResetClearsDecodeTimings()
        => global::Program.FlashbackPlaybackController_ResetClearsDecodeMetrics();
}
}

static partial class Program
{
    internal static void FlashbackEncoderSink_ExerciseRotationFailures(bool scriptedSuccess)
    {
        EnsureTargetAssemblyLoadedForXUnit();
        var directory = Path.Combine(Path.GetTempPath(), $"fb_rotation_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        object? sink = null;
        IDisposable? managerLifetime = null;
        try
        {
            managerLifetime = (IDisposable)CreateInitializedBufferManager(directory);
            var manager = (object)managerLifetime;
            var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            var originalPath = activePath;
            File.WriteAllBytes(activePath, new byte[64]);
            var attempt = 0;
            string? attemptedPath = null;
            if (scriptedSuccess)
            {
                var resultType = RequireType("Sussudio.Services.Recording.RotateOutputResult");
                Func<string, object> rotate = path =>
                {
                    attemptedPath = path;
                    File.WriteAllBytes(path, new byte[16]);
                    if (attempt != 3) throw new IOException("Scripted rotation failure");
                    return Activator.CreateInstance(resultType, activePath, 2L, 64L)!;
                };
                var pathParameter = Expression.Parameter(typeof(string), "path");
                var delegateType = typeof(Func<,>).MakeGenericType(typeof(string), resultType);
                var operation = Expression.Lambda(delegateType,
                    Expression.Convert(Expression.Invoke(Expression.Constant(rotate), pathParameter), resultType),
                    pathParameter).Compile();
                sink = sinkType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { manager.GetType(), delegateType }, null)!.Invoke(new object[] { manager, operation });
            }
            else
            {
                sink = Activator.CreateInstance(sinkType, new[] { manager })!;
            }

            SetPrivateField(sink, "_tsFilePath", activePath);
            SetPrivateField(sink, "_started", true);
            var completions = new List<Task>();
            foreach (var field in new[] { "_videoQueue", "_audioQueue", "_microphoneQueue", "_gpuQueue" })
            {
                var channel = CreateUnboundedChannelFieldValue(sinkType, field);
                SetPrivateField(sink, field, channel);
                var reader = channel.GetType().GetProperty("Reader")!.GetValue(channel)!;
                completions.Add((Task)reader.GetType().GetProperty("Completion")!.GetValue(reader)!);
            }
            var notifications = new List<Exception>();
            sinkType.GetMethod("SetFatalErrorCallback")!.Invoke(sink, new object[] { (Action<Exception>)notifications.Add });
            var consecutive = 0;
            var totalFailures = 0L;
            var nextIndex = (int)GetPrivateField(manager, "_nextSegmentIndex")!;
            var rotateMethod = sinkType.GetMethod("RotateSegment", BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (attempt = 1; attempt <= (scriptedSuccess ? 7 : 4); attempt++)
            {
                var succeeds = scriptedSuccess && attempt == 3;
                var pts = TimeSpan.FromSeconds(attempt);
                Assert.Equal(succeeds, (bool)rotateMethod.Invoke(sink, new object?[] { pts, null })!);
                if (succeeds)
                {
                    activePath = attemptedPath!;
                    nextIndex++;
                    consecutive = 0;
                    Assert.Equal(pts.Ticks, GetPrivateField(manager, "_activeSegmentStartPtsTicks"));
                    var completed = Assert.Single(((IEnumerable)GetPrivateField(manager, "_completedSegments")!).Cast<object>());
                    Assert.Equal(originalPath, completed.GetType().GetProperty("Path")!.GetValue(completed));
                    Assert.Equal(TimeSpan.FromSeconds(2), completed.GetType().GetProperty("StartPts")!.GetValue(completed));
                    Assert.Equal(pts, completed.GetType().GetProperty("EndPts")!.GetValue(completed));
                    Assert.Equal(64L, completed.GetType().GetProperty("SizeBytes")!.GetValue(completed));
                    Assert.Equal(64L, GetPrivateField(manager, "_completedSegmentBytes"));
                    Assert.Equal(0L, GetPrivateField(sink, "_segmentStartBytes"));
                }
                else
                {
                    consecutive++;
                    totalFailures++;
                    if (attemptedPath != null) Assert.False(File.Exists(attemptedPath));
                }
                Assert.Equal(activePath, GetPrivateField(sink, "_tsFilePath"));
                Assert.Equal(activePath, GetPrivateField(manager, "_activeSegmentPath"));
                Assert.True(File.Exists(activePath));
                Assert.Equal(scriptedSuccess && attempt >= 3 ? 1 : 0,
                    ((ICollection)GetPrivateField(manager, "_completedSegments")!).Count);
                Assert.Equal(nextIndex, GetPrivateField(manager, "_nextSegmentIndex"));
                Assert.Equal(pts, GetPrivateField(sink, "_segmentStartPts"));
                Assert.Equal(totalFailures, GetPrivateField(sink, "_segmentRotationFailures"));
                Assert.Equal(consecutive, GetPrivateField(sink, "_consecutiveRotationFailures"));
                var fatal = consecutive >= 3;
                Assert.Equal(fatal ? 1 : 0, notifications.Count);
                Assert.Equal(!fatal, GetPrivateField(sink, "_started"));
                Assert.All(completions, completion => Assert.Equal(fatal, completion.IsCompletedSuccessfully));
                if (fatal)
                {
                    Assert.IsType<IOException>(notifications[0]);
                    Assert.Same(notifications[0], GetPrivateField(sink, "_encodingFailure"));
                    Assert.NotNull(notifications[0].InnerException);
                }
                else Assert.Null(GetPrivateField(sink, "_encodingFailure"));
            }
        }
        finally
        {
            if (sink != null)
            {
                SetPrivateField(sink, "_started", false);
                ((IDisposable)sink).Dispose();
            }
            managerLifetime?.Dispose();
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    internal static Task CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var resolve = serviceType.GetMethod("ResolveFlashbackExportThrottleDelayMs", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFlashbackExportThrottleDelayMs not found.");
        AssertEqual(0, (int)resolve.Invoke(null, new object[] { 0.49, 29L, false })!, "Flashback export throttle idle");
        AssertEqual(1, (int)resolve.Invoke(null, new object[] { 0.49, 0L, true })!, "Flashback export throttle high-resolution live baseline");
        AssertEqual(16, (int)resolve.Invoke(null, new object[] { 0.50, 0L, true })!, "High-resolution baseline must not hide queue pressure");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.90, 0L, true })!, "High-resolution severe pressure still backs off");
        AssertEqual(16, (int)resolve.Invoke(null, new object[] { 0.50, 0L, false })!, "Flashback export throttle queue half full");
        AssertEqual(16, (int)resolve.Invoke(null, new object[] { 0.0, 30L, false })!, "Flashback export throttle oldest frame mild pressure");
        AssertEqual(20, (int)resolve.Invoke(null, new object[] { 0.70, 0L, false })!, "Flashback export throttle medium queue pressure");
        AssertEqual(20, (int)resolve.Invoke(null, new object[] { 0.0, 50L, false })!, "Flashback export throttle medium frame age");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.85, 0L, false })!, "Flashback export throttle severe queue pressure");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.0, 90L, false })!, "Flashback export throttle severe frame age");

        return Task.CompletedTask;
    }

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExportAsync not found.");

        var nonexistentInput = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.ts");
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.mp4");
        var request = Activator.CreateInstance(requestType)!;
        SetPropertyBackingField(request, "InputPath", nonexistentInput);
        SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
        SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
        SetPropertyBackingField(request, "OutputPath", outputPath);
        SetPropertyBackingField(request, "FastStart", true);

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            request,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        var succeeded = GetBoolProperty(result, "Succeeded");
        AssertEqual(false, succeeded, "Export fails when input file not found");
    }

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)!;

        // Create a real temp file so input validation passes
        var tempInput = Path.Combine(Path.GetTempPath(), $"fb_input_{Guid.NewGuid():N}.ts");
            File.WriteAllBytes(tempInput, new byte[] { 0x47 }); // MPEG-TS sync byte
        try
        {
            var request = Activator.CreateInstance(requestType)!;
            SetPropertyBackingField(request, "InputPath", tempInput);
            SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
            SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
            SetPropertyBackingField(request, "OutputPath", "");
            SetPropertyBackingField(request, "FastStart", true);

            var task = exportMethod.Invoke(exporter, new object?[]
            {
                request,
                null,
                CancellationToken.None
            }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export fails when output path empty");
        }
        finally
        {
            try { File.Delete(tempInput); } catch { }
        }
    }

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)!;

        var tempInput = Path.Combine(Path.GetTempPath(), $"fb_input_{Guid.NewGuid():N}.ts");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"fb_export_dir_{Guid.NewGuid():N}");
        File.WriteAllBytes(tempInput, new byte[] { 0x47 });
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var request = Activator.CreateInstance(requestType)!;
            SetPropertyBackingField(request, "InputPath", tempInput);
            SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
            SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
            SetPropertyBackingField(request, "OutputPath", outputDirectory);
            SetPropertyBackingField(request, "FastStart", true);

            var task = exportMethod.Invoke(exporter, new object?[]
            {
                request,
                null,
                CancellationToken.None
            }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export fails when output path is a directory");
            AssertContains(GetStringProperty(result, "StatusMessage"), "output path is a directory");
            AssertEqual(false, File.Exists(outputDirectory + ".tmp"), "Directory-target export does not create temp output");
        }
        finally
        {
            try { File.Delete(tempInput); } catch { }
            try { Directory.Delete(outputDirectory, recursive: true); } catch { }
        }
    }

    internal static async Task FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportSegmentsAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExportSegmentsAsync not found.");

        var emptySegments = Array.CreateInstance(segmentType, 0);
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.mp4");

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            emptySegments,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            outputPath,
            true,
            false,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportSegmentsAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export segments fails when no segments");
    }

    internal static async Task FlashbackExporter_RejectsNullRequests()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", new[] { requestType, typeof(IProgress<>).MakeGenericType(RequireType("Sussudio.Models.ExportProgress")), typeof(CancellationToken) })
            ?? throw new InvalidOperationException("FlashbackExporter.ExportAsync(request) not found.");

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            null,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Null export request reports failure");
        AssertContains(GetStringProperty(result, "StatusMessage"), "request is required");
    }

    internal static Task FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exporter = Activator.CreateInstance(exporterType)!;
        try
        {
            var segment = Activator.CreateInstance(segmentType)!;
            SetPropertyBackingField(segment, "Path", Path.Combine(Path.GetTempPath(), $"fb_missing_{Guid.NewGuid():N}.mp4"));
            var segments = Array.CreateInstance(segmentType, 1);
            segments.SetValue(segment, 0);
            var outputPath = Path.Combine(Path.GetTempPath(), $"fb_cancelled_{Guid.NewGuid():N}.mp4");

            var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

            var result = exportSegmentsCore.Invoke(exporter, new object?[]
            {
                segments,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                outputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Cancelled export reports failure result");
            AssertContains(GetStringProperty(result, "StatusMessage"), "cancelled");
            AssertEqual("flashback-export-cancelled", GetStringProperty(result, "FailureCode"), "Cancellation carries explicit identity");
            AssertEqual(false, File.Exists(outputPath), "Cancelled export does not create output");
            AssertEqual(false, File.Exists(outputPath + ".tmp"), "Cancelled export does not leave temp output");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_CancellationWinsBeforeValidation()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exporter = Activator.CreateInstance(exporterType)!;
        try
        {
            var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
            var singleOutputPath = Path.Combine(Path.GetTempPath(), $"fb_cancel_single_{Guid.NewGuid():N}.mp4");
            var singleResult = exportCore.Invoke(exporter, new object?[]
            {
                Path.Combine(Path.GetTempPath(), $"fb_missing_{Guid.NewGuid():N}.ts"),
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                singleOutputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportCore returned null.");

            AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Cancelled single-file export reports failure");
            AssertContains(GetStringProperty(singleResult, "StatusMessage"), "cancelled");
            AssertEqual("flashback-export-cancelled", GetStringProperty(singleResult, "FailureCode"), "Cancellation carries explicit identity");
            AssertDoesNotContain(GetStringProperty(singleResult, "StatusMessage"), "not found");

            var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
            var emptySegments = Array.CreateInstance(segmentType, 0);
            var segmentOutputPath = Path.Combine(Path.GetTempPath(), $"fb_cancel_segments_{Guid.NewGuid():N}.mp4");
            var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
            {
                emptySegments,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                segmentOutputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

            AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Cancelled segment export reports failure");
            AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "cancelled");
            AssertEqual("flashback-export-cancelled", GetStringProperty(segmentResult, "FailureCode"), "Cancellation carries explicit identity");
            AssertDoesNotContain(GetStringProperty(segmentResult, "StatusMessage"), "no segment paths");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportFailureClassifier_MapsCommandFailures()
    {
        var method = RequireType("Sussudio.Services.Flashback.FlashbackExportFailureCodes").GetMethod("Classify", BindingFlags.Static | BindingFlags.NonPublic)!;
        var codes = RequireType("Sussudio.Services.Flashback.FlashbackExportFailureCodes");
        var create = codes.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
        var categories = new[] { "BufferInactive", "InvalidRequest", "InvalidRange", "UnavailableDuringRecording", "InvalidOutputPath", "InputUnavailable", "OutputWriteFailed", "InputReadFailed", "NoMediaWritten", "IncompleteLiveEdge", "ForceRotateFailed", "SegmentUnavailable", "InvalidInputStream", "Disposed", "Cancelled", "Timeout", "Failed" };
        foreach (var category in categories)
        {
            var code = codes.GetField(category, BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue();
            var result = create.Invoke(null, new object?[] { "cancel-timeout.mp4", "unrelated output path cancel timeout segment path", code, null });
            AssertEqual(category, method.Invoke(null, new[] { result })?.ToString(), "FailureCode selects the wire category independently of display text");
        }
        var unknown = create.Invoke(null, new object?[] { "cancel.mp4", "Flashback export cancelled.", "unknown-code", null });
        AssertEqual("Failed", method.Invoke(null, new[] { unknown })?.ToString(), "Unknown code never falls back to message parsing");
        var source = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        AssertContains(source, "FlashbackExportFailureCodes.Classify(result)");
        AssertDoesNotContain(source, "ContainsFlashbackExportFailureText");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExporter_RejectsInvalidExportRanges()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_invalid_range_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input.ts");
        File.WriteAllBytes(inputPath, new byte[] { 0x47 });

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleOutputPath = Path.Combine(tempDir, "single-invalid.mp4");
                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    inputPath,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(5),
                    singleOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Empty single-file export range reports failure");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "export range is empty or invalid");
                AssertEqual(false, File.Exists(singleOutputPath), "Invalid single-file range does not create output");
                AssertEqual(false, File.Exists(singleOutputPath + ".tmp"), "Invalid single-file range does not leave temp output");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", inputPath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentOutputPath = Path.Combine(tempDir, "segment-invalid.mp4");
                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.FromSeconds(-1),
                    TimeSpan.FromSeconds(1),
                    segmentOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Negative segment export in point reports failure");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "in point must not be negative");
                AssertEqual(false, File.Exists(segmentOutputPath), "Invalid segment range does not create output");
                AssertEqual(false, File.Exists(segmentOutputPath + ".tmp"), "Invalid segment range does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures()
    {
        var sourceText = ReadFlashbackExporterSource();
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketReadLoopText = singleFileText;
        var singleFilePacketWritingText = singleFilePacketReadLoopText;
        var singleFilePacketWriteStateText = singleFilePacketReadLoopText;
        var segmentsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentPacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWriteStateText = segmentPacketReadLoopText;
        var segmentPacketRebasingText = segmentPacketReadLoopText;
        var packetBuffersText = segmentPacketWritingText;

        AssertContains(packetBuffersText, "private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)");
        AssertContains(sourceText, "FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);");
        AssertContains(sourceText, "FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);");
        AssertContains(sourceText, "FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);");
        AssertContains(packetBuffersText, "bufferedStreamIndices?.Clear();");
        AssertContains(packetBuffersText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(packetBuffersText, "FLASHBACK_EXPORT_PACKET_CLONE_FAIL operation={operation}");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.PacketBuffers.cs")),
            "FlashbackExporter.PacketBuffers.cs folded into FlashbackExporter.cs");
        AssertContains(singleFileText, "WriteSingleFilePacketsToActiveOutput(");
        AssertContains(singleFilePacketWritingText, "WriteSingleFilePacketReadLoop(");
        AssertContains(singleFilePacketReadLoopText, "FreeBufferedPackets(packetState.BufferedPackets, packetState.BufferedStreamIndices);");
        AssertContains(singleFilePacketWriteStateText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertContains(segmentsText, "WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentsText, "private FinalizeResult ExportSegmentsCore(");
        AssertContains(segmentsText, "var clone = ClonePacketOrThrow(packet, \"segment_buffer\");");
        AssertContains(segmentPacketWritingText, "private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentPacketWritingText, "WriteSegmentPacketReadLoop(");
        AssertContains(segmentPacketReadLoopText, "private void WriteSegmentPacketReadLoop(");
        AssertContains(segmentPacketReadLoopText, "var clone = ClonePacketOrThrow(packet, \"segment_buffer\");");

        var segmentLoopBlock = ExtractTextBetween(
            segmentPacketReadLoopText,
            "segmentPacketState = CreateSegmentPacketWriteState(",
            "    }\n}");
        AssertContains(segmentPacketWriteStateText, "private int FlushSegmentBufferedPackets(");
        AssertContains(segmentLoopBlock, "totalPackets += FlushSegmentBufferedPackets(");
        AssertOccursBefore(
            segmentLoopBlock,
            "if (segmentPacketState.AllBasesDiscovered)",
            "if (!segmentPacketState.AllBasesDiscovered && segmentPacketState.BufferedPackets.Count > 0)");
        AssertOccursBefore(
            segmentLoopBlock,
            "if (!segmentPacketState.AllBasesDiscovered && segmentPacketState.BufferedPackets.Count > 0)",
            "FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);");
        var segmentFlushBlock = ExtractTextBetween(
            segmentPacketWriteStateText,
            "private int FlushSegmentBufferedPackets(",
            "private enum SegmentPacketWriteOutcome");
        var segmentWriteBlock = ExtractTextBetween(
            segmentPacketRebasingText,
            "private SegmentPacketWriteOutcome WriteRebasedSegmentPacket(",
            "    }\n}");
        AssertContains(segmentFlushBlock, "finally\n        {\n            FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);\n        }");
        AssertContains(segmentFlushBlock, "WriteRebasedSegmentPacket(");
        AssertContains(segmentWriteBlock, "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), \"av_interleaved_write_frame\", FlashbackExportFailureCodes.OutputWriteFailed);");
        AssertOccursBefore(
            segmentFlushBlock,
            "WriteRebasedSegmentPacket(",
            "finally\n        {\n            FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);\n        }");
        AssertContains(segmentLoopBlock, "if (!segmentPacketState.AllBasesDiscovered && segmentPacketState.BufferedPackets.Count > 0)");
        AssertContains(segmentLoopBlock, "segmentPacketState.MinBaseUs ??= 0;");
        AssertContains(segmentLoopBlock, "FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH seg={segIdx}");
        AssertContains(segmentLoopBlock, "FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);");

        var sharedFlushBlock = ExtractTextBetween(
            packetBuffersText,
            "private long FlushBufferedPackets(",
            "private static void FreeBufferedPackets(");
        AssertContains(sharedFlushBlock, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertOccursBefore(
            sharedFlushBlock,
            "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), \"av_interleaved_write_frame\", FlashbackExportFailureCodes.OutputWriteFailed);",
            "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_ProgressCallbacksAreBestEffort()
    {
        var sourceText = ReadFlashbackExporterSource();
        var segmentPacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "progress?.Report(new ExportProgress");
        AssertContains(sourceText, "using System.Diagnostics;");
        AssertContains(sourceText, "private const int ProgressHeartbeatIntervalMs = 1_000;");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, 1, 0), \"single_start\");");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, segments.Count, 0), \"segments_start\");");
        AssertContains(sourceText, "if (ShouldReportProgressHeartbeat(ref lastProgressHeartbeatTick))");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(0, 1, 0), \"single_heartbeat\");");
        var segmentExportLoopBlock = ExtractTextBetween(
            segmentPacketReadLoopText,
            "var segmentVideoFrameDurUs = 33333L;",
            "// EOF: if Phase 1 never completed");
        AssertContains(segmentExportLoopBlock, "ReportProgress(");
        AssertContains(segmentExportLoopBlock, "\"segment_heartbeat\");");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(1, 1, 100.0), \"single_complete\")");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{failureMessage}'\");");
        AssertContains(sourceText, "ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), \"av_write_trailer\", FlashbackExportFailureCodes.OutputWriteFailed);");
        AssertContains(sourceText, "ThrowIfError(CloseOutputIo(), \"avio_closep\", FlashbackExportFailureCodes.OutputWriteFailed);");
        AssertContains(sourceText, "return FlashbackExportFailureCodes.Create(outputPath, outputFailure, outputFailureCode);");
        AssertContains(sourceText, "ReportProgress(\n                    progress,\n                    new ExportProgress(\n                        segIdx + 1,\n                        segments.Count,");
        AssertContains(sourceText, "ReportProgress(progress, new ExportProgress(segments.Count, segments.Count, 100.0), \"segments_complete\")");
        AssertContains(sourceText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)\n    {\n        value = NormalizeExportProgress(value, stage);");
        AssertContains(sourceText, "private static ExportProgress NormalizeExportProgress(ExportProgress value, string stage)");
        AssertContains(sourceText, "if (totalSegments > 0 && segmentsProcessed > totalSegments)");
        AssertContains(sourceText, "var percent = double.IsFinite(value.Percent)\n            ? Math.Clamp(value.Percent, 0.0, 100.0)\n            : 0.0;");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PROGRESS_NORMALIZED stage={stage}");
        AssertContains(sourceText, "return new ExportProgress(segmentsProcessed, totalSegments, percent);");
        AssertContains(sourceText, "private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)");
        AssertContains(sourceText, "(now - last) * 1000.0 / Stopwatch.Frequency < ProgressHeartbeatIntervalMs");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_PROGRESS_WARN stage={stage} type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PROGRESS_UPDATE_WARN");
        AssertDoesNotContain(sourceText, "catch { /* Best-effort: segment may be deleted mid-export; progress tracking is non-critical */ }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream()
    {
        var sourceText = ReadFlashbackExporterSource();
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = streamsText;
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = segmentTemplateText;

        var templateSelectionBlock = ExtractTextBetween(
            sourceText,
            "private bool TryInitializeSegmentOutputTemplate(",
            "    private bool TryOpenSegmentInputForExport");
        var incompleteVideoParamsBlock = ExtractTextBetween(
            sourceText,
            "var videoStream = _activeInputContext->streams[candidateVideoStreamIndex];",
            "CreateOutputContext(tmpPath, fastStart);");

        AssertDoesNotContain(templateSelectionBlock, "TrackSkippedRequestedSegment(segment, \"video_stream_missing\");");
        AssertDoesNotContain(templateSelectionBlock, "TrackSkippedRequestedSegment(segment, \"video_params_incomplete\");");
        AssertContains(templateSelectionBlock, "var candidateVideoStreamIndex = FindVideoStreamIndex(_activeInputContext);");
        AssertContains(templateSelectionBlock, "LogInputStreams(_activeInputContext, candidateStreamCount);");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_stream_missing'");
        AssertContains(templateSelectionBlock, "no usable video stream was found in any segment");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_CANDIDATE");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(incompleteVideoParamsBlock, "var videoStream = _activeInputContext->streams[candidateVideoStreamIndex];");
        AssertContains(incompleteVideoParamsBlock, "var videoHasValidParams = videoWidth > 0 && videoHeight > 0;");
        AssertContains(incompleteVideoParamsBlock, "no segment had complete video parameters");
        AssertContains(segmentInputPreflightText, "var streamLayoutMismatch = FindSegmentStreamLayoutMismatch(");
        AssertContains(segmentInputPreflightText, "reason='stream_layout_mismatch' detail='{streamLayoutMismatch}'");
        AssertContains(streamTemplatesText, "private static string? FindSegmentStreamLayoutMismatch(");
        AssertContains(streamTemplatesText, "inputCodec->codec_type != templateCodec->codec_type");
        AssertContains(streamTemplatesText, "inputCodec->codec_id != templateCodec->codec_id");
        AssertContains(streamTemplatesText, "private static bool VideoDimensionsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)");
        AssertContains(streamTemplatesText, "return !inputHasCompleteDimensions && templateHasCompleteDimensions;");
        AssertContains(streamTemplatesText, "private static bool AudioParamsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)");
        AssertContains(streamTemplatesText, "private static bool AudioParamsMatchOrCanUseTemplate(\n        int inputSampleRate,");
        AssertContains(streamTemplatesText, "inputSampleRate != templateSampleRate");
        AssertContains(streamTemplatesText, "inputChannelCount != templateChannelCount");
        AssertContains(streamTemplatesText, "return inputSampleFormat == templateSampleFormat;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_PreservesLaterValidMicrophoneStreamTemplate()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var templateStreamScore = exporterType.GetMethod(
                "TemplateStreamContributesToTemplateScore",
                BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TemplateStreamContributesToTemplateScore not found.");
        var avMediaType = templateStreamScore.GetParameters()[0].ParameterType;
        var audioParamsMatch = exporterType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == "AudioParamsMatchOrCanUseTemplate" &&
                method.GetParameters().Length == 6);
        var sourceText = ReadFlashbackExporterSource();
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var templateSelectionBlock = ExtractTextBetween(
            sourceText,
            "private bool TryInitializeSegmentOutputTemplate(",
            "    private bool TryOpenSegmentInputForExport");
        var audioType = Enum.Parse(avMediaType, "AVMEDIA_TYPE_AUDIO");
        var videoType = Enum.Parse(avMediaType, "AVMEDIA_TYPE_VIDEO");
        var subtitleType = Enum.Parse(avMediaType, "AVMEDIA_TYPE_SUBTITLE");

        AssertEqual(
            false,
            (bool)templateStreamScore.Invoke(null, new object?[] { audioType, 0, 0, 0, 2 })!,
            "audio template stream without sample rate is not usable");
        AssertEqual(
            false,
            (bool)templateStreamScore.Invoke(null, new object?[] { audioType, 0, 0, 48000, 0 })!,
            "audio template stream without channels is not usable");
        AssertEqual(
            true,
            (bool)templateStreamScore.Invoke(null, new object?[] { audioType, 0, 0, 48000, 2 })!,
            "audio template stream with sample rate and channels is usable");
        AssertEqual(
            false,
            (bool)templateStreamScore.Invoke(null, new object?[] { videoType, 0, 1080, 0, 0 })!,
            "video template stream without width is not usable");
        AssertEqual(
            true,
            (bool)templateStreamScore.Invoke(null, new object?[] { videoType, 1920, 1080, 0, 0 })!,
            "video template stream with dimensions is usable");
        AssertEqual(
            true,
            (bool)templateStreamScore.Invoke(null, new object?[] { subtitleType, 0, 0, 0, 0 })!,
            "non-audio/video streams keep their template slot");

        AssertEqual(
            true,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 0, 2, 8, 48000, 2, 8 })!,
            "incomplete input sample rate can use complete template audio params");
        AssertEqual(
            false,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 48000, 2, 8, 44100, 2, 8 })!,
            "audio sample-rate mismatch is rejected");
        AssertEqual(
            false,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 48000, 2, 8, 48000, 1, 8 })!,
            "audio channel mismatch is rejected");
        AssertEqual(
            false,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 48000, 2, 8, 48000, 2, 9 })!,
            "audio sample-format mismatch is rejected");
        AssertEqual(
            true,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 48000, 2, 8, 48000, 2, 8 })!,
            "matching complete audio params are accepted");

        AssertContains(streamsText, "private static int CountUsableTemplateStreams(AVFormatContext* inputContext, int inputStreamCount)");
        AssertContains(streamsText, "private static bool TemplateStreamContributesToTemplateScore(");
        AssertContains(streamsText, "AVMediaType.AVMEDIA_TYPE_AUDIO => channelCount > 0 && sampleRate > 0,");
        AssertContains(streamsText, "AVMediaType.AVMEDIA_TYPE_VIDEO => width > 0 && height > 0,");
        AssertContains(streamsText, "private static bool AudioParamsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)");
        AssertContains(streamsText, "var inputAudioParamsIncomplete = inputSampleRate <= 0 || inputChannelCount <= 0;");
        AssertContains(streamsText, "var templateHasCompleteAudioParams = templateSampleRate > 0 && templateChannelCount > 0;");
        AssertContains(streamsText, "return templateHasCompleteAudioParams;");
        AssertContains(streamsText, "if (!AudioParamsMatchOrCanUseTemplate(inputCodec, templateCodec))");
        AssertContains(streamsText, "audio_params expected=");
        AssertContains(templateSelectionBlock, "var bestMappedStreamCount = -1;");
        AssertContains(templateSelectionBlock, "var candidateMappedStreamCount = CountUsableTemplateStreams(_activeInputContext, candidateStreamCount);");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_CANDIDATE");
        AssertContains(templateSelectionBlock, "mapped_streams={candidateMappedStreamCount}");
        AssertContains(templateSelectionBlock, "if (candidateMappedStreamCount > bestMappedStreamCount)");
        AssertContains(templateSelectionBlock, "bestTemplatePath = templatePath;");
        AssertContains(templateSelectionBlock, "OpenInput(bestTemplatePath);");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(templateSelectionBlock, "mapped_streams={bestMappedStreamCount}");
        AssertOccursBefore(
            templateSelectionBlock,
            "var candidateMappedStreamCount = CountUsableTemplateStreams(_activeInputContext, candidateStreamCount);",
            "OpenInput(bestTemplatePath);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped()
    {
        var segmentExportCore = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var skipTrackingText = segmentPacketWritingText;
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = segmentTemplateText;

        AssertContains(segmentExportCore, "WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentPacketWritingText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertDoesNotContain(segmentPacketWritingText, "void TrackSkippedRequestedSegment(FlashbackExportSegment segment, string reason)");
        AssertContains(skipTrackingText, "private struct RequestedSegmentSkipTracker");
        AssertContains(skipTrackingText, "public void Track(FlashbackExportSegment segment, string reason)");
        AssertContains(skipTrackingText, "SegmentOverlapsExportRange(segment, _inPoint, _outPoint)");
        AssertContains(skipTrackingText, "public bool TryCreateFailureMessage(out string message)");
        AssertContains(segmentPacketWritingText, "ref requestedSegmentSkips,");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"not_found\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"invalid_stream_count\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"stream_count_mismatch\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"stream_layout_mismatch\");");
        AssertDoesNotContain(segmentPacketWritingText, "requestedSegmentSkips.Track(segment, \"video_stream_missing\");");
        AssertDoesNotContain(segmentPacketWritingText, "requestedSegmentSkips.Track(segment, \"video_params_incomplete\");");
        AssertContains(segmentPacketWritingText, "if (!TryInitializeSegmentOutputTemplate(segments, tmpPath, fastStart, ct, out streamCount, out videoStreamIndex, out streamMap, out var templateFailure, out var templateFailureCode))");
        AssertOccursBefore(segmentPacketWritingText, "if (!TryInitializeSegmentOutputTemplate(segments, tmpPath, fastStart, ct, out streamCount, out videoStreamIndex, out streamMap, out var templateFailure, out var templateFailureCode))", "for (var segIdx = 0; segIdx < segments.Count; segIdx++)");
        AssertContains(skipTrackingText, "requested segment(s) were skipped");
        AssertOccursBefore(segmentPacketWritingText, "if (requestedSegmentSkips.TryCreateFailureMessage(out var skippedSegmentFailureMessage))", "if (totalPackets == 0)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RetainsHiddenDecoderPreroll()
    {
        var exporter = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var normalize = exporter.GetMethod("NormalizePacketTimestamps", BindingFlags.Static | BindingFlags.NonPublic)!;
        var resolveBase = exporter.GetMethod("ResolveSegmentOutputTimestampBaseUs", BindingFlags.Static | BindingFlags.NonPublic)!;
        // A GOP before a non-keyframe cut must retain distinct negative timestamps.
        // Clamping them to zero destroys decoding order and exposes the lead-in.
        Assert.Equal((-300L, -320L), ((long, long))normalize.Invoke(null, new object[] { -300L, -320L, true })!);
        Assert.Equal((-290L, -310L), ((long, long))normalize.Invoke(null, new object[] { -290L, -310L, true })!);
        Assert.Equal((0L, 0L), ((long, long))normalize.Invoke(null, new object[] { -300L, -320L, false })!);
        Assert.Equal((long.MinValue, long.MinValue), ((long, long))normalize.Invoke(null, new object[] { long.MinValue, long.MinValue, true })!);
        Assert.Equal(1_350_000L, resolveBase.Invoke(null, new object[] { 1_000_000L, 350_000L }));
        Assert.Equal(long.MaxValue, resolveBase.Invoke(null, new object[] { long.MaxValue - 2, 10L }));
        var createState = exporter.GetMethod("CreateSegmentPacketWriteState", BindingFlags.Static | BindingFlags.NonPublic)!;
        var state = createState.Invoke(null, new object[] { 0, 2, true, 165_700_000L, 30_000L, 5_130_000L, 0L, 0, 8_333L })!;
        state.GetType().GetProperty("MinBaseUs")!.SetValue(state, 165_630_667L);
        Assert.Equal(165_700_000L, state.GetType().GetProperty("TimestampOriginUs")!.GetValue(state));
        Assert.Equal(165_730_000L, resolveBase.Invoke(null, new object[] { state.GetType().GetProperty("TimestampOriginUs")!.GetValue(state)!, 30_000L }));
        var source = ReadFlashbackExporterSource();
        AssertContains(source, "outputContext->avoid_negative_ts = ffmpeg.AVFMT_AVOID_NEG_TS_DISABLED;");
        AssertContains(source, "\"use_editlist\", \"1\"");
        AssertContains(source, "ffmpeg.av_seek_frame(_activeInputContext, state.VideoStreamIndex, seekTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD)");
        AssertContains(source, "if ((packet->flags & ffmpeg.AV_PKT_FLAG_KEY) == 0) continue;");
        AssertContains(source, "var keyframeLimitUs = Math.Max(targetUs, state.TimestampBasesUs[state.VideoStreamIndex]);");
        AssertContains(source, "if (ptsUs > keyframeLimitUs) break;");
        AssertContains(source, "FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);");
        AssertContains(source, "preservePreroll: state.UseSegmentTimeline && state.SegmentInOffsetUs > 0");
        AssertDoesNotContain(source, "comparePtsUs < state.SegmentInOffsetUs");
        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_PreservesTimelineAcrossDelayedSegmentPackets()
    {
        var exporter = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var resolveOffset = exporter.GetMethod("ResolveSegmentOutputOffsetUs", BindingFlags.Static | BindingFlags.NonPublic)!;
        long Offset(TimeSpan? start, TimeSpan inPoint, long fallback)
            => (long)resolveOffset.Invoke(null, new object?[] { start, inPoint, fallback })!;

        // Real 4K120 rotation: the next file contains two delayed packets before
        // its nominal 150-second boundary. Packing after the preceding file's
        // last frame would overlap them and collapse two frame durations.
        var offsetUs = Offset(TimeSpan.FromMilliseconds(1_002_975), TimeSpan.FromMilliseconds(852_975), 149_983_333);
        Assert.Equal(150_000_000L, offsetUs);
        Assert.Equal(149_983_333L, offsetUs - 16_667);
        Assert.Equal(149_991_667L, offsetUs - 8_333);
        Assert.Equal(175_000L, Offset(TimeSpan.FromMilliseconds(1_002_975), TimeSpan.FromMilliseconds(1_002_800), 158_333));
        Assert.Equal(0L, Offset(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), 0));
        Assert.Equal(7_000_000L, Offset(null, TimeSpan.Zero, 7_000_000));
        AssertContains(ReadFlashbackExporterSource(), "ResolveSegmentOutputOffsetUs(segment.StartPts, inPoint, outputPtsOffsetUs)");
        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_TimestampConversionsAreSaturating()
    {
        var sourceText = ReadFlashbackExporterSource();
        var packetTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "TotalSeconds * ffmpeg.AV_TIME_BASE");
        AssertDoesNotContain(sourceText, "TotalMilliseconds * 1000)");
        AssertContains(sourceText, "var seekTimestamp = ToAvTimeBaseTimestamp(inPoint);");
        AssertContains(sourceText, "ToAvTimeBaseTimestampOrMax(outPoint),");
        AssertContains(sourceText, "var outPtsLimitUs = ToAvTimeBaseTimestampOrMax(outPoint);");
        AssertContains(sourceText, "var segmentInOffsetUs = ToMicrosecondsSaturated(SaturatingSubtract(inPoint, segment.StartPts!.Value));");
        AssertContains(sourceText, "var segmentOutDelta = SaturatingSubtract(");
        AssertContains(sourceText, "var segmentOutOffsetUs = ToMicrosecondsSaturated(segmentOutDelta);");
        AssertContains(sourceText, "SkipBecauseEmpty: segmentOutDelta <= TimeSpan.Zero");
        AssertContains(sourceText, "private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(sourceText, "var segmentLength = new FileInfo(segment.Path).Length;\n                    readableSegmentCount++;\n                    totalEstimatedBytes = AddNonNegativeSaturated(totalEstimatedBytes, segmentLength);");
        AssertContains(sourceText, "bytesProcessed = AddNonNegativeSaturated(bytesProcessed, new FileInfo(segPath).Length);");
        AssertDoesNotContain(sourceText, "inPoint - segment.StartPts!.Value");
        AssertDoesNotContain(sourceText, " - segment.StartPts!.Value\n                        : TimeSpan.Zero;");
        AssertDoesNotContain(sourceText, "totalEstimatedBytes += new FileInfo(segment.Path).Length");
        AssertDoesNotContain(sourceText, "bytesProcessed += new FileInfo(segPath).Length");
        AssertContains(sourceText, "private static long ToAvTimeBaseTimestampOrMax(TimeSpan value)\n        => value == TimeSpan.MaxValue ? long.MaxValue : ToAvTimeBaseTimestamp(value);");
        AssertContains(sourceText, "private static long ToMicrosecondsSaturated(TimeSpan value)");
        AssertContains(sourceText, "if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "if (videoStream != null && IsValidPositiveRational(videoStream->avg_frame_rate))");
        AssertContains(sourceText, "if (videoStream != null && IsValidPositiveRational(videoStream->r_frame_rate))");
        AssertContains(sourceText, "private static bool IsValidPositiveRational(AVRational value)\n        => value.num > 0 && value.den > 0;");
        AssertDoesNotContain(sourceText, "videoStream->avg_frame_rate.num > 0)");
        AssertDoesNotContain(sourceText, "videoStream->r_frame_rate.num > 0)");
        AssertContains(packetTimingText, "private static long ResolveFrameDurationUs(AVStream* videoStream)");
        AssertContains(packetTimingText, "private static long ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(packetTimingText, "private static bool TryResolveTimestampBase(AVPacket* packet, out long timestampBase)");
        AssertContains(packetTimingText, "private static void NormalizePacketTimestampsBeforeWrite(AVPacket* packet, bool preservePreroll = false)");
        AssertContains(packetTimingText, "if (pts != ffmpeg.AV_NOPTS_VALUE && pts < 0) pts = 0;");
        AssertContains(packetTimingText, "if (dts != ffmpeg.AV_NOPTS_VALUE && dts < 0) dts = 0;");
        AssertContains(packetTimingText, "pts != ffmpeg.AV_NOPTS_VALUE && dts != ffmpeg.AV_NOPTS_VALUE && pts < dts");
        AssertContains(packetTimingText, "private long FlushBufferedPackets(");
        AssertContains(packetTimingText, "private static void FreeBufferedPackets(");
        AssertContains(packetTimingText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertEqual(3, sourceText.Split("NormalizePacketTimestampsBeforeWrite(", StringSplitOptions.None).Length - 2, "All export packet write paths normalize timestamps");
        AssertDoesNotContain(sourceText, "if (packet->pts < 0) packet->pts = 0;");
        AssertDoesNotContain(sourceText, "if (packet->dts < 0) packet->dts = 0;");
        AssertDoesNotContain(sourceText, "if (buffPkt->pts < 0) buffPkt->pts = 0;");
        AssertDoesNotContain(sourceText, "if (buffPkt->dts < 0) buffPkt->dts = 0;");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExportOutputTransaction_OwnsFilesystemPublication()
    {
        var transactionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExportOutputTransaction.cs")
            .Replace("\r\n", "\n");
        var exporterText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var finalizeBlock = ExtractTextBetween(
            exporterText,
            "private bool TryFinalizeActiveOutputFile(",
            "private static bool TryValidateSegmentExportInputs(");

        AssertContains(transactionText, "internal sealed class FlashbackExportOutputTransaction : IDisposable");
        AssertDoesNotContain(transactionText, "partial class FlashbackExportOutputTransaction");
        AssertContains(transactionText, "internal static bool TryReserve(");
        AssertContains(transactionText, "internal bool TryPublish(string outputPath, out long outputBytes, out string failureMessage, out string failureCode)");
        AssertContains(transactionText, "private void Abandon()");
        AssertContains(transactionText, "FileMode.CreateNew");
        AssertContains(transactionText, "private readonly record struct TempFileIdentity");
        AssertContains(transactionText, "private bool TryOpenVerifiedHandle(");
        AssertContains(transactionText, "replaceIfExists: false");
        AssertContains(transactionText, "SetFileInformationByHandle(handle, FileInformationClass.FileRenameInfo");
        AssertContains(transactionText, "SetFileInformationByHandle(handle!, FileInformationClass.FileDispositionInfo");
        AssertContains(transactionText, "internal static void CleanupOrphanedTempFiles(string directory)");
        AssertDoesNotContain(transactionText, "AVFormatContext");
        AssertDoesNotContain(transactionText, "av_write_trailer");
        AssertOccursBefore(finalizeBlock, "av_write_trailer", "ThrowIfError(CloseOutputIo(), \"avio_closep\", FlashbackExportFailureCodes.OutputWriteFailed);");
        AssertOccursBefore(finalizeBlock, "ThrowIfError(CloseOutputIo(), \"avio_closep\", FlashbackExportFailureCodes.OutputWriteFailed);", "outputTransaction.TryPublish(");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_OwnsFfmpegExportLifecycle()
    {
        var requestsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var singleFilePacketReadLoopText = requestsText;
        var singleFilePacketWritingText = singleFilePacketReadLoopText;
        var singleFilePacketWriteStateText = singleFilePacketReadLoopText;
        var singleFilePacketRebasingText = singleFilePacketReadLoopText;
        var segmentPacketWritingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentsText = segmentPacketWritingText;
        var segmentPacketReadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentPacketWriteStateText = segmentPacketReadLoopText;
        var segmentPacketRebasingText = segmentPacketReadLoopText;
        var segmentRangeProjectionText = segmentPacketWritingText;
        var segmentSkipTrackingText = segmentPacketWritingText;
        var segmentTemplateText = segmentsText;
        var segmentInputPreflightText = segmentTemplateText;
        var executionPolicyText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var singleFileText = executionPolicyText;
        var validationText = executionPolicyText;
        var segmentValidationText = validationText;
        var libAvErrorsText = lifecycleText;
        var packetTimingText = segmentPacketWritingText;
        var streamsText = lifecycleText;
        var streamTemplatesText = streamsText;
        var timeMathText = packetTimingText;
        var packetBuffersText = packetTimingText;

        AssertContains(lifecycleText, "internal sealed unsafe class FlashbackExporter : IDisposable");
        AssertDoesNotContain(lifecycleText, "partial class FlashbackExporter");
        AssertContains(requestsText, "public Task<FinalizeResult> ExportAsync(");
        AssertContains(requestsText, "request.SegmentPaths.Select(path => new FlashbackExportSegment");
        AssertContains(lifecycleText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(lifecycleText, "private readonly SemaphoreSlim _exportLock = new(1, 1);");
        AssertContains(lifecycleText, "private AVFormatContext* _activeInputContext;");
        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(lifecycleText, "FLASHBACK_EXPORT_DISPOSE_TIMEOUT_OK");
        AssertContains(singleFileText, "private FinalizeResult ExportCore(");
        AssertContains(singleFileText, "WriteSingleFilePacketsToActiveOutput(");
        AssertContains(singleFileText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(singleFilePacketWritingText, "private SingleFilePacketWriteResult WriteSingleFilePacketsToActiveOutput(");
        AssertContains(singleFilePacketWritingText, "private readonly record struct SingleFilePacketWriteResult(FinalizeResult? Failure, long TotalPackets);");
        AssertContains(singleFilePacketWritingText, "WriteSingleFilePacketReadLoop(");
        AssertContains(singleFilePacketWritingText, "LogTimestampBaseDrift(packetState.TimestampBasesUs, packetState.HasTimestampBase);");
        AssertContains(singleFilePacketWritingText, "Flashback export failed: no video packets were written.");
        AssertContains(singleFilePacketReadLoopText, "private void WriteSingleFilePacketReadLoop(");
        AssertContains(singleFilePacketReadLoopText, "var packet = ffmpeg.av_packet_alloc();");
        AssertContains(singleFilePacketReadLoopText, "var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);");
        AssertContains(singleFilePacketReadLoopText, "FreeBufferedPackets(packetState.BufferedPackets, packetState.BufferedStreamIndices);");
        AssertContains(singleFilePacketReadLoopText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertContains(singleFilePacketReadLoopText, "private struct SingleFilePacketWriteState");
        AssertContains(singleFilePacketWriteStateText, "private struct SingleFilePacketWriteState");
        AssertContains(singleFilePacketWriteStateText, "private static readonly AVRational SingleFilePacketUsTimeBase");
        AssertContains(singleFilePacketWriteStateText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertContains(singleFilePacketWriteStateText, "private void FlushSingleFileBufferedPacketsAtEof(");
        AssertContains(singleFilePacketRebasingText, "private void WriteSingleFilePacket(");
        AssertContains(singleFilePacketRebasingText, "private static bool PacketPtsExceedsSingleFileOutPoint(");
        AssertContains(singleFilePacketRebasingText, "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), \"av_interleaved_write_frame\", FlashbackExportFailureCodes.OutputWriteFailed);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SingleFilePacketReadLoop.cs")),
            "single-file packet pump folded into FlashbackExporter.cs");
        AssertDoesNotContain(singleFileText, "var timestampBasesUs = new long[streamCount];");
        AssertDoesNotContain(singleFileText, "LogTimestampBaseDrift(timestampBasesUs, hasTimestampBase);");
        AssertContains(segmentsText, "private FinalizeResult ExportSegmentsCore(");
        AssertContains(segmentsText, "TryValidateSegmentExportInputs(");
        AssertContains(segmentsText, "TryEstimateSegmentExportReadableBytes(");
        AssertContains(segmentsText, "WriteSegmentPacketsToActiveOutput(");
        AssertOccursBefore(segmentsText, "private FinalizeResult ExportSegmentsCore(", "private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentsText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertContains(segmentsText, "var segmentExportWindow = ProjectSegmentExportWindow(segment, inPoint, outPoint, outPtsLimitUs);");
        AssertContains(segmentPacketWritingText, "private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertContains(segmentPacketWritingText, "var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);");
        AssertContains(segmentPacketWritingText, "var segmentExportWindow = ProjectSegmentExportWindow(segment, inPoint, outPoint, outPtsLimitUs);");
        AssertContains(segmentPacketWritingText, "WriteSegmentPacketReadLoop(");
        AssertContains(segmentPacketReadLoopText, "private void WriteSegmentPacketReadLoop(");
        AssertContains(segmentPacketReadLoopText, "var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);");
        AssertContains(segmentPacketReadLoopText, "ffmpeg.av_packet_unref(packet);");
        AssertContains(segmentPacketReadLoopText, "var clone = ClonePacketOrThrow(packet, \"segment_buffer\");");
        AssertContains(segmentPacketReadLoopText, "FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH");
        AssertContains(segmentPacketReadLoopText, "FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);");
        AssertContains(segmentPacketWriteStateText, "private struct SegmentPacketWriteState");
        AssertContains(segmentPacketWriteStateText, "private int FlushSegmentBufferedPackets(");
        AssertContains(segmentPacketRebasingText, "private SegmentPacketWriteOutcome WriteRebasedSegmentPacket(");
        AssertContains(segmentPacketRebasingText, "ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(segmentPacketRebasingText, "packet->dts = lastDtsPerOutputStream[outputStreamIndex] + 1;");
        AssertContains(segmentPacketRebasingText, "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), \"av_interleaved_write_frame\", FlashbackExportFailureCodes.OutputWriteFailed);");
        AssertContains(segmentPacketWriteStateText, "private enum SegmentPacketWriteOutcome");
        AssertContains(segmentPacketWriteStateText, "public List<IntPtr> BufferedPackets { get; }");
        AssertContains(segmentPacketWriteStateText, "public long VideoTimestampRepairUs { get; set; }");
        AssertContains(segmentRangeProjectionText, "private readonly record struct SegmentExportWindow(");
        AssertContains(segmentRangeProjectionText, "private static SegmentExportWindow ProjectSegmentExportWindow(");
        AssertContains(segmentRangeProjectionText, "SkipBecauseEmpty: segmentOutDelta <= TimeSpan.Zero");
        AssertContains(segmentPacketWritingText, "TryOpenSegmentInputForExport(");
        AssertContains(segmentsText, "avformat_find_stream_info(_activeInputContext, null)");
        AssertContains(segmentSkipTrackingText, "private struct RequestedSegmentSkipTracker");
        AssertContains(segmentSkipTrackingText, "public void Track(FlashbackExportSegment segment, string reason)");
        AssertContains(segmentSkipTrackingText, "public bool TryCreateFailureMessage(out string message)");
        AssertContains(segmentsText, "ReleaseExportLockBestEffort(\"segment_export\");");
        AssertContains(segmentInputPreflightText, "private bool TryOpenSegmentInputForExport(");
        AssertContains(segmentInputPreflightText, "ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), \"avformat_find_stream_info\", FlashbackExportFailureCodes.InputReadFailed);");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"not_found\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"invalid_stream_count\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"stream_count_mismatch\");");
        AssertContains(segmentInputPreflightText, "requestedSegmentSkips.Track(segment, \"stream_layout_mismatch\");");
        AssertContains(segmentTemplateText, "private bool TryInitializeSegmentOutputTemplate(");
        AssertContains(segmentTemplateText, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(segmentsText, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(segmentValidationText, "private static bool TryValidateSegmentExportInputs(");
        AssertContains(segmentValidationText, "private static bool TryEstimateSegmentExportReadableBytes(");
        AssertContains(segmentValidationText, "private static int FindInvalidSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertContains(segmentValidationText, "private static int FindDuplicateSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertContains(executionPolicyText, "private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)");
        AssertContains(executionPolicyText, "private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)");
        AssertContains(executionPolicyText, "private const int ExportWriterYieldPacketInterval = 256;");
        AssertContains(executionPolicyText, "private const int ExportWriterThrottlePacketInterval = 4096;");
        AssertContains(executionPolicyText, "private const int ExportWriterThrottleSleepMs = 1;");
        AssertContains(executionPolicyText, "private const int ExportWriterAdaptiveThrottlePacketInterval = 4;");
        AssertContains(executionPolicyText, "private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;");
        AssertContains(executionPolicyText, "private readonly object _adaptiveThrottleSync = new();");
        AssertContains(executionPolicyText, "private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)");
        AssertContains(executionPolicyText, "private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()");
        AssertContains(executionPolicyText, "private static FinalizeResult RunWithAdaptiveThrottle(");
        AssertContains(executionPolicyText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertContains(executionPolicyText, "private bool TryFinalizeActiveOutputFile(");
        AssertContains(executionPolicyText, "ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), \"av_write_trailer\", FlashbackExportFailureCodes.OutputWriteFailed);");
        AssertContains(executionPolicyText, "ThrowIfError(CloseOutputIo(), \"avio_closep\", FlashbackExportFailureCodes.OutputWriteFailed);");
        AssertContains(executionPolicyText, "outputTransaction.TryPublish(outputPath, out outputBytes, out failureMessage, out failureCode)");
        AssertContains(executionPolicyText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{failureMessage}'\");");
        AssertContains(singleFileText, "av_write_trailer(_activeOutputContext)");
        AssertContains(singleFileText, "ThrowIfError(CloseOutputIo(), \"avio_closep\", FlashbackExportFailureCodes.OutputWriteFailed);\n\n        if (!outputTransaction.TryPublish");
        AssertContains(singleFileText, "if (!TryFinalizeActiveOutputFile(outputTransaction, outputPath, out var outputBytes, out var outputFailure, out var outputFailureCode))");
        AssertContains(segmentsText, "if (!TryFinalizeActiveOutputFile(outputTransaction, outputPath, out var outputBytes, out var outputFailure, out var outputFailureCode))");
        AssertContains(lifecycleText, "private bool TryWaitForExportLock(string outputPath, CancellationToken ct, [NotNullWhen(false)] out FinalizeResult? cancellationResult)");
        AssertContains(lifecycleText, "private void ReleaseExportLockBestEffort(string operation)");
        AssertContains(lifecycleText, "private void DisposeExportLockBestEffort()");
        AssertContains(lifecycleText, "private static FinalizeResult CreateCancelledExportResult(string outputPath)");
        AssertContains(lifecycleText, "private static FinalizeResult CreateDisposedExportResult(string outputPath)");
        AssertContains(validationText, "private static bool IsSamePath(string? left, string? right)");
        AssertContains(validationText, "private static bool TryValidateOutputPath(string outputPath, out string fullOutputPath, out string failureMessage)");
        AssertContains(validationText, "private static bool SegmentOverlapsExportRange(");
        AssertContains(validationText, "private static bool TryValidateExportRange(TimeSpan inPoint, TimeSpan outPoint, out string failureMessage)");
        AssertContains(lifecycleText, "private void CloseActiveInput()");
        AssertContains(lifecycleText, "private int CloseOutputIo()");
        AssertContains(lifecycleText, "private void CleanupNativeState()");
        AssertContains(lifecycleText, "private CancellationTokenSource CreateExportCancellationSource(CancellationToken ct)");
        AssertContains(lifecycleText, "private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(lifecycleText, "private void ClearDisposeCtsReference(CancellationTokenSource? disposeCts)");
        AssertContains(lifecycleText, "private void EnsureNotDisposed()");
        AssertContains(libAvErrorsText, "private static void ThrowIfError(int errorCode, string operation, string failureCode)");
        AssertContains(libAvErrorsText, "private static string GetErrorString(int errorCode)");
        AssertContains(packetTimingText, "private static long ResolveFrameDurationUs(AVStream* videoStream)");
        AssertContains(packetTimingText, "private static long ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(packetTimingText, "private static void NormalizePacketTimestampsBeforeWrite(AVPacket* packet, bool preservePreroll = false)");
        AssertContains(packetBuffersText, "private long FlushBufferedPackets(");
        AssertContains(packetBuffersText, "private static void FreeBufferedPackets(");
        AssertContains(packetBuffersText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(packetBuffersText, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.PacketBuffers.cs")),
            "FlashbackExporter.PacketBuffers.cs folded into FlashbackExporter.cs");
        AssertContains(streamsText, "private void OpenInput(string inputPath)");
        AssertContains(streamsText, "private void CreateOutputContext(string tmpPath, bool fastStart)");
        AssertContains(streamsText, "private static void OpenOutputIoAndWriteHeader(AVFormatContext* outputContext, string tmpPath, bool fastStart)");
        AssertContains(streamTemplatesText, "private static int[] CopyTemplateStreams(");
        AssertContains(streamTemplatesText, "private static string? FindSegmentStreamLayoutMismatch(");
        AssertContains(streamTemplatesText, "private static bool VideoDimensionsMatchOrCanUseTemplate(");
        AssertContains(timeMathText, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(timeMathText, "private static long ToAvTimeBaseTimestampOrMax(TimeSpan value)");
        AssertContains(timeMathText, "private static long ToAvTimeBaseTimestamp(TimeSpan value)");
        AssertContains(timeMathText, "private static long ToMicrosecondsSaturated(TimeSpan value)");
        AssertContains(timeMathText, "private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.cs")),
            "FlashbackExporter.cs is the consolidated exporter owner");
        foreach (var removedFile in new[]
        {
            "FlashbackExporter.Lifetime.cs",
            "FlashbackExporter.ExportLock.cs",
            "FlashbackExporter.NativeState.cs",
            "FlashbackExporter.Cancellation.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackExporter.cs");
        }
        foreach (var removedFile in new[]
        {
            "FlashbackExporter.OutputValidation.cs",
            "FlashbackExporter.PathValidation.cs",
            "FlashbackExporter.SegmentSelection.cs",
            "FlashbackExporter.Validation.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackExporter.cs");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SegmentValidation.cs")),
            "FlashbackExporter.SegmentValidation.cs folded into FlashbackExporter.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackExporter.Progress.cs",
            "FlashbackExporter.WriterPacing.cs",
            "FlashbackExporter.RuntimePolicy.cs",
            "FlashbackExporter.SingleFile.cs",
            "FlashbackExporter.OutputFiles.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackExporter.cs");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SegmentPacketRebasing.cs")),
            "FlashbackExporter.SegmentPacketRebasing.cs folded into FlashbackExporter.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.SegmentTemplate.cs")),
            "FlashbackExporter.SegmentTemplate.cs folded into FlashbackExporter.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.Segments.cs")),
            "FlashbackExporter.Segments.cs folded into FlashbackExporter.cs");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation()
    {
        var sourceText = ReadFlashbackExporterSource();
        var packetBuffersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var executionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private readonly object _lifetimeSync = new();");
        AssertContains(sourceText, "return Task.FromResult(CreateDisposedExportResult(request.OutputPath));");
        AssertEqual(2, sourceText.Split("return Task.FromResult(CreateDisposedExportResult(outputPath));", StringSplitOptions.None).Length - 1, "Single and segment wrappers return disposed result");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            cancellationResult = CreateDisposedExportResult(outputPath);\n            return false;\n        }");
        AssertContains(sourceText, "linkedCts = CreateExportCancellationSource(ct);");
        AssertContains(sourceText, "var segmentSnapshot = SnapshotSegments(segments);");
        AssertContains(sourceText, "private static IReadOnlyList<FlashbackExportSegment> SnapshotSegments(IReadOnlyList<FlashbackExportSegment>? segments)");
        AssertContains(sourceText, "snapshot[i] = segment == null\n                ? new FlashbackExportSegment { Path = string.Empty }\n                : segment with { };");
        AssertContains(sourceText, "CancellationTokenSource.CreateLinkedTokenSource(ct, disposeCts.Token)");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposed, this);");
        AssertContains(sourceText, "private static FinalizeResult CreateDisposedExportResult(string outputPath)");
        AssertContains(sourceText, "const string message = \"Flashback exporter is disposed.\";");
        AssertContains(sourceText, "private const int ExportLockWaitTimeoutSeconds = 30;");
        AssertContains(executionText, "private const int ExportWriterYieldPacketInterval = 256;");
        AssertContains(executionText, "private const int ExportWriterThrottlePacketInterval = 4096;");
        AssertContains(executionText, "private const int ExportWriterThrottleSleepMs = 1;");
        AssertContains(executionText, "private const int ExportWriterAdaptiveThrottlePacketInterval = 4;");
        AssertContains(executionText, "private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;");
        AssertContains(sourceText, "_exportLock.Wait(TimeSpan.FromSeconds(ExportLockWaitTimeoutSeconds), ct)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_WAIT_TIMEOUT");
        AssertContains(sourceText, "return RunWithBackgroundPriority(\n                () => RunWithAdaptiveThrottle(\n                    adaptiveThrottleDelayMsProvider,\n                    () => ExportCore(inputPath, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),\n                () => DisposeLinkedCtsBestEffort(linkedCts, \"single_export\"));");
        AssertContains(sourceText, "return RunWithBackgroundPriority(\n                () => RunWithAdaptiveThrottle(\n                    adaptiveThrottleDelayMsProvider,\n                    () => ExportSegmentsCore(segmentSnapshot, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),\n                () => DisposeLinkedCtsBestEffort(linkedCts, \"segment_export\"));");
        AssertContains(sourceText, "thread.Priority = ThreadPriority.BelowNormal;");
        AssertContains(sourceText, "thread.Priority = previousPriority;");
        AssertContains(sourceText, "Func<int>? adaptiveThrottleDelayMsProvider");
        AssertContains(executionText, "private readonly object _adaptiveThrottleSync = new();");
        AssertContains(executionText, "private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)");
        AssertContains(executionText, "private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()");
        AssertContains(executionText, "[ThreadStatic]\n    private static Func<int>? s_adaptiveThrottleDelayMsProvider;");
        AssertContains(executionText, "private static FinalizeResult RunWithAdaptiveThrottle(");
        AssertContains(executionText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertContains(executionText, "packetsWritten % ExportWriterAdaptiveThrottlePacketInterval == 0");
        AssertContains(executionText, "ExportWriterMaxAdaptiveThrottleSleepMs");
        AssertContains(executionText, "Thread.Sleep(ExportWriterThrottleSleepMs);");
        AssertContains(executionText, "Thread.Yield();");
        AssertContains(sourceText, "ThrottleExportWriterIfNeeded(totalPackets);");
        AssertContains(sourceText, "ThrottleExportWriterIfNeeded(written);");
        AssertContains(sourceText, "private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LINKED_CTS_DISPOSE_WARN");
        AssertContains(packetBuffersText, "private long FlushBufferedPackets(");
        AssertContains(packetBuffersText, "NormalizePacketTimestampsBeforeWrite(buffPkt);");
        AssertContains(packetBuffersText, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertContains(packetBuffersText, "private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)");
        AssertContains(packetBuffersText, "ffmpeg.av_packet_free(&p);");
        AssertContains(packetBuffersText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(packetBuffersText, "var clone = ffmpeg.av_packet_clone(packet);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.PacketBuffers.cs")),
            "FlashbackExporter.PacketBuffers.cs folded into FlashbackExporter.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.RuntimePolicy.cs")),
            "FlashbackExporter.RuntimePolicy.cs folded into FlashbackExporter.cs");
        AssertContains(sourceText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(sourceText, "ReleaseExportLockBestEffort(\"segment_export\");");
        AssertContains(sourceText, "private void ReleaseExportLockBestEffort(string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_RELEASE_WARN");
        AssertDoesNotContain(sourceText, "catch (ObjectDisposedException) { }");
        AssertDoesNotContain(sourceText, "}, linkedCts.Token);");
        AssertDoesNotContain(sourceText, "_disposeCts!.Token");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState()
    {
        var sourceText = ReadFlashbackExporterSource();

        var disposeBlock = ExtractTextBetween(
            sourceText,
            "public void Dispose()",
            "    private FinalizeResult ExportCore");
        AssertContains(disposeBlock, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'\");\n        }");
        AssertOccursBefore(disposeBlock, "FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN", "var lockAcquired = _exportLock.Wait(TimeSpan.FromSeconds(10));");
        AssertContains(disposeBlock, "ReleaseExportLockBestEffort(\"dispose\");");
        AssertContains(disposeBlock, "DisposeExportLockBestEffort();");
        AssertContains(disposeBlock, "DisposeLinkedCtsBestEffort(disposeCts, \"dispose\");");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_DISPOSE_WARN");

        var timeoutBlock = ExtractTextBetween(
            sourceText,
            "if (!lockAcquired)",
            "        try\n        {\n            CleanupNativeState();");

        AssertContains(timeoutBlock, "FLASHBACK_EXPORT_DISPOSE: timed out waiting for export lock");
        AssertContains(timeoutBlock, "DisposeLinkedCtsBestEffort(disposeCts, \"dispose_timeout\");");
        AssertContains(timeoutBlock, "ClearDisposeCtsReference(disposeCts);");
        AssertContains(timeoutBlock, "return;");
        AssertDoesNotContain(timeoutBlock, "CleanupNativeState()");
        AssertDoesNotContain(timeoutBlock, "_exportLock.Dispose()");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExporter_InputStreamCountsAreBounded()
    {
        var sourceText = ReadFlashbackExporterSource();
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = streamsText;
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = segmentTemplateText;

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(streamsText, "private static bool TryGetInputStreamCount(");
        AssertContains(streamsText, "if (nativeStreamCount == 0)");
        AssertContains(streamsText, "if (nativeStreamCount > MaxSupportedInputStreams)");
        AssertContains(streamsText, "streamCount = (int)nativeStreamCount;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"single_export\", out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{streamCountFailure}'\");");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_template\", out var candidateStreamCount, out var streamCountFailure))");
        AssertContains(segmentInputPreflightText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_export\", out currentStreamCount, out var streamCountFailure))");
        AssertContains(segmentInputPreflightText, "FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='invalid_stream_count'");
        AssertContains(singleFileText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount)");
        AssertContains(segmentTemplateText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, candidateStreamCount)");
        AssertContains(streamTemplatesText, "private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext, int inputStreamCount)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackExporter.Streams.cs")),
            "FlashbackExporter.Streams.cs folded into FlashbackExporter.cs");
        AssertDoesNotContain(sourceText, "checked((int)_activeInputContext->nb_streams)");
        AssertDoesNotContain(sourceText, "checked((int)inputContext->nb_streams)");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExporter_RejectsEmptySegmentPaths()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_empty_segment_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", " ");
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var outputPath = Path.Combine(tempDir, "empty-segment-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Empty segment path export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "segment path at index 0 is empty");
                AssertEqual(false, File.Exists(outputPath), "Empty segment path export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Empty segment path export does not leave temp output");

                var nullSegments = Array.CreateInstance(segmentType, 1);
                var nullSegmentOutputPath = Path.Combine(tempDir, "null-segment-export.mp4");
                var nullSegmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    nullSegments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    nullSegmentOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null for null segment.");

                AssertEqual(false, GetBoolProperty(nullSegmentResult, "Succeeded"), "Null segment export reports failure");
                AssertContains(GetStringProperty(nullSegmentResult, "StatusMessage"), "segment path at index 0 is empty");
                AssertEqual(false, File.Exists(nullSegmentOutputPath), "Null segment export does not create output");
                AssertEqual(false, File.Exists(nullSegmentOutputPath + ".tmp"), "Null segment export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RejectsDuplicateSegmentPaths()
    {
        var sourceText = ReadFlashbackExporterSource();
        AssertContains(sourceText, "var duplicateSegmentIndex = FindDuplicateSegmentPathIndex(segments);");
        AssertContains(sourceText, "Flashback export failed: duplicate segment path at index {duplicateSegmentIndex}.");
        AssertContains(sourceText, "private static int FindDuplicateSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");

        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_duplicate_segment_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segmentPath = Path.Combine(tempDir, "segment-0.ts");
                File.WriteAllText(segmentPath, "segment");

                var firstSegment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(firstSegment, "Path", segmentPath);
                var duplicateSegment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(duplicateSegment, "Path", Path.Combine(tempDir, ".", "segment-0.ts"));

                var segments = Array.CreateInstance(segmentType, 2);
                segments.SetValue(firstSegment, 0);
                segments.SetValue(duplicateSegment, 1);
                var outputPath = Path.Combine(tempDir, "duplicate-segment-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Duplicate segment path export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "duplicate segment path at index 1");
                AssertEqual(false, File.Exists(outputPath), "Duplicate segment path export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Duplicate segment path export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_missing_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", Path.Combine(tempDir, "missing-segment.ts"));
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var outputPath = Path.Combine(tempDir, "missing-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Missing segment export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "no readable segment files");
                AssertEqual(false, File.Exists(outputPath), "Missing segment export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Missing segment export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_OutputPathValidation_ReturnsFailure()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertContains(sourceText, "if (!TryValidateOutputPath(outputPath, out var normalizedOutputPath, out var outputPathFailure))\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{outputPathFailure}'\");\n            return FlashbackExportFailureCodes.Create(outputPath, outputPathFailure, FlashbackExportFailureCodes.InvalidOutputPath);\n        }\n        outputPath = normalizedOutputPath;");
        AssertContains(sourceText, "if (!TryValidateExportRange(inPoint, outPoint, out var rangeFailure))");
        AssertContains(sourceText, "private static bool TryValidateExportRange(TimeSpan inPoint, TimeSpan outPoint, out string failureMessage)");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: in point must not be negative.\";");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: export range is empty or invalid.\";");
        AssertContains(sourceText, "var invalidSegmentIndex = FindInvalidSegmentPathIndex(segments);");
        AssertContains(sourceText, "Flashback export failed: segment path at index {invalidSegmentIndex} is empty.");
        AssertContains(sourceText, "private static int FindInvalidSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertContains(sourceText, "private static bool TryValidateOutputPath(string outputPath, out string fullOutputPath, out string failureMessage)");
        AssertContains(sourceText, "fullOutputPath = Path.GetFullPath(outputPath);");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: output path is required.\";");
        AssertContains(sourceText, "catch (Exception ex)\n        {\n            failureMessage = $\"Flashback export failed: output path is invalid '{outputPath}'.\";\n            Logger.Log($\"FLASHBACK_EXPORT_PATH_VALIDATE_WARN path='{outputPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            return false;\n        }");
        AssertContains(sourceText, "failureMessage = $\"Flashback export failed: output path is invalid '{outputPath}'.\";");
        AssertContains(sourceText, "failureMessage = $\"Flashback export failed: output directory does not exist for '{outputPath}'.\";");
        AssertContains(sourceText, "if (Directory.Exists(fullOutputPath))\n        {\n            failureMessage = $\"Flashback export failed: output path is a directory '{outputPath}'.\";\n            return false;\n        }");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PATH_COMPARE_WARN");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PROGRESS_ESTIMATE_WARN");

        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var validateOutputPath = exporterType.GetMethod("TryValidateOutputPath", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryValidateOutputPath not found.");
        var args = new object?[] { ".\\flashback-relative-output.mp4", null, null };
        var isValid = (bool)validateOutputPath.Invoke(null, args)!;
        AssertEqual(true, isValid, "Relative output path validates when current directory exists");
        AssertEqual(
            Path.GetFullPath(".\\flashback-relative-output.mp4"),
            (string)args[1]!,
            "Relative output path is normalized to full path");
        AssertEqual(string.Empty, (string)args[2]!, "Valid output path has no failure message");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RejectsOutputPathThatOverwritesSource()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_paths_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var sourcePath = Path.Combine(tempDir, "fb_source_0001.mp4");
            File.WriteAllBytes(sourcePath, new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 });

            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    sourcePath,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    sourcePath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Single-file export rejects source overwrite");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "must not overwrite source segment");
                AssertEqual(8L, new FileInfo(sourcePath).Length, "Single-file rejection preserves source bytes");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", sourcePath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    sourcePath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Segment export rejects source overwrite");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "must not overwrite source segment");
                AssertEqual(8L, new FileInfo(sourcePath).Length, "Segment rejection preserves source bytes");

                var sourceText = ReadFlashbackExporterSource();
                AssertDoesNotContain(sourceText, "var tmpPath = outputPath + \".tmp\";");
                AssertContains(sourceText, "FlashbackExportOutputTransaction.TryReserve(outputPath, out outputTransaction, out var tempOutputFailure, out var tempOutputFailureCode)");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_CreatesUniqueTempOutputPaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_temp_unique_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? firstTransaction = null;
        object? secondTransaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "export.mp4");
            firstTransaction = ReserveOutputTransaction(outputPath);
            var firstTempPath = GetOutputTransactionTemporaryPath(firstTransaction);
            secondTransaction = ReserveOutputTransaction(outputPath);
            var secondTempPath = GetOutputTransactionTemporaryPath(secondTransaction);

            AssertEqual(true, File.Exists(firstTempPath), "First unique temp file is reserved");
            AssertEqual(true, File.Exists(secondTempPath), "Second unique temp file is reserved");
            AssertEqual(false, string.Equals(firstTempPath, secondTempPath, StringComparison.OrdinalIgnoreCase), "Temp paths are unique per export");
            AssertEqual(false, string.Equals(firstTempPath, outputPath + ".tmp", StringComparison.OrdinalIgnoreCase), "Temp path is not deterministic output-plus-tmp");
            AssertContains(Path.GetFileName(firstTempPath), ".mp4.tmp");
            AssertContains(Path.GetFileName(secondTempPath), ".mp4.tmp");
        }
        finally
        {
            if (firstTransaction is IDisposable firstDisposable)
            {
                firstDisposable.Dispose();
            }

            if (secondTransaction is IDisposable secondDisposable)
            {
                secondDisposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_CleanupOrphanedTempFiles_HandlesNonexistentDirectory()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        cleanup.Invoke(null, new object[] { Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}") });

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_CleanupOrphanedTempFiles_DeletesTempFiles()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cleanup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var orphan1 = Path.Combine(tempDir, "clip_a.mp4.tmp");
            var orphan2 = Path.Combine(tempDir, "clip_b.mp4.tmp");
            var recentTemp = Path.Combine(tempDir, "clip_recent.mp4.tmp");
            var lockedTemp = Path.Combine(tempDir, "clip_locked.mp4.tmp");
            var unrelated = Path.Combine(tempDir, "unrelated.mp4");
            var legacyTemp = Path.Combine(tempDir, "fb_export_temp_001.ts");

            File.WriteAllText(orphan1, "data");
            File.WriteAllText(orphan2, "data");
            File.WriteAllText(recentTemp, "keep");
            File.WriteAllText(lockedTemp, "keep");
            File.WriteAllText(unrelated, "keep");
            File.WriteAllText(legacyTemp, "keep");
            var oldEnough = DateTime.UtcNow - TimeSpan.FromMinutes(30);
            File.SetLastWriteTimeUtc(orphan1, oldEnough);
            File.SetLastWriteTimeUtc(orphan2, oldEnough);
            File.SetLastWriteTimeUtc(lockedTemp, oldEnough);

            using var lockedStream = new FileStream(lockedTemp, FileMode.Open, FileAccess.Read, FileShare.None);

            cleanup.Invoke(null, new object[] { tempDir });

            AssertEqual(false, File.Exists(orphan1), "First mp4 temp deleted");
            AssertEqual(false, File.Exists(orphan2), "Second mp4 temp deleted");
            AssertEqual(true, File.Exists(recentTemp), "Recent mp4 temp preserved");
            AssertEqual(true, File.Exists(lockedTemp), "Locked mp4 temp preserved");
            AssertEqual(true, File.Exists(unrelated), "Unrelated file preserved");
            AssertEqual(true, File.Exists(legacyTemp), "Legacy TS temp preserved by mp4 cleanup");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertDoesNotContain(sourceText, "private static void CleanupOrphanedTempFilesNearOutput(string outputPath)");
        AssertDoesNotContain(sourceText, "FLASHBACK_EXPORT_ORPHAN_OUTPUT_SCAN_FAIL");

        var singleExportBlock = ExtractTextBetween(
            sourceText,
            "private FinalizeResult ExportCore(",
            "    private FinalizeResult ExportSegmentsCore(");
        AssertDoesNotContain(singleExportBlock, "var tmpPath = outputPath + \".tmp\";");
        AssertDoesNotContain(singleExportBlock, "CleanupOrphanedTempFilesNearOutput(outputPath);");

        var segmentExportBlock = ExtractTextBetween(
            sourceText,
            "private FinalizeResult ExportSegmentsCore(",
            "    private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertDoesNotContain(segmentExportBlock, "var tmpPath = outputPath + \".tmp\";");
        AssertDoesNotContain(segmentExportBlock, "CleanupOrphanedTempFilesNearOutput(outputPath);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_EmptyTempDoesNotReplaceExistingOutput()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_finalize_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "existing-export.mp4");
            var existingBytes = new byte[] { 0x65, 0x78, 0x70, 0x6f, 0x72, 0x74 };
            File.WriteAllBytes(outputPath, existingBytes);
            transaction = ReserveOutputTransaction(outputPath);
            var tmpPath = GetOutputTransactionTemporaryPath(transaction);
            ReleaseOutputTransactionReservation(transaction);
            var finalized = PublishOutputTransaction(transaction, outputPath, out _, out var failureMessage, out var failureCode);

            AssertEqual(false, finalized, "Invalid temp output is rejected");
            AssertContains(failureMessage, "temporary output file is empty before replacing");
            AssertEqual("flashback-export-no-media-written", failureCode, "Empty temporary media carries NoMediaWritten");
            AssertEqual(true, File.Exists(outputPath), "Existing export remains present");
            AssertEqual(existingBytes.Length, new FileInfo(outputPath).Length, "Existing export length is preserved");
            AssertEqual(false, File.Exists(tmpPath), "Invalid temp output is deleted");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse()
        => VerifyExporterRefusesExistingDestinationAsync(force: false);

    internal static Task FlashbackExporter_RefusesOverwriteWhenForceTrue()
        => VerifyExporterRefusesExistingDestinationAsync(force: true);

    private static async Task VerifyExporterRefusesExistingDestinationAsync(bool force)
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_force_{force}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? exporter = null;

        try
        {
            var inputPath = Path.Combine(tempDir, "input.ts");
            var outputPath = Path.Combine(tempDir, "existing-take.mp4");
            var existingBytes = new byte[] { 0x6f, 0x6c, 0x64 };
            File.WriteAllBytes(inputPath, new byte[] { 0x47 });
            File.WriteAllBytes(outputPath, existingBytes);

            exporter = Activator.CreateInstance(exporterType)!;
            var request = Activator.CreateInstance(requestType)!;
            SetPropertyBackingField(request, "InputPath", inputPath);
            SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
            SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(1));
            SetPropertyBackingField(request, "OutputPath", outputPath);
            SetPropertyBackingField(request, "FastStart", true);
            SetPropertyBackingField(request, "Force", force);

            var export = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportAsync not found.");
            var task = export.Invoke(exporter, new object?[] { request, null, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("FlashbackExporter.ExportAsync did not return Task.");
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), $"Force={force} refuses an existing destination");
            AssertContains(GetStringProperty(result, "StatusMessage"), "Flashback export does not overwrite existing files");
            AssertEqual("flashback-export-invalid-output-path", GetStringProperty(result, "FailureCode"), "Existing destination carries InvalidOutputPath");
            AssertEqual(
                true,
                existingBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)),
                $"Force={force} preserves destination bytes");
            AssertEqual(0, Directory.EnumerateFiles(tempDir, "*.mp4.tmp").Count(), $"Force={force} creates no temporary output");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    internal static Task FlashbackExportOutputTransaction_PublishesWithLiveReservation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_publish_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            var expectedBytes = new byte[] { 0x66, 0x69, 0x6e, 0x61, 0x6c };
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            WriteTransactionTempWhileReservationIsLive(tempPath, expectedBytes);

            var published = PublishOutputTransaction(transaction, outputPath, out var outputBytes, out var failureMessage, out _);

            AssertEqual(true, published, $"Owned temporary output publishes: {failureMessage}");
            AssertEqual((long)expectedBytes.Length, outputBytes, "Published byte count is reported");
            AssertEqual(
                true,
                expectedBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)),
                "Published bytes match the completed temporary output");
            AssertEqual(false, File.Exists(tempPath), "Temporary path is consumed by publication");

            ((IDisposable)transaction).Dispose();
            AssertEqual(
                true,
                expectedBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)),
                "Disposal never removes a published output");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_ReservationBlocksPathReplacement()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_temp_reservation_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "export.mp4");
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            var replacementPath = tempPath + ".replacement";
            var replacementBlocked = false;

            try
            {
                File.Move(tempPath, replacementPath);
            }
            catch (IOException)
            {
                replacementBlocked = true;
            }
            catch (UnauthorizedAccessException)
            {
                replacementBlocked = true;
            }

            AssertEqual(true, replacementBlocked, "Open reservation blocks replacement before native writer opens by name");
            AssertEqual(true, File.Exists(tempPath), "Reserved path remains present");
            AssertEqual(false, File.Exists(replacementPath), "Replacement path was not created");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportOutputTransaction_RefusesDestinationCreatedAfterReservation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_publish_race_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            var existingBytes = new byte[] { 0x65, 0x78, 0x69, 0x73, 0x74, 0x69, 0x6e, 0x67 };
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            WriteTransactionTempWhileReservationIsLive(tempPath, new byte[] { 0x6e, 0x65, 0x77 });
            File.WriteAllBytes(outputPath, existingBytes);

            var published = PublishOutputTransaction(transaction, outputPath, out _, out var failureMessage, out var failureCode);

            AssertEqual(false, published, "A destination created after reservation is not replaced");
            AssertContains(failureMessage, "destination file already exists");
            AssertEqual("flashback-export-invalid-output-path", failureCode, "Destination race carries InvalidOutputPath");
            AssertEqual(
                true,
                existingBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)),
                "The raced-in destination keeps its bytes");
            AssertEqual(false, File.Exists(tempPath), "The owned temporary output is cleaned after refusal");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_PostMoveValidationFailurePreservesOutput()
    {
        var transactionType = RequireType("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var finalizeCore = transactionType.GetMethod("TryPublishCore", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryPublishCore not found.");
        var validatorType = transactionType.GetNestedType("CompletedOutputValidator", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.CompletedOutputValidator not found.");
        var validatorMethod = typeof(Program).GetMethod(
            nameof(ValidateFinalOutputFailureAfterMove),
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ValidateFinalOutputFailureAfterMove not found.");
        var validator = Delegate.CreateDelegate(validatorType, validatorMethod);

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_final_validate_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            transaction = ReserveOutputTransaction(outputPath);
            var tmpPath = GetOutputTransactionTemporaryPath(transaction);
            ReleaseOutputTransactionReservation(transaction);
            File.WriteAllBytes(tmpPath, new byte[] { 0x66, 0x69, 0x6e, 0x61, 0x6c });

            var args = new object?[] { outputPath, 0L, string.Empty, string.Empty, validator };
            var finalized = (bool)(finalizeCore.Invoke(transaction, args)
                ?? throw new InvalidOperationException("TryPublishCore returned null."));

            AssertEqual(false, finalized, "Final validation failure is rejected");
            AssertContains((string)args[2]!, "forced final validation failure");
            AssertEqual("flashback-export-output-write-failed", (string)args[3]!, "Post-move validation preserves explicit failure identity");
            AssertEqual(false, File.Exists(tmpPath), "Temporary output was moved before final validation");
            AssertEqual(true, File.Exists(outputPath), "Invalid moved final output is not deleted by path");
            AssertEqual(5L, new FileInfo(outputPath).Length, "Moved output bytes remain for caller/operator inspection");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_ReplacedTempPathIsNeverPublishedOrDeleted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_replaced_temp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            ReleaseOutputTransactionReservation(transaction);
            File.WriteAllBytes(tempPath, new byte[] { 0x6f, 0x77, 0x6e, 0x65, 0x64 });

            var originalPath = tempPath + ".original";
            File.Move(tempPath, originalPath);
            var replacementBytes = new byte[] { 0x6e, 0x6f, 0x74, 0x2d, 0x6f, 0x75, 0x72, 0x73 };
            File.WriteAllBytes(tempPath, replacementBytes);

            var published = PublishOutputTransaction(transaction, outputPath, out _, out var failureMessage, out var failureCode);

            AssertEqual(false, published, "Replaced temp path is not published");
            AssertContains(failureMessage, "temporary output path was replaced");
            AssertEqual(false, File.Exists(outputPath), "Replacement is not published as final output");
            AssertEqual(5L, new FileInfo(originalPath).Length, "Original reserved temp remains outside the replacement path");
            AssertEqual((long)replacementBytes.Length, new FileInfo(tempPath).Length, "Replacement temp remains intact after failed publication");

            ((IDisposable)transaction).Dispose();
            AssertEqual((long)replacementBytes.Length, new FileInfo(tempPath).Length, "Disposal does not delete a replacement temp path");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_AbandonDeletesOnlyItsUncommittedTemp()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_abandon_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            ReleaseOutputTransactionReservation(transaction);
            File.WriteAllBytes(tempPath, new byte[] { 0x70, 0x61, 0x72, 0x74, 0x69, 0x61, 0x6c });

            ((IDisposable)transaction).Dispose();

            AssertEqual(false, File.Exists(tempPath), "Uncommitted transaction temp is deleted on abandonment");
            AssertEqual(false, File.Exists(outputPath), "Abandonment never creates a final output");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static object ReserveOutputTransaction(string outputPath)
    {
        var transactionType = RequireType("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var reserve = transactionType.GetMethod("TryReserve", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryReserve not found.");
        var args = new object?[] { outputPath, null, string.Empty, string.Empty };
        var reserved = (bool)(reserve.Invoke(null, args)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryReserve returned null."));
        AssertEqual(true, reserved, "Temporary output transaction is reserved");
        return args[1] ?? throw new InvalidOperationException("Temporary output transaction was not returned.");
    }

    private static string GetOutputTransactionTemporaryPath(object transaction)
    {
        var property = transaction.GetType().GetProperty(
            "TemporaryPath",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TemporaryPath not found.");
        return property.GetValue(transaction)?.ToString()
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TemporaryPath was null.");
    }

    private static void ReleaseOutputTransactionReservation(object transaction)
    {
        var release = transaction.GetType().GetMethod("ReleaseReservation", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.ReleaseReservation not found.");
        release.Invoke(transaction, null);
    }

    private static void WriteTransactionTempWhileReservationIsLive(string tempPath, byte[] bytes)
    {
        using var writer = new FileStream(
            tempPath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.ReadWrite);
        writer.SetLength(0);
        writer.Write(bytes, 0, bytes.Length);
        writer.Flush(flushToDisk: true);
    }

    private static bool PublishOutputTransaction(
        object transaction,
        string outputPath,
        out long outputBytes,
        out string failureMessage,
        out string failureCode)
    {
        var publish = transaction.GetType().GetMethod("TryPublish", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryPublish not found.");
        var args = new object?[] { outputPath, 0L, string.Empty, string.Empty };
        var published = (bool)(publish.Invoke(transaction, args)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryPublish returned null."));
        outputBytes = (long)args[1]!;
        failureMessage = (string)args[2]!;
        failureCode = (string)args[3]!;
        return published;
    }

    private static Task FlashbackPlaybackController_PublicPlaybackState_LivesInRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var playbackStateText = rootText;

        AssertContains(playbackStateText, "private volatile FlashbackPlaybackState _state = FlashbackPlaybackState.Live;");
        AssertContains(playbackStateText, "private long _playbackPositionTicks;");
        AssertContains(playbackStateText, "private volatile string _decoderHwAccel = \"N/A\";");
        AssertContains(playbackStateText, "private long _lastAudioPtsTicks;");
        AssertContains(playbackStateText, "private long _lastVideoPtsTicks;");
        AssertContains(playbackStateText, "private bool _wasPlayingBeforeScrub;");
        AssertContains(playbackStateText, "public bool GpuDecodeEnabled { get; set; } = true;");
        AssertContains(playbackStateText, "public FlashbackPlaybackState State => _state;");
        AssertContains(playbackStateText, "public TimeSpan PlaybackPosition");
        AssertContains(playbackStateText, "public TimeSpan GapFromLive");
        AssertContains(playbackStateText, "public bool IsInitialized => _initialized;");
        AssertContains(playbackStateText, "public bool IsDisposed => _disposedFlag != 0;");
        AssertContains(playbackStateText, "public string DecoderHwAccel => _decoderHwAccel;");
        AssertContains(playbackStateText, "private void SetState(FlashbackPlaybackState newState, string reason = \"\")");
        AssertContains(rootText, "private readonly FlashbackBufferManager _bufferManager;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InitialState_IsLive()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");

        var controller = ctor.Invoke(new[] { bufferManager });

        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "Initial state is Live");

        var position = (TimeSpan)GetPropertyValue(controller, "PlaybackPosition")!;
        AssertEqual(TimeSpan.Zero, position, "Initial PlaybackPosition");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CommandsNoOpBeforeInitialize()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)!;
        var controller = ctor.Invoke(new[] { bufferManager });

        var playMethod = controllerType.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance);
        var pauseMethod = controllerType.GetMethod("Pause", BindingFlags.Public | BindingFlags.Instance);
        var goLiveMethod = controllerType.GetMethod("GoLive", BindingFlags.Public | BindingFlags.Instance);

        playMethod?.Invoke(controller, null);
        pauseMethod?.Invoke(controller, null);
        goLiveMethod?.Invoke(controller, null);

        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "State unchanged after no-op commands");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        SetPrivateField(controller, "_initialized", true);

        try
        {
            SeedCommandFailure(controller, "old_failure:EndScrub");
            AssertEqual(false, (bool)controllerType.GetMethod("EndScrub")!.Invoke(controller, null)!, "EndScrub live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "EndScrub no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "EndScrub no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:GoLive");
            AssertEqual(false, (bool)controllerType.GetMethod("GoLive")!.Invoke(controller, null)!, "GoLive live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "GoLive no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "GoLive no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:Nudge");
            AssertEqual(false, (bool)controllerType.GetMethod("NudgePosition")!.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(8.33) })!, "Nudge live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Nudge no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Nudge no-op clears stale failure UTC");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");

        try
        {
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(1) })!, "Initial seek enqueues");
            var initialSeekQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:Seek");
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(2) })!, "Coalesced seek succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced seek clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced seek clears stale failure UTC");
            AssertEqual("Seek", GetStringProperty(controller, "LastCommandQueued"), "Coalesced seek keeps physical queued-command name");
            AssertEqual(initialSeekQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced seek does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "SeekCommandsCoalesced"), "Coalesced seek counter");

            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(3) })!, "Initial scrub update enqueues");
            var initialScrubQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:UpdateScrub");
            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(4) })!, "Coalesced scrub update succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced scrub update clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced scrub update clears stale failure UTC");
            AssertEqual("UpdateScrub", GetStringProperty(controller, "LastCommandQueued"), "Coalesced scrub update keeps physical queued-command name");
            AssertEqual(initialScrubQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced scrub update does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "ScrubUpdatesCoalesced"), "Coalesced scrub update counter");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        // All three scrub-related command paths must clamp via the eviction-aware
        // overload so a long-held scrub doesn't resolve to evicted file PTS.
        const string seekClampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n        var seekResumeTarget = ClampPlaybackTargetToMinimumLiveLead(";
        const string scrubClampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n        decoder ??= CreateDecoder();\n        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));";

        AssertContains(sourceText, seekClampBeforeOpen);
        AssertContains(sourceText, "SaturatingAdd(cmd.Position, frozenValidStart),\n            frozenValidStart,\n            \"seek\");");
        AssertContains(sourceText, "cmd = cmd with { Position = ClampPosition(SaturatingSubtract(seekResumeTarget, frozenValidStart), frozenValidStart) };");
        AssertContains(sourceText, "if (_commandMailbox.ShouldYieldSeekToQueuedPlay(commandChannel))\n        {\n            PlaybackPosition = cmd.Position;\n            pendingExactResumeTarget = seekResumeTarget;");
        AssertContains(sourceText, "decoder ??= CreateDecoder();\n        EnsureFileOpen(decoder, ref fileOpen, seekResumeTarget);");
        AssertEqual(1, sourceText.Split(scrubClampBeforeOpen, StringSplitOptions.None).Length - 1, "BeginScrub clamps before file lookup with frozen reference");
        var updateScrubBlock = ExtractTextBetween(
            sourceText,
            "private void HandleUpdateScrubCommand(",
            "    private void HandleEndScrubCommand(");
        AssertContains(updateScrubBlock, "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n        if (_commandMailbox.ShouldYieldScrubUpdateToQueuedControl(commandChannel))");
        AssertContains(updateScrubBlock, "decoder ??= CreateDecoder();\n        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_TimestampArithmeticIsSaturating()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();

        AssertContains(sourceText, "private static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "if (rightTicks > 0 && leftTicks > long.MaxValue - rightTicks)");
        AssertContains(sourceText, "if (rightTicks < 0 && leftTicks < long.MinValue - rightTicks)");
        AssertContains(sourceText, "if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)");
        AssertContains(sourceText, "if (rightTicks > 0 && leftTicks < long.MinValue + rightTicks)");
        AssertDoesNotContain(sourceText, "cmd.Position + frozenValidStart");
        AssertDoesNotContain(sourceText, "PlaybackPosition + frozenValidStart");
        AssertDoesNotContain(sourceText, "PlaybackPosition + cmd.Delta");
        AssertDoesNotContain(sourceText, "bufferPosition + validStartPts");
        AssertDoesNotContain(sourceText, "pos + frozenValidStart");
        AssertDoesNotContain(sourceText, "nudgeFrame.Pts - frozenValidStart");
        AssertDoesNotContain(sourceText, "frame.Pts - validStartPts");
        AssertDoesNotContain(sourceText, "videoFrame.Pts - frozenValidStart");
        AssertDoesNotContain(sourceText, "latestAbsPts - lastFrameAbsPts");
        AssertDoesNotContain(sourceText, "absoluteLatestPts - absoluteFramePts");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var segmentSwitchText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "return HandleEndOfSegment(decoder, commandChannel, pacingStopwatch, frozenValidStart, ref fileOpen, cancellationToken);");
        AssertContains(sourceText, "TimeSpan frozenValidStart,\n        ref bool fileOpen,\n        CancellationToken cancellationToken)");
        AssertContains(sourceText, "if (cancellationToken.WaitHandle.WaitOne(50))\n        {\n            return false;\n        }");
        AssertContains(sourceText, "TrySwitchToNextSegment(");
        AssertContains(segmentSwitchText, "Logger.Log($\"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR path='{nextFile}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            SnapToLiveOnError(decoder, ex, ref fileOpen);\n            return true;");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FMP4_REOPEN_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            SnapToLiveOnError(decoder, ex, ref fileOpen);\n            return false;");
        AssertContains(segmentSwitchText, "if (nextFile == null || IsSamePlaybackPath(nextFile, currentOpenFilePath))");
        AssertContains(segmentSwitchText, "_currentOpenFilePath = nextFile;\n            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? \"D3D11VA\" : \"Software\";");
        AssertContains(sourceText, "ReopenDecoderPlaybackFile(\n                decoder,\n                currentOpenFilePath,\n                ref fileOpen,\n                updateCurrentOpenPath: false,\n                closeOnlyWhenOpen: false);");
        AssertContains(sourceText, "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)");
        AssertOccursBefore(
            sourceText,
            "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)",
            "if (gapFromLive > 2000)");
        AssertOccursBefore(
            sourceText,
            "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)",
            "FLASHBACK_PLAYBACK_WRITE_HEAD_WAIT");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_NormalPlaybackUsesTightNearLiveSnap()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var playbackLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackSoftwareBudgetText = playbackTimingText;

        AssertContains(sourceText, "private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;");
        AssertContains(sourceText, "private static readonly TimeSpan ContinuousPlaybackNearLiveSnapMinimum = TimeSpan.FromMilliseconds(100);");
        AssertContains(sourceText, "private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);");
        AssertContains(playbackTimingText, "private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;");
        AssertContains(playbackTimingText, "private static readonly TimeSpan ContinuousPlaybackNearLiveSnapMinimum = TimeSpan.FromMilliseconds(100);");
        AssertContains(playbackLoopText, "private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);");
        AssertContains(playbackLoopText, "private bool CheckNearLiveEdge(");
        AssertContains(playbackSoftwareBudgetText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertDoesNotContain(rootText, "private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;");
        AssertDoesNotContain(rootText, "private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);");
        AssertContains(sourceText, "CheckNearLiveEdge(decoder, videoFrame.Pts, newPosition, ref fileOpen)");
        AssertContains(sourceText, "var snapThreshold = requireFrameWarmup\n            ? ResolveContinuousPlaybackNearLiveSnapThreshold()\n            : RecoveryNearLiveSnapThreshold;");
        AssertContains(sourceText, "gapFromLive <= snapThreshold");
        AssertContains(sourceText, "private TimeSpan ResolveContinuousPlaybackNearLiveSnapThreshold()");
        AssertContains(sourceText, "ContinuousPlaybackNearLiveSnapFrames / Math.Min(fps, MaxPlaybackFrameRate)");
        AssertContains(sourceText, "threshold_ms={(long)snapThreshold.TotalMilliseconds}");
        AssertDoesNotContain(sourceText, "gapFromLive <= TimeSpan.FromMilliseconds(2000)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var playbackFramesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");

        var nearLiveBlock = ExtractTextBetween(
            sourceText,
            "Logger.Log($\"FLASHBACK_PLAYBACK_NEAR_LIVE_SNAP",
            "return true;");
        AssertContains(nearLiveBlock, "RestoreLiveAfterNearLiveSnap(decoder, ref fileOpen);");

        var decodeErrorBlock = ExtractTextBetween(
            sourceText,
            "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK",
            "    private bool CheckNearLiveEdge(");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_SNAP_TO_LIVE type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(sourceText, "SetLastCommandFailure($\"decode_error:{ex.GetType().Name}{FormatCommandDetail(position: pos)}\");");
        AssertContains(playbackFramesText, "private void SnapToLiveOnError(");
        AssertContains(playbackFramesText, "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_SEEK_ERROR type={ex.GetType().Name} error='{ex.Message}'\");");
        AssertContains(decodeErrorBlock, "RestoreLiveAfterPlaybackDecodeError(decoder, ref fileOpen);");
        AssertContains(playbackFramesText, "private void RestoreLiveAfterPlaybackDecodeError(FlashbackDecoder decoder, ref bool fileOpen)\n        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, \"decode_error\", resumeRendering: false);");
        AssertContains(playbackFramesText, "private void RestoreLiveAfterNearLiveSnap(FlashbackDecoder decoder, ref bool fileOpen)\n        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, \"near_live\", resumeRendering: false);");
        AssertContains(playbackFramesText, "CloseDecoderFileBestEffort(decoder, operation);\n        fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(playbackFramesText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();");
        AssertContains(playbackFramesText, "SafeResumePreviewSubmission(operation);");
        AssertContains(playbackFramesText, "SetState(FlashbackPlaybackState.Live, operation);");
        AssertContains(sourceText, "private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)\n    {\n        try\n        {\n            if (decoder.IsOpen) decoder.CloseFile();\n        }\n        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_PLAYBACK_DECODER_CLOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'\");\n        }\n    }");
        var ensureFileOpenBlock = ExtractTextBetween(
            sourceText,
            "private void EnsureFileOpen",
            "private void CleanupDecoder");
        AssertContains(ensureFileOpenBlock, "CloseDecoderFileBestEffort(decoder, \"ensure_file_open\");\n                fileOpen = false;\n                _currentOpenFilePath = null;\n                _decoderHwAccel = \"N/A\";");
        AssertContains(ensureFileOpenBlock, "if (string.IsNullOrWhiteSpace(filePath))\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_NO_FILE\");\n            if (decoder.IsOpen)\n            {\n                CloseDecoderFileBestEffort(decoder, \"ensure_file_open_no_file\");\n            }\n\n            fileOpen = false;\n            _currentOpenFilePath = null;\n            _decoderHwAccel = \"N/A\";\n            return;\n        }");
        AssertContains(ensureFileOpenBlock, "Logger.Log($\"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'\");\n            if (decoder.IsOpen)\n            {\n                CloseDecoderFileBestEffort(decoder, \"ensure_file_open_error\");\n            }\n            _decoderHwAccel = \"N/A\";\n            fileOpen = false;");
        AssertContains(ensureFileOpenBlock, "private static bool IsDecoderFileReady(FlashbackDecoder decoder, bool fileOpen)\n        => fileOpen && decoder.IsOpen;");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(PlaybackPosition, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertEqual(6, sourceText.Split("if (!IsDecoderFileReady(decoder, fileOpen))", StringSplitOptions.None).Length - 1, "All EnsureFileOpen callers gate on fileOpen and decoder.IsOpen");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var publicPauseBlock = ExtractTextBetween(
            sourceText,
            "public bool Pause()",
            "    public bool GoLive()");

        var pauseFromLiveBlock = ExtractTextBetween(
            sourceText,
            "else if (State == FlashbackPlaybackState.Live)",
            "    private void HandleNudgeCommand(");

        AssertContains(publicPauseBlock, "return SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });");
        AssertDoesNotContain(publicPauseBlock, "SeekAndDisplay");
        AssertContains(pauseFromLiveBlock, "SafeSuppressPreviewSubmission(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "SafePauseRendering(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "var pauseTarget = ResolvePauseFromLiveTarget(frozenValidStart);");
        AssertContains(pauseFromLiveBlock, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(pausePos, frozenValidStart));");
        AssertContains(pauseFromLiveBlock, "if (!IsDecoderFileReady(decoder, fileOpen))");
        AssertContains(pauseFromLiveBlock, "SetNoFileFailure(CommandKind.Pause, pausePos);");
        AssertContains(pauseFromLiveBlock, "if (_commandMailbox.ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))");
        AssertContains(pauseFromLiveBlock, "FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY");
        AssertContains(pauseFromLiveBlock, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, pausePos, frozenValidStart, CommandKind.Pause, cts.Token))");
        AssertContains(pauseFromLiveBlock, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"pause_from_live_display_failed\");");
        AssertContains(pauseFromLiveBlock, "pendingExactResumeTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(pauseFromLiveBlock, "SetState(FlashbackPlaybackState.Paused, \"user\");");
        AssertContains(pauseFromLiveBlock, "frozen_frame=true");
        AssertContains(sourceText, "private TimeSpan ResolvePauseFromLiveTarget(TimeSpan frozenValidStart)");
        AssertContains(sourceText, "return ClampPlaybackTargetToMinimumLiveLead(latestPts, frozenValidStart, \"pause_from_live\");");
        AssertDoesNotContain(pauseFromLiveBlock, "SeekAndDisplayExactFrame");
        AssertDoesNotContain(sourceText, "private void SeekAndDisplayExactFrame");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();

        var nudgeBlock = ExtractTextBetween(
            sourceText,
            "private void HandleNudgeCommand(",
            "\n}");

        AssertContains(nudgeBlock, "decoder ??= CreateDecoder();");
        AssertContains(nudgeBlock, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));");
        AssertContains(nudgeBlock, "if (!IsDecoderFileReady(decoder, fileOpen))");
        AssertContains(nudgeBlock, "FLASHBACK_PLAYBACK_NUDGE_NO_FILE");
        AssertContains(nudgeBlock, "ReleasePlaybackFrameForLive(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "RestoreLiveAudio();");
        AssertContains(nudgeBlock, "SafeResumePreviewSubmission(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "SafeResumeRendering(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "SetState(FlashbackPlaybackState.Live, \"nudge_no_file\");");
        AssertContains(nudgeBlock, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token))");
        AssertContains(nudgeBlock, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"nudge_display_failed\");");
        AssertDoesNotContain(nudgeBlock, "if (decoder != null)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var metricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var audioRoutingText = rootText;
        var audioCallbackText = audioRoutingText;
        var audioPreviewGuardsText = audioRoutingText;
        var audioPrebufferText = audioRoutingText;
        var audioMasterText = audioRoutingText;
        var audioMasterFallbacksText = audioMasterText;
        var resumePlaybackRenderingBlock = ExtractTextBetween(
            audioRoutingText,
            "private void SafeResumePlaybackRendering(string operation)",
            "\n    private void SafeFlushPlayback(string operation)");
        var playbackTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackSoftwareBudgetText = playbackTimingText;
        var playbackSegmentSwitchText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var audioMasterClockText = audioMasterText;
        var wasapiPlaybackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs")
            .Replace("\r\n", "\n");
        var wasapiPlaybackRenderText = wasapiPlaybackText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.AudioMasterPacing.cs")),
            "audio-master pacing folded into FlashbackPlaybackController.cs");

        AssertContains(sourceText, "private void SafeSuppressPreviewSubmission(string operation)");
        AssertContains(sourceText, "private void SafeResumePreviewSubmission(string operation)");
        AssertContains(sourceText, "private void SafePauseRendering(string operation)");
        AssertContains(sourceText, "private void SafeResumeRendering(string operation)");
        AssertContains(sourceText, "private void SafeResumePlaybackRendering(string operation)");
        AssertContains(sourceText, "private void SafeFlushPlayback(string operation)");
        AssertContains(resumePlaybackRenderingBlock, "audioPlayback?.ResumeRendering(\n                PlaybackAudioPrebufferTargetMs,\n                PlaybackAudioPrebufferTimeoutMs);");
        AssertDoesNotContain(resumePlaybackRenderingBlock, "_audioPlayback?.ResumeRendering();");
        AssertContains(audioPreviewGuardsText, "private void SafeSuppressPreviewSubmission(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeResumePreviewSubmission(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafePauseRendering(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeResumeRendering(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeResumePlaybackRendering(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeFlushPlayback(string operation)");
        AssertContains(sourceText, "private const double PlaybackAudioPrebufferTargetMs = 180.0;");
        AssertContains(sourceText, "private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferTimeoutMs = 1000;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferRetryDelayMs = 20;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;");
        AssertContains(sourceText, "private const int AudioRenderStateTransitionTimeoutMs = 100;");
        AssertContains(sourceText, "private static readonly TimeSpan MinimumPlaybackLiveLead =");
        // The minimum live lead must stay strictly greater than
        // ActiveFmp4ReopenNearLiveGuard (250ms) so clamped targets keep the
        // active-fMP4 reopen recovery path available.
        AssertContains(sourceText, "TimeSpan.FromMilliseconds(Math.Max(300.0, PlaybackAudioPrebufferTargetMs));");
        AssertContains(sourceText, "private TimeSpan ClampPlaybackTargetToMinimumLiveLead(");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_LIVE_LEAD_CLAMP operation={operation}");
        AssertContains(audioPrebufferText, "private const double PlaybackAudioPrebufferTargetMs = 180.0;");
        AssertContains(audioPrebufferText, "private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;");
        AssertContains(audioPrebufferText, "private const int PlaybackAudioPrebufferTimeoutMs = 1000;");
        AssertContains(audioPrebufferText, "private const int PlaybackAudioPrebufferRetryDelayMs = 20;");
        AssertContains(audioPrebufferText, "private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;");
        AssertContains(sourceText, "var prebufferedFrames = new Queue<DecodedVideoFrame>();");
        AssertContains(sourceText, "ClearPrebufferedFrames(prebufferedFrames, $\"command_{cmd.Kind}\");");
        AssertContains(sourceText, "private void PrimePlaybackAudioBuffer(");
        AssertContains(sourceText, "ChannelReader<PlaybackCommand> commandChannel,");
        AssertContains(sourceText, "TimeSpan resumeTarget,");
        AssertContains(sourceText, "while (decodedFrames < PlaybackAudioPrebufferDecodeFrameBudget)");
        AssertContains(sourceText, "if (commandChannel.TryPeek(out var pendingCommand))");
        AssertContains(sourceText, "command_pending={commandPending} pending_command={pendingCommandKind}");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"audio_prebuffer_{operation}\");");
        AssertContains(sourceText, "released_frames={prebufferReleasedFrames}");
        // Borrowed CPU data cannot remain queued across repeated decode calls.
        // Priming beyond one frame must use the bounded rewind path.
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferMaxHeldFrames = 1;");
        AssertContains(sourceText, "prebufferedFrames.Count < PlaybackAudioPrebufferMaxHeldFrames)");
        AssertContains(sourceText, "prebufferedFrames.Enqueue(frame);");
        AssertContains(sourceText, "ClearPrebufferedFrames(prebufferedFrames, $\"prebuffer_cap_{operation}\");");
        AssertContains(sourceText, "if (releasedAnyFrame && decodedFrames > 0)");
        AssertContains(sourceText, "cancellationToken.WaitHandle.WaitOne(waitMs)");
        AssertContains(sourceText, "bufferedMs > PlaybackAudioPrebufferDiscardThresholdMs");
        AssertContains(sourceText, "prebufferAudioGateTicks = Interlocked.Read(ref _lastAudioPtsTicks);");
        AssertContains(sourceText, "prebufferAudioGateTicks = 0;\n            discarded = true;");
        AssertContains(sourceText, "rewound = TryRewindPlaybackAudioPrebuffer(decoder, ref fileOpen, resumeTarget, operation, prebufferAudioGateTicks, cancellationToken);");
        AssertContains(sourceText, "private bool TryRewindPlaybackAudioPrebuffer(");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, resumeTarget, $\"prebuffer_discard_{operation}\", cancellationToken)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_REWIND operation={operation}");
        AssertContains(sourceText, "audio_gate_ms={(long)TimeSpan.FromTicks(Math.Max(0, prebufferAudioGateTicks)).TotalMilliseconds}");
        AssertContains(sourceText, "ClearPrebufferedFrames(prebufferedFrames, $\"prebuffer_discard_{operation}\");");
        AssertContains(sourceText, "eof_retries={eofRetries}");
        AssertContains(sourceText, "rewound={rewound}");
        AssertDoesNotContain(sourceText, "if ((reachedEnd && decodedFrames > 0) ||");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_PREBUFFER operation={operation}");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"seek_resume\")");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"end_scrub_resume\")");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"play\")");
        AssertContains(sourceText, "ApplyAudioRoutingForState(\"audio_update\");");
        AssertContains(sourceText, "private void ApplyAudioRoutingForState(string operation)");
        AssertContains(sourceText, "case FlashbackPlaybackState.Live:\n                RestoreLiveAudio();");
        AssertContains(sourceText, "case FlashbackPlaybackState.Playing:\n                SuppressLiveAudio();\n                SafeResumeRendering(operation);");
        AssertContains(sourceText, "case FlashbackPlaybackState.Paused:\n            case FlashbackPlaybackState.Scrubbing:\n                SuppressLiveAudio();\n                SafePauseRendering(operation);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_WARN");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_RENDER_STATE_TIMEOUT");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_WARN op=suppress operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN op=pause operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN op=flush operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "WaitForRenderingPaused(AudioRenderStateTransitionTimeoutMs)");
        AssertContains(sourceText, "WaitForRenderingRunning(AudioRenderStateTransitionTimeoutMs)");
        AssertContains(sourceText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"scrub_no_file\")");
        AssertContains(sourceText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"go_live\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(operation);");
        AssertContains(sourceText, "RestoreLiveAfterPlaybackDecodeError(decoder, ref fileOpen);");
        AssertContains(sourceText, "SafeFlushPlayback(\"restore_live_audio\")");
        AssertContains(sourceText, "SafeResumeRendering(\"play_no_file\")");
        AssertContains(sourceText, "SafeResumeRendering(\"nudge_no_file\")");
        AssertContains(sourceText, "if (_audioPlayback == null)\n        {\n            decoder.AudioChunkCallback = null;\n            return;\n        }");
        AssertContains(sourceText, "if (!TryValidatePlaybackAudioChunk(chunk, out var invalidReason))");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_DROP reason={invalidReason}");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, $\"playback_audio_{invalidReason}\");");
        AssertContains(sourceText, "private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)");
        AssertContains(audioCallbackText, "private void RestoreAudioCallback(\n        FlashbackDecoder decoder,\n        long audioStartGateTicks = 0,\n        long lastAcceptedAudioPtsTicks = 0)");
        AssertContains(audioCallbackText, "var normalizedLastAcceptedAudioPtsTicks = Math.Max(0, lastAcceptedAudioPtsTicks);");
        AssertContains(audioCallbackText, "Interlocked.Exchange(ref _lastAudioPtsTicks, normalizedLastAcceptedAudioPtsTicks);");
        AssertContains(audioCallbackText, "private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)");
        AssertContains(audioCallbackText, "private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)");
        AssertContains(audioRoutingText, "private void RestoreAudioCallback(\n        FlashbackDecoder decoder,\n        long audioStartGateTicks = 0,\n        long lastAcceptedAudioPtsTicks = 0)");
        AssertContains(audioRoutingText, "private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)");
        AssertContains(audioRoutingText, "private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)");
        AssertContains(sourceText, "reason = \"length_exceeds_buffer\";");
        AssertContains(sourceText, "reason = \"unaligned_length\";");
        AssertContains(sourceText, "private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_RETURN_WARN");
        AssertContains(sourceText, "chunk.Pts.Ticks <= prevPts");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, \"playback_audio_non_monotonic_pts\");");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, \"playback_audio_before_gate\");");
        AssertContains(sourceText, "pb.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);");
        AssertContains(sourceText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertContains(playbackSoftwareBudgetText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertDoesNotContain(rootText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertContains(playbackSoftwareBudgetText, "private bool TrySnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackSoftwareBudgetText, "private bool ShouldSnapLiveForSoftwarePlaybackBudget(");
        AssertContains(playbackSoftwareBudgetText, "private void SnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackSoftwareBudgetText, "private void UpdateDecoderHwAccel(FlashbackDecoder decoder)");
        AssertContains(sourceText, "private bool TrySnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "private bool ShouldSnapLiveForSoftwarePlaybackBudget(");
        AssertContains(sourceText, "GpuDecodeEnabled &&\n               !decoder.IsD3D11HwAccelerated &&\n               pixelRate > MaxContinuousSoftwarePlaybackPixelRate");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SOFTWARE_DECODE_SNAP_TO_LIVE");
        AssertContains(sourceText, "SetLastCommandFailure($\"software_decode_over_budget:{operation}{FormatCommandDetail(position: pos)}\");");
        AssertContains(sourceText, "RestoreLiveAfterSoftwarePlaybackBudgetSnap(decoder, ref fileOpen, operation);");
        AssertContains(sourceText, "TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, \"play\")");
        AssertContains(sourceText, "SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, \"playback_decode\");");
        AssertContains(sourceText, "private void UpdateDecoderHwAccel(FlashbackDecoder decoder)");
        AssertContains(sourceText, "const double syncThresholdMs = 40.0;");
        AssertContains(sourceText, "const double MaxAudioMasterCorrectionMs = 500.0;");
        AssertContains(sourceText, "const double AudioMasterCorrectionGain = 0.10;");
        AssertContains(sourceText, "const double MaxAudioMasterCorrectionFrameRatio = 0.25;");
        AssertContains(sourceText, "private string _pendingAudioMasterFallbackReason = string.Empty;");
        AssertContains(audioMasterClockText, "private long _audioClockPtsTicks;");
        AssertContains(audioMasterClockText, "private long _audioClockWallTicks;");
        AssertContains(audioMasterClockText, "private const long AudioMasterClockStaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;");
        AssertContains(audioMasterClockText, "private void RefreshAudioMasterClock()");
        AssertContains(audioMasterClockText, "private bool TryGetFreshAudioMasterClock(");
        AssertContains(audioMasterClockText, "private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)");
        AssertContains(audioMasterClockText, "public double AvDriftMs");
        AssertContains(audioMasterClockText, "var renderingPts = _audioPlayback?.RenderingPtsTicks ?? 0;");
        AssertContains(audioMasterClockText, "return TimeSpan.FromTicks(renderingPts - videoPts).TotalMilliseconds;");
        AssertContains(audioMasterText, "public long PlaybackAudioMasterDelayDoubles => Interlocked.Read(ref _playbackAudioMasterDelayDoubles);");
        AssertContains(audioMasterText, "public long PlaybackAudioMasterDelayShrinks => Interlocked.Read(ref _playbackAudioMasterDelayShrinks);");
        AssertContains(audioMasterFallbacksText, "private long _playbackAudioMasterFallbacks;");
        AssertContains(audioMasterFallbacksText, "private long _playbackAudioMasterUnavailableFallbacks;");
        AssertContains(audioMasterFallbacksText, "private long _playbackAudioMasterStaleFallbacks;");
        AssertContains(audioMasterFallbacksText, "private long _playbackAudioMasterDriftOutlierFallbacks;");
        AssertContains(audioMasterFallbacksText, "private string _playbackAudioMasterLastFallbackReason = string.Empty;");
        AssertContains(audioMasterFallbacksText, "private string _pendingAudioMasterFallbackReason = string.Empty;");
        AssertContains(audioMasterFallbacksText, "public long PlaybackAudioMasterFallbacks => Interlocked.Read(ref _playbackAudioMasterFallbacks);");
        AssertContains(audioMasterFallbacksText, "public long PlaybackAudioMasterUnavailableFallbacks => Interlocked.Read(ref _playbackAudioMasterUnavailableFallbacks);");
        AssertContains(audioMasterFallbacksText, "public string PlaybackAudioMasterLastFallbackReason => Volatile.Read(ref _playbackAudioMasterLastFallbackReason);");
        AssertContains(audioMasterFallbacksText, "public double PlaybackAudioMasterLastFallbackClockAgeMs => _playbackAudioMasterLastFallbackClockAgeMs;");
        AssertContains(audioMasterFallbacksText, "private void RecordAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)");
        AssertContains(audioMasterFallbacksText, "private static bool IsTransientAudioMasterFallbackCandidate(string reason)");
        AssertContains(audioMasterFallbacksText, "private void CommitPendingAudioMasterFallback()");
        AssertContains(audioMasterFallbacksText, "private void CommitAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)");
        AssertContains(sourceText, "private static bool IsTransientAudioMasterFallbackCandidate(string reason)");
        AssertContains(sourceText, "string.Equals(reason, \"unavailable\", StringComparison.Ordinal)");
        AssertContains(sourceText, "string.Equals(reason, \"stale-clock\", StringComparison.Ordinal)");
        AssertContains(sourceText, "string.Equals(reason, \"drift-outlier\", StringComparison.Ordinal)");
        AssertContains(sourceText, "ClearPendingAudioMasterFallback();");
        AssertContains(sourceText, "CommitPendingAudioMasterFallback();");
        AssertContains(sourceText, "CommitAudioMasterFallback(");
        AssertContains(sourceText, "if (Math.Abs(diffMs) > MaxAudioMasterCorrectionMs)\n            {\n                // WASAPI render PTS can lag decoded video by the endpoint buffer/device");
        AssertContains(sourceText, "WallClockPace(pacingStopwatch, frameDuration);\n                return;");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, commandChannel, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"seek_resume\");");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, commandChannel, ref fileOpen, endScrubTarget, \"end_scrub_resume\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"end_scrub_resume\");");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, commandChannel, ref fileOpen, seekTarget, \"play\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"play\");");
        AssertContains(sourceText, "private void ResetPlaybackPtsCadenceBaseline()");
        AssertContains(playbackSegmentSwitchText, "ResetPlaybackPtsCadenceBaseline();\n            pacingStopwatch.Restart();\n            playbackContinues = true;\n            return true;");
        AssertContains(sourceText, "if (string.IsNullOrEmpty(_pendingAudioMasterFallbackReason))");
        AssertContains(sourceText, "_pendingAudioMasterFallbackReason = reason;");
        AssertContains(sourceText, "CommitAudioMasterFallback(");
        AssertContains(sourceText, "_pendingAudioMasterFallbackReason,");
        AssertContains(sourceText, "(diffMs - syncThresholdMs) * AudioMasterCorrectionGain");
        AssertContains(sourceText, "adjustedDelayMs = nominalDelayMs + Math.Max(0, correctionMs);");
        AssertContains(sourceText, "(-diffMs - syncThresholdMs) * AudioMasterCorrectionGain");
        AssertContains(sourceText, "adjustedDelayMs = Math.Max(0, nominalDelayMs - Math.Max(0, correctionMs));");
        AssertDoesNotContain(sourceText, "adjustedDelayMs = nominalDelayMs * 2;");
        AssertDoesNotContain(sourceText, "adjustedDelayMs = Math.Max(0, nominalDelayMs + diffMs);");
        AssertContains(wasapiPlaybackText, "private readonly ManualResetEventSlim _renderPausedAcknowledged = new(false);");
        AssertContains(wasapiPlaybackText, "private readonly ManualResetEventSlim _renderRunningAcknowledged = new(true);");
        AssertContains(wasapiPlaybackText, "if (Volatile.Read(ref _renderingPaused) != 0 && !_resumeRequested)");
        AssertContains(wasapiPlaybackText, "_resumeRequested = false;\n        _pauseRequested = true;");
        AssertContains(wasapiPlaybackText, "_renderRunningAcknowledged.Reset();\n        _renderPausedAcknowledged.Reset();");
        AssertContains(wasapiPlaybackText, "if (Volatile.Read(ref _renderingPaused) == 0 && !_pauseRequested)");
        AssertContains(wasapiPlaybackText, "public void ResumeRendering(double prebufferMs = 0, int prebufferTimeoutMs = 0)");
        AssertContains(wasapiPlaybackText, "Volatile.Write(ref _resumePrebufferFrames, Math.Max(0, prebufferFrames));");
        AssertContains(wasapiPlaybackText, "_resumeRequested = true;\n        _renderEvent?.Set();");
        AssertContains(wasapiPlaybackText, "public bool WaitForRenderingPaused(int timeoutMs)");
        AssertContains(wasapiPlaybackText, "public bool WaitForRenderingRunning(int timeoutMs)");
        AssertContains(wasapiPlaybackText, "private bool WaitForRenderState(bool paused, int timeoutMs)");
        AssertContains(wasapiPlaybackText, "return _renderPausedAcknowledged.Wait(boundedTimeoutMs);");
        AssertContains(wasapiPlaybackText, "return _renderRunningAcknowledged.Wait(boundedTimeoutMs);");
        AssertContains(wasapiPlaybackRenderText, "internal sealed class WasapiAudioPlayback : IDisposable");
        AssertContains(wasapiPlaybackRenderText, "private void RenderThreadMain()");
        AssertContains(wasapiPlaybackRenderText, "if (!_resumeRequested)");
        AssertContains(wasapiPlaybackRenderText, "continue;");
        AssertContains(wasapiPlaybackRenderText, "_renderPausedAcknowledged.Set();");
        AssertContains(wasapiPlaybackRenderText, "_renderRunningAcknowledged.Set();");
        AssertContains(wasapiPlaybackRenderText, "WASAPI_PLAYBACK_RENDER_RESUME_CANCELED_PENDING_PAUSE");
        AssertContains(wasapiPlaybackRenderText, "WaitForResumePrebuffer();");
        AssertContains(wasapiPlaybackRenderText, "WASAPI_PLAYBACK_RENDER_PREBUFFER target_ms={FramesToMilliseconds(targetFrames):F1}");
        // A queued pause must escape the resume prebuffer wait promptly instead of
        // blocking the render thread for up to the full prebuffer timeout.
        AssertContains(wasapiPlaybackRenderText, "if (_pauseRequested)\n            {\n                pausePending = true;\n                break;\n            }");
        AssertContains(wasapiPlaybackRenderText, "pause_pending={pausePending}");
        AssertContains(wasapiPlaybackRenderText, "private int PlaybackBufferedFramesForResume()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.RenderThread.cs")),
            "WASAPI playback render thread folded into the playback lifecycle root");
        AssertDoesNotContain(wasapiPlaybackText, "public void ResumeRendering()\n    {\n        if (Volatile.Read(ref _started) == 0) return;\n        if (Volatile.Read(ref _renderingPaused) == 0 && !_pauseRequested) return;\n\n        _pauseRequested = false;");
        AssertDoesNotContain(wasapiPlaybackRenderText, "GetCurrentPadding(pre-fill)");
        AssertDoesNotContain(wasapiPlaybackRenderText, "IAudioRenderClient.GetBuffer(pre-fill)");
        AssertDoesNotContain(wasapiPlaybackRenderText, "AUDCLNT_BUFFERFLAGS_SILENT");
        AssertDoesNotContain(wasapiPlaybackRenderText, "WASAPI_PREFILL_WARN");
        AssertContains(wasapiPlaybackText, "private int _playbackQueueDepth;");
        AssertContains(wasapiPlaybackText, "public int PlaybackQueueDepth => Math.Max(0, Volatile.Read(ref _playbackQueueDepth));");
        AssertContains(wasapiPlaybackText, "internal void EnqueuePooledSamples(byte[] pooledBuffer, int validLength, long ptsTicks = 0)");
        AssertContains(wasapiPlaybackText, "if (TryWriteChunk(chunk)) return;");
        AssertContains(wasapiPlaybackText, "private bool TryWriteChunk(PlaybackChunk chunk)");
        AssertContains(wasapiPlaybackText, "Interlocked.Increment(ref _playbackQueueDepth);\n        if (_sampleQueue.Writer.TryWrite(chunk))");
        AssertContains(wasapiPlaybackText, "DecrementPlaybackQueueDepth();\n        return false;");
        AssertContains(wasapiPlaybackText, "private bool TryDequeueChunk(out PlaybackChunk chunk)");
        AssertContains(wasapiPlaybackText, "DecrementPlaybackQueueDepth();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.Queue.cs")),
            "WASAPI playback queue state stays folded into the lifecycle root");
        AssertContains(wasapiPlaybackText, "private const int OutputSampleRate = 48000;");
        AssertContains(wasapiPlaybackText, "private const uint MaxRenderWriteFrames = OutputSampleRate / 50; // 20ms");
        AssertContains(wasapiPlaybackRenderText, "var framesToWrite = Math.Min(_bufferFrameCount - paddingFrames, _maxRenderWriteFrames);");
        AssertDoesNotContain(wasapiPlaybackRenderText, "var framesToWrite = _bufferFrameCount - paddingFrames;");
        AssertContains(wasapiPlaybackRenderText, "UpdateRenderingPtsForActiveChunk();");
        AssertContains(wasapiPlaybackRenderText, "var frameOffset = Math.Max(0, _activeChunkOffset) / OutputBlockAlign;");
        AssertContains(wasapiPlaybackRenderText, "var offsetTicks = frameOffset * TimeSpan.TicksPerSecond / OutputSampleRate;");
        AssertDoesNotContain(wasapiPlaybackText, "_sampleQueue.Reader.Count");
        AssertDoesNotContain(wasapiPlaybackText, "_sampleQueue.Writer.TryWrite(chunk))\n        {\n            Interlocked.Increment(ref _playbackQueueDepth);");
        AssertDoesNotContain(sourceText, "_videoCapture?.SuppressPreviewSubmission();\n                        SuppressLiveAudio();\n                        _audioPlayback?.PauseRendering();");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var segmentEdgesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var segmentSwitchText = segmentEdgesText;
        var positioningText = rootText;
        var decoderReopenText = positioningText;
        var decoderSegmentReopenText = segmentEdgesText;
        var seekDisplayText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var metricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var seekCapTelemetryText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeek(");
        AssertContains(decoderSegmentReopenText, "private bool TryReopenCurrentFmp4BeforeSegmentSwitch(");
        AssertContains(decoderSegmentReopenText, "private bool HandleActiveFmp4ReopenAtSegmentEdge(");
        AssertContains(segmentSwitchText, "TryReopenCurrentFmp4BeforeSegmentSwitch(");
        AssertContains(segmentEdgesText, "TrySwitchToNextSegment(");
        AssertContains(segmentEdgesText, "return HandleActiveFmp4ReopenAtSegmentEdge(");
        AssertContains(segmentSwitchText, "FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR");
        AssertContains(segmentSwitchText, "FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL");
        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeekKeyframe(");
        AssertContains(sourceText, "private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);");
        AssertContains(sourceText, "private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);");
        AssertContains(decoderReopenText, "private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);");
        AssertContains(decoderReopenText, "private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);");
        AssertContains(decoderReopenText, "private bool TrySeekAdjacentSegmentStart(");
        AssertContains(decoderReopenText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK");
        AssertContains(sourceText, "private bool ShouldSkipActiveFmp4ReopenNearLive(TimeSpan seekTarget, string reason)");
        AssertContains(sourceText, "var latestPts = _bufferManager.LatestPts;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_SKIP_NEAR_LIVE");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_ERROR");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR");
        AssertContains(sourceText, "private bool TrySeekAdjacentSegmentStart(");
        AssertContains(sourceText, "var nextPath = _bufferManager.GetNextSegmentFile(currentPath);");
        AssertContains(sourceText, "var nextStart = _bufferManager.GetSegmentStartPts(nextPath);");
        AssertContains(sourceText, "if (targetGap > AdjacentSegmentSeekFallbackWindow)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_FAIL");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_ERROR");
        AssertContains(sourceText, "private static bool IsSamePlaybackPath(string? left, string? right)");
        AssertContains(sourceText, "Path.GetFullPath(left)");
        AssertContains(sourceText, "Path.GetFullPath(right)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PATH_COMPARE_WARN");
        AssertContains(sourceText, "&& IsSamePlaybackPath(path, _bufferManager.ActiveFilePath)");
        AssertContains(sourceText, "if (fileOpen && decoder.IsOpen && IsSamePlaybackPath(filePath, _currentOpenFilePath))\n            return;");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Paused &&\n            IsSamePlaybackPath(prevFile, _currentOpenFilePath) &&\n            !requireExactResumeSeek)");
        AssertContains(sourceText, "MarkDecoderPlaybackFileClosed(ref fileOpen);\n            return false;");
        AssertContains(sourceText, "private bool TrySeekWithActiveFmp4Reopen(");
        AssertContains(sourceText, "if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))\n        {\n            return true;\n        }");
        AssertContains(sourceText, "private bool SeekToWithCapTelemetry(");
        AssertContains(seekCapTelemetryText, "private long _playbackSeekForwardDecodeCapHits;");
        AssertContains(seekCapTelemetryText, "private int _lastPlaybackSeekHitForwardDecodeCap;");
        AssertContains(seekCapTelemetryText, "public long PlaybackSeekForwardDecodeCapHits => Interlocked.Read(ref _playbackSeekForwardDecodeCapHits);");
        AssertContains(seekCapTelemetryText, "public bool LastPlaybackSeekHitForwardDecodeCap => Volatile.Read(ref _lastPlaybackSeekHitForwardDecodeCap) != 0;");
        AssertContains(seekCapTelemetryText, "private bool SeekToWithCapTelemetry(");
        AssertContains(seekCapTelemetryText, "FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP");
        AssertContains(seekCapTelemetryText, "Interlocked.Increment(ref _playbackSeekForwardDecodeCapHits)");
        AssertContains(rootText, "public long PlaybackSeekForwardDecodeCapHits =>");
        AssertContains(rootText, "public bool LastPlaybackSeekHitForwardDecodeCap =>");
        AssertContains(rootText, "private bool SeekToWithCapTelemetry(");
        AssertContains(rootText, "FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP");
        AssertContains(sourceText, "if (ShouldSkipActiveFmp4ReopenNearLive(seekTarget, reason))\n            {\n                SetReopenFailure(reason, \"near_live\", seekTarget);\n                return false;\n            }\n\n            return TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, reason, cancellationToken);");
        AssertContains(sourceText, "if (TrySeekAdjacentSegmentStart(decoder, ref fileOpen, seekTarget, reason, out _, cancellationToken))\n        {\n            return true;\n        }\n\n        SetReopenFailure(reason, \"seek_failed\", seekTarget);");
        AssertContains(sourceText, "if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))\n            {\n                return true;\n            }\n\n            SetReopenFailure(reason, \"seek_failed\", seekTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_SEEK_FAIL");
        AssertContains(sourceText, "if (decoder.SeekToKeyframe(seekTarget, cancellationToken))\n            {\n                return true;\n            }\n\n            SetReopenFailure(reason, \"keyframe_seek_failed\", seekTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_SEEK_FAIL");
        AssertContains(positioningText, "private void ReopenDecoderPlaybackFile(");
        AssertContains(sourceText, "updateCurrentOpenPath: true,\n                closeOnlyWhenOpen: true);");
        AssertContains(sourceText, "updateCurrentOpenPath: false,\n                closeOnlyWhenOpen: false);");
        AssertContains(positioningText, "private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)");
        AssertContains(positioningText, "_decoderHwAccel = \"N/A\";\n        fileOpen = false;\n        _currentOpenFilePath = null;");
        AssertContains(positioningText, "private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)");
        AssertContains(positioningText, "private void CleanupDecoder(ref FlashbackDecoder? decoder, ref bool fileOpen)");
        AssertContains(positioningText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE");
        AssertContains(decoderReopenText, "private void ReopenDecoderPlaybackFile(");
        AssertContains(decoderReopenText, "private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.DecoderReopen.cs")),
            "active fMP4 reopen recovery folded into decoder file ownership");
        AssertContains(sourceText, "private long SuppressAudioForFmp4Reopen(FlashbackDecoder decoder)");
        AssertContains(sourceText, "Interlocked.Increment(ref _playbackReopenAudioNullWindowCount);\n        decoder.AudioChunkCallback = null;");
        AssertContains(sourceText, "private void RestoreAudioAfterFmp4Reopen(");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_REOPEN_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            MarkDecoderPlaybackFileClosed(ref fileOpen);");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            MarkDecoderPlaybackFileClosed(ref fileOpen);");
        AssertContains(sourceText, "SetReopenFailure(reason, \"no_current_file\", seekTarget);");
        AssertContains(sourceText, "SetReopenFailure(reason, ex.GetType().Name, seekTarget);");
        AssertContains(sourceText, "private void SetReopenFailure(string reason, string detail, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"reopen_failed:{reason}:{detail}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token))");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token))");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token))");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"no_file\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"seek_failed\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"submit_failed\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"no_frame\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, ex.GetType().Name, bufferPosition);");
        AssertContains(seekDisplayText, "private bool SeekAndDisplayKeyframe(");
        AssertContains(seekDisplayText, "private bool TryDecodeAndDisplaySeekFrame(");
        AssertContains(seekDisplayText, "private void RecordSeekDisplayDecodeFailure(");
        AssertDoesNotContain(agentMapText, "FlashbackPlaybackController.SeekDisplay.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackPlaybackController.SeekDisplay.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.SeekDisplay.cs")),
            "Flashback seek-display logic folded into playback frame ownership");
        var seekDisplayBlock = ExtractTextBetween(
            seekDisplayText,
            "private bool SeekAndDisplayKeyframe(",
            "\n}");
        AssertContains(seekDisplayBlock, "CancellationToken cancellationToken");
        AssertContains(seekDisplayBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(seekDisplayBlock, "decoder.SeekToKeyframe(filePts, cancellationToken)");
        AssertContains(seekDisplayBlock, "TryDecodeAndDisplaySeekFrame(");
        AssertContains(seekDisplayText, "TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken)");
        AssertContains(seekDisplayText, "var frameOwned = gotFrame;");
        AssertContains(seekDisplayText, "frameOwned = false;");
        AssertContains(seekDisplayText, "ReleaseHeldFrameBestEffort(frame, \"seek_cancelled\")");
        AssertContains(seekDisplayText, "if (frameOwned)\n            {\n                ReleaseHeldFrameBestEffort(frame, \"seek_cancelled\");\n            }");
        AssertContains(seekDisplayBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(seekDisplayBlock, "throw;");
        AssertOccursBefore(seekDisplayBlock, "cancellationToken.ThrowIfCancellationRequested();", "decoder.SeekToKeyframe(filePts, cancellationToken)");
        AssertOccursBefore(seekDisplayBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", "catch (Exception ex)");
        AssertContains(seekDisplayText, "TrySeekAdjacentSegmentStart(decoder, ref fileOpen, filePts, $\"seek_display:{kind}\", out var adjacentFilePts, cancellationToken)");
        AssertContains(seekDisplayText, "RecordSeekDisplayDecodeFailure(kind, bufferPosition, filePts);");
        AssertContains(seekDisplayText, "private void RecordSeekDisplayDecodeFailure(CommandKind kind, TimeSpan bufferPosition, TimeSpan filePts)");
        AssertContains(sourceText, "RecordPlaybackDroppedFrame(\"seek_display_no_frame\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEEK_NO_FRAME_SNAP_TO_LIVE");
        AssertContains(sourceText, "return gotFrame;");
        AssertContains(sourceText, "private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "CloseDecoderFileBestEffort(decoder, operation);\n        fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";\n        ReleasePlaybackFrameForLive(operation);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        if (resumeRendering)\n        {\n            SafeResumeRendering(operation);\n        }\n\n        SetState(FlashbackPlaybackState.Live, operation);");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"seek_display_failed\");");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"begin_scrub_display_failed\");");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"scrub_update_display_failed\");");
        AssertContains(sourceText, "private void SetSeekDisplayFailure(CommandKind kind, string detail, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"seek_display_failed:{kind}:{detail}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "TimeSpan? pendingExactResumeTarget = null;");
        AssertContains(sourceText, "var seekResumeTarget = ClampPlaybackTargetToMinimumLiveLead(");
        AssertContains(sourceText, "var coalescedSeekTarget = seekResumeTarget;");
        AssertContains(sourceText, "pendingExactResumeTarget = seekResumeTarget;");
        AssertContains(sourceText, "var pendingPlayTarget = ClampPlaybackTargetToMinimumLiveLead(");
        AssertContains(sourceText, "pendingExactResumeTarget ?? SaturatingAdd(PlaybackPosition, frozenValidStart),");
        AssertContains(sourceText, "var requireExactResumeSeek = pendingExactResumeTarget.HasValue;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RESUME_EXACT_SEEK");
        AssertContains(sourceText, "if (_commandMailbox.ShouldYieldSeekToQueuedPlay(commandChannel))");
        AssertContains(sourceText, "MarkCommandNoOp(CommandKind.Seek, \"superseded_by_play\", cmd.Position);");
        AssertContains(sourceText, "if (_commandMailbox.ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token))");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, \"end_scrub\", cts.Token))");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, \"play\", cts.Token))");
        AssertContains(sourceText, "if (!ShouldSkipActiveFmp4ReopenNearLive(filePts, \"seek_keyframe\"))\n                    {\n                        Logger.Log($\"FLASHBACK_PLAYBACK_SEEK_REOPEN_ACTIVE offset_ms={(long)filePts.TotalMilliseconds}\");\n                        if (TryReopenCurrentFileAndSeekKeyframe(decoder, ref fileOpen, filePts, \"seek_keyframe\", cancellationToken))\n                            goto seekSuccess;\n                    }");
        AssertContains(sourceText, "SetReopenFailure(\"segment_switch\", \"seek_failed\", segSwitchTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL");
        var segmentSwitch = sourceText.Substring(sourceText.IndexOf("private bool TrySwitchToNextSegment(", StringComparison.Ordinal));
        AssertOccursBefore(segmentSwitch, "decoder.BeginCompletedInputDrain(cancellationToken)", "decoder.CloseFile();");
        Assert.True(segmentSwitch.IndexOf("RestoreAudioCallback(decoder, audioGate, audioGate);", StringComparison.Ordinal) <
                    segmentSwitch.IndexOf("SeekToWithCapTelemetry(decoder, segSwitchTarget", StringComparison.Ordinal),
                    "Segment forward decode must preserve new audio and reject previously queued packets.");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"segment_switch_seek_failed\");");
        AssertContains(sourceText, "SetReopenFailure(\"fmp4_reopen\", \"seek_failed\", resumeTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_SEEK_FAIL");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"fmp4_reopen_seek_failed\");");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath!)");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPoints_DefaultToUnset()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)!;
        var controller = ctor.Invoke(new[] { bufferManager });

        var inPointProp = controllerType.GetProperty("InPoint", BindingFlags.Public | BindingFlags.Instance);
        var outPointProp = controllerType.GetProperty("OutPoint", BindingFlags.Public | BindingFlags.Instance);

        AssertNotNull(inPointProp, "FlashbackPlaybackController.InPoint");
        AssertNotNull(outPointProp, "FlashbackPlaybackController.OutPoint");
        foreach (var propertyName in new[]
                 {
                     "CommandsEnqueued",
                     "CommandsProcessed",
                     "CommandsDropped",
                     "CommandsSkippedNotReady",
                     "ScrubUpdatesCoalesced",
                     "PendingCommands",
                     "MaxPendingCommands",
                     "LastCommandQueueLatencyMs",
                     "MaxCommandQueueLatencyMs",
                     "LastCommandQueued",
                     "LastCommandProcessed",
                     "LastCommandQueuedUtcUnixMs",
                     "LastCommandProcessedUtcUnixMs",
                     "LastCommandFailureUtcUnixMs",
                     "LastCommandFailure",
                     "PlaybackThreadAlive"
                 })
        {
            AssertNotNull(controllerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance), propertyName);
        }

        var clearMethod = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(clearMethod, "FlashbackPlaybackController.ClearInOutPoints");
        clearMethod!.Invoke(controller, null);

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPoints_ClearInvalidCounterpart()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "var outTicks = Interlocked.Read(ref _outPointTicks);\n        if (outTicks != long.MinValue && outTicks <= pos.Ticks)\n        {\n            OutPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_OUT invalid_range\");\n        }");
        AssertContains(sourceText, "var inTicks = Interlocked.Read(ref _inPointTicks);\n        if (inTicks != long.MinValue && inTicks >= pos.Ticks)\n        {\n            InPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_IN invalid_range\");\n        }");
        AssertContains(sourceText, "var pos = overridePosition.HasValue\n            ? NormalizeMarkerPosition(overridePosition.Value)\n            : PlaybackPosition;\n        ClearLastCommandFailure();\n        InPoint = pos;");
        AssertContains(sourceText, "var pos = overridePosition.HasValue\n            ? NormalizeMarkerPosition(overridePosition.Value)\n            : PlaybackPosition;\n        ClearLastCommandFailure();\n        OutPoint = pos;");
        AssertContains(sourceText, "public TimeSpan SetInPoint() => SetInPointAt(null);");
        AssertContains(sourceText, "public TimeSpan SetInPointAt(TimeSpan position) => SetInPointAt((TimeSpan?)position);");
        AssertContains(sourceText, "public TimeSpan SetOutPoint() => SetOutPointAt(null);");
        AssertContains(sourceText, "public TimeSpan SetOutPointAt(TimeSpan position) => SetOutPointAt((TimeSpan?)position);");
        AssertContains(sourceText, "InPoint = null;\n        OutPoint = null;\n        ClearLastCommandFailure();");

        var flashbackCommandController = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        AssertContains(flashbackCommandController, "_context.ViewModel.FlashbackSetInPointAt(_context.ViewModel.FlashbackPlaybackPosition)");
        AssertContains(flashbackCommandController, "_context.ViewModel.FlashbackSetOutPointAt(_context.ViewModel.FlashbackPlaybackPosition)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPointSettersNormalizeMarkers()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();
        var markersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(markersText, "private long _inPointFilePtsTicks = long.MinValue;");
        AssertContains(markersText, "private long _outPointFilePtsTicks = long.MinValue;");
        AssertContains(markersText, "Interlocked.Exchange(ref _inPointTicks, normalized?.Ticks ?? long.MinValue);\n            Interlocked.Exchange(ref _inPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);");
        AssertContains(markersText, "Interlocked.Exchange(ref _outPointTicks, normalized?.Ticks ?? long.MinValue);\n            Interlocked.Exchange(ref _outPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);");
        AssertContains(markersText, "public TimeSpan? InPointFilePts");
        AssertContains(markersText, "public TimeSpan? OutPointFilePts");
        AssertContains(markersText, "public void RestoreInOutPoints(\n        TimeSpan? inPoint,\n        TimeSpan? outPoint,\n        TimeSpan? inPointFilePts,\n        TimeSpan? outPointFilePts)");
        AssertContains(markersText, "Interlocked.Exchange(ref _inPointFilePtsTicks, inPointFilePts.Value.Ticks);");
        AssertContains(markersText, "Interlocked.Exchange(ref _outPointFilePtsTicks, outPointFilePts.Value.Ticks);");
        AssertContains(sourceText, "private TimeSpan NormalizeMarkerPosition(TimeSpan position)\n    {\n        if (position <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }\n\n        var bufferDuration = _bufferManager.BufferedDuration;\n        return position > bufferDuration ? bufferDuration : position;\n    }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPointChangesStopAfterDispose()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        using var controller = (IDisposable)Activator.CreateInstance(controllerType, new[] { bufferManager })!;

        var setInPoint = controllerType.GetMethod("SetInPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetInPoint not found.");
        var setOutPoint = controllerType.GetMethod("SetOutPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetOutPoint not found.");
        var clearInOut = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.ClearInOutPoints not found.");

        setInPoint.Invoke(controller, null);
        controller.Dispose();
        clearInOut.Invoke(controller, null);
        setOutPoint.Invoke(controller, null);

        AssertEqual((TimeSpan?)TimeSpan.Zero, (TimeSpan?)GetPropertyValue(controller, "InPoint"), "disposed InPoint");
        AssertEqual<object?>(null, GetPropertyValue(controller, "OutPoint"), "disposed OutPoint");

        var sourceText = ReadFlashbackPlaybackControllerSource();
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetInPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetOutPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:ClearInOutPoints\");");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_ClampPosition_BoundsMarkersToBufferedDuration()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "var bufferDuration = _bufferManager.BufferedDuration;\n        var inTicks = Interlocked.Read(ref _inPointTicks);");
        AssertContains(sourceText, "var max = outTicks == long.MinValue ? bufferDuration : TimeSpan.FromTicks(outTicks);\n        if (max > bufferDuration) max = bufferDuration;");
        AssertContains(sourceText, "private TimeSpan ClampPosition(TimeSpan position) => ClampPosition(position, null);");
        AssertContains(sourceText, "private TimeSpan ClampPosition(TimeSpan position, TimeSpan? frozenValidStart)");
        AssertContains(sourceText, "var currentValidStart = _bufferManager.ValidStartPts;");
        AssertContains(sourceText, "var evictedDelta = currentValidStart - frozenValidStart.Value;");

        return Task.CompletedTask;
    }
    internal static Task FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var metricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var playbackTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackPtsCadenceText = playbackTimingText;

        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0))");
        AssertContains(sourceText, "frameDuration = ResolveFrameDuration(decoder);");
        AssertContains(sourceText, "private TimeSpan ResolveFrameDuration(FlashbackDecoder decoder)");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = decoder.FrameRate;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = FallbackPlaybackFrameRate;\n        }");
        AssertContains(sourceText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertContains(sourceText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertContains(playbackTimingText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertContains(playbackTimingText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertDoesNotContain(rootText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertDoesNotContain(rootText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertContains(sourceText, "fps = Math.Min(fps, MaxPlaybackFrameRate);");
        AssertContains(sourceText, "_playbackTargetFps = fps;");
        AssertContains(sourceText, "public double PlaybackTargetFps => _playbackTargetFps;");
        AssertContains(sourceText, "return TimeSpan.FromSeconds(1.0 / fps);");
        AssertContains(sourceText, "TrackDecodedPtsCadence(videoFrame.Pts, frameDuration);");
        AssertContains(playbackPtsCadenceText, "private void TrackDecodedPtsCadence(TimeSpan pts, TimeSpan expectedFrameDuration)");
        AssertContains(playbackPtsCadenceText, "private void ResetPlaybackPtsCadenceBaseline()");
        AssertContains(playbackPtsCadenceText, "private void RecordPlaybackPtsCadenceMismatch(");
        AssertContains(playbackPtsCadenceText, "private long _lastPlaybackCadencePtsTicks = -1;");
        AssertContains(playbackPtsCadenceText, "private long _playbackPtsCadenceMismatchCount;");
        AssertContains(playbackPtsCadenceText, "private long _lastPlaybackPtsCadenceMismatchUtcUnixMs;");
        AssertContains(playbackPtsCadenceText, "private double _lastPlaybackPtsCadenceDeltaMs;");
        AssertContains(playbackPtsCadenceText, "private double _lastPlaybackPtsCadenceExpectedMs;");
        AssertContains(playbackPtsCadenceText, "public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);");
        AssertContains(playbackPtsCadenceText, "public long LastPlaybackPtsCadenceMismatchUtcUnixMs => Interlocked.Read(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs);");
        AssertContains(playbackPtsCadenceText, "public double LastPlaybackPtsCadenceDeltaMs => _lastPlaybackPtsCadenceDeltaMs;");
        AssertContains(playbackPtsCadenceText, "public double LastPlaybackPtsCadenceExpectedMs => _lastPlaybackPtsCadenceExpectedMs;");
        AssertContains(playbackPtsCadenceText, "FLASHBACK_PLAYBACK_PTS_CADENCE_MISMATCH");
        AssertDoesNotContain(metricsCollectionText, "public long PlaybackPtsCadenceMismatchCount =>");
        AssertDoesNotContain(metricsCollectionText, "public double LastPlaybackPtsCadenceDeltaMs =>");
        AssertContains(sourceText, "public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _playbackPtsCadenceMismatchCount, 0);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var track = controllerType.GetMethod("TrackDecodedPtsCadence", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TrackDecodedPtsCadence not found.");
        var reset = controllerType.GetMethod("ResetPlaybackMetrics", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResetPlaybackMetrics not found.");
        var expected = TimeSpan.FromMilliseconds(1000.0 / 120.0);

        try
        {
            track.Invoke(controller, new object[] { expected, expected });
            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 60.0), expected });
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "matching decoded PTS cadence count");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(1L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "slow decoded PTS cadence count");
            AssertNearlyEqual(1000.0 / 60.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "slow decoded PTS cadence delta");
            AssertNearlyEqual(expected.TotalMilliseconds, GetDoubleProperty(controller, "LastPlaybackPtsCadenceExpectedMs"), 0.1, "decoded PTS expected cadence");
            if (GetLongProperty(controller, "LastPlaybackPtsCadenceMismatchUtcUnixMs") <= 0)
            {
                throw new InvalidOperationException("Expected decoded PTS cadence mismatch timestamp to be populated.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(2L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "duplicate decoded PTS cadence count");
            AssertNearlyEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "duplicate decoded PTS cadence delta");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(25.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "backward decoded PTS cadence count");
            if (GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs") >= 0)
            {
                throw new InvalidOperationException("Expected backward decoded PTS cadence delta to be negative.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 24.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "valid cadence after backward PTS remains clean");

            reset.Invoke(controller, null);
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "decoded PTS cadence reset count");
            AssertEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), "decoded PTS cadence reset delta");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_ResetClearsDecodeMetrics()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        var resetMetricsBlock = ExtractTextBetween(
            sourceText,
            "private void ResetPlaybackMetrics()",
            "private void RestoreAudioCallback");
        AssertContains(metricsText, "private long _playbackFrameCount;");
        AssertContains(metricsText, "private long _playbackDroppedFrames;");
        AssertContains(metricsText, "private readonly Stopwatch _playbackFpsClock = new();");
        AssertContains(metricsText, "private const int PlaybackCadenceSampleCapacity = 240;");
        AssertContains(metricsText, "private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertContains(metricsText, "public long PlaybackFrameCount => Interlocked.Read(ref _playbackFrameCount);");
        AssertContains(metricsText, "public string LastPlaybackDropReason => Volatile.Read(ref _lastPlaybackDropReason);");
        AssertContains(metricsText, "public double PlaybackAvgFrameMs => _playbackAvgFrameMs;");
        AssertDoesNotContain(metricsText, "private long _lastPlaybackCadencePtsTicks = -1;");
        AssertDoesNotContain(metricsText, "private long _playbackPtsCadenceMismatchCount;");
        AssertContains(metricsText, "private void ResetPlaybackMetrics()");
        AssertContains(metricsText, "Interlocked.Exchange(ref _playbackPreviewPresentId, 0);");
        AssertContains(metricsText, "lock (_playbackDecodeLock)");
        AssertContains(metricsText, "Array.Clear(_playbackDecodeDurationsMs);");
        AssertContains(metricsText, "_playbackDecodeDurationHead = 0;");
        AssertContains(metricsText, "_playbackDecodeDurationCount = 0;");
        AssertContains(metricsText, "public readonly record struct PlaybackCadenceMetrics(");
        AssertContains(metricsText, "public PlaybackCadenceMetrics GetPlaybackCadenceMetrics()");
        AssertContains(metricsText, "PercentileHelpers.FromSorted(sorted, 0.95)");
        AssertContains(metricsText, "PercentileHelpers.FromSorted(samples, 0.99)");
        AssertDoesNotContain(metricsText, "private static double PercentileFromSorted");
        AssertContains(metricsText, "public readonly record struct PlaybackDecodeMetrics(");
        AssertContains(metricsText, "public PlaybackDecodeMetrics GetPlaybackDecodeMetrics()");
        AssertContains(metricsText, "private readonly double[] _playbackDecodeDurationsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertContains(metricsText, "private double _playbackMaxDecodeTotalMs;");
        AssertContains(metricsText, "private string _playbackMaxDecodePhase = string.Empty;");
        AssertContains(metricsText, "public string PlaybackMaxDecodePhase => Volatile.Read(ref _playbackMaxDecodePhase);");
        AssertContains(metricsText, "public double PlaybackMaxDecodeSendMs => _playbackMaxDecodeSendMs;");
        AssertContains(metricsText, "public long PlaybackMaxDecodePositionMs => Interlocked.Read(ref _playbackMaxDecodePositionMs);");
        AssertContains(metricsText, "private bool TryDecodeNextVideoFrameWithMetrics(");
        AssertContains(metricsText, "private void TrackPlaybackDecodeDuration(");
        AssertContains(metricsText, "private static string ResolveDominantDecodePhase(FlashbackDecoder.PlaybackDecodePhaseTimings phaseTimings)");
        AssertContains(rootText, "private long _playbackFrameCount;");
        AssertContains(rootText, "private readonly Stopwatch _playbackFpsClock = new();");
        AssertContains(rootText, "private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertContains(rootText, "private string _playbackMaxDecodePhase = string.Empty;");
        AssertContains(resetMetricsBlock, "Interlocked.Exchange(ref _playbackPreviewPresentId, 0);");
        AssertContains(sourceText, "if (phaseTimings.FeedMs > max) { phase = \"feed\"; max = phaseTimings.FeedMs; }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var playbackFrameOwnershipText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(playbackFrameOwnershipText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(playbackFrameOwnershipText, "private static void SubmitFrame(");
        AssertContains(playbackFrameOwnershipText, "private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)");
        AssertContains(playbackFrameOwnershipText, "private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)");
        AssertContains(playbackFrameOwnershipText, "private DecodedVideoFrame _previousHeldFrame;");
        AssertContains(playbackFrameOwnershipText, "private bool _hasPreviousHeldFrame;");
        AssertContains(playbackFrameOwnershipText, "private void ReleasePreviousHeldFrame()");
        AssertContains(playbackFrameOwnershipText, "private void HoldSubmittedFrame(DecodedVideoFrame frame)");
        AssertContains(playbackFrameOwnershipText, "private void ReleasePlaybackFrameForLive(string operation)");
        AssertContains(playbackFrameOwnershipText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterPlaybackDecodeError(FlashbackDecoder decoder, ref bool fileOpen)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterNearLiveSnap(FlashbackDecoder decoder, ref bool fileOpen)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterSoftwarePlaybackBudgetSnap(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterDecoderPlaybackFailure(");
        AssertContains(playbackFrameOwnershipText, "CloseDecoderFileBestEffort(decoder, operation);");
        AssertContains(playbackFrameOwnershipText, "ReleasePlaybackFrameForLive(operation);");
        AssertContains(playbackFrameOwnershipText, "RestoreLiveAudio();");
        AssertContains(playbackFrameOwnershipText, "SafeResumePreviewSubmission(operation);");
        AssertContains(playbackFrameOwnershipText, "SafeResumeRendering(operation);");
        AssertContains(playbackFrameOwnershipText, "SetState(FlashbackPlaybackState.Live, operation);");
        AssertDoesNotContain(rootText, "private DecodedVideoFrame _previousHeldFrame;");
        AssertDoesNotContain(rootText, "private bool _hasPreviousHeldFrame;");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.PreviewFrames.cs")),
            "Flashback playback preview-frame submission folded into PlaybackFrames");
        AssertContains(rootText, "private IPreviewFrameSink? _previewSink;");
        AssertContains(rootText, "private ILiveVideoSource? _videoCapture;");
        AssertContains(rootText, "private volatile WasapiAudioPlayback? _audioPlayback;");
        AssertContains(rootText, "private volatile WasapiAudioCapture? _audioCapture;");
        AssertContains(rootText, "private volatile bool _initialized;");
        AssertContains(rootText, "private volatile int _disposedFlag;");
        AssertContains(rootText, "private int _previewDetachStopTimeoutActive;");
        AssertContains(rootText, "private int _deferredPreviewAttachApplyRetryScheduled;");
        AssertContains(rootText, "private IPreviewFrameSink? _pendingPreviewSinkAfterDetachTimeout;");
        AssertContains(rootText, "private ILiveVideoSource? _pendingVideoCaptureAfterDetachTimeout;");
        AssertContains(rootText, "public void PrepareForPreviewDetach()");
        AssertContains(rootText, "private void DetachPreviewComponentsAfterStopTimeout()");
        AssertContains(rootText, "private bool TryDeferPreviewAttachAfterStopTimeoutUnsafe(");
        AssertContains(rootText, "private void ApplyDeferredPreviewAttachAfterStopTimeout()");
        AssertContains(rootText, "private void ScheduleDeferredPreviewAttachApplyRetry()");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.Lifecycle.cs")),
            "Flashback playback component lifecycle folded into root controller");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.PreviewDetachLifecycle.cs")),
            "Flashback playback preview-detach lifecycle folded into root controller");
        AssertContains(sourceText, "if (!TryValidatePreviewFrame(frame, out var skipReason))");
        AssertContains(sourceText, "Interlocked.Increment(ref _playbackSubmitFailures);");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:{skipReason}\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_{skipReason}\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason={skipReason}");
        AssertContains(sourceText, "public long PlaybackSubmitFailures => Interlocked.Read(ref _playbackSubmitFailures);");
        AssertContains(sourceText, "public long LastSubmitFailureUtcUnixMs => Interlocked.Read(ref _lastSubmitFailureUtcUnixMs);");
        AssertContains(sourceText, "public string LastSubmitFailure => Volatile.Read(ref _lastSubmitFailure);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _playbackSubmitFailures, 0);");
        AssertContains(sourceText, "ClearLastSubmitFailure();");
        AssertContains(sourceText, "public void UpdatePreviewComponents(IPreviewFrameSink? previewSink, ILiveVideoSource? videoCapture)");
        AssertContains(sourceText, "TryDeferPreviewAttachAfterStopTimeoutUnsafe(previewSink, videoCapture, \"update\")");
        AssertContains(sourceText, "_initialized = previewSink != null && videoCapture != null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_UPDATE sink={previewSink != null} capture={videoCapture != null}");
        AssertContains(sourceText, "ApplyPreviewRoutingForState(\"preview_update\");");
        AssertContains(sourceText, "public void PrepareForPreviewDetach()");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_DETACH state={_state} thread_alive={PlaybackThreadAlive}");
        AssertContains(sourceText, "if (!StopPlaybackThread(PreviewDetachThreadStopTimeout, \"preview_detach\"))\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_PREVIEW_DETACH_ABORT reason=thread_stop_failed\");\n            RestoreLiveAudio();\n            SafeResumePreviewSubmission(\"preview_detach_timeout\");\n            DetachPreviewComponentsAfterStopTimeout();\n            return;\n        }\n\n        ReleasePlaybackFrameForLive(\"preview_detach\");");
        AssertOccursBefore(sourceText, "SafeResumePreviewSubmission(\"preview_detach_timeout\");", "DetachPreviewComponentsAfterStopTimeout();\n            return;");
        AssertOccursBefore(sourceText, "DetachPreviewComponentsAfterStopTimeout();\n            return;", "ReleasePlaybackFrameForLive(\"preview_detach\");");
        AssertContains(sourceText, "RestoreLiveAudio();\n        SafeResumePreviewSubmission(\"preview_detach\");\n        SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "private void DetachPreviewComponentsAfterStopTimeout()");
        AssertContains(sourceText, "Volatile.Write(ref _previewDetachStopTimeoutActive, 1);");
        AssertContains(sourceText, "_pendingPreviewSinkAfterDetachTimeout = null;\n            _pendingVideoCaptureAfterDetachTimeout = null;");
        AssertContains(sourceText, "_previewSink = null;\n            _videoCapture = null;\n            _initialized = false;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_DETACH_DEFER_OWNED_CLEANUP reason=thread_alive");
        AssertContains(sourceText, "private bool TryDeferPreviewAttachAfterStopTimeoutUnsafe(");
        AssertContains(sourceText, "_pendingPreviewSinkAfterDetachTimeout = previewSink;\n        _pendingVideoCaptureAfterDetachTimeout = videoCapture;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER op={operation} reason=thread_alive_after_detach_timeout");
        AssertContains(sourceText, "private void ApplyDeferredPreviewAttachAfterStopTimeout()");
        AssertContains(sourceText, "Monitor.TryEnter(_playbackThreadSync, 0, ref lockTaken);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_APPLY_SKIP reason=lock_busy");
        AssertContains(sourceText, "ScheduleDeferredPreviewAttachApplyRetry();");
        AssertContains(sourceText, "private void ScheduleDeferredPreviewAttachApplyRetry()");
        AssertContains(sourceText, "Interlocked.CompareExchange(ref _deferredPreviewAttachApplyRetryScheduled, 1, 0)");
        AssertContains(sourceText, "await Task.Delay(25).ConfigureAwait(false);");
        AssertContains(sourceText, "if (Volatile.Read(ref _previewDetachStopTimeoutActive) != 0)");
        AssertContains(sourceText, "Volatile.Write(ref _previewDetachStopTimeoutActive, 0);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _deferredPreviewAttachApplyRetryScheduled, 0);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_APPLIED reason=thread_exit");
        AssertContains(sourceText, "ApplyPreviewRoutingForState(\"deferred_preview_attach\");");
        AssertContains(sourceText, "private void ApplyPreviewRoutingForState(string operation)");
        AssertContains(sourceText, "var previewSink = Volatile.Read(ref _previewSink);");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:missing_preview_sink\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_missing_preview_sink\");");
        AssertContains(sourceText, "private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)");
        AssertContains(sourceText, "reason = \"invalid_dimensions\";");
        AssertContains(sourceText, "reason = \"null_texture\";");
        AssertContains(sourceText, "reason = \"invalid_subresource\";");
        AssertContains(sourceText, "reason = \"null_data\";");
        AssertContains(sourceText, "reason = \"invalid_data_length\";");
        AssertContains(sourceText, "reason = \"short_data_length\";");
        AssertContains(sourceText, "private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)");
        AssertContains(sourceText, "var calculated = isHdr\n            ? pixels * 3\n            : pixels + width * (long)(height / 2);");
        AssertContains(sourceText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RELEASE_HELD_FRAME_WARN");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:submit_fail:{ex.GetType().Name}\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_submit_fail\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_previousHeldFrame, \"previous_frame\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(videoFrame, \"av_sync_skip\");");
        AssertContains(sourceText, "private void ReleasePlaybackFrameForLive(string operation)");
        AssertContains(sourceText, "private void ReleasePlaybackFrameForLive(string operation)\n    {\n        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RELEASE_HELD_FOR_LIVE op={operation}");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"seek_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Seek, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"scrub_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.BeginScrub, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"scrub_update_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.UpdateScrub, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"play_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Play, PlaybackPosition);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"nudge_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Nudge, nudgedPos);");
        AssertContains(sourceText, "RestoreLiveAfterNearLiveSnap(decoder, ref fileOpen);");
        AssertContains(sourceText, "RestoreLiveAfterPlaybackDecodeError(decoder, ref fileOpen);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_FAIL");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(nudgeFrame, \"nudge\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(frame, \"seek\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(videoFrame, \"playback\")");
        AssertContains(sourceText, "var countForPresentCadence = string.Equals(operation, \"playback\", StringComparison.Ordinal);");
        AssertContains(sourceText, "var submitTick = Stopwatch.GetTimestamp();");
        AssertContains(sourceText, "var previewPresentId = Interlocked.Increment(ref _playbackPreviewPresentId);");
        AssertContains(sourceText, "SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);");
        AssertContains(sourceText, "sourceSequenceNumber: -1");
        AssertContains(sourceText, "previewPresentId: previewPresentId");
        AssertContains(sourceText, "sourcePtsTicks: frame.Pts.Ticks");
        AssertContains(sourceText, "countForPresentCadence: countForPresentCadence");
        AssertContains(sourceText, "arrivalTick: submitTick");
        AssertContains(sourceText, "schedulerSubmitTick: submitTick");
        AssertDoesNotContain(sourceText, "frame.Width, frame.Height, frame.IsHdr, arrivalTick: 0");
        AssertContains(sourceText, "if (!TrySubmitAndHoldFrame(videoFrame, \"playback\"))\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_SUBMIT_STOP pos_ms={(long)PlaybackPosition.TotalMilliseconds}\");\n                RestoreLiveAfterPlaybackSubmitFailure(decoder, ref fileOpen, \"playback_submit_failed\");\n                return false;\n            }");
        AssertContains(sourceText, "private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        if (resumeRendering)\n        {\n            SafeResumeRendering(operation);\n        }\n\n        SetState(FlashbackPlaybackState.Live, operation);");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n        try\n        {\n            SubmitFrame(frame);");
        AssertContains(sourceText, "SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);\n            HoldSubmittedFrame(frame);");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n            SubmitFrame(videoFrame);");

        return Task.CompletedTask;
    }

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



    internal static Task FlashbackEncoderSink_ForceRotateDrainingDoesNotRejectVideoByLifecycleState()
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

            // The encoder-lane fence must not reject live video.
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { false }) as string, "Force-rotate draining accepts CPU video");
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { true }) as string, "Force-rotate draining accepts GPU video");

            SetPrivateField(sink, "_videoQueueDepth", 180);
            SetPrivateField(sink, "_gpuQueueDepth", 8);
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { false }) as string, "Force-rotate draining ignores CPU queue depth");
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { true }) as string, "Force-rotate draining ignores GPU queue depth");

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
        AssertContains(sourceText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(sourceText, "private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)");
        AssertContains(sourceText, "private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)");
        AssertContains(sourceText, "Rotation has an encoder-lane fence.");
        AssertDoesNotContain(sourceText, "return \"force_rotate_draining\";");
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
            "private bool RotateSegment(TimeSpan currentPts, string? preparedPath = null)",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateBlock, "string? completedPath = null;");
        AssertContains(rotateBlock, "string? newPath = null;");
        AssertContains(rotateBlock, "var encoderRotated = false;");
        AssertContains(rotateBlock, "completedPath = _tsFilePath;");
        AssertContains(rotateBlock, "var completedStartPts = _segmentStartPts;");
        AssertContains(rotateBlock, "newPath = preparedPath ?? _bufferManager.GenerateSegmentPath();");
        AssertContains(rotateBlock, "encoderRotated = true;");
        AssertOccursBefore(rotateBlock, "encoderRotated = true;", "_tsFilePath = newPath;");
        AssertOccursBefore(rotateBlock, "_tsFilePath = newPath;", "_bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, segmentBytes);");
        AssertContains(rotateBlock, "if (newPath != null && !encoderRotated)");
        AssertContains(rotateBlock, "_bufferManager.AbandonReservedSegmentPath(newPath);");
        AssertContains(rotateBlock, "_bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);");

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
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE", "var request = new ForceRotateRequest(preparedPath);");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_INACTIVE");
        AssertContains(forceRotateBlock, "if (_encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.Failed();");
        AssertContains(forceRotateBlock, "_bufferManager.ReserveSegmentPath();");
        AssertContains(forceRotateBlock, "var request = new ForceRotateRequest(preparedPath);");
        AssertContains(forceRotateBlock, "if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK", "_forceRotateRequest = request;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs").Replace("\r\n", "\n");
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs").Replace("\r\n", "\n");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.ForceRotate.cs")),
            "FlashbackEncoderSink.ForceRotate.cs folded into FlashbackEncoderSink.cs");
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
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate", "if (!RotateSegment(currentPts, localRequest.PreparedPath))");
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
        AssertContains(startupQueuesText, "FLASHBACK_SINK_CPU_INPUT_UPLOAD");
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
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs").Replace("\r\n", "\n");
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
        AssertContains(packetDrainText, "PooledVideoFrame.GetFrameSizeBytes");
        AssertContains(packetDrainText, "var pts = OnVideoFrameEncoded();");
        AssertContains(packetDrainText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");

        AssertContains(encodingProgressText, "private TimeSpan OnVideoFrameEncoded()");
        AssertContains(encodingProgressText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(encodingProgressText, "_bufferManager.UpdateLatestPts(pts);");
        AssertContains(encodingProgressText, "FrameEncoded?.Invoke(this, encoded);");
        AssertContains(encodingProgressText, "FLASHBACK_SINK_ROTATE");
        AssertContains(encodingProgressText, "FLASHBACK_SINK_ROTATE_FAIL");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "bounded video/GPU/audio/microphone packet drains");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.PacketDrain.cs")),
            "FlashbackEncoderSink.PacketDrain.cs folded into FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.EncodingProgress.cs")),
            "FlashbackEncoderSink.EncodingProgress.cs folded into FlashbackEncoderSink.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_QueueingOwnsInputsAndCleanup()
    {
        var queueingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
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
        AssertContains(inputsText, "Rotation has an encoder-lane fence.");
        AssertDoesNotContain(inputsText, "return \"force_rotate_draining\";");
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
        AssertContains(inputsText, "private bool TryEnqueueAudioPacket(");
        var audioQueueAdmission = ExtractTextBetween(
            inputsText,
            "private bool TryEnqueueAudioPacket(",
            "    private static bool TryWriteAudioPacket(");
        AssertDoesNotContain(audioQueueAdmission, "Volatile.Read(ref _forceRotateDraining)");
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
        AssertContains(queueingText, "private void ResetVideoDiagnostics()");

        AssertContains(docsText, "FlashbackEncoderSink.cs");
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
                $"{removedFile} folded into FlashbackEncoderSink.cs");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.AudioQueueSubmission.cs")),
            "FlashbackEncoderSink.AudioQueueSubmission.cs folded into FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.VideoQueueSubmission.cs")),
            "FlashbackEncoderSink.VideoQueueSubmission.cs folded into FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Inputs.cs")),
            "FlashbackEncoderSink.Inputs.cs folded into FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Queues.cs")),
            "FlashbackEncoderSink.Queues.cs folded into FlashbackEncoderSink.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ForceRotateLivesWithEncodingLoop()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var forceRotateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
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
        AssertContains(forceRotateText, "var request = new ForceRotateRequest(preparedPath);");
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
        AssertContains(forceRotateText, "if (!RotateSegment(currentPts, localRequest.PreparedPath))");
        AssertContains(forceRotateText, "localRequest.Complete(_bufferManager.GetValidSegmentPaths(localIn, localOut));");
        AssertContains(rootText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertContains(rootText, "public bool IsForceRotateActive =>");
        AssertContains(rootText, "public bool WaitForForceRotateIdle(TimeSpan timeout)");
        AssertContains(rootText, "private bool _forceRotateRequested;");
        AssertContains(rootText, "private TimeSpan _forceRotateInPoint;");
        AssertContains(rootText, "private TimeSpan _forceRotateOutPoint;");
        AssertContains(rootText, "private bool _forceRotateDraining;");
        AssertContains(rootText, "private sealed class ForceRotateRequest");
        AssertContains(rootText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertDoesNotContain(docsText, "FlashbackEncoderSink.ForceRotate.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.ForceRotate.cs")),
            "FlashbackEncoderSink.ForceRotate.cs folded into FlashbackEncoderSink.cs");
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
                $"{removedFile} folded into FlashbackEncoderSink.cs");
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
        var inputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(inputsText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertContains(inputsText, "bool IRawVideoFrameLeaseTryEncoder.TryEnqueueRawVideoFrame(PooledVideoFrameLease frame)");
        AssertContains(inputsText, "public bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)");
        AssertContains(inputsText, "PooledVideoFrame.GetFrameSizeBytes");
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

        AssertContains(rootText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertContains(rootText, "public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Inputs.Video.cs")),
            "FlashbackEncoderSink video producer inputs folded into FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Inputs.Audio.cs")),
            "FlashbackEncoderSink audio producer inputs folded into FlashbackEncoderSink.cs");

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
        var queuesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var packetBuffersText = queuesText;
        var packetTypesText = packetBuffersText;
        var inputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
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
        AssertContains(optionsText, "private readonly record struct VideoFramePacket");
        AssertContains(optionsText, "private static byte[] GetBuffer");

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
        AssertContains(rootText, "private static byte[] GetBuffer");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "recording-to-Flashback session mapping");
        AssertContains(docsText, "generated session ID formatting");
        AssertContains(docsText, "FlashbackEncoderSink.cs");
        AssertContains(docsText, "packet DTOs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Options.cs")),
            "FlashbackEncoderSink.Options.cs folded into FlashbackEncoderSink.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.PacketBuffers.cs")),
            "FlashbackEncoderSink.PacketBuffers.cs folded into FlashbackEncoderSink.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var mailboxType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox");
        var commandType = mailboxType.GetNestedType("Command", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = mailboxType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var commandMailboxField = controllerType.GetField("_commandMailbox", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandMailbox not found.");
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

        var mailbox = commandMailboxField.GetValue(controller)
            ?? throw new InvalidOperationException("Command mailbox missing.");
        var generation = mailbox.GetType().GetProperty("CurrentGeneration", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(mailbox)
            ?? throw new InvalidOperationException("Command mailbox generation missing.");
        var channel = generation.GetType().GetProperty("Channel", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(generation)
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


    private static string ReadFlashbackPlaybackControllerPlaybackSource()
        => ReadFlashbackPlaybackControllerSource();

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
        AssertContains(sourceText, "_videoCodecCtx->thread_count = Math.Clamp(Environment.ProcessorCount / 2, 1, 12);");
        AssertOccursBefore(sourceText, "_videoCodecCtx->thread_type = ffmpeg.FF_THREAD_FRAME | ffmpeg.FF_THREAD_SLICE;", "ThrowIfError(\n            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),");
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
        AssertContains(sourceText, "codecId is AVCodecID.AV_CODEC_ID_HEVC or AVCodecID.AV_CODEC_ID_H264");
        AssertContains(sourceText, "ffmpeg.av_dict_set(&probeOptions[i], \"skip_frame\", \"all\", 0)");
        AssertContains(sourceText, "ffmpeg.av_dict_free(&probeOptions[i]);");
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

    internal static Task FlashbackDecoder_AudioSetupLivesWithDecoderRoot()
    {
        var decoderText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(decoderText, "private void InitializeAudioDecoder()");
        AssertContains(decoderText, "private void InitializeAudioResampler()");
        AssertContains(decoderText, "private void DecodeAndDeliverAudioPacket(AVPacket* packet)");
        AssertContains(decoderText, "private DecodedAudioChunk ConvertAndOutputAudioFrame()");
        AssertContains(decoderText, "FLASHBACK_DECODER_AUDIO codec=");
        AssertContains(decoderText, "swr_alloc_set_opts2");
        AssertContains(decoderText, "DecodeAndDeliverAudioPacket(_packet);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.AudioOutput.cs")),
            "Flashback decoder audio output folded into decoder root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.Playback.cs")),
            "Flashback decoder playback packet feed folded into decoder root");

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
        AssertContains(sourceText, "private static bool IsConvertibleSdrPlanarFormat(AVPixelFormat format)");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P ||\n           format == AVPixelFormat.AV_PIX_FMT_YUVJ420P");
        AssertContains(sourceText, "if (!isHdr && IsConvertibleSdrPlanarFormat(format))");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P10LE");
        AssertContains(sourceText, "failure = $\"unsupported_format:{format}\";");
        AssertContains(sourceText, "IsConvertibleSdrPlanarFormat(_decodedPixelFormat) ||");
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

        AssertContains(sourceText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool BeginCompletedInputDrain(CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "if (_completedInputDrainStarted)");
        AssertContains(sourceText, "ffmpeg.avcodec_send_packet(_videoCodecCtx, null)");
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

        AssertContains(sourceText, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "if (!FeedNextVideoPacket(cancellationToken))");

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
        var d3d11Text = rootText;
        AssertContains(rootText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11.cs")),
            "Flashback decoder D3D11VA initialization lives with the decoder root.");
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
        var d3d11Text = decoderText;
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

        AssertContains(rootText, "private static int CalculateFrameBufferSize(int width, int height, bool isHdr)");
        AssertContains(rootText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertContains(rootText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertContains(rootText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertContains(rootText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(rootText, "private static bool TryGetInputStreamCount(AVFormatContext* formatCtx, out int streamCount, out string failureMessage)");
        AssertContains(rootText, "private static bool IsValidStreamIndex(int streamIndex, int streamCount)");
        AssertContains(rootText, "private void CopyFramePlanesToBuffer(");
        AssertContains(rootText, "private void ConvertYuv420pToNv12(");
        AssertContains(rootText, "private void ConvertYuv420p10leToP010(");
        AssertContains(rootText, "private static void InterleaveUvRow(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.VideoSetup.cs")),
            "FlashbackDecoder.VideoSetup.cs folded into FlashbackDecoder.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.VideoConversion.cs")),
            "FlashbackDecoder.VideoConversion.cs folded into FlashbackDecoder.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.VideoOutput.cs")),
            "FlashbackDecoder.VideoOutput.cs folded into FlashbackDecoder.cs");
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

    internal static Task FlashbackDecoder_StateGuardsTimingAndErrorsLiveWithDecoderRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertContains(rootText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertContains(rootText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(rootText, "private static string GetErrorString(int errorCode)");
        AssertContains(rootText, "private static InvalidOperationException CreateException(string message)");
        AssertContains(rootText, "private void ThrowIfNotInitialized()");
        AssertContains(rootText, "private void ThrowIfNotOpen()");
        AssertContains(rootText, "private void ThrowIfDisposed()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.Playback.cs")),
            "Flashback decoder playback/timing folded into decoder root");

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

    internal static Task FlashbackDecoder_VideoSetupLivesWithDecoderRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private void InitializeVideoDecoder()");
        AssertContains(rootText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        AssertContains(rootText, "private bool TryInitializeD3D11VADecoder(AVCodecParameters* codecPar)");
        AssertContains(rootText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(rootText, "private void AllocateVideoOutputBuffers()");
        AssertContains(rootText, "private DecodedVideoFrame ConvertAndOutputVideoFrame()");
        AssertContains(rootText, "private void CopyFramePlanesToBuffer(");
        AssertContains(rootText, "private void ConvertYuv420pToNv12(");
        AssertContains(rootText, "private void ConvertYuv420p10leToP010(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.VideoSetup.cs")),
            "FlashbackDecoder video setup folded into decoder root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11.cs")),
            "FlashbackDecoder D3D11VA setup folded into decoder root");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_PlaybackFlowLivesWithDecoderRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "FLASHBACK_DECODER_SEEK_FALLBACK_OK");
        AssertContains(rootText, "FLASHBACK_DECODER_SEEK_CAP_HIT");
        AssertContains(rootText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(rootText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertContains(rootText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertOccursBefore(
            rootText,
            "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)",
            "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.Playback.cs")),
            "FlashbackDecoder playback flow folded into decoder root");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DecodeLoopLivesWithDecoderRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;");
        AssertContains(rootText, "public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;");
        AssertContains(rootText, "public readonly record struct PlaybackDecodePhaseTimings(");
        AssertContains(rootText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(rootText, "private void DecodeAndDeliverAudioPacket(AVPacket* packet)");
        AssertContains(rootText, "private DecodedAudioChunk ConvertAndOutputAudioFrame()");
        AssertContains(rootText, "private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)");
        AssertContains(rootText, "ffmpeg.av_read_frame(_formatCtx, _packet)");
        AssertContains(rootText, "DecodeAndDeliverAudioPacket(_packet);");

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
