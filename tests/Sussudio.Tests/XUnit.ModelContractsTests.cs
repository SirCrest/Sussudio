using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class SnapshotModelsTests
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
            ("TelemetryEpoch", typeof(long)),
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
        Assert.Equal(0L, GetPropertyValue(snapshot, "TelemetryEpoch"));
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

    private static void AssertNotNull(object? value, string _)
        => Assert.NotNull(value);

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

    // XUnit.AutomationContractsTests covers the production source-generated routing; this harness
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
        Assert.Equal(expected.Type, property!.PropertyType);
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
            Assert.Equal(expectedState, nullability.ReadState);
            if (expected.Setter == SnapshotSetterExpectation.InitOnly)
            {
                Assert.Equal(expectedState, nullability.WriteState);
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
                Assert.Equal(expectedElementState, elementNullability.ReadState);
                Assert.Equal(expectedElementState, elementNullability.WriteState);
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
        Assert.Single(items);
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
        Assert.Equal(expectedValue, value);
    }

    private static readonly string[] AutomationSnapshotCpuMjpegMetricProperties =
    {
        "MjpegDecoderCount",
        "MjpegReorderSampleCount",
        "MjpegPipelineSampleCount",
        "MjpegTotalDecoded",
        "MjpegTotalEmitted",
        "MjpegTotalDropped",
        "MjpegCompressedFramesQueued",
        "MjpegCompressedFramesDequeued",
        "MjpegCompressedDropsQueueFull",
        "MjpegCompressedDropsByteBudget",
        "MjpegCompressedDropsDisposed",
        "MjpegDecodeFailures",
        "MjpegReorderCollisions",
        "MjpegEmitFailures",
        "MjpegCompressedQueueDepth",
        "MjpegCompressedQueueBytes",
        "MjpegCompressedQueueByteBudget",
        "MjpegReorderSkips",
        "MjpegReorderBufferDepth",
        "MjpegPeakReorderDepth",
        "MjpegPeakCompressedQueueBytes",
        "MjpegReorderRingForceDrops",
    };

    private static readonly string[] MjpegDecoderAutomationSnapshotProperties =
    {
        "WorkerIndex",
        "SampleCount",
        "AvgMs",
        "P95Ms",
        "MaxMs",
    };

    private static void AssertAutomationSnapshotCpuMjpegMetricContract(Type snapshotType)
    {
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        Assert.Contains("public int MjpegDecodeSampleCount { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public int MjpegDecoderCount { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder { get; init; } = Array.Empty<MjpegDecoderAutomationSnapshot>();", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public bool MjpegPreviewJitterEnabled { get; init; }", automationSnapshotText, StringComparison.Ordinal);

        foreach (var propertyName in AutomationSnapshotCpuMjpegMetricProperties)
        {
            AssertNotNull(snapshotType.GetProperty(propertyName), $"AutomationSnapshot.{propertyName}");
        }

        var decoderType = RequireType("Sussudio.Models.MjpegDecoderAutomationSnapshot");
        var perDecoderProperty = snapshotType.GetProperty("MjpegPerDecoder")
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder missing.");
        var elementType = perDecoderProperty.PropertyType.GetElementType()
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder element type missing.");
        Assert.Equal(decoderType, elementType);

        foreach (var propertyName in MjpegDecoderAutomationSnapshotProperties)
        {
            AssertNotNull(decoderType.GetProperty(propertyName), $"MjpegDecoderAutomationSnapshot.{propertyName}");
        }
    }

    private static void AssertAutomationSnapshotProperties(Type snapshotType, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            AssertNotNull(snapshotType.GetProperty(propertyName), $"AutomationSnapshot.{propertyName}");
        }
    }

    [Fact]
    public void AutomationSnapshot_ExposesCpuMjpegMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotCpuMjpegMetricContract(snapshotType);
    }

    [Fact]
    public void AutomationSnapshot_ExposesMjpegPreviewMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "MjpegPreviewJitterLastSelectedPreviewPresentId",
            "MjpegPreviewJitterLastSelectedSourceSequenceNumber",
            "MjpegPreviewJitterLastSelectedSourceLatencyMs",
            "MjpegPreviewJitterLastDroppedSourceSequenceNumber",
            "MjpegPreviewJitterClearedDropCount",
            "MjpegPreviewJitterResumeReprimeCount",
            "MjpegPreviewJitterLastDropReason",
            "MjpegPacketHashSampleCount",
            "MjpegPacketHashInputObservedFps",
            "MjpegPacketHashUniqueObservedFps",
            "MjpegPacketHashDuplicateFramePercent",
            "MjpegPacketHashPattern",
            "MjpegPacketHashRecentDuplicateFlags");
        Assert.Contains("public bool MjpegPreviewJitterEnabled { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public int MjpegPacketHashSampleCount { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public int VisualCadenceSampleCount { get; init; }", automationSnapshotText, StringComparison.Ordinal);
    }

    [Fact]
    public void AutomationSnapshot_ExposesPreviewDiagnosticsMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "PreviewD3DFrameLatencyWaitTimeoutCount",
            "PreviewD3DFrameLatencyWaitP95Ms",
            "PreviewD3DFrameLatencyWaitMaxMs",
            "PreviewD3DFrameStatsRecentMissedRefreshCount",
            "PreviewD3DFrameStatsRecentFailureCount",
            "PreviewD3DRenderThreadFailureCount",
            "PreviewD3DLastRenderThreadFailureType",
            "PreviewD3DLastRenderThreadFailureMessage",
            "PreviewD3DLastRenderThreadFailureHResult",
            "DiagnosticHealthStatus",
            "DiagnosticLikelyStage",
            "DiagnosticSummary",
            "DiagnosticEvidence",
            "DiagnosticSourceLane",
            "DiagnosticDecodeLane",
            "DiagnosticPreviewLane",
            "DiagnosticRenderLane",
            "DiagnosticPresentLane",
            "DiagnosticRecordingLane",
            "DiagnosticAudioLane",
            "PreviewPacingLikelySlowStage",
            "PreviewPacingSlowStageConfidence",
            "PreviewPacingSlowStageEvidence");
    }

    [Fact]
    public void AutomationSnapshot_ExposesCaptureCommandMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "CaptureCommandCommandsEnqueued",
            "CaptureCommandCommandsCompleted",
            "CaptureCommandCommandsFailed",
            "CaptureCommandCommandsCanceled",
            "CaptureCommandCommandsCoalesced",
            "CaptureCommandPendingCommands",
            "CaptureCommandMaxPendingCommands",
            "CaptureCommandOldestPendingCommandAgeMs",
            "CaptureCommandLastQueueLatencyMs",
            "CaptureCommandMaxQueueLatencyMs",
            "CaptureCommandLastCommand",
            "CaptureCommandLastOutcome",
            "CaptureCommandLastCorrelationId",
            "CaptureCommandLastError");
    }

    [Fact]
    public void AutomationSnapshot_ExposesCaptureCadenceMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "EstimatedPipelineLatencyMs",
            "ExpectedCaptureFrameRate",
            "CaptureCadenceSampleCount",
            "CaptureCadenceObservedFps",
            "CaptureCadenceP95IntervalMs",
            "CaptureCadenceP99IntervalMs",
            "CaptureCadenceOnePercentLowFps",
            "CaptureCadenceFivePercentLowFps",
            "CaptureCadenceRecentIntervalsMs",
            "CaptureCadenceEstimatedDroppedFrames",
            "CaptureCadenceEstimatedDropPercent");
        Assert.Contains("public long EstimatedPipelineLatencyMs { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public int MjpegDecodeSampleCount { get; init; }", automationSnapshotText, StringComparison.Ordinal);
    }

    [Fact]
    public void AutomationSnapshot_ExposesRecordingMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "RecordingVideoFramesSubmittedToEncoder",
            "RecordingVideoEncoderPts",
            "RecordingVideoEncoderPacketsWritten",
            "RecordingVideoEncoderDroppedFrames",
            "RecordingVideoSequenceGaps",
            "RecordingVideoQueueOldestFrameAgeMs",
            "RecordingVideoQueueLatencyP95Ms",
            "RecordingVideoQueueLatencyP99Ms",
            "RecordingVideoBackpressureWaitMs",
            "RecordingVideoBackpressureEvents");
    }

    [Fact]
    public void AutomationSnapshot_ExposesFlashbackRecordingMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackTotalBytesWritten",
            "FlashbackTempDriveFreeBytes",
            "FlashbackStartupCacheBudgetBytes",
            "FlashbackStartupCacheBytes",
            "FlashbackStartupCacheSessionCount",
            "FlashbackStartupCacheDeletedSessionCount",
            "FlashbackStartupCacheFreedBytes",
            "FlashbackStartupCacheOverBudget",
            "FatalCleanupInProgress",
            "FlashbackCleanupInProgress",
            "FlashbackForceRotateActive",
            "FlashbackVideoFramesSubmittedToEncoder",
            "FlashbackVideoEncoderPacketsWritten",
            "FlashbackVideoSequenceGaps",
            "FlashbackBackendSettingsStale",
            "FlashbackBackendSettingsStaleReason",
            "FlashbackBackendActiveFormat",
            "FlashbackBackendRequestedFormat",
            "FlashbackBackendActivePreset",
            "FlashbackBackendRequestedPreset",
            "FlashbackVideoQueueOldestFrameAgeMs",
            "FlashbackVideoQueueLatencyP95Ms",
            "FlashbackVideoQueueLatencyP99Ms",
            "FlashbackVideoBackpressureWaitMs",
            "FlashbackVideoBackpressureEvents",
            "FlashbackAudioQueueCapacity",
            "FlashbackVideoQueueRejectedFrames",
            "FlashbackVideoQueueLastRejectReason",
            "FlashbackGpuQueueRejectedFrames",
            "FlashbackGpuQueueLastRejectReason");
        Assert.Contains("public bool FlashbackActive { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public int FlashbackAudioQueueCapacity { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public string FlashbackPlaybackState { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public bool FlashbackExportActive { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public bool FlashbackForceRotateActive { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public long FlashbackVideoFramesSubmittedToEncoder { get; init; }", automationSnapshotText, StringComparison.Ordinal);
    }

    [Fact]
    public void AutomationSnapshot_ExposesFlashbackPlaybackMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackPlaybackThreadAlive",
            "FlashbackPlaybackDroppedFrames",
            "FlashbackPlaybackAudioMasterDelayDoubles",
            "FlashbackPlaybackAudioMasterDelayShrinks",
            "FlashbackPlaybackAudioMasterFallbacks",
            "FlashbackPlaybackSegmentSwitches",
            "FlashbackPlaybackFmp4Reopens",
            "FlashbackPlaybackWriteHeadWaits",
            "FlashbackPlaybackNearLiveSnaps",
            "FlashbackPlaybackDecodeErrorSnaps",
            "FlashbackPlaybackSubmitFailures",
            "FlashbackPlaybackLastDropUtcUnixMs",
            "FlashbackPlaybackLastDropReason",
            "FlashbackPlaybackLastSubmitFailureUtcUnixMs",
            "FlashbackPlaybackLastSubmitFailure",
            "FlashbackPlaybackLastSegmentSwitchUtcUnixMs",
            "FlashbackPlaybackLastFmp4ReopenUtcUnixMs",
            "FlashbackPlaybackLastWriteHeadWaitGapMs",
            "FlashbackPlaybackCadenceSampleCount",
            "FlashbackPlaybackP95FrameMs",
            "FlashbackPlaybackP99FrameMs",
            "FlashbackPlaybackMaxFrameMs",
            "FlashbackPlaybackSlowFrames",
            "FlashbackPlaybackSlowFramePercent",
            "FlashbackPlaybackTargetFps",
            "FlashbackPlaybackOnePercentLowFps",
            "FlashbackPlaybackPtsCadenceMismatchCount",
            "FlashbackPlaybackLastPtsCadenceDeltaMs",
            "FlashbackPlaybackLastPtsCadenceExpectedMs",
            "FlashbackPlaybackSeekForwardDecodeCapHits",
            "FlashbackPlaybackLastSeekHitForwardDecodeCap",
            "FlashbackPlaybackDecodeSampleCount",
            "FlashbackPlaybackDecodeAvgMs",
            "FlashbackPlaybackDecodeP95Ms",
            "FlashbackPlaybackDecodeP99Ms",
            "FlashbackPlaybackDecodeMaxMs",
            "FlashbackPlaybackMaxDecodePhase",
            "FlashbackPlaybackMaxDecodeReceiveMs",
            "FlashbackPlaybackMaxDecodeFeedMs",
            "FlashbackPlaybackMaxDecodeReadMs",
            "FlashbackPlaybackMaxDecodeSendMs",
            "FlashbackPlaybackMaxDecodeAudioMs",
            "FlashbackPlaybackMaxDecodeConvertMs",
            "FlashbackPlaybackMaxDecodeUtcUnixMs",
            "FlashbackPlaybackMaxDecodePositionMs",
            "CaptureCadenceP99IntervalMs",
            "CaptureCadenceOnePercentLowFps",
            "FlashbackPlaybackCommandsEnqueued",
            "FlashbackPlaybackCommandsProcessed",
            "FlashbackPlaybackCommandsDropped",
            "FlashbackPlaybackCommandsSkippedNotReady",
            "FlashbackPlaybackScrubUpdatesCoalesced",
            "FlashbackPlaybackSeekCommandsCoalesced",
            "FlashbackPlaybackCommandQueueCapacity",
            "FlashbackPlaybackPendingCommands",
            "FlashbackPlaybackMaxPendingCommands",
            "FlashbackPlaybackLastCommandQueueLatencyMs",
            "FlashbackPlaybackMaxCommandQueueLatencyMs",
            "FlashbackPlaybackMaxCommandQueueLatencyCommand",
            "FlashbackPlaybackLastCommandQueued",
            "FlashbackPlaybackLastCommandProcessed",
            "FlashbackPlaybackLastCommandQueuedUtcUnixMs",
            "FlashbackPlaybackLastCommandProcessedUtcUnixMs",
            "FlashbackPlaybackLastCommandFailureUtcUnixMs",
            "FlashbackPlaybackLastCommandFailure");
        Assert.Contains("public string FlashbackPlaybackState { get; init; } = \"N/A\";", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public double[] FlashbackPlaybackRecentFrameIntervalsMs { get; init; } = Array.Empty<double>();", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public bool FlashbackExportActive { get; init; }", automationSnapshotText, StringComparison.Ordinal);
    }

    [Fact]
    public void AutomationSnapshot_ExposesFlashbackExportMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackExportActive",
            "FlashbackExportId",
            "FlashbackExportStatus",
            "FlashbackExportOutputPath",
            "FlashbackExportStartedUtcUnixMs",
            "FlashbackExportLastProgressUtcUnixMs",
            "FlashbackExportCompletedUtcUnixMs",
            "FlashbackExportElapsedMs",
            "FlashbackExportLastProgressAgeMs",
            "FlashbackExportOutputBytes",
            "FlashbackExportThroughputBytesPerSec",
            "FlashbackExportSegmentsProcessed",
            "FlashbackExportTotalSegments",
            "FlashbackExportPercent",
            "FlashbackExportInPointMs",
            "FlashbackExportOutPointMs",
            "FlashbackExportMessage",
            "FlashbackExportFailureKind",
            "FlashbackExportForceRotateFallbacks",
            "FlashbackExportLastForceRotateFallbackUtcUnixMs",
            "FlashbackExportLastForceRotateFallbackSegments",
            "FlashbackExportLastForceRotateFallbackInPointMs",
            "FlashbackExportLastForceRotateFallbackOutPointMs",
            "LastExportId");
        Assert.Contains("public bool FlashbackExportActive { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public string FlashbackExportStatus { get; init; } = \"NotStarted\";", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public string? LastExportMessage { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public string FlashbackPlaybackState { get; init; }", automationSnapshotText, StringComparison.Ordinal);
    }

    [Fact]
    public void AutomationSnapshot_ExposesVisualCadenceMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "VisualCadenceSampleCount",
            "VisualCadenceChangeObservedFps",
            "VisualCadenceRepeatFramePercent",
            "VisualCadenceMotionConfidence",
            "VisualCadenceRecentChangeIntervalsMs",
            "VisualCenterCadenceSampleCount",
            "VisualCenterCadenceChangeObservedFps",
            "VisualCenterCadenceRepeatFramePercent",
            "VisualCenterCadenceMotionConfidence",
            "VisualCenterCadenceRecentChangeIntervalsMs");
        Assert.Contains("public int VisualCadenceSampleCount { get; init; }", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();", automationSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder", automationSnapshotText, StringComparison.Ordinal);
    }

    [Fact]
    public void AutomationOptionsSnapshot_ExposesAdvancedControlState()
    {
        var optionsType = RequireType("Sussudio.Models.AutomationOptionsSnapshot");
        var stringOptionType = RequireType("Sussudio.Models.AutomationStringOption");
        var intOptionType = RequireType("Sussudio.Models.AutomationIntOption");

        AssertNotNull(optionsType.GetProperty("Presets"), "AutomationOptionsSnapshot.Presets");
        AssertNotNull(optionsType.GetProperty("SplitEncodeModes"), "AutomationOptionsSnapshot.SplitEncodeModes");
        AssertNotNull(optionsType.GetProperty("VideoFormats"), "AutomationOptionsSnapshot.VideoFormats");
        AssertNotNull(optionsType.GetProperty("MjpegDecoderCounts"), "AutomationOptionsSnapshot.MjpegDecoderCounts");
        AssertNotNull(optionsType.GetProperty("MicrophoneDevices"), "AutomationOptionsSnapshot.MicrophoneDevices");
        AssertNotNull(optionsType.GetProperty("FlashbackBufferMinuteOptions"), "AutomationOptionsSnapshot.FlashbackBufferMinuteOptions");
        AssertNotNull(optionsType.GetProperty("SelectedPreset"), "AutomationOptionsSnapshot.SelectedPreset");
        AssertNotNull(optionsType.GetProperty("SelectedSplitEncodeMode"), "AutomationOptionsSnapshot.SelectedSplitEncodeMode");
        AssertNotNull(optionsType.GetProperty("SelectedVideoFormat"), "AutomationOptionsSnapshot.SelectedVideoFormat");
        AssertNotNull(optionsType.GetProperty("SelectedMicrophoneDeviceId"), "AutomationOptionsSnapshot.SelectedMicrophoneDeviceId");
        AssertNotNull(optionsType.GetProperty("PreviewVolumePercent"), "AutomationOptionsSnapshot.PreviewVolumePercent");
        AssertNotNull(optionsType.GetProperty("IsMicrophoneEnabled"), "AutomationOptionsSnapshot.IsMicrophoneEnabled");
        AssertNotNull(optionsType.GetProperty("MicrophoneVolumePercent"), "AutomationOptionsSnapshot.MicrophoneVolumePercent");
        AssertNotNull(optionsType.GetProperty("FlashbackBufferMinutes"), "AutomationOptionsSnapshot.FlashbackBufferMinutes");
        AssertNotNull(optionsType.GetProperty("FlashbackGpuDecode"), "AutomationOptionsSnapshot.FlashbackGpuDecode");
        AssertNotNull(optionsType.GetProperty("IsFlashbackEnabled"), "AutomationOptionsSnapshot.IsFlashbackEnabled");
        AssertNotNull(optionsType.GetProperty("IsStatsVisible"), "AutomationOptionsSnapshot.IsStatsVisible");

        var presetsProperty = optionsType.GetProperty("Presets")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.Presets missing.");
        Assert.Equal(stringOptionType, presetsProperty.PropertyType.GetElementType());

        var decoderCountsProperty = optionsType.GetProperty("MjpegDecoderCounts")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.MjpegDecoderCounts missing.");
        Assert.Equal(intOptionType, decoderCountsProperty.PropertyType.GetElementType());

        var flashbackBufferOptionsProperty = optionsType.GetProperty("FlashbackBufferMinuteOptions")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.FlashbackBufferMinuteOptions missing.");
        Assert.Equal(intOptionType, flashbackBufferOptionsProperty.PropertyType.GetElementType());

        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        AssertNotNull(snapshotType.GetProperty("SelectedVideoFormat"), "AutomationSnapshot.SelectedVideoFormat");
        AssertNotNull(snapshotType.GetProperty("PreviewVolumePercent"), "AutomationSnapshot.PreviewVolumePercent");
        AssertNotNull(snapshotType.GetProperty("IsStatsVisible"), "AutomationSnapshot.IsStatsVisible");
    }

    [Fact]
    public void CaptureDiagnosticsSnapshot_DefaultsAndRoundTripsCoreTelemetry()
    {
        var diagnosticsRootText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs");
        Assert.Contains("public class CaptureDiagnosticsSnapshot", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public SourceTelemetryAvailability SourceTelemetryAvailability { get; init; } = SourceTelemetryAvailability.Unknown;", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public bool? SourceIsHdr { get; init; }", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public int CaptureCadenceSampleCount { get; init; }", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public int RecordingVideoQueueCapacity { get; init; }", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public long AudioChunksDropped { get; init; }", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public bool FlashbackActive { get; init; }", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public bool FlashbackForceRotateActive { get; init; }", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public sealed record MjpegDecoderHealthSnapshot(", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public int MjpegDecodeSampleCount { get; init; }", diagnosticsRootText, StringComparison.Ordinal);
        Assert.Contains("public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();", diagnosticsRootText, StringComparison.Ordinal);
        Assert.DoesNotContain("partial class CaptureDiagnosticsSnapshot", diagnosticsRootText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureDiagnosticsSnapshot.cs")));
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureSnapshotModels.cs")));

        var snapshotType = RequireType("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var decoderType = RequireType("Sussudio.Models.MjpegDecoderHealthSnapshot");

        RegisterCaptureDiagnosticsSnapshotProperties(snapshotType);
        AssertDeclaredProperties(
            decoderType,
            new SnapshotPropertySpec[]
            {
                new("WorkerIndex", typeof(int)),
                new("SampleCount", typeof(int)),
                new("AvgMs", typeof(double)),
                new("P95Ms", typeof(double)),
                new("MaxMs", typeof(double))
            });

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var snapshot = CreateInstance("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        var timestamp = (DateTimeOffset)GetPropertyValue(snapshot, "TimestampUtc")!;
        if (timestamp < before || timestamp > after)
        {
            throw new InvalidOperationException("CaptureDiagnosticsSnapshot.TimestampUtc should default to current UTC time.");
        }

        Assert.Equal(0L, GetLongProperty(snapshot, "CaptureSessionEpoch"));
        Assert.Equal(0L, GetLongProperty(snapshot, "SourceTelemetryEpoch"));
        Assert.Equal(ParseEnum("Sussudio.Models.CaptureSessionState", "Uninitialized"), GetPropertyValue(snapshot, "SessionState"));
        AssertNonNullStringValue(snapshot, "RecordingBackend", "None", "CaptureDiagnosticsSnapshot.RecordingBackend default");
        AssertNonNullStringValue(snapshot, "AudioPathMode", "None", "CaptureDiagnosticsSnapshot.AudioPathMode default");
        AssertNonNullStringValue(snapshot, "MuxResult", "NotAttempted", "CaptureDiagnosticsSnapshot.MuxResult default");
        Assert.Equal(ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryAvailability"));
        Assert.Equal(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryOrigin"));
        Assert.Equal(ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryConfidence"));
        AssertNonNullStringValue(snapshot, "SourceTelemetryOriginDetail", "Unknown", "CaptureDiagnosticsSnapshot.SourceTelemetryOriginDetail default");
        AssertNonNullStringValue(snapshot, "SourceTelemetryBackend", "Unknown", "CaptureDiagnosticsSnapshot.SourceTelemetryBackend default");
        AssertNonNullStringValue(snapshot, "SourceTelemetryCircuitState", "Closed", "CaptureDiagnosticsSnapshot.SourceTelemetryCircuitState default");
        AssertNonNullStringValue(snapshot, "HdrAutoDowngradeReason", string.Empty, "CaptureDiagnosticsSnapshot.HdrAutoDowngradeReason default");
        AssertNonNullStringValue(snapshot, "MjpegPacketHashLastHash", string.Empty, "CaptureDiagnosticsSnapshot.MjpegPacketHashLastHash default");
        AssertNonNullStringValue(snapshot, "MjpegPacketHashPattern", "NoSamples", "CaptureDiagnosticsSnapshot.MjpegPacketHashPattern default");
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentInputIntervalsMs")!));
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentUniqueIntervalsMs")!));
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentDuplicateFlags")!));
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "CaptureCadenceRecentIntervalsMs")!));
        AssertNonNullStringValue(snapshot, "VisualCadenceMotionConfidence", "NoSamples", "CaptureDiagnosticsSnapshot.VisualCadenceMotionConfidence default");
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCadenceRecentOutputIntervalsMs")!));
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCadenceRecentChangeIntervalsMs")!));
        AssertNonNullStringValue(snapshot, "VisualCenterCadenceMotionConfidence", "NoSamples", "CaptureDiagnosticsSnapshot.VisualCenterCadenceMotionConfidence default");
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCenterCadenceRecentOutputIntervalsMs")!));
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCenterCadenceRecentChangeIntervalsMs")!));
        Assert.Equal(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPerDecoder")!));

        var decoder = CreateMjpegDecoderHealthSnapshot(decoderType, 1, 120, 2.1, 3.4, 5.6);
        var perDecoder = Array.CreateInstance(decoderType, 1);
        perDecoder.SetValue(decoder, 0);
        SetPropertyOrBackingField(snapshot, "CaptureSessionEpoch", 42L);
        SetPropertyOrBackingField(snapshot, "SourceTelemetryEpoch", 99L);
        SetPropertyOrBackingField(snapshot, "SessionState", ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"));
        SetPropertyOrBackingField(snapshot, "IsRecording", true);
        SetPropertyOrBackingField(snapshot, "RecordingBackend", "FFmpeg");
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", 3840u);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", 2160u);
        SetPropertyOrBackingField(snapshot, "SourceTelemetryAvailability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(snapshot, "SourceTelemetryOrigin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(snapshot, "SourceTelemetryConfidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(snapshot, "MjpegDecoderCount", 1);
        SetPropertyOrBackingField(snapshot, "MjpegPerDecoder", perDecoder);
        SetPropertyOrBackingField(snapshot, "VideoDropsQueueSaturated", 2L);
        SetPropertyOrBackingField(snapshot, "RecordingEncodingFailed", true);
        SetPropertyOrBackingField(snapshot, "RecordingEncodingFailureType", "InvalidOperationException");
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueCapacity", 360);
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueMaxDepth", 12);
        SetPropertyOrBackingField(snapshot, "RecordingVideoFramesSubmittedToEncoder", 11L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoEncoderPacketsWritten", 10L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoEncoderPts", 12L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoEncoderDroppedFrames", 1L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoSequenceGaps", 2L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueOldestFrameAgeMs", 8L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueLatencyP95Ms", 4.5);
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueLatencyP99Ms", 6.5);
        SetPropertyOrBackingField(snapshot, "RecordingVideoBackpressureWaitMs", 20L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoBackpressureEvents", 2L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoBackpressureLastWaitMs", 6L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoBackpressureMaxWaitMs", 14L);
        SetPropertyOrBackingField(snapshot, "RecordingGpuFramesDropped", 4L);
        SetPropertyOrBackingField(snapshot, "FlashbackEncodingFailed", true);
        SetPropertyOrBackingField(snapshot, "FlashbackTotalBytesWritten", 2_000_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackTempDriveFreeBytes", 1_000_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheBudgetBytes", 100_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheBytes", 120_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheSessionCount", 3);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheDeletedSessionCount", 2);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheFreedBytes", 80_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheOverBudget", true);
        SetPropertyOrBackingField(snapshot, "FatalCleanupInProgress", true);
        SetPropertyOrBackingField(snapshot, "FlashbackCleanupInProgress", true);
        SetPropertyOrBackingField(snapshot, "FlashbackForceRotateActive", true);
        SetPropertyOrBackingField(snapshot, "FlashbackForceRotateRequested", true);
        SetPropertyOrBackingField(snapshot, "FlashbackForceRotateDraining", true);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoQueueCapacity", 180);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoFramesSubmittedToEncoder", 21L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoEncoderPacketsWritten", 20L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoSequenceGaps", 3L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoQueueOldestFrameAgeMs", 9L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoQueueLatencyP95Ms", 5.5);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoQueueLatencyP99Ms", 7.5);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoBackpressureWaitMs", 30L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoBackpressureEvents", 3L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoBackpressureLastWaitMs", 7L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoBackpressureMaxWaitMs", 15L);
        SetPropertyOrBackingField(snapshot, "FlashbackGpuFramesDropped", 5L);
        SetPropertyOrBackingField(snapshot, "AudioChunksDropped", 3L);

        var roundTripDecoder = ((Array)GetPropertyValue(snapshot, "MjpegPerDecoder")!).GetValue(0)!;
        Assert.Equal(42L, GetLongProperty(snapshot, "CaptureSessionEpoch"));
        Assert.Equal(99L, GetLongProperty(snapshot, "SourceTelemetryEpoch"));
        Assert.Equal(ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"), GetPropertyValue(snapshot, "SessionState"));
        Assert.True(GetBoolProperty(snapshot, "IsRecording"));
        Assert.Equal("FFmpeg", GetStringProperty(snapshot, "RecordingBackend"));
        Assert.Equal(3840, Convert.ToInt32(GetPropertyValue(snapshot, "NegotiatedWidth")));
        Assert.Equal(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"), GetPropertyValue(snapshot, "SourceTelemetryOrigin"));
        Assert.Equal(1, GetCountProperty(GetPropertyValue(snapshot, "MjpegPerDecoder")!));
        Assert.Equal(1, GetIntProperty(roundTripDecoder, "WorkerIndex"));
        Assert.Equal(120, GetIntProperty(roundTripDecoder, "SampleCount"));
        Assert.Equal(2.1, GetDoubleProperty(roundTripDecoder, "AvgMs"));
        Assert.Equal(3.4, GetDoubleProperty(roundTripDecoder, "P95Ms"));
        Assert.Equal(5.6, GetDoubleProperty(roundTripDecoder, "MaxMs"));
        Assert.Equal(2L, GetLongProperty(snapshot, "VideoDropsQueueSaturated"));
        Assert.True(GetBoolProperty(snapshot, "RecordingEncodingFailed"));
        Assert.Equal("InvalidOperationException", GetStringProperty(snapshot, "RecordingEncodingFailureType"));
        Assert.Equal(360, GetIntProperty(snapshot, "RecordingVideoQueueCapacity"));
        Assert.Equal(12, GetIntProperty(snapshot, "RecordingVideoQueueMaxDepth"));
        Assert.Equal(11L, GetLongProperty(snapshot, "RecordingVideoFramesSubmittedToEncoder"));
        Assert.Equal(10L, GetLongProperty(snapshot, "RecordingVideoEncoderPacketsWritten"));
        Assert.Equal(12L, GetLongProperty(snapshot, "RecordingVideoEncoderPts"));
        Assert.Equal(1L, GetLongProperty(snapshot, "RecordingVideoEncoderDroppedFrames"));
        Assert.Equal(2L, GetLongProperty(snapshot, "RecordingVideoSequenceGaps"));
        Assert.Equal(8L, GetLongProperty(snapshot, "RecordingVideoQueueOldestFrameAgeMs"));
        Assert.Equal(4.5, GetDoubleProperty(snapshot, "RecordingVideoQueueLatencyP95Ms"));
        Assert.Equal(6.5, GetDoubleProperty(snapshot, "RecordingVideoQueueLatencyP99Ms"));
        Assert.Equal(20L, GetLongProperty(snapshot, "RecordingVideoBackpressureWaitMs"));
        Assert.Equal(2L, GetLongProperty(snapshot, "RecordingVideoBackpressureEvents"));
        Assert.Equal(6L, GetLongProperty(snapshot, "RecordingVideoBackpressureLastWaitMs"));
        Assert.Equal(14L, GetLongProperty(snapshot, "RecordingVideoBackpressureMaxWaitMs"));
        Assert.Equal(4L, GetLongProperty(snapshot, "RecordingGpuFramesDropped"));
        Assert.True(GetBoolProperty(snapshot, "FlashbackEncodingFailed"));
        Assert.Equal(2_000_000L, GetLongProperty(snapshot, "FlashbackTotalBytesWritten"));
        Assert.Equal(1_000_000L, GetLongProperty(snapshot, "FlashbackTempDriveFreeBytes"));
        Assert.Equal(100_000L, GetLongProperty(snapshot, "FlashbackStartupCacheBudgetBytes"));
        Assert.Equal(120_000L, GetLongProperty(snapshot, "FlashbackStartupCacheBytes"));
        Assert.Equal(3, GetIntProperty(snapshot, "FlashbackStartupCacheSessionCount"));
        Assert.Equal(2, GetIntProperty(snapshot, "FlashbackStartupCacheDeletedSessionCount"));
        Assert.Equal(80_000L, GetLongProperty(snapshot, "FlashbackStartupCacheFreedBytes"));
        Assert.True(GetBoolProperty(snapshot, "FlashbackStartupCacheOverBudget"));
        Assert.True(GetBoolProperty(snapshot, "FatalCleanupInProgress"));
        Assert.True(GetBoolProperty(snapshot, "FlashbackCleanupInProgress"));
        Assert.True(GetBoolProperty(snapshot, "FlashbackForceRotateActive"));
        Assert.True(GetBoolProperty(snapshot, "FlashbackForceRotateRequested"));
        Assert.True(GetBoolProperty(snapshot, "FlashbackForceRotateDraining"));
        Assert.Equal(180, GetIntProperty(snapshot, "FlashbackVideoQueueCapacity"));
        Assert.Equal(21L, GetLongProperty(snapshot, "FlashbackVideoFramesSubmittedToEncoder"));
        Assert.Equal(20L, GetLongProperty(snapshot, "FlashbackVideoEncoderPacketsWritten"));
        Assert.Equal(3L, GetLongProperty(snapshot, "FlashbackVideoSequenceGaps"));
        Assert.Equal(9L, GetLongProperty(snapshot, "FlashbackVideoQueueOldestFrameAgeMs"));
        Assert.Equal(5.5, GetDoubleProperty(snapshot, "FlashbackVideoQueueLatencyP95Ms"));
        Assert.Equal(7.5, GetDoubleProperty(snapshot, "FlashbackVideoQueueLatencyP99Ms"));
        Assert.Equal(30L, GetLongProperty(snapshot, "FlashbackVideoBackpressureWaitMs"));
        Assert.Equal(3L, GetLongProperty(snapshot, "FlashbackVideoBackpressureEvents"));
        Assert.Equal(7L, GetLongProperty(snapshot, "FlashbackVideoBackpressureLastWaitMs"));
        Assert.Equal(15L, GetLongProperty(snapshot, "FlashbackVideoBackpressureMaxWaitMs"));
        Assert.Equal(5L, GetLongProperty(snapshot, "FlashbackGpuFramesDropped"));
        Assert.Equal(3L, GetLongProperty(snapshot, "AudioChunksDropped"));
        var decoderJsonRoundTrip = ReflectionJsonRoundTrip(decoderType, decoder);
        Assert.Equal(120, GetIntProperty(decoderJsonRoundTrip, "SampleCount"));
        var jsonRoundTrip = ReflectionJsonRoundTrip(snapshotType, snapshot);
        Assert.Equal(42L, GetLongProperty(jsonRoundTrip, "CaptureSessionEpoch"));
        Assert.Equal(99L, GetLongProperty(jsonRoundTrip, "SourceTelemetryEpoch"));
        Assert.Equal("FFmpeg", GetStringProperty(jsonRoundTrip, "RecordingBackend"));
        Assert.True(GetBoolProperty(jsonRoundTrip, "RecordingEncodingFailed"));
        Assert.Equal(2_000_000L, GetLongProperty(jsonRoundTrip, "FlashbackTotalBytesWritten"));
        Assert.Equal(120_000L, GetLongProperty(jsonRoundTrip, "FlashbackStartupCacheBytes"));
        Assert.True(GetBoolProperty(jsonRoundTrip, "FatalCleanupInProgress"));
        Assert.True(GetBoolProperty(jsonRoundTrip, "FlashbackCleanupInProgress"));
        Assert.True(GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateActive"));
        Assert.True(GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateRequested"));
        Assert.True(GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateDraining"));
        Assert.Equal(180, GetIntProperty(jsonRoundTrip, "FlashbackVideoQueueCapacity"));
        Assert.Equal(1, GetCountProperty(GetPropertyValue(jsonRoundTrip, "MjpegPerDecoder")!));
        Assert.Equal(1, GetIntProperty(GetSingleEnumerableItem(GetPropertyValue(jsonRoundTrip, "MjpegPerDecoder")!), "WorkerIndex"));

    }

    private static void RegisterCaptureDiagnosticsSnapshotProperties(Type snapshotType)
    {
        var decoderType = RequireType("Sussudio.Models.MjpegDecoderHealthSnapshot");
        var sessionStateType = RequireType("Sussudio.Models.CaptureSessionState");
        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");
        var originType = RequireType("Sussudio.Models.SourceTelemetryOrigin");
        var confidenceType = RequireType("Sussudio.Models.SourceTelemetryConfidence");

        AssertDeclaredProperties(
            snapshotType,
            new SnapshotPropertySpec[]
            {
                new("TimestampUtc", typeof(DateTimeOffset)),
                new("CaptureSessionEpoch", typeof(long)),
                new("SourceTelemetryEpoch", typeof(long)),
                new("SessionState", sessionStateType),
                new("IsRecording", typeof(bool)),
                NonNullString("RecordingBackend"),
                NonNullString("AudioPathMode"),
                NonNullString("MuxResult"),
                new("FlashbackActive", typeof(bool)),
                new("FlashbackBufferedDurationMs", typeof(long)),
                new("FlashbackSegmentCount", typeof(int)),
                new("FlashbackDiskBytes", typeof(long)),
                new("FlashbackTotalBytesWritten", typeof(long)),
                new("FlashbackTempDriveFreeBytes", typeof(long)),
                new("FlashbackStartupCacheBudgetBytes", typeof(long)),
                new("FlashbackStartupCacheBytes", typeof(long)),
                new("FlashbackStartupCacheSessionCount", typeof(int)),
                new("FlashbackStartupCacheDeletedSessionCount", typeof(int)),
                new("FlashbackStartupCacheFreedBytes", typeof(long)),
                new("FlashbackStartupCacheOverBudget", typeof(bool)),
                new("RecordingElapsedMs", typeof(long)),
                new("LastFrameArrivalMs", typeof(long)),
                new("EstimatedPipelineLatencyMs", typeof(long)),
                new("ExpectedFrameRate", typeof(double)),
                new("NegotiatedWidth", typeof(uint?)),
                new("NegotiatedHeight", typeof(uint?)),
                new("NegotiatedFrameRate", typeof(double?)),
                NullableString("NegotiatedFrameRateArg"),
                new("NegotiatedFrameRateNumerator", typeof(uint?)),
                new("NegotiatedFrameRateDenominator", typeof(uint?)),
                NullableString("NegotiatedPixelFormat"),
                NullableString("RequestedReaderSubtype"),
                NullableString("ReaderSourceStreamType"),
                NullableString("ReaderSourceSubtype"),
                NullableString("FirstObservedFramePixelFormat"),
                NullableString("LatestObservedFramePixelFormat"),
                new("ObservedP010FrameCount", typeof(long)),
                new("ObservedNv12FrameCount", typeof(long)),
                new("ObservedOtherFrameCount", typeof(long)),
                new("SourceTelemetryAvailability", availabilityType),
                new("SourceTelemetryOrigin", originType),
                new("SourceTelemetryConfidence", confidenceType),
                NonNullString("SourceTelemetryOriginDetail"),
                NullableString("SourceTelemetryDiagnosticSummary"),
                new("SourceTelemetryTimestampUtc", typeof(DateTimeOffset?)),
                NonNullString("SourceTelemetryBackend"),
                new("SourceTelemetrySuppressed", typeof(bool)),
                NullableString("SourceTelemetrySuppressedReason"),
                NonNullString("SourceTelemetryCircuitState"),
                new("SourceWidth", typeof(int?)),
                new("SourceHeight", typeof(int?)),
                new("SourceFrameRateExact", typeof(double?)),
                NullableString("SourceFrameRateArg"),
                new("SourceIsHdr", typeof(bool?)),
                new("HdrAutoDowngraded", typeof(bool)),
                NonNullString("HdrAutoDowngradeReason"),
                new("CaptureCadenceSampleCount", typeof(int)),
                new("CaptureCadenceObservedFps", typeof(double)),
                new("CaptureCadenceExpectedIntervalMs", typeof(double)),
                new("CaptureCadenceAverageIntervalMs", typeof(double)),
                new("CaptureCadenceP95IntervalMs", typeof(double)),
                new("CaptureCadenceP99IntervalMs", typeof(double)),
                new("CaptureCadenceMaxIntervalMs", typeof(double)),
                new("CaptureCadenceOnePercentLowFps", typeof(double)),
                new("CaptureCadenceFivePercentLowFps", typeof(double)),
                new("CaptureCadenceSampleDurationMs", typeof(double)),
                new("CaptureCadenceRecentIntervalsMs", typeof(double[])),
                new("CaptureCadenceJitterStdDevMs", typeof(double)),
                new("CaptureCadenceSevereGapCount", typeof(long)),
                new("CaptureCadenceEstimatedDroppedFrames", typeof(long)),
                new("CaptureCadenceEstimatedDropPercent", typeof(double)),
                new("MjpegDecodeSampleCount", typeof(int)),
                new("MjpegDecodeAvgMs", typeof(double)),
                new("MjpegDecodeP95Ms", typeof(double)),
                new("MjpegDecodeMaxMs", typeof(double)),
                new("MjpegInteropCopySampleCount", typeof(int)),
                new("MjpegInteropCopyAvgMs", typeof(double)),
                new("MjpegInteropCopyP95Ms", typeof(double)),
                new("MjpegInteropCopyMaxMs", typeof(double)),
                new("MjpegCallbackSampleCount", typeof(int)),
                new("MjpegCallbackAvgMs", typeof(double)),
                new("MjpegCallbackP95Ms", typeof(double)),
                new("MjpegCallbackMaxMs", typeof(double)),
                new("MjpegDecoderCount", typeof(int)),
                new("MjpegReorderSampleCount", typeof(int)),
                new("MjpegReorderAvgMs", typeof(double)),
                new("MjpegReorderP95Ms", typeof(double)),
                new("MjpegReorderMaxMs", typeof(double)),
                new("MjpegPipelineSampleCount", typeof(int)),
                new("MjpegPipelineAvgMs", typeof(double)),
                new("MjpegPipelineP95Ms", typeof(double)),
                new("MjpegPipelineMaxMs", typeof(double)),
                new("MjpegTotalDecoded", typeof(long)),
                new("MjpegTotalEmitted", typeof(long)),
                new("MjpegTotalDropped", typeof(long)),
                new("MjpegCompressedFramesQueued", typeof(long)),
                new("MjpegCompressedFramesDequeued", typeof(long)),
                new("MjpegCompressedDropsQueueFull", typeof(long)),
                new("MjpegCompressedDropsByteBudget", typeof(long)),
                new("MjpegCompressedDropsDisposed", typeof(long)),
                new("MjpegDecodeFailures", typeof(long)),
                new("MjpegReorderCollisions", typeof(long)),
                new("MjpegEmitFailures", typeof(long)),
                new("MjpegCompressedQueueDepth", typeof(int)),
                new("MjpegCompressedQueueBytes", typeof(long)),
                new("MjpegCompressedQueueByteBudget", typeof(long)),
                new("MjpegReorderSkips", typeof(long)),
                new("MjpegReorderBufferDepth", typeof(int)),
                new("MjpegPeakReorderDepth", typeof(int)),
                new("MjpegPeakCompressedQueueBytes", typeof(long)),
                new("MjpegReorderRingForceDrops", typeof(long)),
                new("MjpegPreviewJitterEnabled", typeof(bool)),
                new("MjpegPreviewJitterTargetDepth", typeof(int)),
                new("MjpegPreviewJitterMaxDepth", typeof(int)),
                new("MjpegPreviewJitterQueueDepth", typeof(int)),
                new("MjpegPreviewJitterTotalQueued", typeof(long)),
                new("MjpegPreviewJitterTotalSubmitted", typeof(long)),
                new("MjpegPreviewJitterTotalDropped", typeof(long)),
                new("MjpegPreviewJitterUnderflowCount", typeof(long)),
                new("MjpegPreviewJitterResumeReprimeCount", typeof(long)),
                new("MjpegPreviewJitterInputSampleCount", typeof(int)),
                new("MjpegPreviewJitterInputAvgMs", typeof(double)),
                new("MjpegPreviewJitterInputP95Ms", typeof(double)),
                new("MjpegPreviewJitterInputMaxMs", typeof(double)),
                new("MjpegPreviewJitterOutputSampleCount", typeof(int)),
                new("MjpegPreviewJitterOutputAvgMs", typeof(double)),
                new("MjpegPreviewJitterOutputP95Ms", typeof(double)),
                new("MjpegPreviewJitterOutputMaxMs", typeof(double)),
                new("MjpegPreviewJitterLatencySampleCount", typeof(int)),
                new("MjpegPreviewJitterLatencyAvgMs", typeof(double)),
                new("MjpegPreviewJitterLatencyP95Ms", typeof(double)),
                new("MjpegPreviewJitterLatencyMaxMs", typeof(double)),
                new("MjpegPreviewJitterDeadlineDropCount", typeof(long)),
                new("MjpegPreviewJitterClearedDropCount", typeof(long)),
                new("MjpegPreviewJitterTargetIncreaseCount", typeof(long)),
                new("MjpegPreviewJitterTargetDecreaseCount", typeof(long)),
                new("MjpegPreviewJitterLastSelectedPreviewPresentId", typeof(long)),
                new("MjpegPreviewJitterLastSelectedSourceSequenceNumber", typeof(long)),
                new("MjpegPreviewJitterLastSelectedQpc", typeof(long)),
                new("MjpegPreviewJitterLastSelectedSourceLatencyMs", typeof(double)),
                new("MjpegPreviewJitterLastDroppedSourceSequenceNumber", typeof(long)),
                new("MjpegPreviewJitterLastDropQpc", typeof(long)),
                NonNullString("MjpegPreviewJitterLastDropReason"),
                new("MjpegPreviewJitterLastUnderflowQpc", typeof(long)),
                NonNullString("MjpegPreviewJitterLastUnderflowReason"),
                new("MjpegPreviewJitterLastUnderflowQueueDepth", typeof(int)),
                new("MjpegPreviewJitterLastUnderflowInputAgeMs", typeof(double)),
                new("MjpegPreviewJitterLastUnderflowOutputAgeMs", typeof(double)),
                new("MjpegPreviewJitterLastScheduleLateMs", typeof(double)),
                new("MjpegPreviewJitterMaxScheduleLateMs", typeof(double)),
                new("MjpegPreviewJitterScheduleLateCount", typeof(long)),
                new("MjpegPacketHashSampleCount", typeof(int)),
                new("MjpegPacketHashUniqueFrameCount", typeof(long)),
                new("MjpegPacketHashDuplicateFrameCount", typeof(long)),
                new("MjpegPacketHashLongestDuplicateRun", typeof(long)),
                new("MjpegPacketHashInputObservedFps", typeof(double)),
                new("MjpegPacketHashUniqueObservedFps", typeof(double)),
                new("MjpegPacketHashDuplicateFramePercent", typeof(double)),
                NonNullString("MjpegPacketHashLastHash"),
                new("MjpegPacketHashLastFrameDuplicate", typeof(bool)),
                NonNullString("MjpegPacketHashPattern"),
                NonNullRef("MjpegPacketHashRecentInputIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("MjpegPacketHashRecentUniqueIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("MjpegPacketHashRecentDuplicateFlags", typeof(int[]), SnapshotNullability.NotNull),
                new("VisualCadenceSampleCount", typeof(int)),
                new("VisualCadenceChangedFrameCount", typeof(long)),
                new("VisualCadenceRepeatFrameCount", typeof(long)),
                new("VisualCadenceLongestRepeatRun", typeof(long)),
                new("VisualCadenceOutputObservedFps", typeof(double)),
                new("VisualCadenceChangeObservedFps", typeof(double)),
                new("VisualCadenceRepeatFramePercent", typeof(double)),
                new("VisualCadenceLastDelta", typeof(double)),
                new("VisualCadenceAverageDelta", typeof(double)),
                new("VisualCadenceP95Delta", typeof(double)),
                new("VisualCadenceMotionScore", typeof(double)),
                NonNullString("VisualCadenceMotionConfidence"),
                NonNullRef("VisualCadenceRecentOutputIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("VisualCadenceRecentChangeIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                new("VisualCenterCadenceSampleCount", typeof(int)),
                new("VisualCenterCadenceChangedFrameCount", typeof(long)),
                new("VisualCenterCadenceRepeatFrameCount", typeof(long)),
                new("VisualCenterCadenceLongestRepeatRun", typeof(long)),
                new("VisualCenterCadenceOutputObservedFps", typeof(double)),
                new("VisualCenterCadenceChangeObservedFps", typeof(double)),
                new("VisualCenterCadenceRepeatFramePercent", typeof(double)),
                new("VisualCenterCadenceLastDelta", typeof(double)),
                new("VisualCenterCadenceAverageDelta", typeof(double)),
                new("VisualCenterCadenceP95Delta", typeof(double)),
                new("VisualCenterCadenceMotionScore", typeof(double)),
                NonNullString("VisualCenterCadenceMotionConfidence"),
                NonNullRef("VisualCenterCadenceRecentOutputIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("VisualCenterCadenceRecentChangeIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("MjpegPerDecoder", decoderType.MakeArrayType(), SnapshotNullability.NotNull),
                new("ConversionQueueDepth", typeof(int)),
                new("FfmpegVideoQueueDepth", typeof(int)),
                new("FfmpegAudioQueueDepth", typeof(int)),
                new("VideoFramesArrived", typeof(long)),
                new("VideoFramesQueued", typeof(long)),
                new("VideoFramesDropped", typeof(long)),
                new("VideoFramesDroppedBacklog", typeof(long)),
                new("VideoFramesConverted", typeof(long)),
                new("VideoFramesEnqueued", typeof(long)),
                new("VideoDropsQueueSaturated", typeof(long)),
                new("VideoDropsBacklogEviction", typeof(long)),
                new("RecordingEncodingFailed", typeof(bool)),
                NullableString("RecordingEncodingFailureType"),
                NullableString("RecordingEncodingFailureMessage"),
                new("RecordingVideoQueueCapacity", typeof(int)),
                new("RecordingVideoQueueMaxDepth", typeof(int)),
                new("RecordingVideoFramesSubmittedToEncoder", typeof(long)),
                new("RecordingVideoEncoderPts", typeof(long)),
                new("RecordingVideoEncoderPacketsWritten", typeof(long)),
                new("RecordingVideoEncoderDroppedFrames", typeof(long)),
                new("RecordingVideoSequenceGaps", typeof(long)),
                new("RecordingVideoQueueOldestFrameAgeMs", typeof(long)),
                new("RecordingVideoQueueLastLatencyMs", typeof(long)),
                new("RecordingVideoQueueLatencySampleCount", typeof(int)),
                new("RecordingVideoQueueLatencyAvgMs", typeof(double)),
                new("RecordingVideoQueueLatencyP95Ms", typeof(double)),
                new("RecordingVideoQueueLatencyP99Ms", typeof(double)),
                new("RecordingVideoQueueLatencyMaxMs", typeof(double)),
                new("RecordingVideoBackpressureWaitMs", typeof(long)),
                new("RecordingVideoBackpressureEvents", typeof(long)),
                new("RecordingVideoBackpressureLastWaitMs", typeof(long)),
                new("RecordingVideoBackpressureMaxWaitMs", typeof(long)),
                new("RecordingGpuQueueDepth", typeof(int)),
                new("RecordingGpuQueueCapacity", typeof(int)),
                new("RecordingGpuQueueMaxDepth", typeof(int)),
                new("RecordingGpuFramesEnqueued", typeof(long)),
                new("RecordingGpuFramesDropped", typeof(long)),
                new("RecordingCudaQueueDepth", typeof(int)),
                new("RecordingCudaQueueCapacity", typeof(int)),
                new("RecordingCudaQueueMaxDepth", typeof(int)),
                new("RecordingCudaFramesEnqueued", typeof(long)),
                new("RecordingCudaFramesDropped", typeof(long)),
                new("FlashbackEncodingFailed", typeof(bool)),
                NullableString("FlashbackEncodingFailureType"),
                NullableString("FlashbackEncodingFailureMessage"),
                new("FatalCleanupInProgress", typeof(bool)),
                new("FlashbackCleanupInProgress", typeof(bool)),
                new("FlashbackForceRotateActive", typeof(bool)),
                new("FlashbackForceRotateRequested", typeof(bool)),
                new("FlashbackForceRotateDraining", typeof(bool)),
                new("FlashbackVideoQueueCapacity", typeof(int)),
                new("FlashbackVideoQueueMaxDepth", typeof(int)),
                new("FlashbackVideoFramesSubmittedToEncoder", typeof(long)),
                new("FlashbackVideoEncoderPts", typeof(long)),
                new("FlashbackVideoEncoderPacketsWritten", typeof(long)),
                new("FlashbackVideoEncoderDroppedFrames", typeof(long)),
                new("FlashbackVideoSequenceGaps", typeof(long)),
                new("FlashbackVideoQueueRejectedFrames", typeof(long)),
                NonNullString("FlashbackVideoQueueLastRejectReason"),
                new("FlashbackVideoQueueOldestFrameAgeMs", typeof(long)),
                new("FlashbackVideoQueueLastLatencyMs", typeof(long)),
                new("FlashbackVideoQueueLatencySampleCount", typeof(int)),
                new("FlashbackVideoQueueLatencyAvgMs", typeof(double)),
                new("FlashbackVideoQueueLatencyP95Ms", typeof(double)),
                new("FlashbackVideoQueueLatencyP99Ms", typeof(double)),
                new("FlashbackVideoQueueLatencyMaxMs", typeof(double)),
                new("FlashbackVideoBackpressureWaitMs", typeof(long)),
                new("FlashbackVideoBackpressureEvents", typeof(long)),
                new("FlashbackVideoBackpressureLastWaitMs", typeof(long)),
                new("FlashbackVideoBackpressureMaxWaitMs", typeof(long)),
                new("FlashbackGpuQueueDepth", typeof(int)),
                new("FlashbackGpuQueueCapacity", typeof(int)),
                new("FlashbackGpuQueueMaxDepth", typeof(int)),
                new("FlashbackGpuFramesEnqueued", typeof(long)),
                new("FlashbackGpuFramesDropped", typeof(long)),
                new("FlashbackGpuQueueRejectedFrames", typeof(long)),
                NonNullString("FlashbackGpuQueueLastRejectReason"),
                new("AudioDropsQueueSaturated", typeof(long)),
                new("AudioDropsBacklogEviction", typeof(long)),
                new("AudioChunksDropped", typeof(long))
            });
    }

    private static object CreateMjpegDecoderHealthSnapshot(
        Type decoderType,
        int workerIndex,
        int sampleCount,
        double avgMs,
        double p95Ms,
        double maxMs)
        => Activator.CreateInstance(decoderType, workerIndex, sampleCount, avgMs, p95Ms, maxMs)
           ?? throw new InvalidOperationException("Failed to create MjpegDecoderHealthSnapshot.");

    private static SnapshotPropertySpec[] CaptureHealthSnapshotPropertySpecs(Type detailType)
    {
        return new SnapshotPropertySpec[]
        {
            new("FlashbackOutputBytes", typeof(long)),
            NullableString("FlashbackFilePath"),
            new("FlashbackEncodedFrames", typeof(long)),
            new("FlashbackDroppedFrames", typeof(long)),
            new("FlashbackGpuEncoding", typeof(bool)),
            new("FlashbackBackendSettingsStale", typeof(bool)),
            NonNullString("FlashbackBackendSettingsStaleReason"),
            NonNullString("FlashbackBackendActiveFormat"),
            NonNullString("FlashbackBackendRequestedFormat"),
            NonNullString("FlashbackBackendActivePreset"),
            NonNullString("FlashbackBackendRequestedPreset"),
            NullableString("EncoderCodecName"),
            new("EncoderTargetBitRate", typeof(uint)),
            new("EncoderWidth", typeof(int)),
            new("EncoderHeight", typeof(int)),
            new("EncoderFrameRate", typeof(double)),
            new("EncoderFrameRateNumerator", typeof(int?)),
            new("EncoderFrameRateDenominator", typeof(int?)),
            new("FlashbackVideoQueueDepth", typeof(int)),
            new("FlashbackAudioQueueDepth", typeof(int)),
            new("FlashbackAudioQueueCapacity", typeof(int)),
            NonNullString("FlashbackPlaybackState"),
            new("FlashbackPlaybackPositionMs", typeof(long)),
            NonNullString("FlashbackDecoderHwAccel"),
            new("FlashbackPlaybackFrameCount", typeof(long)),
            new("FlashbackPlaybackLateFrames", typeof(long)),
            new("FlashbackPlaybackDroppedFrames", typeof(long)),
            new("FlashbackPlaybackAudioMasterDelayDoubles", typeof(long)),
            new("FlashbackPlaybackAudioMasterDelayShrinks", typeof(long)),
            new("FlashbackPlaybackAudioMasterFallbacks", typeof(long)),
            new("FlashbackPlaybackAudioMasterUnavailableFallbacks", typeof(long)),
            new("FlashbackPlaybackAudioMasterStaleFallbacks", typeof(long)),
            new("FlashbackPlaybackAudioMasterDriftOutlierFallbacks", typeof(long)),
            NonNullString("FlashbackPlaybackAudioMasterLastFallbackReason"),
            new("FlashbackPlaybackAudioMasterLastFallbackDriftMs", typeof(double)),
            new("FlashbackPlaybackAudioMasterLastFallbackClockAgeMs", typeof(double)),
            new("FlashbackPlaybackSegmentSwitches", typeof(long)),
            new("FlashbackPlaybackFmp4Reopens", typeof(long)),
            new("FlashbackPlaybackWriteHeadWaits", typeof(long)),
            new("FlashbackPlaybackNearLiveSnaps", typeof(long)),
            new("FlashbackPlaybackDecodeErrorSnaps", typeof(long)),
            new("FlashbackPlaybackSubmitFailures", typeof(long)),
            new("FlashbackPlaybackLastDropUtcUnixMs", typeof(long)),
            NonNullString("FlashbackPlaybackLastDropReason"),
            new("FlashbackPlaybackLastSubmitFailureUtcUnixMs", typeof(long)),
            NonNullString("FlashbackPlaybackLastSubmitFailure"),
            new("FlashbackPlaybackLastSegmentSwitchUtcUnixMs", typeof(long)),
            new("FlashbackPlaybackLastFmp4ReopenUtcUnixMs", typeof(long)),
            new("FlashbackPlaybackLastWriteHeadWaitGapMs", typeof(long)),
            new("FlashbackPlaybackTargetFps", typeof(double)),
            new("FlashbackPlaybackObservedFps", typeof(double)),
            new("FlashbackPlaybackAvgFrameMs", typeof(double)),
            new("FlashbackPlaybackCadenceSampleCount", typeof(int)),
            new("FlashbackPlaybackP95FrameMs", typeof(double)),
            new("FlashbackPlaybackP99FrameMs", typeof(double)),
            new("FlashbackPlaybackMaxFrameMs", typeof(double)),
            new("FlashbackPlaybackSlowFrames", typeof(long)),
            new("FlashbackPlaybackSlowFramePercent", typeof(double)),
            new("FlashbackPlaybackOnePercentLowFps", typeof(double)),
            new("FlashbackPlaybackFivePercentLowFps", typeof(double)),
            new("FlashbackPlaybackSampleDurationMs", typeof(double)),
            new("FlashbackPlaybackRecentFrameIntervalsMs", typeof(double[])),
            new("FlashbackPlaybackPtsCadenceMismatchCount", typeof(long)),
            new("FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs", typeof(long)),
            new("FlashbackPlaybackLastPtsCadenceDeltaMs", typeof(double)),
            new("FlashbackPlaybackLastPtsCadenceExpectedMs", typeof(double)),
            new("FlashbackPlaybackSeekForwardDecodeCapHits", typeof(long)),
            new("FlashbackPlaybackLastSeekHitForwardDecodeCap", typeof(bool)),
            new("FlashbackPlaybackDecodeSampleCount", typeof(int)),
            new("FlashbackPlaybackDecodeAvgMs", typeof(double)),
            new("FlashbackPlaybackDecodeP95Ms", typeof(double)),
            new("FlashbackPlaybackDecodeP99Ms", typeof(double)),
            new("FlashbackPlaybackDecodeMaxMs", typeof(double)),
            NonNullString("FlashbackPlaybackMaxDecodePhase"),
            new("FlashbackPlaybackMaxDecodeReceiveMs", typeof(double)),
            new("FlashbackPlaybackMaxDecodeFeedMs", typeof(double)),
            new("FlashbackPlaybackMaxDecodeReadMs", typeof(double)),
            new("FlashbackPlaybackMaxDecodeSendMs", typeof(double)),
            new("FlashbackPlaybackMaxDecodeAudioMs", typeof(double)),
            new("FlashbackPlaybackMaxDecodeConvertMs", typeof(double)),
            new("FlashbackPlaybackMaxDecodeUtcUnixMs", typeof(long)),
            new("FlashbackPlaybackMaxDecodePositionMs", typeof(long)),
            new("FlashbackAvDriftMs", typeof(double)),
            new("FlashbackPlaybackThreadAlive", typeof(bool)),
            new("FlashbackPlaybackCommandsEnqueued", typeof(long)),
            new("FlashbackPlaybackCommandsProcessed", typeof(long)),
            new("FlashbackPlaybackCommandsDropped", typeof(long)),
            new("FlashbackPlaybackCommandsSkippedNotReady", typeof(long)),
            new("FlashbackPlaybackScrubUpdatesCoalesced", typeof(long)),
            new("FlashbackPlaybackSeekCommandsCoalesced", typeof(long)),
            new("FlashbackPlaybackCommandQueueCapacity", typeof(int)),
            new("FlashbackPlaybackPendingCommands", typeof(int)),
            new("FlashbackPlaybackMaxPendingCommands", typeof(int)),
            new("FlashbackPlaybackLastCommandQueueLatencyMs", typeof(long)),
            new("FlashbackPlaybackMaxCommandQueueLatencyMs", typeof(long)),
            NonNullString("FlashbackPlaybackMaxCommandQueueLatencyCommand"),
            NonNullString("FlashbackPlaybackLastCommandQueued"),
            NonNullString("FlashbackPlaybackLastCommandProcessed"),
            new("FlashbackPlaybackLastCommandQueuedUtcUnixMs", typeof(long)),
            new("FlashbackPlaybackLastCommandProcessedUtcUnixMs", typeof(long)),
            new("FlashbackPlaybackLastCommandFailureUtcUnixMs", typeof(long)),
            NonNullString("FlashbackPlaybackLastCommandFailure"),
            new("FlashbackExportActive", typeof(bool)),
            new("FlashbackExportId", typeof(long)),
            NonNullString("FlashbackExportStatus"),
            NonNullString("FlashbackExportOutputPath"),
            new("FlashbackExportStartedUtcUnixMs", typeof(long)),
            new("FlashbackExportLastProgressUtcUnixMs", typeof(long)),
            new("FlashbackExportCompletedUtcUnixMs", typeof(long)),
            new("FlashbackExportElapsedMs", typeof(long)),
            new("FlashbackExportLastProgressAgeMs", typeof(long)),
            new("FlashbackExportOutputBytes", typeof(long)),
            new("FlashbackExportThroughputBytesPerSec", typeof(double)),
            new("FlashbackExportSegmentsProcessed", typeof(int)),
            new("FlashbackExportTotalSegments", typeof(int)),
            new("FlashbackExportPercent", typeof(double)),
            new("FlashbackExportInPointMs", typeof(long)),
            new("FlashbackExportOutPointMs", typeof(long)),
            NonNullString("FlashbackExportMessage"),
            NonNullString("FlashbackExportFailureKind"),
            new("FlashbackExportForceRotateFallbacks", typeof(long)),
            new("FlashbackExportLastForceRotateFallbackUtcUnixMs", typeof(long)),
            new("FlashbackExportLastForceRotateFallbackSegments", typeof(int)),
            new("FlashbackExportLastForceRotateFallbackInPointMs", typeof(long)),
            new("FlashbackExportLastForceRotateFallbackOutPointMs", typeof(long)),
            NullableString("FlashbackExportVerificationFormat"),
            NullableString("FlashbackCodecDowngradeReason"),
            new("LastExportId", typeof(long)),
            NullableString("LastExportPath"),
            new("LastExportSuccess", typeof(bool?)),
            NullableString("LastExportMessage"),
            NullableString("SourceVideoFormat"),
            NullableString("SourceColorimetry"),
            NullableString("SourceQuantization"),
            NullableString("SourceHdrTransferFunction"),
            new("SourceHdrTransferCode", typeof(int?)),
            NullableString("SourceFirmware"),
            NullableString("SourceAudioFormat"),
            NullableString("SourceAudioSampleRate"),
            NullableString("SourceInputSource"),
            NullableString("SourceUsbHostProtocol"),
            NullableString("SourceHdcpMode"),
            NullableString("SourceHdcpVersion"),
            NullableString("SourceRxTxHdcpVersion"),
            NullableString("SourceRawTimingHex"),
            NonNullRef("SourceTelemetryDetails", typeof(IReadOnlyList<>).MakeGenericType(detailType), SnapshotNullability.NotNull),
            new("LastVideoEnqueueAgeMs", typeof(long)),
            new("LastVideoWriteAgeMs", typeof(long)),
            new("AvSyncCaptureDriftMs", typeof(double?)),
            new("AvSyncCaptureDriftRateMsPerSec", typeof(double?)),
            new("AvSyncEncoderDriftMs", typeof(double?)),
            new("AvSyncEncoderCorrectionSamples", typeof(long?)),
        };
    }

    private static SnapshotPropertySpec[] CaptureHealthSourceTelemetryDetailPropertySpecs()
    {
        return new SnapshotPropertySpec[]
        {
            NonNullString("Group"),
            NonNullString("Label"),
            NonNullString("DisplayValue"),
            NullableString("RawValue"),
        };
    }

    private static void AssertCaptureHealthSnapshotDefaultsAndInheritance(Type diagnosticsType, Type healthType)
    {
        if (!healthType.IsSealed)
        {
            throw new InvalidOperationException("CaptureHealthSnapshot must remain sealed.");
        }

        if (!diagnosticsType.IsAssignableFrom(healthType))
        {
            throw new InvalidOperationException("CaptureHealthSnapshot must inherit CaptureDiagnosticsSnapshot.");
        }
        var health = CreateInstance("Sussudio.Models.CaptureHealthSnapshot");
        AssertNonNullStringValue(health, "RecordingBackend", "None", "CaptureHealthSnapshot inherited RecordingBackend default");
        AssertNonNullStringValue(health, "FlashbackPlaybackState", "N/A", "CaptureHealthSnapshot.FlashbackPlaybackState default");
        AssertNonNullStringValue(health, "FlashbackDecoderHwAccel", "N/A", "CaptureHealthSnapshot.FlashbackDecoderHwAccel default");
        AssertNonNullStringValue(health, "FlashbackPlaybackMaxCommandQueueLatencyCommand", "None", "CaptureHealthSnapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand default");
        AssertNonNullStringValue(health, "FlashbackPlaybackLastCommandQueued", "None", "CaptureHealthSnapshot.FlashbackPlaybackLastCommandQueued default");
        AssertNonNullStringValue(health, "FlashbackPlaybackLastCommandProcessed", "None", "CaptureHealthSnapshot.FlashbackPlaybackLastCommandProcessed default");
        AssertNonNullStringValue(health, "FlashbackExportStatus", "NotStarted", "CaptureHealthSnapshot.FlashbackExportStatus default");
        AssertNonNullStringValue(health, "FlashbackExportFailureKind", string.Empty, "CaptureHealthSnapshot.FlashbackExportFailureKind default");
        Assert.Equal(0, GetCountProperty(GetPropertyValue(health, "SourceTelemetryDetails")!));
    }

    private static object CreateSourceTelemetryDetailEntry(Type detailType)
    {
        var detailEntry = Activator.CreateInstance(detailType, "Signal", "Colorimetry", "BT.2020", "bt2020")
            ?? throw new InvalidOperationException("Failed to create SourceTelemetryDetailEntry.");
        return detailEntry;
    }

    private static void AssertSourceTelemetryDetailEntryValues(object detailEntry)
    {
        Assert.Equal("Signal", GetStringProperty(detailEntry, "Group"));
        Assert.Equal("Colorimetry", GetStringProperty(detailEntry, "Label"));
        Assert.Equal("BT.2020", GetStringProperty(detailEntry, "DisplayValue"));
        Assert.Equal("bt2020", GetStringProperty(detailEntry, "RawValue"));
    }

    private static void AssertSourceTelemetryDetailEntryJsonRoundTrip(Type detailType, object detailEntry)
    {
        var detailJsonRoundTrip = ReflectionJsonRoundTrip(detailType, detailEntry);
        Assert.Equal("BT.2020", GetStringProperty(detailJsonRoundTrip, "DisplayValue"));
    }

    private static object CreatePopulatedCaptureHealthSnapshot(Type healthType, Type detailType, object detailEntry)
    {
        var health = CreateInstance(healthType.FullName!);
        var details = CreateGenericList(detailType, detailEntry);

        SetPropertyOrBackingField(health, "RecordingBackend", "FFmpeg");
        SetPropertyOrBackingField(health, "FlashbackOutputBytes", 123456L);
        SetPropertyOrBackingField(health, "FlashbackFilePath", "flashback.ts");
        SetPropertyOrBackingField(health, "FlashbackPlaybackState", "Paused");
        SetPropertyOrBackingField(health, "FlashbackDecoderHwAccel", "D3D11");
        SetPropertyOrBackingField(health, "FlashbackPlaybackDroppedFrames", 4L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackSegmentSwitches", 2L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackFmp4Reopens", 3L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackWriteHeadWaits", 5L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackNearLiveSnaps", 1L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeErrorSnaps", 0L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackSubmitFailures", 6L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastDropUtcUnixMs", 666L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastDropReason", "av_sync_skip");
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastSubmitFailureUtcUnixMs", 777L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastSubmitFailure", "seek:null_texture");
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastSegmentSwitchUtcUnixMs", 123L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastFmp4ReopenUtcUnixMs", 456L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastWriteHeadWaitGapMs", 789L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackTargetFps", 120d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackPtsCadenceMismatchCount", 2L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs", 123456700L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastPtsCadenceDeltaMs", 16.67d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastPtsCadenceExpectedMs", 8.33d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackSeekForwardDecodeCapHits", 3L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastSeekHitForwardDecodeCap", true);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeSampleCount", 120);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeAvgMs", 1.25d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeP95Ms", 2.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeP99Ms", 3.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeMaxMs", 4.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodePhase", "audio");
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeReceiveMs", 0.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeFeedMs", 4.0d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeReadMs", 0.75d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeSendMs", 3.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeAudioMs", 3.25d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeConvertMs", 0.25d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeUtcUnixMs", 123456789L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodePositionMs", 2345L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackCommandsEnqueued", 9L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackScrubUpdatesCoalesced", 7L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackSeekCommandsCoalesced", 8L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackCommandQueueCapacity", 256);
        SetPropertyOrBackingField(health, "FlashbackPlaybackPendingCommands", 2);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxPendingCommands", 5);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastCommandQueueLatencyMs", 14L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxCommandQueueLatencyMs", 88L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxCommandQueueLatencyCommand", "Play");
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastCommandQueued", "UpdateScrub");
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastCommandFailureUtcUnixMs", 999L);
        SetPropertyOrBackingField(health, "FlashbackVideoQueueRejectedFrames", 11L);
        SetPropertyOrBackingField(health, "FlashbackVideoQueueLastRejectReason", "force_rotate_draining");
        SetPropertyOrBackingField(health, "FlashbackGpuQueueRejectedFrames", 13L);
        SetPropertyOrBackingField(health, "FlashbackGpuQueueLastRejectReason", "encoding_failed:InvalidOperationException");
        SetPropertyOrBackingField(health, "FlashbackBackendSettingsStale", true);
        SetPropertyOrBackingField(health, "FlashbackBackendSettingsStaleReason", "preset:P1->P2");
        SetPropertyOrBackingField(health, "FlashbackBackendActiveFormat", "HevcMp4");
        SetPropertyOrBackingField(health, "FlashbackBackendRequestedFormat", "HevcMp4");
        SetPropertyOrBackingField(health, "FlashbackBackendActivePreset", "P1");
        SetPropertyOrBackingField(health, "FlashbackBackendRequestedPreset", "P2");
        SetPropertyOrBackingField(health, "FlashbackExportActive", true);
        SetPropertyOrBackingField(health, "FlashbackExportStatus", "Running");
        SetPropertyOrBackingField(health, "FlashbackExportFailureKind", "NoMediaWritten");
        SetPropertyOrBackingField(health, "FlashbackExportForceRotateFallbacks", 2L);
        SetPropertyOrBackingField(health, "FlashbackExportLastForceRotateFallbackUtcUnixMs", 12345L);
        SetPropertyOrBackingField(health, "FlashbackExportLastForceRotateFallbackSegments", 3);
        SetPropertyOrBackingField(health, "FlashbackExportLastForceRotateFallbackInPointMs", 1000L);
        SetPropertyOrBackingField(health, "FlashbackExportLastForceRotateFallbackOutPointMs", 9000L);
        SetPropertyOrBackingField(health, "FlashbackExportVerificationFormat", "HevcMp4");
        SetPropertyOrBackingField(health, "FlashbackCodecDowngradeReason", "AV1->HEVC");
        SetPropertyOrBackingField(health, "FlashbackExportPercent", 37.5d);
        SetPropertyOrBackingField(health, "FlashbackExportElapsedMs", 2000L);
        SetPropertyOrBackingField(health, "FlashbackExportLastProgressAgeMs", 100L);
        SetPropertyOrBackingField(health, "FlashbackExportOutputBytes", 1048576L);
        SetPropertyOrBackingField(health, "FlashbackExportThroughputBytesPerSec", 524288d);
        SetPropertyOrBackingField(health, "FlashbackExportSegmentsProcessed", 3);
        SetPropertyOrBackingField(health, "LastExportId", 42L);
        SetPropertyOrBackingField(health, "SourceVideoFormat", "YCbCr422");
        SetPropertyOrBackingField(health, "SourceHdrTransferCode", 2);
        SetPropertyOrBackingField(health, "SourceTelemetryDetails", details);
        SetPropertyOrBackingField(health, "LastVideoEnqueueAgeMs", 17L);
        SetPropertyOrBackingField(health, "AvSyncCaptureDriftMs", -1.5d);
        SetPropertyOrBackingField(health, "AvSyncEncoderCorrectionSamples", 48L);

        return health;
    }

    private static void AssertCaptureHealthSnapshotRoundTripValues(object health)
    {
        var roundTripDetail = GetSingleEnumerableItem(GetPropertyValue(health, "SourceTelemetryDetails")!);
        Assert.Equal("FFmpeg", GetStringProperty(health, "RecordingBackend"));
        Assert.Equal(123456L, GetLongProperty(health, "FlashbackOutputBytes"));
        Assert.Equal("flashback.ts", GetStringProperty(health, "FlashbackFilePath"));
        Assert.Equal("Paused", GetStringProperty(health, "FlashbackPlaybackState"));
        Assert.Equal("D3D11", GetStringProperty(health, "FlashbackDecoderHwAccel"));
        Assert.Equal(4L, GetLongProperty(health, "FlashbackPlaybackDroppedFrames"));
        Assert.Equal(2L, GetLongProperty(health, "FlashbackPlaybackSegmentSwitches"));
        Assert.Equal(3L, GetLongProperty(health, "FlashbackPlaybackFmp4Reopens"));
        Assert.Equal(5L, GetLongProperty(health, "FlashbackPlaybackWriteHeadWaits"));
        Assert.Equal(1L, GetLongProperty(health, "FlashbackPlaybackNearLiveSnaps"));
        Assert.Equal(0L, GetLongProperty(health, "FlashbackPlaybackDecodeErrorSnaps"));
        Assert.Equal(6L, GetLongProperty(health, "FlashbackPlaybackSubmitFailures"));
        Assert.Equal(666L, GetLongProperty(health, "FlashbackPlaybackLastDropUtcUnixMs"));
        Assert.Equal("av_sync_skip", GetStringProperty(health, "FlashbackPlaybackLastDropReason"));
        Assert.Equal(777L, GetLongProperty(health, "FlashbackPlaybackLastSubmitFailureUtcUnixMs"));
        Assert.Equal("seek:null_texture", GetStringProperty(health, "FlashbackPlaybackLastSubmitFailure"));
        Assert.Equal(123L, GetLongProperty(health, "FlashbackPlaybackLastSegmentSwitchUtcUnixMs"));
        Assert.Equal(456L, GetLongProperty(health, "FlashbackPlaybackLastFmp4ReopenUtcUnixMs"));
        Assert.Equal(789L, GetLongProperty(health, "FlashbackPlaybackLastWriteHeadWaitGapMs"));
        Assert.Equal(120d, GetDoubleProperty(health, "FlashbackPlaybackTargetFps"));
        Assert.Equal(2L, GetLongProperty(health, "FlashbackPlaybackPtsCadenceMismatchCount"));
        Assert.Equal(123456700L, GetLongProperty(health, "FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs"));
        Assert.Equal(16.67d, GetDoubleProperty(health, "FlashbackPlaybackLastPtsCadenceDeltaMs"));
        Assert.Equal(8.33d, GetDoubleProperty(health, "FlashbackPlaybackLastPtsCadenceExpectedMs"));
        Assert.Equal(3L, GetLongProperty(health, "FlashbackPlaybackSeekForwardDecodeCapHits"));
        Assert.True(GetBoolProperty(health, "FlashbackPlaybackLastSeekHitForwardDecodeCap"));
        Assert.Equal(120, GetIntProperty(health, "FlashbackPlaybackDecodeSampleCount"));
        Assert.Equal(1.25d, GetDoubleProperty(health, "FlashbackPlaybackDecodeAvgMs"));
        Assert.Equal(2.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeP95Ms"));
        Assert.Equal(3.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeP99Ms"));
        Assert.Equal(4.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeMaxMs"));
        Assert.Equal("audio", GetStringProperty(health, "FlashbackPlaybackMaxDecodePhase"));
        Assert.Equal(0.5d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeReceiveMs"));
        Assert.Equal(4.0d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeFeedMs"));
        Assert.Equal(0.75d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeReadMs"));
        Assert.Equal(3.5d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeSendMs"));
        Assert.Equal(3.25d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeAudioMs"));
        Assert.Equal(0.25d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeConvertMs"));
        Assert.Equal(123456789L, GetLongProperty(health, "FlashbackPlaybackMaxDecodeUtcUnixMs"));
        Assert.Equal(2345L, GetLongProperty(health, "FlashbackPlaybackMaxDecodePositionMs"));
        Assert.Equal(9L, GetLongProperty(health, "FlashbackPlaybackCommandsEnqueued"));
        Assert.Equal(7L, GetLongProperty(health, "FlashbackPlaybackScrubUpdatesCoalesced"));
        Assert.Equal(8L, GetLongProperty(health, "FlashbackPlaybackSeekCommandsCoalesced"));
        Assert.Equal(256, GetIntProperty(health, "FlashbackPlaybackCommandQueueCapacity"));
        Assert.Equal(2, GetIntProperty(health, "FlashbackPlaybackPendingCommands"));
        Assert.Equal(5, GetIntProperty(health, "FlashbackPlaybackMaxPendingCommands"));
        Assert.Equal(14L, GetLongProperty(health, "FlashbackPlaybackLastCommandQueueLatencyMs"));
        Assert.Equal(88L, GetLongProperty(health, "FlashbackPlaybackMaxCommandQueueLatencyMs"));
        Assert.Equal("Play", GetStringProperty(health, "FlashbackPlaybackMaxCommandQueueLatencyCommand"));
        Assert.Equal("UpdateScrub", GetStringProperty(health, "FlashbackPlaybackLastCommandQueued"));
        Assert.Equal(999L, GetLongProperty(health, "FlashbackPlaybackLastCommandFailureUtcUnixMs"));
        Assert.Equal(11L, GetLongProperty(health, "FlashbackVideoQueueRejectedFrames"));
        Assert.Equal("force_rotate_draining", GetStringProperty(health, "FlashbackVideoQueueLastRejectReason"));
        Assert.Equal(13L, GetLongProperty(health, "FlashbackGpuQueueRejectedFrames"));
        Assert.Equal("encoding_failed:InvalidOperationException", GetStringProperty(health, "FlashbackGpuQueueLastRejectReason"));
        Assert.True(GetBoolProperty(health, "FlashbackBackendSettingsStale"));
        Assert.Equal("preset:P1->P2", GetStringProperty(health, "FlashbackBackendSettingsStaleReason"));
        Assert.Equal("HevcMp4", GetStringProperty(health, "FlashbackBackendActiveFormat"));
        Assert.Equal("P2", GetStringProperty(health, "FlashbackBackendRequestedPreset"));
        Assert.True(GetBoolProperty(health, "FlashbackExportActive"));
        Assert.Equal("Running", GetStringProperty(health, "FlashbackExportStatus"));
        Assert.Equal("NoMediaWritten", GetStringProperty(health, "FlashbackExportFailureKind"));
        Assert.Equal(2L, GetLongProperty(health, "FlashbackExportForceRotateFallbacks"));
        Assert.Equal(12345L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackUtcUnixMs"));
        Assert.Equal(3, GetIntProperty(health, "FlashbackExportLastForceRotateFallbackSegments"));
        Assert.Equal(1000L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackInPointMs"));
        Assert.Equal(9000L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackOutPointMs"));
        Assert.Equal("HevcMp4", GetStringProperty(health, "FlashbackExportVerificationFormat"));
        Assert.Equal("AV1->HEVC", GetStringProperty(health, "FlashbackCodecDowngradeReason"));
        Assert.Equal(37.5d, GetDoubleProperty(health, "FlashbackExportPercent"));
        Assert.Equal(2000L, GetLongProperty(health, "FlashbackExportElapsedMs"));
        Assert.Equal(100L, GetLongProperty(health, "FlashbackExportLastProgressAgeMs"));
        Assert.Equal(1048576L, GetLongProperty(health, "FlashbackExportOutputBytes"));
        Assert.Equal(524288d, GetDoubleProperty(health, "FlashbackExportThroughputBytesPerSec"));
        Assert.Equal(3, GetIntProperty(health, "FlashbackExportSegmentsProcessed"));
        Assert.Equal(42L, GetLongProperty(health, "LastExportId"));
        Assert.Equal("YCbCr422", GetStringProperty(health, "SourceVideoFormat"));
        Assert.Equal(2, Convert.ToInt32(GetPropertyValue(health, "SourceHdrTransferCode")));
        Assert.Equal(1, GetCountProperty(GetPropertyValue(health, "SourceTelemetryDetails")!));
        Assert.Equal("Signal", GetStringProperty(roundTripDetail, "Group"));
        Assert.Equal("Colorimetry", GetStringProperty(roundTripDetail, "Label"));
        Assert.Equal("BT.2020", GetStringProperty(roundTripDetail, "DisplayValue"));
        Assert.Equal("bt2020", GetStringProperty(roundTripDetail, "RawValue"));
        Assert.Equal(17L, GetLongProperty(health, "LastVideoEnqueueAgeMs"));
        Assert.Equal(-1.5d, (double)GetPropertyValue(health, "AvSyncCaptureDriftMs")!);
        Assert.Equal(48L, Convert.ToInt64(GetPropertyValue(health, "AvSyncEncoderCorrectionSamples")));
    }

    private static void AssertCaptureHealthSnapshotJsonRoundTrip(Type healthType, object health)
    {
        var jsonRoundTrip = ReflectionJsonRoundTrip(healthType, health);
        Assert.Equal("Paused", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackState"));
        Assert.Equal(6L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackSubmitFailures"));
        Assert.Equal(666L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastDropUtcUnixMs"));
        Assert.Equal("av_sync_skip", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackLastDropReason"));
        Assert.Equal(777L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastSubmitFailureUtcUnixMs"));
        Assert.Equal("seek:null_texture", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackLastSubmitFailure"));
        Assert.Equal(9L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackCommandsEnqueued"));
        Assert.Equal(256, GetIntProperty(jsonRoundTrip, "FlashbackPlaybackCommandQueueCapacity"));
        Assert.Equal(999L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastCommandFailureUtcUnixMs"));
        Assert.Equal(11L, GetLongProperty(jsonRoundTrip, "FlashbackVideoQueueRejectedFrames"));
        Assert.Equal("force_rotate_draining", GetStringProperty(jsonRoundTrip, "FlashbackVideoQueueLastRejectReason"));
        Assert.Equal(13L, GetLongProperty(jsonRoundTrip, "FlashbackGpuQueueRejectedFrames"));
        Assert.Equal("encoding_failed:InvalidOperationException", GetStringProperty(jsonRoundTrip, "FlashbackGpuQueueLastRejectReason"));
        Assert.Equal(2L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackPtsCadenceMismatchCount"));
        Assert.Equal(16.67d, GetDoubleProperty(jsonRoundTrip, "FlashbackPlaybackLastPtsCadenceDeltaMs"));
        Assert.Equal(3L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackSeekForwardDecodeCapHits"));
        Assert.True(GetBoolProperty(jsonRoundTrip, "FlashbackPlaybackLastSeekHitForwardDecodeCap"));
        Assert.True(GetBoolProperty(jsonRoundTrip, "FlashbackBackendSettingsStale"));
        Assert.Equal("preset:P1->P2", GetStringProperty(jsonRoundTrip, "FlashbackBackendSettingsStaleReason"));
        Assert.Equal("Running", GetStringProperty(jsonRoundTrip, "FlashbackExportStatus"));
        Assert.Equal("NoMediaWritten", GetStringProperty(jsonRoundTrip, "FlashbackExportFailureKind"));
        Assert.Equal(2L, GetLongProperty(jsonRoundTrip, "FlashbackExportForceRotateFallbacks"));
        Assert.Equal(3, GetIntProperty(jsonRoundTrip, "FlashbackExportLastForceRotateFallbackSegments"));
        Assert.Equal("HevcMp4", GetStringProperty(jsonRoundTrip, "FlashbackExportVerificationFormat"));
        Assert.Equal("AV1->HEVC", GetStringProperty(jsonRoundTrip, "FlashbackCodecDowngradeReason"));
        Assert.Equal(1048576L, GetLongProperty(jsonRoundTrip, "FlashbackExportOutputBytes"));
        Assert.Equal(42L, GetLongProperty(jsonRoundTrip, "LastExportId"));
        Assert.Equal("YCbCr422", GetStringProperty(jsonRoundTrip, "SourceVideoFormat"));
        Assert.Equal(1, GetCountProperty(GetPropertyValue(jsonRoundTrip, "SourceTelemetryDetails")!));
        Assert.Equal("BT.2020", GetStringProperty(GetSingleEnumerableItem(GetPropertyValue(jsonRoundTrip, "SourceTelemetryDetails")!), "DisplayValue"));
    }

    [Fact]
    public void CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync()
    {
        var diagnosticsType = RequireType("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        var healthRootText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs");

        AssertCaptureHealthSnapshotDefaultsAndInheritance(diagnosticsType, healthType);
        RegisterCaptureDiagnosticsSnapshotProperties(diagnosticsType);
        AssertDeclaredProperties(healthType, CaptureHealthSnapshotPropertySpecs(detailType));
        AssertDeclaredProperties(detailType, CaptureHealthSourceTelemetryDetailPropertySpecs());
        Assert.Contains("public sealed class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot", healthRootText, StringComparison.Ordinal);
        Assert.Contains("public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails", healthRootText, StringComparison.Ordinal);
        Assert.Contains("public bool FlashbackBackendSettingsStale { get; init; }", healthRootText, StringComparison.Ordinal);
        Assert.Contains("public int FlashbackAudioQueueCapacity { get; init; }", healthRootText, StringComparison.Ordinal);
        Assert.Contains("public string FlashbackPlaybackState { get; init; } = \"N/A\";", healthRootText, StringComparison.Ordinal);
        Assert.Contains("public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;", healthRootText, StringComparison.Ordinal);
        Assert.Contains("public string FlashbackExportStatus { get; init; } = \"NotStarted\";", healthRootText, StringComparison.Ordinal);
        Assert.Contains("public string? FlashbackExportVerificationFormat { get; init; }", healthRootText, StringComparison.Ordinal);
        Assert.DoesNotContain("partial class CaptureHealthSnapshot", healthRootText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureHealthSnapshot.cs")));
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureSnapshotModels.cs")));

        var detailEntry = CreateSourceTelemetryDetailEntry(detailType);
        AssertSourceTelemetryDetailEntryValues(detailEntry);
        AssertSourceTelemetryDetailEntryJsonRoundTrip(detailType, detailEntry);

        var health = CreatePopulatedCaptureHealthSnapshot(healthType, detailType, detailEntry);
        AssertCaptureHealthSnapshotRoundTripValues(health);
        AssertCaptureHealthSnapshotJsonRoundTrip(healthType, health);
    }
}

public sealed class ViewModelBuildersTests
{
    [Fact]
    public void Build_PreservesAutomationOptionsSnapshotContract()
    {
        var asm = SussudioAssembly.Load();
        var builderType = asm.GetType("Sussudio.ViewModels.AutomationOptionsSnapshotBuilder", throwOnError: true)!;
        var inputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsSnapshotInput", throwOnError: true)!;
        var deviceInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsDeviceInput", throwOnError: true)!;
        var resolutionInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsResolutionInput", throwOnError: true)!;
        var frameRateInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsFrameRateInput", throwOnError: true)!;

        var timestamp = new DateTimeOffset(2026, 5, 16, 1, 2, 3, TimeSpan.Zero);
        var input = CreateInput(inputType,
            ("TimestampUtc", timestamp),
            ("Devices", InputArray(deviceInputType,
                CreateInput(deviceInputType, ("Id", "device-a"), ("Name", "Device A")),
                CreateInput(deviceInputType, ("Id", "DEVICE-B"), ("Name", "Device B")))),
            ("AudioInputDevices", InputArray(deviceInputType,
                CreateInput(deviceInputType, ("Id", "audio-a"), ("Name", "Audio A")))),
            ("MicrophoneDevices", InputArray(deviceInputType,
                CreateInput(deviceInputType, ("Id", "mic-a"), ("Name", "Mic A")),
                CreateInput(deviceInputType, ("Id", "MIC-B"), ("Name", "Mic B")))),
            ("Resolutions", InputArray(resolutionInputType,
                CreateInput(resolutionInputType,
                    ("Value", "1920x1080"),
                    ("Width", 1920u),
                    ("Height", 1080u),
                    ("IsEnabled", true),
                    ("DisableReason", null)),
                CreateInput(resolutionInputType,
                    ("Value", "3840x2160"),
                    ("Width", 3840u),
                    ("Height", 2160u),
                    ("IsEnabled", false),
                    ("DisableReason", "Unavailable")))),
            ("FrameRates", InputArray(frameRateInputType,
                CreateInput(frameRateInputType,
                    ("Value", 59.94d),
                    ("FriendlyValue", 60d),
                    ("ExactValueArg", "60000/1001"),
                    ("IsEnabled", true),
                    ("DisableReason", null),
                    ("IsSelected", false)),
                CreateInput(frameRateInputType,
                    ("Value", 60d),
                    ("FriendlyValue", 60d),
                    ("ExactValueArg", null),
                    ("IsEnabled", false),
                    ("DisableReason", null),
                    ("IsSelected", true)))),
            ("RecordingFormats", new[] { "H264", "AV1" }),
            ("Qualities", new[] { "High", "Medium" }),
            ("Presets", new[] { "Quality", "Speed" }),
            ("SplitEncodeModes", new[] { "Auto", "Disabled" }),
            ("VideoFormats", new[] { "Auto", "MJPG" }),
            ("FlashbackBufferMinuteOptions", new[] { 1, 2, 5, 10, 15, 30 }),
            ("SelectedDeviceId", "device-b"),
            ("SelectedAudioInputDeviceId", "AUDIO-A"),
            ("SelectedMicrophoneDeviceId", "mic-b"),
            ("SelectedResolution", "1920X1080"),
            ("SelectedFrameRate", 60d),
            ("SelectedRecordingFormat", "av1"),
            ("SelectedQuality", "medium"),
            ("SelectedPreset", "speed"),
            ("SelectedSplitEncodeMode", "disabled"),
            ("SelectedVideoFormat", "mjpg"),
            ("MjpegDecoderCount", 99),
            ("PreviewVolume", 0.425d),
            ("IsMicrophoneEnabled", true),
            ("MicrophoneVolume", 62.5d),
            ("FlashbackBufferMinutes", 10),
            ("FlashbackGpuDecode", true),
            ("IsFlashbackEnabled", false),
            ("IsStatsVisible", true));

        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var snapshot = build.Invoke(null, new[] { input })!;

        Assert.Equal(timestamp, Get(snapshot, "TimestampUtc"));
        Assert.Equal("device-b", Get(snapshot, "SelectedDeviceId"));
        Assert.Equal("AUDIO-A", Get(snapshot, "SelectedAudioInputDeviceId"));
        Assert.Equal("mic-b", Get(snapshot, "SelectedMicrophoneDeviceId"));
        Assert.Equal("1920X1080", Get(snapshot, "SelectedResolution"));
        Assert.Equal(60d, Get(snapshot, "SelectedFrameRate"));
        Assert.Equal(8, Get(snapshot, "MjpegDecoderCount"));
        Assert.Equal(42.5d, Get(snapshot, "PreviewVolumePercent"));
        Assert.True((bool)Get(snapshot, "IsMicrophoneEnabled")!);
        Assert.Equal(62.5d, Get(snapshot, "MicrophoneVolumePercent"));
        Assert.Equal(10, Get(snapshot, "FlashbackBufferMinutes"));
        Assert.True((bool)Get(snapshot, "FlashbackGpuDecode")!);
        Assert.False((bool)Get(snapshot, "IsFlashbackEnabled")!);
        Assert.True((bool)Get(snapshot, "IsStatsVisible")!);

        var devices = (Array)Get(snapshot, "Devices")!;
        Assert.False((bool)Get(devices.GetValue(0)!, "IsSelected")!);
        Assert.True((bool)Get(devices.GetValue(1)!, "IsSelected")!);

        var audioDevices = (Array)Get(snapshot, "AudioInputDevices")!;
        Assert.True((bool)Get(audioDevices.GetValue(0)!, "IsSelected")!);

        var microphoneDevices = (Array)Get(snapshot, "MicrophoneDevices")!;
        Assert.False((bool)Get(microphoneDevices.GetValue(0)!, "IsSelected")!);
        Assert.True((bool)Get(microphoneDevices.GetValue(1)!, "IsSelected")!);

        var resolutions = (Array)Get(snapshot, "Resolutions")!;
        Assert.Equal(string.Empty, Get(resolutions.GetValue(0)!, "DisableReason"));
        Assert.True((bool)Get(resolutions.GetValue(0)!, "IsSelected")!);
        Assert.Equal(3840, Get(resolutions.GetValue(1)!, "Width"));
        Assert.Equal("Unavailable", Get(resolutions.GetValue(1)!, "DisableReason"));

        var frameRates = (Array)Get(snapshot, "FrameRates")!;
        Assert.Equal("60000/1001", Get(frameRates.GetValue(0)!, "ExactValueArg"));
        Assert.Equal(string.Empty, Get(frameRates.GetValue(0)!, "DisableReason"));
        Assert.Equal(string.Empty, Get(frameRates.GetValue(1)!, "ExactValueArg"));
        Assert.True((bool)Get(frameRates.GetValue(1)!, "IsSelected")!);

        var recordingFormats = (Array)Get(snapshot, "RecordingFormats")!;
        Assert.True((bool)Get(recordingFormats.GetValue(1)!, "IsSelected")!);
        Assert.Equal(string.Empty, Get(recordingFormats.GetValue(1)!, "DisableReason"));

        var decoderCounts = (Array)Get(snapshot, "MjpegDecoderCounts")!;
        Assert.Equal(8, decoderCounts.Length);
        for (var i = 0; i < decoderCounts.Length; i++)
        {
            var option = decoderCounts.GetValue(i)!;
            Assert.Equal(i + 1, Get(option, "Value"));
            Assert.Equal(i == 7, (bool)Get(option, "IsSelected")!);
        }

        var flashbackBufferMinuteOptions = (Array)Get(snapshot, "FlashbackBufferMinuteOptions")!;
        Assert.Equal(6, flashbackBufferMinuteOptions.Length);
        Assert.Equal(10, Get(flashbackBufferMinuteOptions.GetValue(3)!, "Value"));
        Assert.True((bool)Get(flashbackBufferMinuteOptions.GetValue(3)!, "IsSelected")!);
    }

    [Fact]
    public void Build_PreservesViewModelRuntimeSnapshotContract()
    {
        var asm = SussudioAssembly.Load();
        var builderType = asm.GetType("Sussudio.ViewModels.ViewModelRuntimeSnapshotBuilder", throwOnError: true)!;
        var inputType = asm.GetType("Sussudio.ViewModels.ViewModelRuntimeSnapshotInput", throwOnError: true)!;
        var sessionSnapshotType = asm.GetType("Sussudio.Services.Capture.CaptureSessionSnapshot", throwOnError: true)!;
        var commandOutcomeType = asm.GetType("Sussudio.Services.Capture.CaptureCommandOutcome", throwOnError: true)!;

        var timestamp = new DateTimeOffset(2026, 5, 16, 12, 0, 10, TimeSpan.Zero);
        var telemetryTimestamp = timestamp.AddSeconds(-12);
        var sessionSnapshot = CreateInput(sessionSnapshotType,
            ("SessionGeneration", 42L),
            ("CommandsEnqueued", 11L),
            ("CommandsCompleted", 7L),
            ("CommandsFailed", 2L),
            ("CommandsCanceled", 1L),
            ("CommandsCoalesced", 3L),
            ("PendingCommands", 4),
            ("MaxPendingCommands", 6),
            ("OldestPendingCommandAgeMs", 123L),
            ("LastCommandQueueLatencyMs", 45L),
            ("MaxCommandQueueLatencyMs", 67L),
            ("LastOutcome", Enum.Parse(commandOutcomeType, "Completed")));

        var input = CreateInput(inputType,
            ("TimestampUtc", timestamp),
            ("SessionSnapshot", sessionSnapshot),
            ("IsInitialized", true),
            ("IsPreviewing", true),
            ("IsRecording", false),
            ("IsAudioEnabled", true),
            ("IsAudioPreviewEnabled", true),
            ("IsCustomAudioInputEnabled", false),
            ("StatusText", "Ready"),
            ("SelectedDeviceId", "device-1"),
            ("SelectedDeviceName", "Device One"),
            ("SelectedAudioInputDeviceId", "audio-1"),
            ("SelectedAudioInputDeviceName", "Audio One"),
            ("SelectedResolution", "3840x2160"),
            ("SelectedFrameRate", 119.88d),
            ("SelectedFriendlyFrameRate", 120d),
            ("SelectedExactFrameRate", 119.88d),
            ("SelectedExactFrameRateArg", "120000/1001"),
            ("DisabledResolutionReason", "resolution reason"),
            ("DisabledFrameRateReason", "frame reason"),
            ("HdrResolutionSupportHint", "HDR OK"),
            ("DetectedSourceFrameRate", 119.88d),
            ("DetectedSourceFrameRateArg", "120000/1001"),
            ("SourceFrameRateOrigin", "Telemetry"),
            ("SourceWidth", 3840),
            ("SourceHeight", 2160),
            ("SourceIsHdr", true),
            ("SourceTelemetryAvailability", "Available"),
            ("SourceTelemetryOriginDetail", "Native"),
            ("SourceTelemetryConfidence", "High"),
            ("SourceTelemetryDiagnosticSummary", "clean"),
            ("SourceTelemetryTimestampUtc", telemetryTimestamp),
            ("SourceTelemetryEpoch", 99L),
            ("SourceTelemetrySummaryText", "source summary"),
            ("SourceTargetSummaryText", "target summary"),
            ("SelectedRecordingFormat", "HEVC"),
            ("SelectedQuality", "High"),
            ("SelectedPreset", "P5"),
            ("SelectedSplitEncodeMode", "Auto"),
            ("SelectedVideoFormat", "MJPG"),
            ("CustomBitrateMbps", 42d),
            ("PreviewVolume", 0.375d),
            ("IsStatsVisible", true),
            ("IsHdrAvailable", true),
            ("IsHdrEnabled", true),
            ("HdrRuntimeState", "Active"),
            ("HdrReadinessReason", "ready"),
            ("LiveResolution", "3840x2160"),
            ("LiveFrameRate", "119.88"),
            ("LivePixelFormat", "P010"),
            ("OutputPath", "C:\\Capture"),
            ("RecordingTime", "00:01:02"),
            ("RecordingSizeInfo", "10 MB"),
            ("RecordingBitrateInfo", "100 Mbps"),
            ("AudioPeak", 0.75d),
            ("AudioClipping", true));

        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var snapshot = build.Invoke(null, new[] { input })!;

        Assert.Equal(timestamp, Get(snapshot, "TimestampUtc"));
        Assert.Equal(42L, Get(snapshot, "CaptureSessionEpoch"));
        Assert.Equal(99L, Get(snapshot, "SourceTelemetryEpoch"));
        Assert.True((bool)Get(snapshot, "IsInitialized")!);
        Assert.True((bool)Get(snapshot, "IsPreviewing")!);
        Assert.Equal("Ready", Get(snapshot, "StatusText"));
        Assert.Equal("device-1", Get(snapshot, "SelectedDeviceId"));
        Assert.Equal("3840x2160", Get(snapshot, "SelectedResolution"));
        Assert.Equal(119.88d, Get(snapshot, "SelectedFrameRate"));
        Assert.Equal("120000/1001", Get(snapshot, "SelectedExactFrameRateArg"));
        Assert.Equal(true, Get(snapshot, "SourceIsHdr"));
        Assert.Equal(12, Get(snapshot, "SourceTelemetryAgeSeconds"));
        Assert.Equal("source summary", Get(snapshot, "SourceTelemetrySummaryText"));
        Assert.Equal("HEVC", Get(snapshot, "SelectedRecordingFormat"));
        Assert.Equal(37.5d, Get(snapshot, "PreviewVolumePercent"));
        Assert.True((bool)Get(snapshot, "IsStatsVisible")!);
        Assert.Equal("Active", Get(snapshot, "HdrRuntimeState"));
        Assert.Equal("P010", Get(snapshot, "LivePixelFormat"));
        Assert.Equal("C:\\Capture", Get(snapshot, "OutputPath"));
        Assert.Equal("00:01:02", Get(snapshot, "RecordingTime"));
        Assert.Equal(0.75d, Get(snapshot, "AudioPeak"));
        Assert.True((bool)Get(snapshot, "AudioClipping")!);

        Assert.Equal(11L, Get(snapshot, "CaptureCommandCommandsEnqueued"));
        Assert.Equal(7L, Get(snapshot, "CaptureCommandCommandsCompleted"));
        Assert.Equal(2L, Get(snapshot, "CaptureCommandCommandsFailed"));
        Assert.Equal(1L, Get(snapshot, "CaptureCommandCommandsCanceled"));
        Assert.Equal(3L, Get(snapshot, "CaptureCommandCommandsCoalesced"));
        Assert.Equal(4, Get(snapshot, "CaptureCommandPendingCommands"));
        Assert.Equal(6, Get(snapshot, "CaptureCommandMaxPendingCommands"));
        Assert.Equal(123L, Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs"));
        Assert.Equal(45L, Get(snapshot, "CaptureCommandLastQueueLatencyMs"));
        Assert.Equal(67L, Get(snapshot, "CaptureCommandMaxQueueLatencyMs"));
        Assert.Equal("None", Get(snapshot, "CaptureCommandLastCommand"));
        Assert.Equal("Completed", Get(snapshot, "CaptureCommandLastOutcome"));
        Assert.Equal(string.Empty, Get(snapshot, "CaptureCommandLastCorrelationId"));
        Assert.Equal(string.Empty, Get(snapshot, "CaptureCommandLastError"));
    }

    [Fact]
    public void LiveSignalTextProjection_PreservesPixelFormatFallbackOrder()
    {
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs");
        var liveSignalText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs");
        var builderType = RequireType("Sussudio.ViewModels.LiveSignalTextPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var buildMethod = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build was not found.");

        Assert.Contains("_context.UpdateLiveCaptureInfo(runtimeSnapshot);", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.ResetLiveCaptureInfo();", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("IsAudioPreviewActive =", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateLiveCaptureInfo(", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void ResetLiveCaptureInfo()", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.Contains("private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("IsAudioPreviewActive = runtime.IsAudioPreviewActive;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("var liveSignalText = LiveSignalTextPresentationBuilder.Build(", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("_captureService.EncoderCodecName,", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveInfoUnavailable);", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveResolution = liveSignalText.Resolution;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveFrameRate = liveSignalText.FrameRate;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LivePixelFormat = liveSignalText.PixelFormat;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("private void ResetLiveCaptureInfo()", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("partial void OnIsPreviewingChanged(bool value)", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("if (!value && !IsRecording)", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("IsAudioPreviewActive = false;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveResolution = LiveInfoUnavailable;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveFrameRate = LiveInfoUnavailable;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LivePixelFormat = LiveInfoUnavailable;", capturePresentationText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CapturePresentation.cs")),
            "MainViewModel.CapturePresentation.cs folded into capture state");
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.LiveSignalPresentation.cs")),
            "old live signal presentation file removed");
        Assert.DoesNotContain("runtime.ReaderSourceSubtype ??", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("runtime.LatestObservedFramePixelFormat ??", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.Contains("internal static class LiveSignalTextPresentationBuilder", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("internal static LiveSignalTextPresentation Build(", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("frameRateValue.Value.ToString(\"0.00\")", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.ReaderSourceSubtype ??", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.LatestObservedFramePixelFormat ??", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("\"hevc_nvenc\" => \" → HEVC\"", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("\"h264_nvenc\" => \" → H264\"", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("\"av1_nvenc\" => \" → AV1\"", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("? unavailableText", liveSignalText, StringComparison.Ordinal);
        Assert.True(
            liveSignalText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) <
            liveSignalText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal),
            "MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");

        var actualRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(actualRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(actualRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedWidth", (uint?)1920);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedHeight", (uint?)1080);
        SetPropertyOrBackingField(actualRuntime, "ActualWidth", (uint?)3840);
        SetPropertyOrBackingField(actualRuntime, "ActualHeight", (uint?)2160);
        SetPropertyOrBackingField(actualRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedFrameRate", 59.94d);
        SetPropertyOrBackingField(actualRuntime, "ActualFrameRate", 119.88d);
        SetPropertyOrBackingField(actualRuntime, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(actualRuntime, "RequestedReaderSubtype", "YUY2");
        SetPropertyOrBackingField(actualRuntime, "LatestObservedFramePixelFormat", "P010");
        SetPropertyOrBackingField(actualRuntime, "NegotiatedPixelFormat", "NV12");
        SetPropertyOrBackingField(actualRuntime, "VideoNegotiatedSubtype", "I420");
        SetPropertyOrBackingField(actualRuntime, "ReaderSourceSubtype", "RGB32");

        var actualPresentation = buildMethod.Invoke(null, new object?[] { actualRuntime, "av1_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        Assert.Equal("3840x2160", GetStringProperty(actualPresentation, "Resolution"));
        Assert.Equal("119.88", GetStringProperty(actualPresentation, "FrameRate"));
        Assert.Equal("RGB32 → AV1", GetStringProperty(actualPresentation, "PixelFormat"));

        var fallbackRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create fallback CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(fallbackRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(fallbackRuntime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedPixelFormat", "MJPG");

        var fallbackPresentation = buildMethod.Invoke(null, new object?[] { fallbackRuntime, "hevc_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        Assert.Equal("1280x720", GetStringProperty(fallbackPresentation, "Resolution"));
        Assert.Equal("30.00", GetStringProperty(fallbackPresentation, "FrameRate"));
        Assert.Equal("MJPG → HEVC", GetStringProperty(fallbackPresentation, "PixelFormat"));

        var unavailablePresentation = buildMethod.Invoke(
                null,
                new object?[] { CreateLiveSignalUnavailableRuntime(snapshotType), "libx264", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "Resolution"));
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "FrameRate"));
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "PixelFormat"));
    }

    [Fact]
    public void SourceTelemetryPresentationBuilder_PreservesSummaryAndTargetText()
    {
        var builderType = RequireType("Sussudio.ViewModels.SourceTelemetryPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildSourceSummary = builderType.GetMethod("BuildSourceSummary", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildSourceSummary was not found.");
        var buildAgeText = builderType.GetMethod("BuildAgeText", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildAgeText was not found.");
        var buildTargetSummary = builderType.GetMethod("BuildTargetSummary", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildTargetSummary was not found.");

        var now = new DateTimeOffset(2026, 5, 14, 22, 10, 30, TimeSpan.Zero);
        var unavailable = snapshotType.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!.Invoke(null, new object?[] { "telemetry-not-started", null })!;
        Assert.Equal(
            "Source: waiting for signal telemetry",
            buildSourceSummary.Invoke(null, new[] { unavailable, now }));

        var full = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(full, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(full, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(full, "Width", 3840);
        SetPropertyOrBackingField(full, "Height", 2160);
        SetPropertyOrBackingField(full, "FrameRateExact", 120000d / 1001d);
        SetPropertyOrBackingField(full, "FrameRateArg", "120000/1001");
        SetPropertyOrBackingField(full, "IsHdr", true);
        SetPropertyOrBackingField(full, "TimestampUtc", now.AddSeconds(-17));
        Assert.Equal(
            "Source: 3840x2160 @ 120000/1001 | HDR | Available/High | updated 17s ago",
            buildSourceSummary.Invoke(null, new[] { full, now }));

        var partial = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create partial SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(partial, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Stale"));
        SetPropertyOrBackingField(partial, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Low"));
        SetPropertyOrBackingField(partial, "FrameRateExact", 59.94d);
        SetPropertyOrBackingField(partial, "TimestampUtc", now.AddSeconds(2));
        Assert.Equal(
            "Source: ?x? @ 59.94 | HDR? | Stale/Low | updated now",
            buildSourceSummary.Invoke(null, new[] { partial, now }));

        Assert.Equal("updated ?", buildAgeText.Invoke(null, new object?[] { null, now }));
        Assert.Equal(
            "Target: Auto (3840 x 2160) @ 60 (exact 60000/1001) | HDR=Ready",
            buildTargetSummary.Invoke(null, new object?[] { "Auto (3840 x 2160)", 59.94d, 60d, 60000d / 1001d, "60000/1001", "Ready" }));
        Assert.Equal(
            "Target: 1080p @ 0 (exact ?) | HDR=Unknown",
            buildTargetSummary.Invoke(null, new object?[] { "1080p", 0d, null, null, null, " " }));
    }

    [Fact]
    public void SourceTelemetryPresentationBuilder_LivesInFocusedHelper()
    {
        var telemetryText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs");
        var controllerGraphText = ExtractTextBetween(
            ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n"),
            "private sealed class MainViewModelControllerGraph",
            "/// <summary>\n/// Owns bounded byte-sample smoothing");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs");
        var builderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs");
        var sourceTelemetryBuilderText = ExtractTextBetween(
            builderText,
            "internal static class SourceTelemetryPresentationBuilder",
            "internal static class AutomationOptionsSnapshotBuilder");

        Assert.Contains("_context.BuildSourceTelemetrySummary(_context.GetLatestSourceTelemetry(), DateTimeOffset.UtcNow);", telemetryText, StringComparison.Ordinal);
        Assert.Contains("_context.SetSourceTelemetrySummaryText(_context.BuildSourceTelemetrySummary(snapshot, DateTimeOffset.UtcNow));", telemetryText, StringComparison.Ordinal);
        Assert.Contains("BuildSourceTelemetrySummary = SourceTelemetryPresentationBuilder.BuildSourceSummary,", controllerGraphText, StringComparison.Ordinal);
        Assert.Contains("_context.UpdateTargetSummary();", telemetryText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateHdrRuntimeStatusFromCapture(", telemetryText, StringComparison.Ordinal);
        Assert.Contains("private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("HdrRuntimeState = runtime.HdrRuntimeState;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("HdrReadinessReason = runtime.HdrReadinessReason;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("UpdateTargetSummary();", capturePresentationText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateTargetSummary()", telemetryText, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceTelemetryPresentationBuilder.BuildTargetSummary(", telemetryText, StringComparison.Ordinal);
        Assert.Contains("private void UpdateTargetSummary()", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("SourceTargetSummaryText = SourceTelemetryPresentationBuilder.BuildTargetSummary(", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("GetSelectedResolutionDisplayText(),", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("SelectedFriendlyFrameRate,", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("SelectedExactFrameRate,", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("SelectedExactFrameRateArg,", capturePresentationText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.HdrRuntimePresentation.cs")),
            "old HDR runtime presentation file removed");
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.TargetSummaryPresentation.cs")),
            "old target summary presentation file removed");
        Assert.Contains("private string GetSelectedResolutionDisplayText()", capturePresentationText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.TargetPresentation.cs")),
            "old target presentation file removed");
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutoResolutionPresentation.cs")),
            "old auto resolution presentation file removed");
        Assert.DoesNotContain("private static string BuildSourceTelemetrySummaryText(", telemetryText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static string BuildTelemetryAgeText(", telemetryText, StringComparison.Ordinal);
        Assert.DoesNotContain("Source: waiting for signal telemetry", telemetryText, StringComparison.Ordinal);
        Assert.DoesNotContain("Target: {GetSelectedResolutionDisplayText()}", telemetryText, StringComparison.Ordinal);
        Assert.Contains("internal static class SourceTelemetryPresentationBuilder", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.Contains("internal static string BuildSourceSummary(SourceSignalTelemetrySnapshot snapshot, DateTimeOffset nowUtc)", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.Contains("internal static string BuildAgeText(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.Contains("TelemetryAgeHelper.ComputeAgeSeconds(timestampUtc, nowUtc)", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.Contains("snapshot.FrameRateArg ??", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.Contains("snapshot.FrameRateExact?.ToString(\"0.###\")", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.Contains("snapshot.IsHdr.HasValue ? (snapshot.IsHdr.Value ? \"HDR\" : \"SDR\") : \"HDR?\"", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.Contains("internal static string BuildTargetSummary(", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.Contains("string.IsNullOrWhiteSpace(hdrRuntimeState) ? \"Unknown\" : hdrRuntimeState", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSelectedResolutionDisplayText()", sourceTelemetryBuilderText, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceTelemetrySummaryText =", sourceTelemetryBuilderText, StringComparison.Ordinal);
    }

    private static object CreateInput(Type type, params (string Property, object? Value)[] values)
    {
        var instance = Activator.CreateInstance(type)
                       ?? throw new InvalidOperationException($"Failed to create {type.FullName}.");
        foreach (var (property, value) in values)
        {
            Set(instance, property, value);
        }

        return instance;
    }

    private static Array InputArray(Type elementType, params object[] values)
    {
        var array = Array.CreateInstance(elementType, values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            array.SetValue(values[i], i);
        }

        return array;
    }

    private static void Set(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        property.SetValue(instance, value);
    }

    private static object? Get(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        return property.GetValue(instance);
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object ParseEnum(string typeName, string value)
        => Enum.Parse(RequireType(typeName), value);

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static string GetStringProperty(object instance, string propertyName)
        => Get(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static object CreateLiveSignalUnavailableRuntime(Type snapshotType)
    {
        var runtime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create unavailable CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(runtime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(runtime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(runtime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(runtime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(runtime, "RequestedPixelFormat", null);
        return runtime;
    }

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

    private static string GetRepoRoot()
        => RuntimeContractSource.GetRepoRoot();

    private static string ExtractTextBetween(string source, string startToken, string endToken)
    {
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Start token '{startToken}' was not found.");
        var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
        Assert.True(end >= 0, $"End token '{endToken}' was not found after '{startToken}'.");
        return source.Substring(start, end - start);
    }
}

public sealed class CaptureConfigurationModelsTests
{
    private enum SetterExpectation
    {
        Set,
        InitOnly,
        None
    }

    private enum NullabilityExpectation
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private enum PropertyScope
    {
        Instance,
        Static
    }

    private sealed record PropertySpec(
        string Name,
        Type Type,
        SetterExpectation Setter,
        NullabilityExpectation Nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation ElementNullability = NullabilityExpectation.NotApplicable,
        PropertyScope Scope = PropertyScope.Instance,
        bool IsRequired = false);


    private static PropertySpec Property(
        string name,
        Type type,
        SetterExpectation setter,
        NullabilityExpectation nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation elementNullability = NullabilityExpectation.NotApplicable,
        PropertyScope scope = PropertyScope.Instance,
        bool isRequired = false)
        => new(name, type, setter, nullability, elementNullability, scope, isRequired);

    private static PropertySpec String(string name, SetterExpectation setter, NullabilityExpectation nullability)
        => Property(name, typeof(string), setter, nullability);

    private static PropertySpec RequiredString(string name, SetterExpectation setter)
        => Property(name, typeof(string), setter, NullabilityExpectation.NotNull, isRequired: true);

    private static PropertySpec RequiredProperty(string name, Type type, SetterExpectation setter)
        => Property(name, type, setter, isRequired: true);

    private static void AssertDeclaredProperties(Type type, IReadOnlyCollection<PropertySpec> expectedProperties)
    {
        var instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var actualNames = type.GetProperties(instanceFlags | staticFlags)
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
            var flags = expected.Scope == PropertyScope.Static ? staticFlags : instanceFlags;
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
                Assert.Equal(expected.Setter == SetterExpectation.InitOnly, IsInitOnlySetter(property));
            }

            AssertNullability(type, property, expected);
        }
    }

    private static void AssertNullability(Type type, PropertyInfo property, PropertySpec expected)
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

    private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags)
    {
        var method = type.GetMethod(name, flags);
        Assert.NotNull(method);
        return method!;
    }

    private static object CreateInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)!;

    private static object? Get(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static)!.GetValue(instance);

    private static T Get<T>(object instance, string propertyName)
        => (T)Get(instance, propertyName)!;

    private static void Set(object instance, string propertyName, object? value)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance | ReflectionFlags.Static)
            ?? type.GetField($"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}", ReflectionFlags.Instance | ReflectionFlags.Static)
            ?? type.GetField(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static object? Invoke(object instance, string methodName)
        => RequireMethod(instance.GetType(), methodName, ReflectionFlags.Instance).Invoke(instance, Array.Empty<object>());

    private static object ParseEnum(Assembly asm, string typeName, string value)
        => Enum.Parse(RequireType(asm, typeName), value);

    private static void AssertEnumValues(Type enumType, params (string Name, int Value)[] expectedValues)
    {
        Assert.Equal(expectedValues.Length, Enum.GetNames(enumType).Length);
        foreach (var (name, value) in expectedValues)
        {
            Assert.Equal(value, Convert.ToInt32(Enum.Parse(enumType, name)));
        }
    }

    [Fact]
    public void CaptureModeOptions_PreserveDisplayTextAndMetadata()
    {
        var asm = SussudioAssembly.Load();
        var resolutionType = RequireType(asm, "Sussudio.Models.ResolutionOption");
        var frameRateType = RequireType(asm, "Sussudio.Models.FrameRateOption");

        AssertDeclaredProperties(
            resolutionType,
            new[]
            {
                RequiredString("Value", SetterExpectation.InitOnly),
                RequiredProperty("Width", typeof(uint), SetterExpectation.InitOnly),
                RequiredProperty("Height", typeof(uint), SetterExpectation.InitOnly),
                Property("IsEnabled", typeof(bool), SetterExpectation.InitOnly),
                String("DisableReason", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                String("DisplayTextOverride", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                String("DisplayText", SetterExpectation.None, NullabilityExpectation.NotNull)
            });
        AssertDeclaredProperties(
            frameRateType,
            new[]
            {
                RequiredProperty("FriendlyValue", typeof(double), SetterExpectation.InitOnly),
                RequiredProperty("Value", typeof(double), SetterExpectation.InitOnly),
                String("Rational", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                Property("Numerator", typeof(uint?), SetterExpectation.InitOnly),
                Property("Denominator", typeof(uint?), SetterExpectation.InitOnly),
                Property("IsEnabled", typeof(bool), SetterExpectation.InitOnly),
                String("DisableReason", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                String("DisplayTextOverride", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                String("DisplayText", SetterExpectation.None, NullabilityExpectation.NotNull)
            });

        var resolution = CreateInstance(resolutionType);
        Set(resolution, "Value", "3840x2160");
        Set(resolution, "Width", 3840u);
        Set(resolution, "Height", 2160u);
        Assert.Equal("3840x2160", Get<string>(resolution, "DisplayText"));
        Assert.Equal(string.Empty, Get<string>(resolution, "DisableReason"));

        Set(resolution, "DisplayTextOverride", "4K UHD");
        Assert.Equal("4K UHD", Get<string>(resolution, "DisplayText"));
        Set(resolution, "DisplayTextOverride", "   ");
        Assert.Equal("3840x2160", Get<string>(resolution, "DisplayText"));

        var frameRate = CreateInstance(frameRateType);
        Set(frameRate, "FriendlyValue", 59.94d);
        Set(frameRate, "Value", 60000d / 1001d);
        Set(frameRate, "Rational", "60000/1001");
        Set(frameRate, "Numerator", 60000u);
        Set(frameRate, "Denominator", 1001u);
        Assert.Equal("60", Get<string>(frameRate, "DisplayText"));
        Set(frameRate, "DisplayTextOverride", "59.94");
        Assert.Equal("59.94", Get<string>(frameRate, "DisplayText"));
        Assert.Equal("60000/1001", Get<string>(frameRate, "Rational"));
    }

    [Fact]
    public void CaptureModeOptionsBuilder_BuildsResolutionAndVideoFormatOptions()
    {
        var asm = SussudioAssembly.Load();
        var builderType = RequireType(asm, "Sussudio.ViewModels.CaptureModeOptionsBuilder");
        var mediaFormatType = RequireType(asm, "Sussudio.Models.MediaFormat");
        var telemetryType = RequireType(asm, "Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildResolutionOptions = RequireMethod(builderType, "BuildResolutionOptions", ReflectionFlags.Static);
        var buildVideoFormatOptions = RequireMethod(builderType, "BuildVideoFormatOptions", ReflectionFlags.Static);

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x1024",
            CreateMediaFormat(mediaFormatType, 1280, 1024, 60, "P010", isHdr: true));

        var telemetry = CreateInstance(telemetryType);
        Set(telemetry, "Width", 1920);
        Set(telemetry, "Height", 1080);

        var filteredOptions = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new object?[] { formatsByResolution, true, false, telemetry })!)
            .Cast<object>()
            .ToArray();
        Assert.Equal(2, filteredOptions.Length);
        Assert.Equal("3840x2160", Get<string>(filteredOptions[0], "Value"));
        Assert.True(Get<bool>(filteredOptions[0], "IsEnabled"));
        var sdrOnlyResolution = filteredOptions.Single(option => Get<string>(option, "Value") == "1920x1080");
        Assert.False(Get<bool>(sdrOnlyResolution, "IsEnabled"));
        Assert.Equal("HDR mode is not supported at this resolution.", Get<string>(sdrOnlyResolution, "DisableReason"));

        var unfilteredOptions = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new object?[] { formatsByResolution, true, true, telemetry })!)
            .Cast<object>()
            .ToArray();
        Assert.Contains(unfilteredOptions, option => Get<string>(option, "Value") == "1280x1024");

        var videoFormats = CreateMediaFormatList(
            mediaFormatType,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "mjpg", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "NV12", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "nv12", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, " ", isHdr: false));
        var videoOptions = ((IEnumerable)buildVideoFormatOptions.Invoke(null, new object?[] { videoFormats })!)
            .Cast<string>()
            .ToArray();
        Assert.Equal(new[] { "Auto", "NV12", "MJPG", "P010" }, videoOptions);
    }

    [Theory]
    [InlineData(null, 1080, 1280u, 1024u, true)]
    [InlineData(1920, null, 1280u, 1024u, true)]
    [InlineData(0, 1080, 1280u, 1024u, true)]
    [InlineData(1920, 0, 1280u, 1024u, true)]
    [InlineData(-1920, 1080, 1280u, 1024u, true)]
    [InlineData(1920, 1080, 0u, 1024u, true)]
    [InlineData(1920, 1080, 1280u, 0u, true)]
    [InlineData(1920, 1080, 3840u, 2160u, true)]
    [InlineData(1920, 1080, 3840u, 2159u, false)]
    [InlineData(65536, 1, 65536u, 65537u, false)]
    [InlineData(int.MaxValue, int.MaxValue - 1, 4294967294u, 4294967292u, true)]
    [InlineData(int.MaxValue, int.MaxValue - 1, 4294967294u, 4294967293u, false)]
    [InlineData(int.MaxValue, int.MaxValue, uint.MaxValue, uint.MaxValue, true)]
    public void CaptureModeOptionsBuilder_PreservesAspectRatioBoundaryBehavior(
        int? sourceWidth,
        int? sourceHeight,
        uint optionWidth,
        uint optionHeight,
        bool expectedIncluded)
    {
        var asm = SussudioAssembly.Load();
        var builderType = RequireType(asm, "Sussudio.ViewModels.CaptureModeOptionsBuilder");
        var mediaFormatType = RequireType(asm, "Sussudio.Models.MediaFormat");
        var telemetryType = RequireType(asm, "Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildResolutionOptions = RequireMethod(builderType, "BuildResolutionOptions", ReflectionFlags.Static);
        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "candidate",
            CreateMediaFormat(mediaFormatType, optionWidth, optionHeight, 60, "NV12", isHdr: false));
        var telemetry = CreateInstance(telemetryType);
        Set(telemetry, "Width", sourceWidth);
        Set(telemetry, "Height", sourceHeight);

        var options = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new object?[] { formatsByResolution, false, false, telemetry })!)
            .Cast<object>()
            .ToArray();

        Assert.Equal(expectedIncluded, options.Length == 1);
    }

    [Fact]
    public void DeviceModeSupportPolicy_AppliesElgato4KXHdrUsbLimits()
    {
        var asm = SussudioAssembly.Load();
        var policyType = RequireType(asm, "Sussudio.ViewModels.DeviceModeSupportPolicy");
        var deviceType = RequireType(asm, "Sussudio.Models.CaptureDevice");
        var mediaFormatType = RequireType(asm, "Sussudio.Models.MediaFormat");
        var isSupported = RequireMethod(policyType, "IsSupported", ReflectionFlags.Static);
        var describeUnsupported = RequireMethod(policyType, "DescribeUnsupported", ReflectionFlags.Static);

        var device = CreateInstance(deviceType);
        Set(device, "Id", @"\\?\usb#vid_0fd9&pid_009b&mi_00#unit-test");
        Set(device, "Name", "Game Capture 4K X");

        Assert.True((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 30, "P010", isHdr: true)
        })!);
        Assert.True((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 2560, 1440, 60, "P010", isHdr: true)
        })!);
        Assert.False((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "P010", isHdr: true)
        })!);
        Assert.False((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 2560, 1440, 120, "P010", isHdr: true)
        })!);
        Assert.True((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "MJPG", isHdr: false)
        })!);

        var reason = (string)describeUnsupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "P010", isHdr: true)
        })!;
        Assert.Contains("4K30 or 1440p60", reason);
    }

    [Fact]
    public void CaptureSettings_DefaultsAndOutputContracts()
    {
        var asm = SussudioAssembly.Load();
        var settingsType = RequireType(asm, "Sussudio.Models.CaptureSettings");
        var recordingFormatType = RequireType(asm, "Sussudio.Models.RecordingFormat");
        var videoQualityType = RequireType(asm, "Sussudio.Models.VideoQuality");
        var hdrOutputModeType = RequireType(asm, "Sussudio.Models.HdrOutputMode");
        var previewModeType = RequireType(asm, "Sussudio.Models.PreviewMode");
        var splitEncodeSupportType = RequireType(asm, "Sussudio.Models.SplitEncodeSupport");
        var nvencPresetType = RequireType(asm, "Sussudio.Models.NvencPreset");
        var splitEncodeModeType = RequireType(asm, "Sussudio.Models.SplitEncodeMode");

        AssertEnumValues(recordingFormatType, ("H264Mp4", 0), ("HevcMp4", 1), ("Av1Mp4", 2));
        AssertEnumValues(videoQualityType, ("Auto", 0), ("Low", 1), ("Medium", 2), ("High", 3), ("SuperHigh", 4), ("Custom", 5));
        AssertEnumValues(hdrOutputModeType, ("Off", 0), ("Hdr10Pq", 1));
        AssertEnumValues(previewModeType, ("GpuFast", 0), ("TrueHdr", 1));
        AssertEnumValues(nvencPresetType, ("Auto", 0), ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7), ("Fast", 8), ("Slow", 9));
        AssertEnumValues(splitEncodeModeType, ("Auto", 0), ("Disabled", 1), ("TwoWay", 2), ("ThreeWay", 3), ("ForcedOn", 4));

        AssertDeclaredProperties(
            settingsType,
            new[]
            {
                Property("Width", typeof(uint), SetterExpectation.Set),
                Property("Height", typeof(uint), SetterExpectation.Set),
                Property("FrameRate", typeof(double), SetterExpectation.Set),
                String("RequestedFrameRateArg", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("RequestedFrameRateNumerator", typeof(uint?), SetterExpectation.Set),
                Property("RequestedFrameRateDenominator", typeof(uint?), SetterExpectation.Set),
                String("RequestedPixelFormat", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("Format", recordingFormatType, SetterExpectation.Set),
                Property("Quality", videoQualityType, SetterExpectation.Set),
                Property("NvencPreset", nvencPresetType, SetterExpectation.Set),
                Property("SplitEncodeMode", splitEncodeModeType, SetterExpectation.Set),
                Property("CustomBitrateMbps", typeof(double), SetterExpectation.Set),
                Property("HdrEnabled", typeof(bool), SetterExpectation.Set),
                Property("HdrOutputMode", hdrOutputModeType, SetterExpectation.Set),
                Property("HdrNominalPeakNits", typeof(int), SetterExpectation.Set),
                Property("HdrMaxCll", typeof(int), SetterExpectation.Set),
                Property("HdrMaxFall", typeof(int), SetterExpectation.Set),
                String("HdrMasterDisplayMetadata", SetterExpectation.Set, NullabilityExpectation.NotNull),
                Property("PreviewMode", previewModeType, SetterExpectation.Set),
                String("OutputPath", SetterExpectation.Set, NullabilityExpectation.NotNull),
                Property("AudioEnabled", typeof(bool), SetterExpectation.Set),
                Property("UseCustomAudioInput", typeof(bool), SetterExpectation.Set),
                String("AudioDeviceId", SetterExpectation.Set, NullabilityExpectation.Nullable),
                String("AudioDeviceName", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("MicrophoneEnabled", typeof(bool), SetterExpectation.Set),
                String("MicrophoneDeviceId", SetterExpectation.Set, NullabilityExpectation.Nullable),
                String("MicrophoneDeviceName", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("ForceMjpegDecode", typeof(bool), SetterExpectation.Set),
                Property("FlashbackGpuDecode", typeof(bool), SetterExpectation.Set),
                Property("FlashbackBufferMinutes", typeof(int), SetterExpectation.Set),
                Property("MjpegDecoderCount", typeof(int), SetterExpectation.Set),
                Property("UseMjpegHighFrameRateMode", typeof(bool), SetterExpectation.None)
            });
        AssertDeclaredProperties(
            splitEncodeSupportType,
            new[]
            {
                Property("Supports2Way", typeof(bool), SetterExpectation.InitOnly),
                Property("Supports3Way", typeof(bool), SetterExpectation.InitOnly),
                Property("NvencUnavailable", splitEncodeSupportType, SetterExpectation.None, scope: PropertyScope.Static)
            });

        var settings = CreateInstance(settingsType);
        Assert.Equal(1920u, Get<uint>(settings, "Width"));
        Assert.Equal(1080u, Get<uint>(settings, "Height"));
        Assert.Equal(60d, Get<double>(settings, "FrameRate"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"), Get(settings, "Format"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"), Get(settings, "Quality"));
        Assert.Equal("Auto", Get(settings, "NvencPreset")!.ToString());
        Assert.Equal("Auto", Get(settings, "SplitEncodeMode")!.ToString());
        Assert.Equal(50d, Get<double>(settings, "CustomBitrateMbps"));
        Assert.False(Get<bool>(settings, "HdrEnabled"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.HdrOutputMode", "Hdr10Pq"), Get(settings, "HdrOutputMode"));
        Assert.Equal(1000, Get<int>(settings, "HdrNominalPeakNits"));
        Assert.Equal(string.Empty, Get<string>(settings, "HdrMasterDisplayMetadata"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.PreviewMode", "GpuFast"), Get(settings, "PreviewMode"));
        Assert.True(Get<bool>(settings, "AudioEnabled"));
        Assert.False(Get<bool>(settings, "UseCustomAudioInput"));
        Assert.False(Get<bool>(settings, "MicrophoneEnabled"));
        Assert.False(Get<bool>(settings, "ForceMjpegDecode"));
        Assert.True(Get<bool>(settings, "FlashbackGpuDecode"));
        Assert.Equal(5, Get<int>(settings, "FlashbackBufferMinutes"));
        Assert.Equal(6, Get<int>(settings, "MjpegDecoderCount"));
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        var outputDir = Path.Combine(Path.GetTempPath(), $"capture_settings_{Guid.NewGuid():N}");
        Set(settings, "OutputPath", outputDir);
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        var fullPath = Invoke(settings, "GetFullOutputPath")!.ToString()!;
        Assert.Equal(outputDir, Path.GetDirectoryName(fullPath));
        Assert.Contains("_HEVC.mp4", Path.GetFileName(fullPath), StringComparison.Ordinal);

        var splitSupport = Activator.CreateInstance(splitEncodeSupportType, true, false)!;
        Assert.True(Get<bool>(splitSupport, "Supports2Way"));
        Assert.False(Get<bool>(splitSupport, "Supports3Way"));
        var nvencUnavailable = splitEncodeSupportType.GetProperty("NvencUnavailable", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.False(Get<bool>(nvencUnavailable, "Supports2Way"));
        Assert.False(Get<bool>(nvencUnavailable, "Supports3Way"));
    }

    [Fact]
    public void CaptureSettings_MjpegHighFrameRateMode_HandlesForceCaseAndInstanceState()
    {
        var asm = SussudioAssembly.Load();
        var settingsType = RequireType(asm, "Sussudio.Models.CaptureSettings");
        var method = RequireMethod(settingsType, "IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static);

        Assert.True((bool)method.Invoke(null, new object?[] { "mjpg", 3840u, 2160u, 100d, false, false })!);
        Assert.True((bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60d, false, true })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120d, false, true })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120d, true, true })!);

        var settings = CreateInstance(settingsType);
        Set(settings, "RequestedPixelFormat", "MJPG");
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 120d);
        Set(settings, "HdrEnabled", false);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 60d);
        Set(settings, "HdrEnabled", false);
        Set(settings, "ForceMjpegDecode", true);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
    }

    [Fact]
    public void CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest()
    {
        var settings = CreateInstance(RequireType(SussudioAssembly.Load(), "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 120d);
        Set(settings, "RequestedPixelFormat", "MJPG");
        Set(settings, "HdrEnabled", false);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        Set(settings, "HdrEnabled", false);
        Set(settings, "Width", 1920u);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 60.0);
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"));

        var bps = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Assert.InRange(bps, 150_000_000u, 200_000_000u);

        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 30.0);
        var lowBitrate = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Assert.InRange(lowBitrate, 24_000_000u, 26_000_000u);
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 60.0);
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"));

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        var h264 = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        var hevc = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1 = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));

        Assert.True(hevc < h264);
        Assert.True(av1 < hevc);
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_ClampsCustomQuality()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "Custom"));

        Set(settings, "CustomBitrateMbps", 999.0);
        Assert.Equal(300_000_000u, Convert.ToUInt32(Invoke(settings, "GetTargetBitrate")));
        Set(settings, "CustomBitrateMbps", 0.1);
        Assert.Equal(1_000_000u, Convert.ToUInt32(Invoke(settings, "GetTargetBitrate")));
    }

    [Fact]
    public void CaptureSettings_GetOutputFileName_IncludesFormatSuffix()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1Name = Invoke(settings, "GetOutputFileName")!.ToString()!;
        Assert.Contains("_AV1.", av1Name, StringComparison.Ordinal);
        Assert.Contains(".mp4", av1Name, StringComparison.Ordinal);

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        Assert.Contains("_HEVC.", Invoke(settings, "GetOutputFileName")!.ToString()!, StringComparison.Ordinal);

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        Assert.Contains("_H264.", Invoke(settings, "GetOutputFileName")!.ToString()!, StringComparison.Ordinal);
    }

    [Fact]
    public void CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat()
    {
        var settingsType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.CaptureSettings");
        var method = RequireMethod(settingsType, "IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static);

        Assert.True((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, false, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, true, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120.0, false, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60.0, false, false })!);
    }

    [Fact]
    public void RecordingSettingsSelectionPolicy_PreservesHdrAndSdrChoices()
    {
        AssertRecordingFormatSelection(
            "HDR filters H.264 and falls back to HEVC",
            detectedFormats: new[] { "H.264", "HEVC", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "HDR preserves existing AV1 selection",
            detectedFormats: new[] { "H.264", "HEVC", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "AV1",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "AV1");

        AssertRecordingFormatSelection(
            "HDR falls back to AV1 when HEVC is unavailable",
            detectedFormats: new[] { "H.264", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "AV1" },
            expectedSelectedFormat: "AV1");

        AssertRecordingFormatSelection(
            "HDR preserves last known real formats when refresh has no HDR formats",
            detectedFormats: new[] { "H.264" },
            currentAvailableFormats: new[] { "HEVC", "AV1" },
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "SDR preserves valid current format",
            detectedFormats: new[] { "H.264", "HEVC" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "HEVC",
            isHdrEnabled: false,
            expectedFormats: new[] { "H.264", "HEVC" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "SDR prefers H.264 when current format is unavailable",
            detectedFormats: new[] { "HEVC", "H264" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "VP9",
            isHdrEnabled: false,
            expectedFormats: new[] { "HEVC", "H264" },
            expectedSelectedFormat: "H264");

        AssertRecordingFormatSelection(
            "Empty SDR capabilities fall back to default format",
            detectedFormats: Array.Empty<string>(),
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: null,
            isHdrEnabled: false,
            expectedFormats: new[] { "H.264" },
            expectedSelectedFormat: "H.264");
    }

    [Fact]
    public void RecordingSettingsSelectionPolicy_ParsesModelValues()
    {
        var asm = SussudioAssembly.Load();
        var policyType = RequireType(asm, "Sussudio.ViewModels.RecordingSettingsSelectionPolicy");
        var parseRecordingFormat = RequireMethod(policyType, "ParseRecordingFormat", ReflectionFlags.Static);
        var parseVideoQuality = RequireMethod(policyType, "ParseVideoQuality", ReflectionFlags.Static);
        var clampCustomBitrate = RequireMethod(policyType, "ClampCustomBitrateMbps", ReflectionFlags.Static);

        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"), parseRecordingFormat.Invoke(null, new object?[] { "H.264" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"), parseRecordingFormat.Invoke(null, new object?[] { "HEVC" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"), parseRecordingFormat.Invoke(null, new object?[] { "AV1" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "Auto"), parseVideoQuality.Invoke(null, new object?[] { "Auto" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "SuperHigh"), parseVideoQuality.Invoke(null, new object?[] { "Super High" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"), parseVideoQuality.Invoke(null, new object?[] { "unexpected" }));
        Assert.Equal(1d, (double)clampCustomBitrate.Invoke(null, new object?[] { -5d })!);
        Assert.Equal(42d, (double)clampCustomBitrate.Invoke(null, new object?[] { 42d })!);
        Assert.Equal(300d, (double)clampCustomBitrate.Invoke(null, new object?[] { 999d })!);
    }

    [Fact]
    public void MediaFormat_OwnershipFollowsCaptureWhileCodecMappingStaysWithRecording()
    {
        var captureModels = RuntimeContractSource.ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs");
        var recordingModels = RuntimeContractSource.ReadRepoFile("Sussudio/Models/Recording/RecordingModels.cs");

        Assert.Contains("public class MediaFormat", captureModels);
        Assert.DoesNotContain("public class MediaFormat", recordingModels);
        Assert.DoesNotContain("MapNvencCodecName", captureModels);
        Assert.Contains("public sealed class EncoderSupport", recordingModels);

        var assembly = SussudioAssembly.Load();
        var mediaFormatType = RequireType(assembly, "Sussudio.Models.MediaFormat");
        var encoderSupportType = RequireType(assembly, "Sussudio.Models.EncoderSupport");
        Assert.Null(mediaFormatType.GetMethod("MapNvencCodecName", ReflectionFlags.Static));
        Assert.NotNull(encoderSupportType.GetMethod("MapNvencCodecName", ReflectionFlags.Static));
    }

    [Fact]
    public void EncoderSupport_ComputesAvailabilityAndPreferredEncoders()
    {
        var supportType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.EncoderSupport");
        AssertDeclaredProperties(
            supportType,
            new[]
            {
                Property("HasH264Nvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasHevcNvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasAv1Nvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibX264", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibX265", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibSvtAv1", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibAomAv1", typeof(bool), SetterExpectation.InitOnly),
                Property("HasH264", typeof(bool), SetterExpectation.None),
                Property("HasHevc", typeof(bool), SetterExpectation.None),
                Property("HasAv1", typeof(bool), SetterExpectation.None),
                String("PreferredAv1Encoder", SetterExpectation.None, NullabilityExpectation.Nullable),
                Property("Empty", supportType, SetterExpectation.None, scope: PropertyScope.Static)
            });

        var empty = supportType.GetProperty("Empty", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.False(Get<bool>(empty, "HasH264"));
        Assert.False(Get<bool>(empty, "HasHevc"));
        Assert.False(Get<bool>(empty, "HasAv1"));
        Assert.Null(Get(empty, "PreferredAv1Encoder"));

        var nvencAv1 = CreateInstance(supportType);
        Set(nvencAv1, "HasAv1Nvenc", true);
        Set(nvencAv1, "HasLibSvtAv1", true);
        Assert.True(Get<bool>(nvencAv1, "HasAv1"));
        Assert.Equal("av1_nvenc", Get<string>(nvencAv1, "PreferredAv1Encoder"));

        var svtAv1 = CreateInstance(supportType);
        Set(svtAv1, "HasLibSvtAv1", true);
        Set(svtAv1, "HasLibAomAv1", true);
        Assert.Equal("libsvtav1", Get<string>(svtAv1, "PreferredAv1Encoder"));

        var softwareFallbacks = CreateInstance(supportType);
        Set(softwareFallbacks, "HasLibX264", true);
        Set(softwareFallbacks, "HasLibX265", true);
        Set(softwareFallbacks, "HasLibAomAv1", true);
        Assert.True(Get<bool>(softwareFallbacks, "HasH264"));
        Assert.True(Get<bool>(softwareFallbacks, "HasHevc"));
        Assert.Equal("libaom-av1", Get<string>(softwareFallbacks, "PreferredAv1Encoder"));
    }

    [Fact]
    public void MediaFormat_Equality_WithMatchingRationalFrameRates()
    {
        var mediaFormatType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.MediaFormat");
        var a = CreateMediaFormat(
            mediaFormatType,
            width: 1920u,
            height: 1080u,
            frameRateNumerator: 60000u,
            frameRateDenominator: 1001u,
            pixelFormat: "NV12",
            isHdr: false);
        var b = CreateMediaFormat(
            mediaFormatType,
            width: 1920u,
            height: 1080u,
            frameRateNumerator: 60000u,
            frameRateDenominator: 1001u,
            pixelFormat: "NV12",
            isHdr: false);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void MediaFormat_Inequality_WhenDimensionsDiffer()
    {
        var mediaFormatType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.MediaFormat");
        var a = CreateMediaFormat(
            mediaFormatType,
            width: 1920u,
            height: 1080u,
            frameRate: 60.0,
            pixelFormat: "NV12",
            isHdr: false);
        var b = CreateMediaFormat(
            mediaFormatType,
            width: 3840u,
            height: 2160u,
            frameRate: 60.0,
            pixelFormat: "NV12",
            isHdr: false);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void MediaFormat_GetHashCode_ConsistencyForEqualObjects()
    {
        var mediaFormatType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.MediaFormat");
        var a = CreateMediaFormat(
            mediaFormatType,
            width: 3840u,
            height: 2160u,
            frameRateNumerator: 120000u,
            frameRateDenominator: 1001u,
            pixelFormat: "P010",
            isHdr: true);
        var b = CreateMediaFormat(
            mediaFormatType,
            width: 3840u,
            height: 2160u,
            frameRateNumerator: 120000u,
            frameRateDenominator: 1001u,
            pixelFormat: "P010",
            isHdr: true);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    private static object CreateResolutionFormatDictionary(Type mediaFormatType)
        => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(
               typeof(string),
               typeof(List<>).MakeGenericType(mediaFormatType)))!;

    private static void AddResolutionFormats(object formatsByResolution, Type mediaFormatType, string resolutionKey, params object[] formats)
        => ((IDictionary)formatsByResolution).Add(resolutionKey, CreateMediaFormatList(mediaFormatType, formats));

    private static object CreateMediaFormatList(Type mediaFormatType, params object[] formats)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(mediaFormatType))!;
        foreach (var format in formats)
        {
            list.Add(format);
        }

        return list;
    }

    private static object CreateMediaFormat(Type mediaFormatType, uint width, uint height, double frameRate, string pixelFormat, bool isHdr)
    {
        var format = CreateInstance(mediaFormatType);
        Set(format, "Width", width);
        Set(format, "Height", height);
        Set(format, "FrameRate", frameRate);
        Set(format, "PixelFormat", pixelFormat);
        Set(format, "IsHdr", isHdr);
        return format;
    }

    private static object CreateMediaFormat(
        Type mediaFormatType,
        uint width,
        uint height,
        string pixelFormat,
        bool isHdr,
        double? frameRate = null,
        uint? frameRateNumerator = null,
        uint? frameRateDenominator = null)
    {
        var format = CreateInstance(mediaFormatType);
        Set(format, "Width", width);
        Set(format, "Height", height);
        if (frameRate.HasValue)
        {
            Set(format, "FrameRate", frameRate.Value);
        }

        if (frameRateNumerator.HasValue)
        {
            Set(format, "FrameRateNumerator", frameRateNumerator.Value);
        }

        if (frameRateDenominator.HasValue)
        {
            Set(format, "FrameRateDenominator", frameRateDenominator.Value);
        }

        Set(format, "PixelFormat", pixelFormat);
        Set(format, "IsHdr", isHdr);
        return format;
    }

    private static void AssertRecordingFormatSelection(
        string fieldName,
        string[] detectedFormats,
        string[] currentAvailableFormats,
        string? selectedFormat,
        bool isHdrEnabled,
        string[] expectedFormats,
        string expectedSelectedFormat)
    {
        var asm = SussudioAssembly.Load();
        var policyType = RequireType(asm, "Sussudio.ViewModels.RecordingSettingsSelectionPolicy");
        var select = RequireMethod(policyType, "Select", ReflectionFlags.Static);
        var selection = select.Invoke(
                null,
                new object?[]
                {
                    detectedFormats,
                    currentAvailableFormats,
                    selectedFormat,
                    isHdrEnabled,
                    "H.264",
                    "HEVC",
                    "AV1"
                })!;

        var availableFormats = ((IEnumerable)Get(selection, "AvailableFormats")!)
            .Cast<string>()
            .ToArray();
        var selected = Get<string>(selection, "SelectedFormat");
        Assert.Equal(expectedFormats, availableFormats);
        Assert.Equal(expectedSelectedFormat, selected);
    }
}

public class StatsPresentationTests
{
    [Fact]
    public void FrameTimeGraph_UsesBudgetRangeThatPreservesVisibleHitches()
    {
        var scale120 = Sussudio.Controllers.FrameTimeGraphScale.FromExpectedFps(120);
        Assert.Equal(1000.0 / 120, scale120.FrameBudgetMs, 4);
        Assert.True(scale120.MaximumFrameTimeMs > 3 * scale120.FrameBudgetMs);
        var fallback = Sussudio.Controllers.FrameTimeGraphScale.FromExpectedFps(0);
        Assert.Equal(60, fallback.ExpectedFps);
        Assert.True(fallback.MaximumFrameTimeMs > 50);
    }

    [Fact]
    public void FrameTimeGraphGeometry_HasIndependentTimestampOwner()
    {
        var owner = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeGraphGeometry.cs");
        var composition = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs");
        Assert.Contains("internal static class FrameTimeGraphGeometry", owner);
        Assert.Contains("TimestampQpc", owner);
        Assert.DoesNotContain("FrameTimeOverlayGeometry", composition);
        Assert.DoesNotContain("Points.Clear", composition);
    }

    [Fact]
    public void DockEncoderPresentation_FormatsCodecAndBitrate()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.ViewModels.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        object Build(string? codecName, bool recording = true)
        {
            var snapshot = CreateUninitializedObject(snapshotType);
            SetPropertyBackingField(snapshot, "Recording", recording);
            SetPropertyBackingField(snapshot, "EncoderCodecName", codecName);
            SetPropertyBackingField(snapshot, "EncoderWidth", 3840);
            SetPropertyBackingField(snapshot, "EncoderHeight", 2160);
            SetPropertyBackingField(snapshot, "EncoderFrameRate", 59.94);
            SetPropertyBackingField(snapshot, "EncoderTargetBitRate", 50_000_000u);
            SetPropertyBackingField(snapshot, "AvSyncEncoderDriftMs", (double?)2.25d);
            SetPropertyBackingField(snapshot, "AvSyncEncoderCorrectionSamples", (long?)7L);
            SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", string.Empty);

            return buildDockPresentation.Invoke(null, new[] { snapshot })
                ?? throw new InvalidOperationException("BuildDockPresentation returned null.");
        }

        var hevc = Build("hevc_nvenc");
        Assert.True(GetBoolProperty(hevc, "EncoderActive"));
        Assert.Equal("HEVC (NVENC)", GetStringProperty(hevc, "EncoderCodec"));
        Assert.Equal("3840 x 2160", GetStringProperty(hevc, "EncoderResolution"));
        Assert.Equal("59.94 fps", GetStringProperty(hevc, "EncoderFrameRate"));
        Assert.Equal("50 Mbps", GetStringProperty(hevc, "EncoderBitrate"));
        Assert.True(GetBoolProperty(hevc, "EncoderDriftVisible"));
        Assert.Equal("+2.2ms (7 corr)", GetStringProperty(hevc, "EncoderDrift"));

        var av1 = Build("av1_nvenc");
        Assert.Equal("AV1 (NVENC)", GetStringProperty(av1, "EncoderCodec"));

        var passthrough = Build("software_custom");
        Assert.Equal("software_custom", GetStringProperty(passthrough, "EncoderCodec"));

        var inactive = Build(null);
        Assert.False(GetBoolProperty(inactive, "EncoderActive"));
        Assert.Equal(string.Empty, GetStringProperty(inactive, "EncoderCodec"));

        var idleDrift = Build("h264_nvenc", recording: false);
        Assert.False(GetBoolProperty(idleDrift, "EncoderDriftVisible"));
        Assert.Equal(string.Empty, GetStringProperty(idleDrift, "EncoderDrift"));
    }

    [Fact]
    public void WindowPresentation_FormatsDetachedWindowText()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.ViewModels.StatsSnapshot");
        var buildWindowPresentation = builderType.GetMethod("BuildStatsWindowPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildStatsWindowPresentation was not found.");

        var snapshot = CreateUninitializedObject(snapshotType);
        SetPropertyBackingField(snapshot, "Previewing", true);
        SetPropertyBackingField(snapshot, "Recording", false);
        SetPropertyBackingField(snapshot, "DiagnosticHealthStatus", "Healthy");
        SetPropertyBackingField(snapshot, "DiagnosticLikelyStage", "none");
        SetPropertyBackingField(snapshot, "DiagnosticEvidence", string.Empty);
        SetPropertyBackingField(snapshot, "DiagnosticSummary", "All monitored frame lanes are within current thresholds.");
        SetPropertyBackingField(snapshot, "SourceWidth", (int?)3840);
        SetPropertyBackingField(snapshot, "SourceHeight", (int?)2160);
        SetPropertyBackingField(snapshot, "SourceFrameRateExact", (double?)119.88d);
        SetPropertyBackingField(snapshot, "SourceIsHdr", (bool?)true);
        SetPropertyBackingField(snapshot, "SourceColorimetry", "BT.2020");
        SetPropertyBackingField(snapshot, "SourceVideoFormat", "YCbCr422");
        SetPropertyBackingField(snapshot, "TelemetryOrigin", "NativeXu");
        SetPropertyBackingField(snapshot, "TelemetryConfidence", "High");
        SetPropertyBackingField(snapshot, "SourceObservedFps", 119.8d);
        SetPropertyBackingField(snapshot, "SourceExpectedFps", 120d);
        SetPropertyBackingField(snapshot, "SourceAvgIntervalMs", 8.333d);
        SetPropertyBackingField(snapshot, "SourceP95IntervalMs", 8.75d);
        SetPropertyBackingField(snapshot, "SourceJitterMs", 0.125d);
        SetPropertyBackingField(snapshot, "SourceSevereGaps", 2L);
        SetPropertyBackingField(snapshot, "SourceEstDrops", 3L);
        SetPropertyBackingField(snapshot, "SourceEstDropPct", 0.25d);
        SetPropertyBackingField(snapshot, "PreviewObservedFps", 118.2d);
        SetPropertyBackingField(snapshot, "PreviewAvgIntervalMs", 8.44d);
        SetPropertyBackingField(snapshot, "PreviewP95IntervalMs", 9.1d);
        SetPropertyBackingField(snapshot, "PreviewSlowFrames", 4L);
        SetPropertyBackingField(snapshot, "PreviewSlowPct", 1.5d);
        SetPropertyBackingField(snapshot, "PipelineLatencyMs", 3.4d);
        SetPropertyBackingField(snapshot, "SourceFramesDelivered", 500L);
        SetPropertyBackingField(snapshot, "SourceFramesDropped", 5L);
        SetPropertyBackingField(snapshot, "RendererFramesRendered", 490L);
        SetPropertyBackingField(snapshot, "RendererFramesDropped", 6L);
        SetPropertyBackingField(snapshot, "PerformanceScore", 98.75d);

        var presentation = buildWindowPresentation.Invoke(null, new[] { snapshot })
            ?? throw new InvalidOperationException("BuildStatsWindowPresentation returned null.");

        Assert.Equal("Previewing", GetStringProperty(presentation, "SessionState"));
        Assert.Equal("Healthy", GetStringProperty(presentation, "DiagnosticStatus"));
        Assert.Equal("All monitored frame lanes are within current thresholds.", GetStringProperty(presentation, "DiagnosticEvidence"));
        Assert.Equal("3840 x 2160", GetStringProperty(presentation, "SourceResolution"));
        Assert.Equal("119.88 fps", GetStringProperty(presentation, "SourceFrameRate"));
        Assert.Equal("On (BT.2020)", GetStringProperty(presentation, "SourceHdr"));
        Assert.Equal("YCbCr422", GetStringProperty(presentation, "SourceFormat"));
        Assert.Equal("NativeXu (High)", GetStringProperty(presentation, "TelemetryOrigin"));
        Assert.Equal("119.80", GetStringProperty(presentation, "SourceFps"));
        Assert.Equal("8.33ms avg", GetStringProperty(presentation, "SourceAvg"));
        Assert.Equal("3 drops (0.3%)", GetStringProperty(presentation, "SourceDrops"));
        Assert.Equal("4 frames (1.5%)", GetStringProperty(presentation, "PreviewSlow"));
        Assert.Equal("3.40ms avg", GetStringProperty(presentation, "PipelineLatency"));
        Assert.Equal("98.8 / 100", GetStringProperty(presentation, "PerformanceScore"));

        var telemetryDetails = GetPropertyValue(presentation, "TelemetryDetails")
            ?? throw new InvalidOperationException("StatsWindowPresentation.TelemetryDetails was null.");
        Assert.True(GetBoolProperty(telemetryDetails, "IsEmpty"));
        Assert.Equal("All monitored frame lanes are within current thresholds.", GetStringProperty(telemetryDetails, "EmptyText"));
    }

    [Fact]
    public void VisualPresentation_TreatsExpectedDisplayRepeatAsGood()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.ViewModels.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        var snapshot = CreateUninitializedObject(snapshotType);
        SetPropertyBackingField(snapshot, "Previewing", true);
        SetPropertyBackingField(snapshot, "SourceExpectedFps", 60d);
        SetPropertyBackingField(snapshot, "SourceFrameRateExact", (double?)60d);
        SetPropertyBackingField(snapshot, "VisualCadenceSamples", 120);
        SetPropertyBackingField(snapshot, "VisualCadenceOutputFps", 120d);
        SetPropertyBackingField(snapshot, "VisualCadenceChangeFps", 60d);
        SetPropertyBackingField(snapshot, "VisualCadenceRepeatPercent", 50d);
        SetPropertyBackingField(snapshot, "VisualCadenceLongestRepeatRun", 1L);
        SetPropertyBackingField(snapshot, "VisualCadenceMotionScore", 12.5d);
        SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", "High");

        var presentation = buildDockPresentation.Invoke(null, new[] { snapshot })
            ?? throw new InvalidOperationException("BuildDockPresentation returned null.");

        Assert.Equal("120 Hz", GetStringProperty(presentation, "SummaryVisualFps"));
        Assert.Equal("crop 120 Hz", GetStringProperty(presentation, "VisualFps"));
        Assert.Equal("12.5% px / High", GetStringProperty(presentation, "VisualMotion"));
        Assert.Equal("Good", GetPropertyValue(presentation, "SummaryVisualFpsStatus")?.ToString());
        Assert.Equal("Good", GetPropertyValue(presentation, "VisualFpsStatus")?.ToString());
    }

    [Fact]
    public void CompactPreviewSummary_UsesCurrentFrameTimeAndOnePercentLow()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs");
        var dockPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs");
        var statsSnapshotProviderText = statsOverlayCompositionText;
        var frameTimeOverlayControllerText = statsOverlayCompositionText;
        var frameTimeOverlayGeometryText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeGraphGeometry.cs");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs");
        var statsSnapshotBuilderText = statsSnapshotText;
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs");

        Assert.Contains("PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0", statsSnapshotProviderText);
        Assert.Contains("PreviewOnePercentLowFps: StatsPresentationBuilder.Sanitize(renderer.PreviewOnePercentLowFps)", statsSnapshotBuilderText);
        AssertStatsPresentationPreviewFormattingLivesInBuilder(
            statsPresentationText,
            statsOverlayText,
            statsOverlayCompositionText,
            frameTimeOverlayControllerText);
        Assert.Contains("internal static class FrameTimeGraphGeometry", frameTimeOverlayGeometryText);
        Assert.Contains("SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);", dockPresentationControllerText);
        Assert.Contains("SetTextIfChanged(_context.PreviewFpsValue, presentation.PreviewFps);", dockPresentationControllerText);
        Assert.DoesNotContain("SetMetricBrush(Stats_SummaryRendererFpsValue", statsOverlayText);
        Assert.Contains("double PreviewOnePercentLowFps", statsSnapshotText);
        Assert.DoesNotContain("double PreviewFivePercentLowFps", statsWindowText);
        Assert.Contains("x:Name=\"Stats_SummaryRendererFpsValue\"", mainWindowXaml);
        Assert.Contains("TextWrapping=\"Wrap\"", mainWindowXaml);
        Assert.Contains("RequestedTheme=\"Dark\"", mainWindowXaml);
    }

[Fact]
    public void StatsPresentationLogic_LivesInFocusedBuilder()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var statsDockRefreshControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var frameTimeOverlayText = statsOverlayCompositionText;
        var frameTimeOverlayControllerText = statsOverlayCompositionText;
        var frameTimeOverlayGeometryText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeGraphGeometry.cs");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationModelsText = statsPresentationText;
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");
        var statsWindowPresentationControllerText = statsWindowText;
        var statsWindowTelemetryDetailsControllerText = statsWindowPresentationControllerText;

        Assert.Contains("internal static class StatsPresentationBuilder", statsPresentationText, StringComparison.Ordinal);
        Assert.DoesNotContain("internal static partial class StatsPresentationBuilder", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("public static StatsDockPresentation BuildDockPresentation(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string ResolvePreviewResolutionText(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string ResolveCaptureSummaryText(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("public static StatsFrameTimePresentation BuildFrameTimePresentation(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string FormatVisualRepeatSummary(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string FormatVisualCadenceSummary(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string FormatVisualMotionSummary(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string FormatHz(double value)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private const double VisualRepeatTolerancePercent = 0.25;", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static bool IsVisualRepeatWithinExpectedDrift(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static double GetExpectedVisualRepeatPercent(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static StatsEncoderPresentation BuildEncoderPresentation(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string FormatEncoderCodecName(string codecName)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string FormatEncoderBitrate(uint targetBitRate)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static string FormatEncoderDrift(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("public static StatsDiagnosticRowsPresentation BuildDiagnosticRows(", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static List<(string Label, string Value)> ParseDiagnosticSummary", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareDecodeRows(", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("StatsHardwareDecodeRowsInput mjpeg)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(StatsHardwareGpuRowsInput? nvml)", statsPresentationText, StringComparison.Ordinal);
        Assert.DoesNotContain("using Sussudio.Services.Gpu;", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("public static StatsDiagnosticSummary BuildStatsDiagnosticSummary(", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("DiagnosticThresholds.CalculatePercent(rendererDrops, rendererSubmitted)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static StatsMetricStatus ResolveFrameLaneStatus(", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static StatsMetricStatus ResolveDecodedVisualStatus(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("public static StatsWindowPresentation BuildStatsWindowPresentation(StatsSnapshot snapshot)", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("private static StatsWindowTelemetryDetailsPresentation BuildStatsWindowTelemetryDetails(", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("internal sealed record StatsDockPresentation(", statsPresentationModelsText, StringComparison.Ordinal);
        Assert.Contains("internal sealed record StatsWindowPresentation(", statsPresentationModelsText, StringComparison.Ordinal);
        Assert.Contains("internal sealed record StatsWindowTelemetryDetailsPresentation(", statsPresentationModelsText, StringComparison.Ordinal);
        Assert.Contains("internal sealed record StatsFrameTimePresentation(", statsPresentationModelsText, StringComparison.Ordinal);
        Assert.Contains("internal readonly record struct StatsHardwareRowPresentation(", statsPresentationModelsText, StringComparison.Ordinal);
        Assert.Contains("internal readonly record struct StatsHardwareDecodeRowsInput(", statsPresentationModelsText, StringComparison.Ordinal);
        Assert.Contains("internal readonly record struct StatsHardwareGpuRowsInput(", statsPresentationModelsText, StringComparison.Ordinal);
        Assert.Contains("internal enum StatsMetricStatus", statsPresentationModelsText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "ViewModels", "StatsPresentationModels.cs")),
            "stats presentation DTOs folded into StatsPresentationBuilder.cs");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "ViewModels", "StatsSnapshot.cs")),
            "stats snapshot DTO and builder folded into StatsPresentationBuilder.cs");
        Assert.Contains("var presentation = StatsPresentationBuilder.BuildDockPresentation(snapshot);", statsDockRefreshControllerText, StringComparison.Ordinal);
        Assert.Contains("_frameTimeOverlayPresentationController.Apply(snapshot);", frameTimeOverlayText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class FrameTimeOverlayPresentationController", frameTimeOverlayControllerText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(
                FindRepoRoot(),
                "Sussudio",
                "Controllers",
                "Stats",
                "FrameTimeOverlayPresentationController.cs")),
            "frame-time overlay presentation lives with stats overlay composition ownership");
        Assert.Contains("public void Apply(StatsSnapshot snapshot)", frameTimeOverlayControllerText, StringComparison.Ordinal);
        Assert.Contains("var presentation = StatsPresentationBuilder.BuildFrameTimePresentation(snapshot);", frameTimeOverlayControllerText, StringComparison.Ordinal);
        Assert.Contains("SetTextIfChanged(_context.SourceValue, presentation.SourceText);", frameTimeOverlayControllerText, StringComparison.Ordinal);
        Assert.Contains("internal static class FrameTimeGraphGeometry", frameTimeOverlayGeometryText, StringComparison.Ordinal);
        Assert.Contains("public static bool TryProjectSegment(", frameTimeOverlayGeometryText, StringComparison.Ordinal);
        Assert.Contains("PreviewFrameTimeSampleFlags.GapBefore", frameTimeOverlayGeometryText, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateLine(", frameTimeOverlayControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)", statsDockRefreshControllerText, StringComparison.Ordinal);
        Assert.Contains("var presentation = StatsPresentationBuilder.BuildStatsWindowPresentation(snapshot);", statsWindowText, StringComparison.Ordinal);
        Assert.Contains("_presentationController.Apply(presentation);", statsWindowText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsWindowPresentationController", statsWindowPresentationControllerText, StringComparison.Ordinal);
        Assert.Contains("public void Apply(StatsWindowPresentation presentation)", statsWindowPresentationControllerText, StringComparison.Ordinal);
        Assert.Contains("private readonly StatsWindowTelemetryDetailsController _telemetryDetailsController;", statsWindowPresentationControllerText, StringComparison.Ordinal);
        Assert.Contains("_telemetryDetailsController.Apply(presentation.TelemetryDetails);", statsWindowPresentationControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateTelemetryDetails(StatsWindowTelemetryDetailsPresentation presentation)", statsWindowPresentationControllerText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(
                FindRepoRoot(),
                "Sussudio",
                "Controllers",
                "Stats",
                "StatsWindowPresentationController.cs")),
            "detached stats-window presentation lives with StatsWindow.xaml.cs");
        Assert.Contains("internal sealed class StatsWindowTelemetryDetailsController", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("public void Apply(StatsWindowTelemetryDetailsPresentation presentation)", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.TelemetryDetailsContent.Children.Clear();", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("Text = presentation.EmptyText,", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("Margin = new Thickness(0, 8, 0, 2),", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("HorizontalAlignment = HorizontalAlignment.Right,", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("TextWrapping = TextWrapping.Wrap", statsWindowTelemetryDetailsControllerText, StringComparison.Ordinal);
        Assert.Contains("var telemetryDetailsController = new StatsWindowTelemetryDetailsController(new StatsWindowTelemetryDetailsControllerContext", statsWindowText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static string FormatFps(", statsWindowText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static string FormatMs(", statsWindowText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static string FormatPercent(", statsWindowText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static string FormatSourceHdr(", statsWindowText, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildFrameTimePresentation(snapshot)", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private enum MetricStatus", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static string ResolveCaptureSummaryText", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static List<(string Label, string Value)> ParseDiagnosticSummary", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsPresentationBuilder.BuildDockPresentation(snapshot)", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)", statsOverlayText, StringComparison.Ordinal);
    }

    [Fact]
    public void StatsPanels_UseSourceTelemetry_ForHdmiInput()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsDockRefreshControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");
        var statsWindowXaml = ReadRepoFile("Sussudio/StatsWindow.xaml").Replace("\r\n", "\n");
        var nativeXuText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("var sourceFormat = snapshot.SourceVideoFormat ?? \"\\u2014\";", statsPresentationText, StringComparison.Ordinal);
        Assert.DoesNotContain("var sourceFormat =\n            snapshot.ReaderSourceSubtype ??", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("StatsPresentationBuilder.BuildDockPresentation(snapshot)", statsDockRefreshControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsPresentationBuilder.BuildStatsWindowPresentation(snapshot)", statsWindowText, StringComparison.Ordinal);
        Assert.Contains("SourceHdr: FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry),", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("SourceFormat: snapshot.SourceVideoFormat ?? \"\\u2014\",", statsPresentationText, StringComparison.Ordinal);
        Assert.Contains("Text=\"Video Format\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"Diagnostics_Content\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SourceFormatValue\"", statsWindowXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"TelemetryDetailsContent\"", statsWindowXaml, StringComparison.Ordinal);
        Assert.Contains("VideoFormat = aviInfo.ColorSpace,", nativeXuText, StringComparison.Ordinal);
        Assert.Contains("Colorimetry = aviInfo.Colorimetry,", nativeXuText, StringComparison.Ordinal);
        Assert.Contains("Quantization = aviInfo.Quantization,", nativeXuText, StringComparison.Ordinal);
        Assert.Contains("HdrTransferFunction = ResolveHdrTransferFunction(hdrInfo.Eotf),", nativeXuText, StringComparison.Ordinal);
    }

    [Fact]
    public void StatsDockPresentationApplication_LivesInController()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var statsDockCompositionText = ExtractTextBetween(
            statsOverlayCompositionText,
            "internal sealed class StatsOverlayCompositionController : IDisposable",
            "internal enum StatsDockSimpleRowPool");
        var refreshControllerText = statsOverlayCompositionText;
        var controllerText = refreshControllerText;

        Assert.Contains("private readonly StatsDockRefreshController _statsDockRefreshController;", statsOverlayCompositionText, StringComparison.Ordinal);
        Assert.Contains("private StatsDockRefreshController CreateDockRefreshController(", statsOverlayCompositionText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsDockControllerGraph", statsOverlayCompositionText, StringComparison.Ordinal);
        Assert.Contains("var statsDockPresentationController = new StatsDockPresentationController(context.DockTargets);", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("return CreateRefreshController(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.DoesNotContain("CreatePresentationController(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("private StatsDockRefreshController CreateRefreshController(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.DoesNotContain("public void RefreshDock(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.DoesNotContain("public void RefreshDiagnosticsSection()", statsDockCompositionText, StringComparison.Ordinal);
        AssertOccursBefore(statsOverlayCompositionText, "_frameTimeOverlayPresentationController = CreateFrameTimeOverlayPresentationController(context);", "_statsDockRefreshController = CreateDockRefreshController(context);");
        AssertOccursBefore(statsOverlayCompositionText, "_statsDockRefreshController = CreateDockRefreshController(context);", "_statsOverlayController = CreateOverlayController(context);");
        AssertOccursBefore(statsDockCompositionText, "var statsDockPresentationController = new StatsDockPresentationController(context.DockTargets);", "var statsDockRowChromeController = CreateRowChromeController(context);");
        AssertOccursBefore(statsDockCompositionText, "var statsDockRowChromeController = CreateRowChromeController(context);", "var statsDiagnosticRowsController = CreateDiagnosticRowsController(context);");
        AssertOccursBefore(statsDockCompositionText, "var statsDiagnosticRowsController = CreateDiagnosticRowsController(context);", "var statsHardwareRowsInputProvider = new StatsHardwareRowsInputProvider(context.HardwareSources);");
        AssertOccursBefore(statsDockCompositionText, "var statsHardwareRowsInputProvider = new StatsHardwareRowsInputProvider(context.HardwareSources);", "var statsHardwareRowsController = CreateHardwareRowsController(");
        AssertOccursBefore(statsDockCompositionText, "var statsHardwareRowsController = CreateHardwareRowsController(", "return CreateRefreshController(");
        Assert.Contains("internal sealed class StatsDockRefreshControllerContext", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsDockRefreshController", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("public required Func<bool> IsStatsDockVisible { get; init; }", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("public required Func<bool> IsDiagnosticsSectionVisible { get; init; }", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("public void RefreshDock(StatsSnapshot snapshot, bool refreshDetails)", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("public void RefreshDiagnosticsSection()", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.IsWindowClosing() || !_context.IsStatsDockVisible()", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsPresentationBuilder.BuildDockPresentation(snapshot)", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.DockPresentationController.Apply(presentation);", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.HardwareRowsController.UpdateDecodeSection();", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.HardwareRowsController.UpdateGpuSection();", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("if (!_context.IsDiagnosticsSectionVisible())", refreshControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsDockPresentationControllerContext", controllerText, StringComparison.Ordinal);
        Assert.Contains("public StatsDockPresentationController(StatsOverlayDockTargetsContext context)", controllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsDockPresentationController", controllerText, StringComparison.Ordinal);
        Assert.Contains("public void Apply(StatsDockPresentation presentation)", controllerText, StringComparison.Ordinal);
        Assert.Contains("SetTextIfChanged(_context.SessionStateValue, presentation.SessionState);", controllerText, StringComparison.Ordinal);
        Assert.Contains("SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);", controllerText, StringComparison.Ordinal);
        Assert.Contains("SetVisibilityIfChanged(_context.AvSyncEncoderRow, presentation.EncoderDriftVisible ? Visibility.Visible : Visibility.Collapsed);", controllerText, StringComparison.Ordinal);
        Assert.Contains("SetVisibilityIfChanged(_context.EncoderSection, presentation.EncoderActive ? Visibility.Visible : Visibility.Collapsed);", controllerText, StringComparison.Ordinal);
        Assert.Contains("MetricGoodBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x70, 0xF0, 0x8B))", controllerText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockPresentationController.cs")),
            "stats dock presentation application lives with stats overlay composition ownership");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockRefreshController.cs")),
            "stats dock refresh ownership folded into StatsOverlayCompositionController.cs");
        Assert.DoesNotContain("SetMetricBrush(", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("SetTextIfChanged(Stats_", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static readonly SolidColorBrush MetricNeutralBrush", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsPresentationBuilder.BuildDockPresentation(snapshot)", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateStatsDock()", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void RefreshDiagnosticsSection()", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateDiagnosticsSection(", statsOverlayText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockControllerGraph.Contexts.cs")),
            "stats dock factories use the existing overlay composition context");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockControllerGraph.cs")),
            "stats dock refresh has no runtime graph wrapper");
    }

    [Fact]
    public void StatsDockRowChrome_LivesInFocusedController()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var statsDockCompositionText = ExtractTextBetween(
            statsOverlayCompositionText,
            "internal sealed class StatsOverlayCompositionController : IDisposable",
            "internal enum StatsDockSimpleRowPool");
        var mainWindowText = MainWindowCompositionSource.Read();
        var statsDockRowsText = statsOverlayCompositionText;
        var controllerText = statsDockRowsText;
        var rowChromePresenterText = statsDockRowsText;
        var rowChromeControllerText = statsDockRowsText;
        var refreshControllerText = statsOverlayCompositionText;
        var hardwareRowsControllerStart = refreshControllerText.IndexOf(
            "internal sealed class StatsHardwareRowsController\n",
            StringComparison.Ordinal);
        var hardwareRowsControllerContextText = refreshControllerText.Substring(
            refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsControllerContext", StringComparison.Ordinal),
            refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsInputProvider", StringComparison.Ordinal)
                - refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsControllerContext", StringComparison.Ordinal));
        var hardwareRowsControllerText = hardwareRowsControllerContextText + refreshControllerText.Substring(hardwareRowsControllerStart);
        var hardwareRowsInputProviderText = refreshControllerText.Substring(
            refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsInputProvider", StringComparison.Ordinal),
            hardwareRowsControllerStart
                - refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsInputProvider", StringComparison.Ordinal));
        var hardwareSourcesText = ExtractTextBetween(
            statsOverlayCompositionText,
            "internal sealed class StatsOverlayHardwareSourceContext",
            "internal sealed class StatsOverlayFrameTimeTargetsContext");
        var hardwareRowsInputBuilderText = hardwareRowsInputProviderText;
        var hardwareRowsBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");

        Assert.Contains("private static StatsDiagnosticRowsController CreateDiagnosticRowsController(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("private static StatsDockRowChromeController CreateRowChromeController(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("private static StatsHardwareRowsController CreateHardwareRowsController(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("ResourceOwner = context.Shell.StatsDockPanel", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("DiagnosticsContent = context.DockTargets.DiagnosticsContent", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("RowChromeController = statsDockRowChromeController", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("new StatsHardwareRowsInputProvider(context.HardwareSources)", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("InputProvider = statsHardwareRowsInputProvider", statsDockCompositionText, StringComparison.Ordinal);
        AssertOccursBefore(statsDockCompositionText, "var statsHardwareRowsInputProvider = new StatsHardwareRowsInputProvider(context.HardwareSources);", "var statsHardwareRowsController = CreateHardwareRowsController(");
        Assert.DoesNotContain("GetDecodeRowsInput = () =>", statsDockCompositionText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsHardwareRowsInputBuilder.BuildDecodeRowsInput(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsHardwareRowsInputBuilder.BuildGpuRowsInput(", statsDockCompositionText, StringComparison.Ordinal);
        Assert.Contains("_context.DiagnosticRowsController.UpdateDiagnostics(presentation);", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.HardwareRowsController.UpdateDecodeSection();", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.HardwareRowsController.UpdateGpuSection();", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsHardwareRowsControllerContext", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsHardwareRowsController", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("private const int MaxExpectedDecodeRowCount = 14;", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("private const int FixedGpuRowCount = 10;", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("public void UpdateDecodeSection()", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("public void UpdateGpuSection()", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsPresentationBuilder.BuildHardwareDecodeRows(", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("public required StatsHardwareRowsInputProvider InputProvider { get; init; }", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("var input = _context.InputProvider.GetDecodeRowsInput();", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsPresentationBuilder.BuildHardwareDecodeRows(input.Value)", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsPresentationBuilder.BuildHardwareGpuRows(_context.InputProvider.GetGpuRowsInput())", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("public required Func<StatsHardwareDecodeRowsInput?> GetDecodeRowsInput { get; init; }", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("public required Func<StatsHardwareGpuRowsInput?> GetGpuRowsInput { get; init; }", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsHardwareRowsInputProvider", hardwareRowsInputProviderText, StringComparison.Ordinal);
        Assert.Contains("public StatsHardwareRowsInputProvider(StatsOverlayHardwareSourceContext context)", hardwareRowsInputProviderText, StringComparison.Ordinal);
        Assert.Contains("public required Func<ParallelMjpegDecodePipeline.PipelineTimingMetrics?> GetMjpegPipelineTimingDetails { get; init; }", hardwareSourcesText, StringComparison.Ordinal);
        Assert.Contains("public required Func<int?> GetPendingPreviewFrameCount { get; init; }", hardwareSourcesText, StringComparison.Ordinal);
        Assert.Contains("public required Func<NvmlSnapshot?> GetNvmlSnapshot { get; init; }", hardwareSourcesText, StringComparison.Ordinal);
        Assert.Contains("var mjpegMetrics = _context.GetMjpegPipelineTimingDetails();", hardwareRowsInputProviderText, StringComparison.Ordinal);
        Assert.Contains("if (!mjpegMetrics.HasValue || mjpegMetrics.Value.DecoderCount <= 0)", hardwareRowsInputProviderText, StringComparison.Ordinal);
        Assert.Contains("StatsHardwareRowsInputBuilder.BuildDecodeRowsInput(", hardwareRowsInputProviderText, StringComparison.Ordinal);
        Assert.Contains("_context.GetPendingPreviewFrameCount()", hardwareRowsInputProviderText, StringComparison.Ordinal);
        Assert.Contains("StatsHardwareRowsInputBuilder.BuildGpuRowsInput(_context.GetNvmlSnapshot())", hardwareRowsInputProviderText, StringComparison.Ordinal);
        Assert.Contains("internal static class StatsHardwareRowsInputBuilder", hardwareRowsInputBuilderText, StringComparison.Ordinal);
        Assert.Contains("public static StatsHardwareDecodeRowsInput BuildDecodeRowsInput(", hardwareRowsInputBuilderText, StringComparison.Ordinal);
        Assert.Contains("ParallelMjpegDecodePipeline.PipelineTimingMetrics mjpeg,", hardwareRowsInputBuilderText, StringComparison.Ordinal);
        Assert.Contains("public static StatsHardwareGpuRowsInput? BuildGpuRowsInput(NvmlSnapshot? nvml)", hardwareRowsInputBuilderText, StringComparison.Ordinal);
        Assert.Contains("PcieTxMBps: nvml.PcieTxMBps,", hardwareRowsInputBuilderText, StringComparison.Ordinal);
        Assert.Contains("VramUsedMB: nvml.VramUsedMB,", hardwareRowsInputBuilderText, StringComparison.Ordinal);
        Assert.Contains("GpuPowerW: nvml.GpuPowerW,", hardwareRowsInputBuilderText, StringComparison.Ordinal);
        Assert.Contains("public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareDecodeRows(", hardwareRowsBuilderText, StringComparison.Ordinal);
        Assert.Contains("StatsHardwareDecodeRowsInput mjpeg)", hardwareRowsBuilderText, StringComparison.Ordinal);
        Assert.Contains("public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(StatsHardwareGpuRowsInput? nvml)", hardwareRowsBuilderText, StringComparison.Ordinal);
        Assert.DoesNotContain("using Sussudio.Services.Gpu;", hardwareRowsBuilderText, StringComparison.Ordinal);
        Assert.Contains("using Sussudio.Services.Gpu;", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("internal readonly record struct StatsHardwareRowPresentation(string Label, string Value);", hardwareRowsBuilderText, StringComparison.Ordinal);
        Assert.Contains("internal readonly record struct StatsHardwareDecodeRowsInput(", hardwareRowsBuilderText, StringComparison.Ordinal);
        Assert.Contains("internal readonly record struct StatsHardwareGpuRowsInput(", hardwareRowsBuilderText, StringComparison.Ordinal);
        Assert.Contains("_context.RowChromeController.CollapseSimpleRows(StatsDockSimpleRowPool.Decode);", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.RowChromeController.UpdateSimpleRows(", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsDockSimpleRowPool.Decode,", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsDockSimpleRowPool.Gpu,", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static StatsHardwareDecodeRowsInput CreateDecodeRowsInput(", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static StatsHardwareGpuRowsInput? CreateGpuRowsInput(", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("new StatsHardwareDecodeRowsInput(", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("new StatsHardwareGpuRowsInput(", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("using Sussudio.Services.Gpu;", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("GetMjpegPipelineTimingDetails", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("GetPendingPreviewFrameCount", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("GetNvmlSnapshot", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.Contains("using Sussudio.Services.Gpu;", refreshControllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsDiagnosticRowsControllerContext", controllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsDiagnosticRowsController", controllerText, StringComparison.Ordinal);
        Assert.Contains("public required FrameworkElement ResourceOwner { get; init; }", controllerText, StringComparison.Ordinal);
        Assert.Contains("public required StackPanel DiagnosticsContent { get; init; }", controllerText, StringComparison.Ordinal);
        Assert.Contains("private readonly List<DiagnosticsPoolSlot> _diagnosticsRowPool = new();", controllerText, StringComparison.Ordinal);
        Assert.Contains("private TextBlock? _diagnosticsEmptyStateTextBlock;", controllerText, StringComparison.Ordinal);
        Assert.Contains("private readonly StatsDockRowChromePresenter _rowChrome;", controllerText, StringComparison.Ordinal);
        Assert.Contains("public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)", controllerText, StringComparison.Ordinal);
        Assert.Contains("Text = \"No diagnostics available\",", controllerText, StringComparison.Ordinal);
        Assert.Contains("private void EnsureDiagnosticsPoolCapacity(int requiredCount)", controllerText, StringComparison.Ordinal);
        Assert.Contains("private void UpdateDiagnosticsPoolSlot(", controllerText, StringComparison.Ordinal);
        Assert.Contains("private TextBlock CreateDiagnosticGroupHeader(string title)", controllerText, StringComparison.Ordinal);
        Assert.Contains("var rowSlot = _rowChrome.CreateRowSlot();", controllerText, StringComparison.Ordinal);
        Assert.Contains("_rowChrome.UpdateRowSlot(slot.RowSlot, label, value, alt);", controllerText, StringComparison.Ordinal);
        Assert.Contains("StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.RowSlot.Row, Visibility.Collapsed);", controllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("_context.RowChromeController.UpdateDiagnosticsRows(presentation);", controllerText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDiagnosticRowsController.cs")),
            "diagnostic stats rows folded into StatsOverlayCompositionController.cs");
        Assert.Contains("internal sealed class StatsDockRowChromeControllerContext", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsDockRowChromeController", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("internal enum StatsDockSimpleRowPool", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("private readonly StatsDockRowChromePresenter _rowChrome;", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("private readonly List<StatsDockRowChromeSlot> _decodeRowPool = new();", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("private readonly List<StatsDockRowChromeSlot> _gpuRowPool = new();", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("public void CollapseSimpleRows(StatsDockSimpleRowPool poolKind)", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("public void UpdateSimpleRows(", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("_rowChrome.UpdateRowSlot(pool[i], row.Label, row.Value, alt: (i % 2) != 0);", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("StatsDockRowChromePresenter.CollapseRows(pool, startIndex: rows.Count);", rowChromeControllerText, StringComparison.Ordinal);
        Assert.Contains("internal sealed record StatsDockRowChromeSlot(Border Row, TextBlock Label, TextBlock Value);", rowChromePresenterText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class StatsDockRowChromePresenter", rowChromePresenterText, StringComparison.Ordinal);
        Assert.Contains("public StatsDockRowChromeSlot CreateRowSlot(string label = \"\", string value = \"\", bool alt = false)", rowChromePresenterText, StringComparison.Ordinal);
        Assert.Contains("public void UpdateRowSlot(StatsDockRowChromeSlot slot, string label, string value, bool alt)", rowChromePresenterText, StringComparison.Ordinal);
        Assert.Contains("Style = GetStyle(\"DockStatsLabelStyle\")", rowChromePresenterText, StringComparison.Ordinal);
        Assert.Contains("Style = GetStyle(\"DockStatsValueStyle\"),", rowChromePresenterText, StringComparison.Ordinal);
        Assert.Contains("=> GetStyle(alt ? \"DockStatsRowAltStyle\" : \"DockStatsRowStyle\");", rowChromePresenterText, StringComparison.Ordinal);
        Assert.Contains("public static void CollapseRows(IReadOnlyList<StatsDockRowChromeSlot> pool, int startIndex = 0)", rowChromePresenterText, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockRowChromePresenter.cs")),
            "stats dock row chrome folded into StatsOverlayCompositionController.cs");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockRowsController.cs")),
            "stats dock row chrome folded into StatsOverlayCompositionController.cs");
        Assert.DoesNotContain("public void UpdateDiagnosticsRows(StatsDiagnosticRowsPresentation presentation)", rowChromeControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private Border CreateRow(", rowChromeControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private Border CreateRow(", controllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("Style = GetStyle(alt ? \"DockStatsRowAltStyle\" : \"DockStatsRowStyle\"),", rowChromeControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("Style = GetStyle(alt ? \"DockStatsRowAltStyle\" : \"DockStatsRowStyle\"),", controllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("new List<StatsHardwareRowPresentation>", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("StatsDiagnosticRowsController", hardwareRowsControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private Border CreateDiagnosticRow(", controllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("_decodeRowPool", mainWindowText, StringComparison.Ordinal);
        Assert.DoesNotContain("_diagnosticsRowPool", mainWindowText, StringComparison.Ordinal);
        Assert.DoesNotContain("private sealed record DiagnosticRowSlot(", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void EnsureDiagnosticRowPool(", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private Border CreateDiagnosticRow(", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateDecodeSection()", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateGpuSection()", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("_statsDiagnosticRowsController.UpdateDiagnostics(presentation);", statsOverlayText, StringComparison.Ordinal);
        Assert.DoesNotContain("new List<StatsHardwareRowPresentation>", statsOverlayText, StringComparison.Ordinal);
    }

    private static void AssertStatsPresentationPreviewFormattingLivesInBuilder(
        string statsPresentationText,
        string statsOverlayText,
        string frameTimeOverlayText,
        string frameTimeOverlayControllerText)
    {
        Assert.Contains("private static string FormatPreviewCadenceSummary(StatsSnapshot snapshot)", statsPresentationText);
        Assert.Contains("private static double ResolveCurrentPreviewFrameTimeMs(StatsSnapshot snapshot)", statsPresentationText);
        Assert.Contains("ResolveCurrentPreviewFrameTimeMs(snapshot)", statsPresentationText);
        Assert.Contains("P99 equivalent {FormatFps(snapshot.PreviewOnePercentLowFps)} fps", statsPresentationText);
        Assert.Contains("{currentFrameTime}\\n{p99Equivalent}", statsPresentationText);
        Assert.DoesNotContain("private static string FormatPreviewCadenceSummary(", statsOverlayText);
        Assert.DoesNotContain("private static double ResolveCurrentPreviewFrameTimeMs(", statsOverlayText);
        Assert.DoesNotContain("private static string FormatPreviewCadenceSummary(", frameTimeOverlayText);
        Assert.DoesNotContain("private static double ResolveCurrentPreviewFrameTimeMs(", frameTimeOverlayText);
        Assert.DoesNotContain("private static string FormatPreviewCadenceSummary(", frameTimeOverlayControllerText);
        Assert.DoesNotContain("private static double ResolveCurrentPreviewFrameTimeMs(", frameTimeOverlayControllerText);
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static string ExtractTextBetween(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Start marker '{startMarker}' was not found.");
        }

        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"End marker '{endMarker}' was not found.");
        }

        return source.Substring(start, end - start);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string FindRepoRoot()
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

    private static object CreateUninitializedObject(Type type)
        => RuntimeHelpers.GetUninitializedObject(type);

    private static void SetPropertyBackingField(object instance, string propertyName, object? value)
    {
        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static bool GetBoolProperty(object instance, string propertyName)
        => (bool)(GetPropertyValue(instance, propertyName)
                  ?? throw new InvalidOperationException($"{propertyName} was not a bool."));

    private static double GetDoubleProperty(object instance, string propertyName)
        => Convert.ToDouble(GetPropertyValue(instance, propertyName), System.Globalization.CultureInfo.InvariantCulture);

    private static void AssertNearlyEqual(double expected, double actual, double tolerance)
        => Assert.True(
            Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:0.####}, got {actual:0.####}; tolerance {tolerance:0.####}.");

    private static void AssertOccursBefore(string actual, string first, string second)
    {
        var firstIndex = actual.IndexOf(first, StringComparison.Ordinal);
        if (firstIndex < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{first}'.");
        }

        var secondIndex = actual.IndexOf(second, StringComparison.Ordinal);
        if (secondIndex < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{second}'.");
        }

        Assert.True(firstIndex < secondIndex, $"Expected '{first}' to occur before '{second}'.");
    }
}

public class StatsHardwareRowsTests
{
    [Fact]
    public void HardwareRowsInputProvider_ReturnsNoDecodeInputWithoutMetrics()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getDecodeRowsInput = providerType.GetMethod("GetDecodeRowsInput", ReflectionFlags.Instance)
                                 ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetDecodeRowsInput not found.");
        var nullMetricsProvider = CreateStatsHardwareRowsInputProvider(null, 3, null);
        Assert.Null(getDecodeRowsInput.Invoke(nullMetricsProvider, null));
    }

    [Fact]
    public void HardwareRowsInputProvider_ReturnsNoDecodeInputWithoutDecoders()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getDecodeRowsInput = providerType.GetMethod("GetDecodeRowsInput", ReflectionFlags.Instance)
                                 ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetDecodeRowsInput not found.");

        var zeroDecoderProvider = CreateStatsHardwareRowsInputProvider(
            CreateStatsHardwarePipelineTimingMetrics(decoderCount: 0),
            3,
            null);
        Assert.Null(getDecodeRowsInput.Invoke(zeroDecoderProvider, null));
    }

    [Fact]
    public void HardwareRowsInputProvider_ProjectsPendingPreviewFrames()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getDecodeRowsInput = providerType.GetMethod("GetDecodeRowsInput", ReflectionFlags.Instance)
                                 ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetDecodeRowsInput not found.");

        var validProvider = CreateStatsHardwareRowsInputProvider(
            CreateStatsHardwarePipelineTimingMetrics(),
            7,
            null);
        var decodeInput = getDecodeRowsInput.Invoke(validProvider, null)
                          ?? throw new InvalidOperationException("Provider returned null decode input for valid metrics.");
        Assert.Equal(7, Convert.ToInt32(GetPropertyValue(decodeInput, "PendingPreviewFrameCount"), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void HardwareRowsInputProvider_ReturnsNoGpuInputWithoutGpuTelemetry()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getGpuRowsInput = providerType.GetMethod("GetGpuRowsInput", ReflectionFlags.Instance)
                              ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetGpuRowsInput not found.");
        var validProvider = CreateStatsHardwareRowsInputProvider(CreateStatsHardwarePipelineTimingMetrics(), 7, null);
        Assert.Null(getGpuRowsInput.Invoke(validProvider, null));
    }

    [Fact]
    public void HardwareRowsPresentation_FormatsDecodeRows()
    {
        using var culture = CultureScope.Use("en-US");

        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildDecodeRows = builderType.GetMethod("BuildHardwareDecodeRows", ReflectionFlags.Static)
                              ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareDecodeRows not found.");
        var decodeRows = StatsHardwareRowsToMap(buildDecodeRows.Invoke(null, new object?[]
        {
            CreateStatsHardwareMjpegMetrics()
        }) ?? throw new InvalidOperationException("BuildHardwareDecodeRows returned null."));

        Assert.Equal("4.00ms (250fps peak)", decodeRows["Throughput"]);
        Assert.Equal("8.00 / 12.25ms  avg/P95 (2T)", decodeRows["Decode"]);
        Assert.Equal("0.75 / 1.50ms  avg/P95", decodeRows["Reorder"]);
        Assert.Equal("9.25 / 15.50ms  avg/P95", decodeRows["Pipeline"]);
        Assert.Equal("1,234 emitted / 56 dropped", decodeRows["Frames"]);
        Assert.Equal("compressed=4 (5.0/10.0MB)  reorder=6  preview=3  skips=2", decodeRows["Buffers"]);
        Assert.Equal("4.50 / 7.75ms", decodeRows["Thread 0"]);
        Assert.Equal("5.25 / 8.50ms", decodeRows["Thread 1"]);
    }

    [Fact]
    public void HardwareRowsPresentation_ReportsUnavailableGpuTelemetry()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildGpuRows = builderType.GetMethod("BuildHardwareGpuRows", ReflectionFlags.Static)
                           ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareGpuRows not found.");
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        var unavailableGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new object?[] { null })
                                 ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null for unavailable snapshot."));
        Assert.Equal("NVML not available", unavailableGpuRows["Status"]);
        Assert.Null(buildGpuInput.Invoke(null, new object?[] { null }));
    }

    [Fact]
    public void HardwareRowsPresentation_FormatsGpuRows()
    {
        using var culture = CultureScope.Use("en-US");
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildGpuRows = builderType.GetMethod("BuildHardwareGpuRows", ReflectionFlags.Static)
                           ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareGpuRows not found.");

        var gpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
        {
            CreateStatsHardwareNvmlSnapshot()
        }) ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null."));

        Assert.Equal("RTX Test", gpuRows["GPU"]);
        Assert.Equal("41% (Mem: 52%)", gpuRows["Utilization"]);
        Assert.Equal("13%", gpuRows["NVDEC"]);
        Assert.Equal("17%", gpuRows["NVENC"]);
        Assert.Equal("1.5 MB/s", gpuRows["PCIe TX"]);
        Assert.Equal("2.0 MB/s", gpuRows["PCIe RX"]);
        Assert.Equal("3 / 12 MB", gpuRows["VRAM"]);
        Assert.Equal("66\u00B0C", gpuRows["Temperature"]);
        Assert.Equal("123.5W", gpuRows["Power"]);
        Assert.Equal("2500 MHz (Mem: 7000 MHz)", gpuRows["Clocks"]);
    }

    [Fact]
    public void HardwareRowsPresentation_UsesFallbacksForMissingGpuFields()
    {
        using var culture = CultureScope.Use("en-US");
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildGpuRows = builderType.GetMethod("BuildHardwareGpuRows", ReflectionFlags.Static)
                           ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareGpuRows not found.");

        var fallbackGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
        {
            CreateStatsHardwareNvmlSnapshotWithFallbacks()
        }) ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null for fallback snapshot."));

        Assert.Equal("\u2014", fallbackGpuRows["GPU"]);
        Assert.Equal("0% (Mem: 0%)", fallbackGpuRows["Utilization"]);
        Assert.Equal("0.0 MB/s", fallbackGpuRows["PCIe TX"]);
        Assert.Equal("0 / 0 MB", fallbackGpuRows["VRAM"]);
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateStatsHardwareNvmlSnapshot()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        return buildGpuInput.Invoke(null, new[] { CreateStatsHardwareNvmlTelemetrySnapshot() })
               ?? throw new InvalidOperationException("BuildGpuRowsInput returned null.");
    }

    private static object CreateStatsHardwareNvmlTelemetrySnapshot()
        => InvokeStatsHardwareConstructor(
            RequireType("Sussudio.Services.Gpu.NvmlSnapshot"),
            "RTX Test",
            (uint?)41,
            (uint?)52,
            (uint?)13,
            (uint?)17,
            (uint?)1536,
            (uint?)2048,
            (ulong?)(3UL * 1024UL * 1024UL),
            (ulong?)(12UL * 1024UL * 1024UL),
            (uint?)66,
            (uint?)123456,
            (uint?)2500,
            (uint?)7000);

    private static object CreateStatsHardwareNvmlSnapshotWithFallbacks()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        return buildGpuInput.Invoke(null, new[] { CreateStatsHardwareNvmlTelemetrySnapshotWithFallbacks() })
               ?? throw new InvalidOperationException("BuildGpuRowsInput returned null for fallback snapshot.");
    }

    private static object CreateStatsHardwareNvmlTelemetrySnapshotWithFallbacks()
        => InvokeStatsHardwareConstructor(
            RequireType("Sussudio.Services.Gpu.NvmlSnapshot"),
            " ",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    private static object CreateStatsHardwareMjpegMetrics()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildDecodeInput = inputBuilderType.GetMethod("BuildDecodeRowsInput", ReflectionFlags.Static)
                               ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildDecodeRowsInput not found.");

        return buildDecodeInput.Invoke(null, new object?[] { CreateStatsHardwarePipelineTimingMetrics(), (int?)3 })
               ?? throw new InvalidOperationException("BuildDecodeRowsInput returned null.");
    }

    private static object CreateStatsHardwarePipelineTimingMetrics(int decoderCount = 2)
    {
        var metricsType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PerDecoderMetrics");
        var perDecoder = Array.CreateInstance(perDecoderType, decoderCount);
        if (decoderCount > 0)
        {
            perDecoder.SetValue(InvokeStatsHardwareConstructor(perDecoderType, 0, 5, 4.5d, 7.75d, 9.5d), 0);
        }

        if (decoderCount > 1)
        {
            perDecoder.SetValue(InvokeStatsHardwareConstructor(perDecoderType, 1, 4, 5.25d, 8.5d, 10.25d), 1);
        }

        return InvokeStatsHardwareConstructor(
            metricsType,
            decoderCount,
            9,
            8.0d,
            12.25d,
            14.0d,
            7,
            0.75d,
            1.5d,
            2.25d,
            11,
            9.25d,
            15.5d,
            20.0d,
            1500L,
            1234L,
            56L,
            1240L,
            1234L,
            1L,
            2L,
            3L,
            4L,
            5L,
            6L,
            7L,
            4,
            5L * 1024L * 1024L,
            10L * 1024L * 1024L,
            2L,
            6,
            8,
            6L * 1024L * 1024L,
            1L,
            perDecoder);
    }

    private static object CreateStatsHardwareRowsInputProvider(
        object? mjpegMetrics,
        int? pendingPreviewFrameCount,
        object? nvmlSnapshot)
    {
        var contextType = RequireType("Sussudio.Controllers.StatsOverlayHardwareSourceContext");
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var context = Activator.CreateInstance(contextType)
                      ?? throw new InvalidOperationException("Failed to create StatsOverlayHardwareSourceContext.");

        SetPropertyOrBackingField(
            context,
            "GetMjpegPipelineTimingDetails",
            CreateStatsHardwareProviderCallback(contextType, "GetMjpegPipelineTimingDetails", () => mjpegMetrics));
        SetPropertyOrBackingField(
            context,
            "GetPendingPreviewFrameCount",
            CreateStatsHardwareProviderCallback(contextType, "GetPendingPreviewFrameCount", () => pendingPreviewFrameCount));
        SetPropertyOrBackingField(
            context,
            "GetNvmlSnapshot",
            CreateStatsHardwareProviderCallback(contextType, "GetNvmlSnapshot", () => nvmlSnapshot));

        return Activator.CreateInstance(providerType, context)
               ?? throw new InvalidOperationException("Failed to create StatsHardwareRowsInputProvider.");
    }

    private static Delegate CreateStatsHardwareProviderCallback(
        Type contextType,
        string propertyName,
        Func<object?> callback)
    {
        var property = contextType.GetProperty(propertyName, ReflectionFlags.Instance)
                       ?? throw new InvalidOperationException($"Missing provider context property '{propertyName}'.");
        var invoke = typeof(Func<object?>).GetMethod(nameof(Func<object?>.Invoke))
                     ?? throw new InvalidOperationException("Missing Func<object?>.Invoke.");
        var returnType = property.PropertyType.GetMethod(nameof(Func<object?>.Invoke))?.ReturnType
                         ?? throw new InvalidOperationException($"Provider context property '{propertyName}' was not a delegate.");
        var callbackValue = Expression.Call(Expression.Constant(callback), invoke);
        var convertedValue = Expression.Convert(callbackValue, returnType);
        return Expression.Lambda(property.PropertyType, convertedValue).Compile();
    }

    private static object InvokeStatsHardwareConstructor(Type type, params object?[] arguments)
    {
        var constructor = type.GetConstructors(ReflectionFlags.Instance)
            .Single(candidate => candidate.GetParameters().Length == arguments.Length);
        return constructor.Invoke(arguments);
    }

    private static Dictionary<string, string> StatsHardwareRowsToMap(object rows)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in (IEnumerable)rows)
        {
            if (row == null)
            {
                continue;
            }

            map[GetStringProperty(row, "Label")] = GetStringProperty(row, "Value");
        }

        return map;
    }

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance);
        if (property?.CanWrite == true)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
                    ?? throw new InvalidOperationException($"Property or backing field '{propertyName}' was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previousCulture;
        private readonly CultureInfo _previousUiCulture;

        private CultureScope(CultureInfo culture)
        {
            _previousCulture = CultureInfo.CurrentCulture;
            _previousUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public static CultureScope Use(string cultureName)
            => new(CultureInfo.GetCultureInfo(cultureName));

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
        }
    }
}
