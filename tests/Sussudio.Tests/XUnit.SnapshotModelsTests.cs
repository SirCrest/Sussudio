using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshots_ExposeHighConfidenceSourceTelemetryFields()
    {
        var contractsText = ReadAutomationSnapshotFamilyText();
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs");
        var diagnosticsHubText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs"),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs"),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs"),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs"),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"),
            sourceSignalProjectionText);

        Assert.Contains("public string? SourceFirmware { get; init; }", contractsText);
        Assert.Contains("public string? SourceAudioFormat { get; init; }", contractsText);
        Assert.Contains("public string? SourceAudioSampleRate { get; init; }", contractsText);
        Assert.Contains("public string? SourceInputSource { get; init; }", contractsText);
        Assert.Contains("public string? SourceUsbHostProtocol { get; init; }", contractsText);
        Assert.Contains("public string? SourceHdcpMode { get; init; }", contractsText);
        Assert.Contains("public string? SourceHdcpVersion { get; init; }", contractsText);
        Assert.Contains("public string? SourceRxTxHdcpVersion { get; init; }", contractsText);
        Assert.Contains("public string? SourceRawTimingHex { get; init; }", contractsText);

        Assert.Contains("SourceFirmware = sourceSignal.Firmware,", diagnosticsHubText);
        Assert.Contains("SourceAudioFormat = sourceSignal.AudioFormat,", diagnosticsHubText);
        Assert.Contains("SourceAudioSampleRate = sourceSignal.AudioSampleRate,", diagnosticsHubText);
        Assert.Contains("SourceInputSource = sourceSignal.InputSource,", diagnosticsHubText);
        Assert.Contains("SourceUsbHostProtocol = sourceSignal.UsbHostProtocol,", diagnosticsHubText);
        Assert.Contains("SourceHdcpMode = sourceSignal.HdcpMode,", diagnosticsHubText);
        Assert.Contains("SourceHdcpVersion = sourceSignal.HdcpVersion,", diagnosticsHubText);
        Assert.Contains("SourceRxTxHdcpVersion = sourceSignal.RxTxHdcpVersion,", diagnosticsHubText);
        Assert.Contains("SourceRawTimingHex = sourceSignal.RawTimingHex", diagnosticsHubText);
        Assert.Contains("Firmware = captureRuntime.SourceFirmware,", sourceSignalProjectionText);
        Assert.Contains("AudioFormat = captureRuntime.SourceAudioFormat,", sourceSignalProjectionText);
        Assert.Contains("RawTimingHex = captureRuntime.SourceRawTimingHex", sourceSignalProjectionText);
    }

    [Fact]
    public void Sussudio_Models_SourceSignalTelemetrySnapshot_CreateUnavailableUsesExpectedDefaults()
    {
        var asm = SussudioAssembly.Load();
        var snapshotType = asm.GetType("Sussudio.Models.SourceSignalTelemetrySnapshot", throwOnError: true)!;
        var createUnavailable = snapshotType.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!;

        var snapshot = createUnavailable.Invoke(null, new object?[] { "test-reason", null })!;

        Assert.Equal("Unavailable", GetPropertyValue(snapshot, "Availability")!.ToString());
        Assert.Equal("Unknown", GetPropertyValue(snapshot, "Origin")!.ToString());
        Assert.Equal("Unavailable", (string)GetPropertyValue(snapshot, "OriginDetail")!);
        Assert.Contains("test-reason", (string)GetPropertyValue(snapshot, "DiagnosticSummary")!);
    }

    [Fact]
    public void Sussudio_Models_SourceSignalTelemetrySnapshot_DefaultsAndShapePreserveContract()
    {
        var asm = SussudioAssembly.Load();
        var snapshotType = asm.GetType("Sussudio.Models.SourceSignalTelemetrySnapshot", throwOnError: true)!;
        var detailType = asm.GetType("Sussudio.Models.SourceTelemetryDetailEntry", throwOnError: true)!;

        AssertProperties(snapshotType, new[]
        {
            ("TimestampUtc", typeof(DateTimeOffset)),
            ("Availability", asm.GetType("Sussudio.Models.SourceTelemetryAvailability", throwOnError: true)!),
            ("Origin", asm.GetType("Sussudio.Models.SourceTelemetryOrigin", throwOnError: true)!),
            ("OriginDetail", typeof(string)),
            ("Confidence", asm.GetType("Sussudio.Models.SourceTelemetryConfidence", throwOnError: true)!),
            ("Width", typeof(int?)),
            ("Height", typeof(int?)),
            ("FrameRateExact", typeof(double?)),
            ("FrameRateArg", typeof(string)),
            ("IsHdr", typeof(bool?)),
            ("VideoFormat", typeof(string)),
            ("Colorimetry", typeof(string)),
            ("Quantization", typeof(string)),
            ("HdrTransferFunction", typeof(string)),
            ("HdrTransferCode", typeof(int?)),
            ("Firmware", typeof(string)),
            ("AudioFormat", typeof(string)),
            ("AudioSampleRate", typeof(string)),
            ("InputSource", typeof(string)),
            ("AdcOnOff", typeof(bool?)),
            ("AdcVolumeGain", typeof(int?)),
            ("AnalogGainByte", typeof(int?)),
            ("UacVolumeGain", typeof(int?)),
            ("UacOut1Mute", typeof(bool?)),
            ("UacOut2Mute", typeof(bool?)),
            ("UacOut2MixerSource", typeof(int?)),
            ("UsbHostProtocol", typeof(string)),
            ("TxEdidValid", typeof(bool?)),
            ("HdcpMode", typeof(string)),
            ("HdcpVersion", typeof(string)),
            ("RxTxHdcpVersion", typeof(string)),
            ("CustomerVersion", typeof(string)),
            ("RescueVersion", typeof(int?)),
            ("RawTimingHex", typeof(string)),
            ("DetailEntries", typeof(IReadOnlyList<>).MakeGenericType(detailType)),
            ("DiagnosticSummary", typeof(string)),
            ("EgavInitializeResultName", typeof(string)),
            ("EgavOpenResultName", typeof(string)),
            ("EgavSignalStatusResultName", typeof(string)),
            ("EgavIsVideoHdrResultName", typeof(string)),
            ("AudioInputAvailability", asm.GetType("Sussudio.Models.SourceAudioInputAvailability", throwOnError: true)!),
            ("AudioInputMode", typeof(Nullable<>).MakeGenericType(asm.GetType("Sussudio.Models.SourceAudioInputMode", throwOnError: true)!)),
            ("AudioInputOrigin", typeof(string)),
            ("HasDimensions", typeof(bool)),
            ("HasFrameRate", typeof(bool)),
            ("HasSignalData", typeof(bool))
        });
        AssertProperties(detailType, new[]
        {
            ("Group", typeof(string)),
            ("Label", typeof(string)),
            ("DisplayValue", typeof(string)),
            ("RawValue", typeof(string))
        });

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var snapshot = Activator.CreateInstance(snapshotType)!;
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var timestamp = (DateTimeOffset)GetPropertyValue(snapshot, "TimestampUtc")!;
        Assert.InRange(timestamp, before, after);
        Assert.Equal("Unknown", GetPropertyValue(snapshot, "Availability")!.ToString());
        Assert.Equal("Unknown", GetPropertyValue(snapshot, "Origin")!.ToString());
        Assert.Equal("Unknown", (string)GetPropertyValue(snapshot, "OriginDetail")!);
        Assert.Equal("Unknown", GetPropertyValue(snapshot, "Confidence")!.ToString());
        Assert.Equal("Unavailable", GetPropertyValue(snapshot, "AudioInputAvailability")!.ToString());
        Assert.Equal("not-implemented", (string)GetPropertyValue(snapshot, "AudioInputOrigin")!);
        Assert.Empty(((IEnumerable)GetPropertyValue(snapshot, "DetailEntries")!).Cast<object>());
        Assert.False((bool)GetPropertyValue(snapshot, "HasDimensions")!);
        Assert.False((bool)GetPropertyValue(snapshot, "HasFrameRate")!);
        Assert.False((bool)GetPropertyValue(snapshot, "HasSignalData")!);
        Assert.Equal(string.Empty, InvokeInstanceMethod(snapshot, "GetModeKey"));
    }

    [Fact]
    public void Sussudio_Models_SourceSignalTelemetrySnapshot_RoundTripsValuesAndJson()
    {
        var asm = SussudioAssembly.Load();
        var snapshotType = asm.GetType("Sussudio.Models.SourceSignalTelemetrySnapshot", throwOnError: true)!;
        var detailType = asm.GetType("Sussudio.Models.SourceTelemetryDetailEntry", throwOnError: true)!;
        var detailEntry = Activator.CreateInstance(detailType, "Audio / Input", "Analog Gain", "12 dB", "0C")!;
        var details = CreateGenericList(detailType, detailEntry);
        var snapshot = Activator.CreateInstance(snapshotType)!;

        SetProperty(snapshot, "Availability", ParseEnum(asm, "Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetProperty(snapshot, "Origin", ParseEnum(asm, "Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetProperty(snapshot, "OriginDetail", "NativeXuAtCommandProvider");
        SetProperty(snapshot, "Confidence", ParseEnum(asm, "Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetProperty(snapshot, "Width", 3840);
        SetProperty(snapshot, "Height", 2160);
        SetProperty(snapshot, "FrameRateExact", 120000d / 1001d);
        SetProperty(snapshot, "FrameRateArg", "120000/1001");
        SetProperty(snapshot, "IsHdr", true);
        SetProperty(snapshot, "VideoFormat", "YCbCr422");
        SetProperty(snapshot, "Colorimetry", "BT.2020");
        SetProperty(snapshot, "Quantization", "Limited");
        SetProperty(snapshot, "HdrTransferFunction", "HDR10 / PQ");
        SetProperty(snapshot, "HdrTransferCode", 2);
        SetProperty(snapshot, "Firmware", "1.2.3");
        SetProperty(snapshot, "AudioFormat", "PCM");
        SetProperty(snapshot, "AudioSampleRate", "48 kHz");
        SetProperty(snapshot, "InputSource", "HDMI");
        SetProperty(snapshot, "AdcOnOff", true);
        SetProperty(snapshot, "AdcVolumeGain", 12);
        SetProperty(snapshot, "AnalogGainByte", 0x0C);
        SetProperty(snapshot, "UacVolumeGain", 24);
        SetProperty(snapshot, "UacOut1Mute", false);
        SetProperty(snapshot, "UacOut2Mute", true);
        SetProperty(snapshot, "UacOut2MixerSource", 1);
        SetProperty(snapshot, "UsbHostProtocol", "Isochronous");
        SetProperty(snapshot, "TxEdidValid", true);
        SetProperty(snapshot, "HdcpMode", "Off");
        SetProperty(snapshot, "HdcpVersion", "0200");
        SetProperty(snapshot, "RxTxHdcpVersion", "0200/0200");
        SetProperty(snapshot, "CustomerVersion", "custom-a");
        SetProperty(snapshot, "RescueVersion", 7);
        SetProperty(snapshot, "RawTimingHex", "3000CA0830117008");
        SetProperty(snapshot, "DetailEntries", details);
        SetProperty(snapshot, "DiagnosticSummary", "ok");
        SetProperty(snapshot, "EgavInitializeResultName", "Ok");
        SetProperty(snapshot, "EgavOpenResultName", "Ok");
        SetProperty(snapshot, "EgavSignalStatusResultName", "Ok");
        SetProperty(snapshot, "EgavIsVideoHdrResultName", "Ok");
        SetProperty(snapshot, "AudioInputAvailability", ParseEnum(asm, "Sussudio.Models.SourceAudioInputAvailability", "Available"));
        SetProperty(snapshot, "AudioInputMode", ParseEnum(asm, "Sussudio.Models.SourceAudioInputMode", "Analog"));
        SetProperty(snapshot, "AudioInputOrigin", "native-xu");

        var roundTripDetail = ((IEnumerable)GetPropertyValue(snapshot, "DetailEntries")!).Cast<object>().Single();
        Assert.Equal("Available", GetPropertyValue(snapshot, "Availability")!.ToString());
        Assert.Equal("NativeXu", GetPropertyValue(snapshot, "Origin")!.ToString());
        Assert.Equal("NativeXuAtCommandProvider", (string)GetPropertyValue(snapshot, "OriginDetail")!);
        Assert.Equal(3840, (int)GetPropertyValue(snapshot, "Width")!);
        Assert.Equal("YCbCr422", (string)GetPropertyValue(snapshot, "VideoFormat")!);
        Assert.Equal("HDR10 / PQ", (string)GetPropertyValue(snapshot, "HdrTransferFunction")!);
        Assert.Equal("PCM", (string)GetPropertyValue(snapshot, "AudioFormat")!);
        Assert.True((bool)GetPropertyValue(snapshot, "AdcOnOff")!);
        Assert.Equal("0200/0200", (string)GetPropertyValue(snapshot, "RxTxHdcpVersion")!);
        Assert.Equal("Audio / Input", (string)GetPropertyValue(roundTripDetail, "Group")!);
        Assert.Equal("Analog Gain", (string)GetPropertyValue(roundTripDetail, "Label")!);
        Assert.Equal("12 dB", (string)GetPropertyValue(roundTripDetail, "DisplayValue")!);
        Assert.Equal("0C", (string)GetPropertyValue(roundTripDetail, "RawValue")!);
        Assert.Equal("Available", GetPropertyValue(snapshot, "AudioInputAvailability")!.ToString());
        Assert.Equal("Analog", GetPropertyValue(snapshot, "AudioInputMode")!.ToString());
        Assert.Equal("native-xu", (string)GetPropertyValue(snapshot, "AudioInputOrigin")!);
        Assert.True((bool)GetPropertyValue(snapshot, "HasDimensions")!);
        Assert.True((bool)GetPropertyValue(snapshot, "HasFrameRate")!);
        Assert.True((bool)GetPropertyValue(snapshot, "HasSignalData")!);
        Assert.Equal("3840x2160@120000/1001:hdr", InvokeInstanceMethod(snapshot, "GetModeKey"));

        var detailJsonRoundTrip = JsonRoundTrip(detailEntry, detailType);
        Assert.Equal("Analog Gain", (string)GetPropertyValue(detailJsonRoundTrip, "Label")!);
        var snapshotJsonRoundTrip = JsonRoundTrip(snapshot, snapshotType);
        Assert.Equal("NativeXuAtCommandProvider", (string)GetPropertyValue(snapshotJsonRoundTrip, "OriginDetail")!);
        Assert.Equal("YCbCr422", (string)GetPropertyValue(snapshotJsonRoundTrip, "VideoFormat")!);
        Assert.Equal("PCM", (string)GetPropertyValue(snapshotJsonRoundTrip, "AudioFormat")!);
        var jsonDetail = ((IEnumerable)GetPropertyValue(snapshotJsonRoundTrip, "DetailEntries")!).Cast<object>().Single();
        Assert.Equal("Analog Gain", (string)GetPropertyValue(jsonDetail, "Label")!);
    }

    private static void AssertProperties(Type type, IReadOnlyList<(string Name, Type Type)> specs)
    {
        foreach (var (name, expectedType) in specs)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.Equal(expectedType, property!.PropertyType);
        }
    }

    private static object? GetPropertyValue(object instance, string name)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance);

    private static bool GetBoolProperty(object instance, string name)
        => (bool)GetPropertyValue(instance, name)!;

    private static int GetIntProperty(object instance, string name)
        => Convert.ToInt32(GetPropertyValue(instance, name), CultureInfo.InvariantCulture);

    private static long GetLongProperty(object instance, string name)
        => Convert.ToInt64(GetPropertyValue(instance, name), CultureInfo.InvariantCulture);

    private static double GetDoubleProperty(object instance, string name)
        => Convert.ToDouble(GetPropertyValue(instance, name), CultureInfo.InvariantCulture);

    private static string GetStringProperty(object instance, string name)
        => (string)GetPropertyValue(instance, name)!;

    private static int GetCountProperty(object value)
        => value is ICollection collection
            ? collection.Count
            : ((IEnumerable)value).Cast<object>().Count();

    private static void SetProperty(object instance, string name, object? value)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.SetValue(instance, value);

    private static void SetPropertyOrBackingField(object instance, string name, object? value)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} backing field not found.");
        field.SetValue(instance, value);
    }

    private static object? InvokeInstanceMethod(object instance, string name)
        => instance.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!.Invoke(instance, Array.Empty<object>());

    private static object ParseEnum(Assembly asm, string typeName, string value)
        => Enum.Parse(asm.GetType(typeName, throwOnError: true)!, value);

    private static object ParseEnum(string typeName, string value)
        => ParseEnum(SussudioAssembly.Load(), typeName, value);

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateInstance(string typeName)
        => Activator.CreateInstance(RequireType(typeName))
           ?? throw new InvalidOperationException($"Failed to create {typeName}.");

    private static void AssertEqual<T>(T expected, object? actual, string _)
        => Assert.Equal(expected, actual);

    private static void AssertNotNull(object? value, string _)
        => Assert.NotNull(value);

    private static void AssertContains(string text, string expectedSubstring)
        => Assert.Contains(expectedSubstring, text, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string text, string unexpectedSubstring)
        => Assert.DoesNotContain(unexpectedSubstring, text, StringComparison.Ordinal);

    private static object CreateGenericList(Type itemType, params object[] items)
    {
        var listType = typeof(List<>).MakeGenericType(itemType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static object JsonRoundTrip(object instance, Type type)
    {
        var json = JsonSerializer.Serialize(instance, type);
        return JsonSerializer.Deserialize(json, type)
            ?? throw new InvalidOperationException($"{type.FullName} JSON round trip returned null.");
    }

    private static string ReadAutomationSnapshotFamilyText()
    {
        var files = Directory.EnumerateFiles(
            Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Automation"),
            "*.cs",
            SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(File.ReadAllText);

        return string.Join("\n", files).Replace("\r\n", "\n");
    }

    private static string ReadRepoFile(string relativePath)
    {
        var fullPath = Path.Combine(GetRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(fullPath).Replace("\r\n", "\n");
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            var gitPath = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private enum SnapshotSetterExpectation
    {
        InitOnly,
        None
    }

    private enum SnapshotNullability
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private sealed record SnapshotPropertySpec(
        string Name,
        Type Type,
        SnapshotSetterExpectation Setter = SnapshotSetterExpectation.InitOnly,
        SnapshotNullability Nullability = SnapshotNullability.NotApplicable,
        SnapshotNullability ElementNullability = SnapshotNullability.NotApplicable);

    private static readonly Dictionary<Type, SnapshotPropertySpec[]> SnapshotPropertySpecsByType = new();

    // LoggingJsonContext.Tests covers the production source-generated routing; this harness
    // validates the DTO reflection JSON shape because it loads the app in an isolated context.
    private static object ReflectionJsonRoundTrip(Type type, object value)
    {
        var json = JsonSerializer.Serialize(value, type);
        using var document = JsonDocument.Parse(json);
        AssertReflectionJsonPropertyNames(type, document.RootElement);
        return JsonSerializer.Deserialize(json, type)
            ?? throw new InvalidOperationException($"{type.Name} reflection JSON round-trip returned null.");
    }

    private static void AssertReflectionJsonPropertyNames(Type type, JsonElement rootElement)
    {
        if (rootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"{type.Name} reflection JSON should serialize as an object.");
        }

        var actualNames = rootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var expectedNames = GetExpectedRegisteredReflectionJsonPropertyNames(type)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var expectedName in expectedNames)
        {
            if (!actualNames.Contains(expectedName))
            {
                throw new InvalidOperationException($"{type.Name} reflection JSON missing property '{expectedName}'.");
            }
        }

        var unexpectedNames = actualNames
            .Except(expectedNames, StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (unexpectedNames.Length != 0)
        {
            throw new InvalidOperationException(
                $"{type.Name} reflection JSON emitted unexpected properties: {string.Join(", ", unexpectedNames)}.");
        }
    }

    private static IEnumerable<string> GetExpectedRegisteredReflectionJsonPropertyNames(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod == null || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            var declaringType = property.DeclaringType ?? type;
            if (!SnapshotPropertySpecsByType.TryGetValue(declaringType, out var expectedProperties))
            {
                throw new InvalidOperationException(
                    $"{type.Name}.{property.Name} reflection JSON check requires registered specs for {declaringType.Name}.");
            }

            var matchedExpectedProperty = expectedProperties.Any(
                expected => string.Equals(expected.Name, property.Name, StringComparison.Ordinal));
            if (!matchedExpectedProperty)
            {
                throw new InvalidOperationException(
                    $"{type.Name}.{property.Name} reflection JSON check was not covered by the registered {declaringType.Name} property specs.");
            }

            yield return property.Name;
        }
    }

    private static void AssertDeclaredProperties(Type type, SnapshotPropertySpec[] expectedProperties)
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .ToDictionary(property => property.Name, StringComparer.Ordinal);
        var actualNames = properties.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var expectedNames = expectedProperties.Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (!actualNames.SequenceEqual(expectedNames))
        {
            throw new InvalidOperationException(
                $"{type.Name} public property set changed. Expected: {string.Join(", ", expectedNames)}; actual: {string.Join(", ", actualNames)}.");
        }

        SnapshotPropertySpecsByType[type] = expectedProperties;
        foreach (var expected in expectedProperties)
        {
            RequireSnapshotProperty(type, expected);
        }
    }

    private static SnapshotPropertySpec NonNullString(string name)
        => new(name, typeof(string), Nullability: SnapshotNullability.NotNull);

    private static SnapshotPropertySpec NullableString(string name)
        => new(name, typeof(string), Nullability: SnapshotNullability.Nullable);

    private static SnapshotPropertySpec NonNullRef(
        string name,
        Type type,
        SnapshotNullability elementNullability = SnapshotNullability.NotApplicable)
        => new(name, type, Nullability: SnapshotNullability.NotNull, ElementNullability: elementNullability);

    private static SnapshotPropertySpec GetterOnly(string name, Type type)
        => new(name, type, SnapshotSetterExpectation.None);

    private static PropertyInfo RequireSnapshotProperty(Type type, SnapshotPropertySpec expected)
    {
        var property = type.GetProperty(expected.Name, BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(property, $"{type.Name}.{expected.Name}");
        AssertEqual(expected.Type, property!.PropertyType, $"{type.Name}.{expected.Name} property type");
        if (property.GetMethod == null || !property.GetMethod.IsPublic)
        {
            throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public getter.");
        }

        if (expected.Setter == SnapshotSetterExpectation.None)
        {
            if (property.SetMethod != null)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must not expose a setter.");
            }
        }
        else
        {
            if (property.SetMethod == null || !property.SetMethod.IsPublic)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public init setter.");
            }

            var isInitOnly = property.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
            if (!isInitOnly)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must be init-only.");
            }
        }

        if (expected.Nullability != SnapshotNullability.NotApplicable)
        {
            var nullability = new NullabilityInfoContext().Create(property);
            var expectedState = expected.Nullability == SnapshotNullability.Nullable
                ? NullabilityState.Nullable
                : NullabilityState.NotNull;
            AssertEqual(expectedState, nullability.ReadState, $"{type.Name}.{expected.Name} read nullability");
            if (expected.Setter == SnapshotSetterExpectation.InitOnly)
            {
                AssertEqual(expectedState, nullability.WriteState, $"{type.Name}.{expected.Name} write nullability");
            }

            if (expected.ElementNullability != SnapshotNullability.NotApplicable)
            {
                var elementNullability = property.PropertyType.IsArray
                    ? nullability.ElementType
                    : nullability.GenericTypeArguments.FirstOrDefault();
                if (elementNullability == null)
                {
                    throw new InvalidOperationException($"{type.Name}.{expected.Name} did not expose element nullability.");
                }

                var expectedElementState = expected.ElementNullability == SnapshotNullability.Nullable
                    ? NullabilityState.Nullable
                    : NullabilityState.NotNull;
                AssertEqual(expectedElementState, elementNullability.ReadState, $"{type.Name}.{expected.Name} element read nullability");
                AssertEqual(expectedElementState, elementNullability.WriteState, $"{type.Name}.{expected.Name} element write nullability");
            }
        }

        return property;
    }

    private static object CreateGenericList(Type elementType, object item)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType)
            ?? throw new InvalidOperationException($"Failed to create List<{elementType.Name}>.");
        listType.GetMethod("Add", new[] { elementType })!.Invoke(list, new[] { item });
        return list;
    }

    private static object GetSingleEnumerableItem(object value)
    {
        var items = ((IEnumerable)value).Cast<object>().ToArray();
        AssertEqual(1, items.Length, "IEnumerable item count");
        return items[0];
    }

    private static void AssertNonNullStringValue(
        object instance,
        string propertyName,
        string expectedValue,
        string fieldName)
    {
        var value = GetPropertyValue(instance, propertyName)
            ?? throw new InvalidOperationException($"{fieldName}: expected non-null string value.");
        AssertEqual(expectedValue, value, fieldName);
    }

}
