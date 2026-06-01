using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

        AssertContains(automationSnapshotText, "public int MjpegDecodeSampleCount { get; init; }");
        AssertContains(automationSnapshotText, "public int MjpegDecoderCount { get; init; }");
        AssertContains(automationSnapshotText, "public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder { get; init; } = Array.Empty<MjpegDecoderAutomationSnapshot>();");
        AssertContains(automationSnapshotText, "public bool MjpegPreviewJitterEnabled { get; init; }");

        foreach (var propertyName in AutomationSnapshotCpuMjpegMetricProperties)
        {
            AssertNotNull(snapshotType.GetProperty(propertyName), $"AutomationSnapshot.{propertyName}");
        }

        var decoderType = RequireType("Sussudio.Models.MjpegDecoderAutomationSnapshot");
        var perDecoderProperty = snapshotType.GetProperty("MjpegPerDecoder")
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder missing.");
        var elementType = perDecoderProperty.PropertyType.GetElementType()
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder element type missing.");
        AssertEqual(decoderType, elementType, "AutomationSnapshot.MjpegPerDecoder[] element type");

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
        AssertContains(automationSnapshotText, "public bool MjpegPreviewJitterEnabled { get; init; }");
        AssertContains(automationSnapshotText, "public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;");
        AssertContains(automationSnapshotText, "public int MjpegPacketHashSampleCount { get; init; }");
        AssertContains(automationSnapshotText, "public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();");
        AssertContains(automationSnapshotText, "public int VisualCadenceSampleCount { get; init; }");
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
        AssertContains(automationSnapshotText, "public long EstimatedPipelineLatencyMs { get; init; }");
        AssertContains(automationSnapshotText, "public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(automationSnapshotText, "public int MjpegDecodeSampleCount { get; init; }");
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
        AssertContains(automationSnapshotText, "public bool FlashbackActive { get; init; }");
        AssertContains(automationSnapshotText, "public int FlashbackAudioQueueCapacity { get; init; }");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackState { get; init; }");
        AssertContains(automationSnapshotText, "public bool FlashbackExportActive { get; init; }");
        AssertContains(automationSnapshotText, "public bool FlashbackForceRotateActive { get; init; }");
        AssertContains(automationSnapshotText, "public long FlashbackVideoFramesSubmittedToEncoder { get; init; }");
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
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackState { get; init; } = \"N/A\";");
        AssertContains(automationSnapshotText, "public double[] FlashbackPlaybackRecentFrameIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(automationSnapshotText, "public bool FlashbackExportActive { get; init; }");
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
        AssertContains(automationSnapshotText, "public bool FlashbackExportActive { get; init; }");
        AssertContains(automationSnapshotText, "public string FlashbackExportStatus { get; init; } = \"NotStarted\";");
        AssertContains(automationSnapshotText, "public string? LastExportMessage { get; init; }");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackState { get; init; }");
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
        AssertContains(automationSnapshotText, "public int VisualCadenceSampleCount { get; init; }");
        AssertContains(automationSnapshotText, "public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(automationSnapshotText, "public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder");
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
        AssertNotNull(optionsType.GetProperty("SelectedPreset"), "AutomationOptionsSnapshot.SelectedPreset");
        AssertNotNull(optionsType.GetProperty("SelectedSplitEncodeMode"), "AutomationOptionsSnapshot.SelectedSplitEncodeMode");
        AssertNotNull(optionsType.GetProperty("SelectedVideoFormat"), "AutomationOptionsSnapshot.SelectedVideoFormat");
        AssertNotNull(optionsType.GetProperty("PreviewVolumePercent"), "AutomationOptionsSnapshot.PreviewVolumePercent");
        AssertNotNull(optionsType.GetProperty("IsStatsVisible"), "AutomationOptionsSnapshot.IsStatsVisible");

        var presetsProperty = optionsType.GetProperty("Presets")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.Presets missing.");
        AssertEqual(stringOptionType, presetsProperty.PropertyType.GetElementType(), "AutomationOptionsSnapshot.Presets[] element type");

        var decoderCountsProperty = optionsType.GetProperty("MjpegDecoderCounts")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.MjpegDecoderCounts missing.");
        AssertEqual(intOptionType, decoderCountsProperty.PropertyType.GetElementType(), "AutomationOptionsSnapshot.MjpegDecoderCounts[] element type");

        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        AssertNotNull(snapshotType.GetProperty("SelectedVideoFormat"), "AutomationSnapshot.SelectedVideoFormat");
        AssertNotNull(snapshotType.GetProperty("PreviewVolumePercent"), "AutomationSnapshot.PreviewVolumePercent");
        AssertNotNull(snapshotType.GetProperty("IsStatsVisible"), "AutomationSnapshot.IsStatsVisible");
    }

    [Fact]
    public void CaptureDiagnosticsSnapshot_DefaultsAndRoundTripsCoreTelemetry()
    {
        var diagnosticsRootText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs");
        AssertContains(diagnosticsRootText, "public class CaptureDiagnosticsSnapshot");
        AssertContains(diagnosticsRootText, "public SourceTelemetryAvailability SourceTelemetryAvailability { get; init; } = SourceTelemetryAvailability.Unknown;");
        AssertContains(diagnosticsRootText, "public bool? SourceIsHdr { get; init; }");
        AssertContains(diagnosticsRootText, "public int CaptureCadenceSampleCount { get; init; }");
        AssertContains(diagnosticsRootText, "public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(diagnosticsRootText, "public int RecordingVideoQueueCapacity { get; init; }");
        AssertContains(diagnosticsRootText, "public long AudioChunksDropped { get; init; }");
        AssertContains(diagnosticsRootText, "public bool FlashbackActive { get; init; }");
        AssertContains(diagnosticsRootText, "public bool FlashbackForceRotateActive { get; init; }");
        AssertContains(diagnosticsRootText, "public sealed record MjpegDecoderHealthSnapshot(");
        AssertContains(diagnosticsRootText, "public int MjpegDecodeSampleCount { get; init; }");
        AssertContains(diagnosticsRootText, "public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertDoesNotContain(diagnosticsRootText, "partial class CaptureDiagnosticsSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureDiagnosticsSnapshot.cs")),
            "CaptureDiagnosticsSnapshot.cs folded into CaptureModels.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureSnapshotModels.cs")),
            "CaptureSnapshotModels.cs folded into CaptureModels.cs");

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

        AssertEqual(ParseEnum("Sussudio.Models.CaptureSessionState", "Uninitialized"), GetPropertyValue(snapshot, "SessionState"), "CaptureDiagnosticsSnapshot.SessionState default");
        AssertNonNullStringValue(snapshot, "RecordingBackend", "None", "CaptureDiagnosticsSnapshot.RecordingBackend default");
        AssertNonNullStringValue(snapshot, "AudioPathMode", "None", "CaptureDiagnosticsSnapshot.AudioPathMode default");
        AssertNonNullStringValue(snapshot, "MuxResult", "NotAttempted", "CaptureDiagnosticsSnapshot.MuxResult default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryAvailability"), "CaptureDiagnosticsSnapshot.SourceTelemetryAvailability default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryOrigin"), "CaptureDiagnosticsSnapshot.SourceTelemetryOrigin default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryConfidence"), "CaptureDiagnosticsSnapshot.SourceTelemetryConfidence default");
        AssertNonNullStringValue(snapshot, "SourceTelemetryOriginDetail", "Unknown", "CaptureDiagnosticsSnapshot.SourceTelemetryOriginDetail default");
        AssertNonNullStringValue(snapshot, "SourceTelemetryBackend", "Unknown", "CaptureDiagnosticsSnapshot.SourceTelemetryBackend default");
        AssertNonNullStringValue(snapshot, "SourceTelemetryCircuitState", "Closed", "CaptureDiagnosticsSnapshot.SourceTelemetryCircuitState default");
        AssertNonNullStringValue(snapshot, "HdrAutoDowngradeReason", string.Empty, "CaptureDiagnosticsSnapshot.HdrAutoDowngradeReason default");
        AssertNonNullStringValue(snapshot, "MjpegPacketHashLastHash", string.Empty, "CaptureDiagnosticsSnapshot.MjpegPacketHashLastHash default");
        AssertNonNullStringValue(snapshot, "MjpegPacketHashPattern", "NoSamples", "CaptureDiagnosticsSnapshot.MjpegPacketHashPattern default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentInputIntervalsMs")!), "CaptureDiagnosticsSnapshot.MjpegPacketHashRecentInputIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentUniqueIntervalsMs")!), "CaptureDiagnosticsSnapshot.MjpegPacketHashRecentUniqueIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentDuplicateFlags")!), "CaptureDiagnosticsSnapshot.MjpegPacketHashRecentDuplicateFlags default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "CaptureCadenceRecentIntervalsMs")!), "CaptureDiagnosticsSnapshot.CaptureCadenceRecentIntervalsMs default count");
        AssertNonNullStringValue(snapshot, "VisualCadenceMotionConfidence", "NoSamples", "CaptureDiagnosticsSnapshot.VisualCadenceMotionConfidence default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCadenceRecentOutputIntervalsMs")!), "CaptureDiagnosticsSnapshot.VisualCadenceRecentOutputIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCadenceRecentChangeIntervalsMs")!), "CaptureDiagnosticsSnapshot.VisualCadenceRecentChangeIntervalsMs default count");
        AssertNonNullStringValue(snapshot, "VisualCenterCadenceMotionConfidence", "NoSamples", "CaptureDiagnosticsSnapshot.VisualCenterCadenceMotionConfidence default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCenterCadenceRecentOutputIntervalsMs")!), "CaptureDiagnosticsSnapshot.VisualCenterCadenceRecentOutputIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCenterCadenceRecentChangeIntervalsMs")!), "CaptureDiagnosticsSnapshot.VisualCenterCadenceRecentChangeIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPerDecoder")!), "CaptureDiagnosticsSnapshot.MjpegPerDecoder default count");

        var decoder = CreateMjpegDecoderHealthSnapshot(decoderType, 1, 120, 2.1, 3.4, 5.6);
        var perDecoder = Array.CreateInstance(decoderType, 1);
        perDecoder.SetValue(decoder, 0);
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
        AssertEqual(ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"), GetPropertyValue(snapshot, "SessionState"), "CaptureDiagnosticsSnapshot.SessionState round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "IsRecording"), "CaptureDiagnosticsSnapshot.IsRecording round-trip");
        AssertEqual("FFmpeg", GetStringProperty(snapshot, "RecordingBackend"), "CaptureDiagnosticsSnapshot.RecordingBackend round-trip");
        AssertEqual(3840, Convert.ToInt32(GetPropertyValue(snapshot, "NegotiatedWidth")), "CaptureDiagnosticsSnapshot.NegotiatedWidth round-trip");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"), GetPropertyValue(snapshot, "SourceTelemetryOrigin"), "CaptureDiagnosticsSnapshot.SourceTelemetryOrigin round-trip");
        AssertEqual(1, GetCountProperty(GetPropertyValue(snapshot, "MjpegPerDecoder")!), "CaptureDiagnosticsSnapshot.MjpegPerDecoder round-trip count");
        AssertEqual(1, GetIntProperty(roundTripDecoder, "WorkerIndex"), "MjpegDecoderHealthSnapshot.WorkerIndex round-trip");
        AssertEqual(120, GetIntProperty(roundTripDecoder, "SampleCount"), "MjpegDecoderHealthSnapshot.SampleCount round-trip");
        AssertEqual(2.1, GetDoubleProperty(roundTripDecoder, "AvgMs"), "MjpegDecoderHealthSnapshot.AvgMs round-trip");
        AssertEqual(3.4, GetDoubleProperty(roundTripDecoder, "P95Ms"), "MjpegDecoderHealthSnapshot.P95Ms round-trip");
        AssertEqual(5.6, GetDoubleProperty(roundTripDecoder, "MaxMs"), "MjpegDecoderHealthSnapshot.MaxMs round-trip");
        AssertEqual(2L, GetLongProperty(snapshot, "VideoDropsQueueSaturated"), "CaptureDiagnosticsSnapshot.VideoDropsQueueSaturated round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "RecordingEncodingFailed"), "CaptureDiagnosticsSnapshot.RecordingEncodingFailed round-trip");
        AssertEqual("InvalidOperationException", GetStringProperty(snapshot, "RecordingEncodingFailureType"), "CaptureDiagnosticsSnapshot.RecordingEncodingFailureType round-trip");
        AssertEqual(360, GetIntProperty(snapshot, "RecordingVideoQueueCapacity"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueCapacity round-trip");
        AssertEqual(12, GetIntProperty(snapshot, "RecordingVideoQueueMaxDepth"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueMaxDepth round-trip");
        AssertEqual(11L, GetLongProperty(snapshot, "RecordingVideoFramesSubmittedToEncoder"), "CaptureDiagnosticsSnapshot.RecordingVideoFramesSubmittedToEncoder round-trip");
        AssertEqual(10L, GetLongProperty(snapshot, "RecordingVideoEncoderPacketsWritten"), "CaptureDiagnosticsSnapshot.RecordingVideoEncoderPacketsWritten round-trip");
        AssertEqual(12L, GetLongProperty(snapshot, "RecordingVideoEncoderPts"), "CaptureDiagnosticsSnapshot.RecordingVideoEncoderPts round-trip");
        AssertEqual(1L, GetLongProperty(snapshot, "RecordingVideoEncoderDroppedFrames"), "CaptureDiagnosticsSnapshot.RecordingVideoEncoderDroppedFrames round-trip");
        AssertEqual(2L, GetLongProperty(snapshot, "RecordingVideoSequenceGaps"), "CaptureDiagnosticsSnapshot.RecordingVideoSequenceGaps round-trip");
        AssertEqual(8L, GetLongProperty(snapshot, "RecordingVideoQueueOldestFrameAgeMs"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueOldestFrameAgeMs round-trip");
        AssertEqual(4.5, GetDoubleProperty(snapshot, "RecordingVideoQueueLatencyP95Ms"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueLatencyP95Ms round-trip");
        AssertEqual(6.5, GetDoubleProperty(snapshot, "RecordingVideoQueueLatencyP99Ms"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueLatencyP99Ms round-trip");
        AssertEqual(20L, GetLongProperty(snapshot, "RecordingVideoBackpressureWaitMs"), "CaptureDiagnosticsSnapshot.RecordingVideoBackpressureWaitMs round-trip");
        AssertEqual(2L, GetLongProperty(snapshot, "RecordingVideoBackpressureEvents"), "CaptureDiagnosticsSnapshot.RecordingVideoBackpressureEvents round-trip");
        AssertEqual(6L, GetLongProperty(snapshot, "RecordingVideoBackpressureLastWaitMs"), "CaptureDiagnosticsSnapshot.RecordingVideoBackpressureLastWaitMs round-trip");
        AssertEqual(14L, GetLongProperty(snapshot, "RecordingVideoBackpressureMaxWaitMs"), "CaptureDiagnosticsSnapshot.RecordingVideoBackpressureMaxWaitMs round-trip");
        AssertEqual(4L, GetLongProperty(snapshot, "RecordingGpuFramesDropped"), "CaptureDiagnosticsSnapshot.RecordingGpuFramesDropped round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackEncodingFailed"), "CaptureDiagnosticsSnapshot.FlashbackEncodingFailed round-trip");
        AssertEqual(2_000_000L, GetLongProperty(snapshot, "FlashbackTotalBytesWritten"), "CaptureDiagnosticsSnapshot.FlashbackTotalBytesWritten round-trip");
        AssertEqual(1_000_000L, GetLongProperty(snapshot, "FlashbackTempDriveFreeBytes"), "CaptureDiagnosticsSnapshot.FlashbackTempDriveFreeBytes round-trip");
        AssertEqual(100_000L, GetLongProperty(snapshot, "FlashbackStartupCacheBudgetBytes"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheBudgetBytes round-trip");
        AssertEqual(120_000L, GetLongProperty(snapshot, "FlashbackStartupCacheBytes"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheBytes round-trip");
        AssertEqual(3, GetIntProperty(snapshot, "FlashbackStartupCacheSessionCount"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheSessionCount round-trip");
        AssertEqual(2, GetIntProperty(snapshot, "FlashbackStartupCacheDeletedSessionCount"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheDeletedSessionCount round-trip");
        AssertEqual(80_000L, GetLongProperty(snapshot, "FlashbackStartupCacheFreedBytes"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheFreedBytes round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackStartupCacheOverBudget"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheOverBudget round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FatalCleanupInProgress"), "CaptureDiagnosticsSnapshot.FatalCleanupInProgress round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackCleanupInProgress"), "CaptureDiagnosticsSnapshot.FlashbackCleanupInProgress round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackForceRotateActive"), "CaptureDiagnosticsSnapshot.FlashbackForceRotateActive round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackForceRotateRequested"), "CaptureDiagnosticsSnapshot.FlashbackForceRotateRequested round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackForceRotateDraining"), "CaptureDiagnosticsSnapshot.FlashbackForceRotateDraining round-trip");
        AssertEqual(180, GetIntProperty(snapshot, "FlashbackVideoQueueCapacity"), "CaptureDiagnosticsSnapshot.FlashbackVideoQueueCapacity round-trip");
        AssertEqual(21L, GetLongProperty(snapshot, "FlashbackVideoFramesSubmittedToEncoder"), "CaptureDiagnosticsSnapshot.FlashbackVideoFramesSubmittedToEncoder round-trip");
        AssertEqual(20L, GetLongProperty(snapshot, "FlashbackVideoEncoderPacketsWritten"), "CaptureDiagnosticsSnapshot.FlashbackVideoEncoderPacketsWritten round-trip");
        AssertEqual(3L, GetLongProperty(snapshot, "FlashbackVideoSequenceGaps"), "CaptureDiagnosticsSnapshot.FlashbackVideoSequenceGaps round-trip");
        AssertEqual(9L, GetLongProperty(snapshot, "FlashbackVideoQueueOldestFrameAgeMs"), "CaptureDiagnosticsSnapshot.FlashbackVideoQueueOldestFrameAgeMs round-trip");
        AssertEqual(5.5, GetDoubleProperty(snapshot, "FlashbackVideoQueueLatencyP95Ms"), "CaptureDiagnosticsSnapshot.FlashbackVideoQueueLatencyP95Ms round-trip");
        AssertEqual(7.5, GetDoubleProperty(snapshot, "FlashbackVideoQueueLatencyP99Ms"), "CaptureDiagnosticsSnapshot.FlashbackVideoQueueLatencyP99Ms round-trip");
        AssertEqual(30L, GetLongProperty(snapshot, "FlashbackVideoBackpressureWaitMs"), "CaptureDiagnosticsSnapshot.FlashbackVideoBackpressureWaitMs round-trip");
        AssertEqual(3L, GetLongProperty(snapshot, "FlashbackVideoBackpressureEvents"), "CaptureDiagnosticsSnapshot.FlashbackVideoBackpressureEvents round-trip");
        AssertEqual(7L, GetLongProperty(snapshot, "FlashbackVideoBackpressureLastWaitMs"), "CaptureDiagnosticsSnapshot.FlashbackVideoBackpressureLastWaitMs round-trip");
        AssertEqual(15L, GetLongProperty(snapshot, "FlashbackVideoBackpressureMaxWaitMs"), "CaptureDiagnosticsSnapshot.FlashbackVideoBackpressureMaxWaitMs round-trip");
        AssertEqual(5L, GetLongProperty(snapshot, "FlashbackGpuFramesDropped"), "CaptureDiagnosticsSnapshot.FlashbackGpuFramesDropped round-trip");
        AssertEqual(3L, GetLongProperty(snapshot, "AudioChunksDropped"), "CaptureDiagnosticsSnapshot.AudioChunksDropped round-trip");
        var decoderJsonRoundTrip = ReflectionJsonRoundTrip(decoderType, decoder);
        AssertEqual(120, GetIntProperty(decoderJsonRoundTrip, "SampleCount"), "MjpegDecoderHealthSnapshot JSON SampleCount");
        var jsonRoundTrip = ReflectionJsonRoundTrip(snapshotType, snapshot);
        AssertEqual("FFmpeg", GetStringProperty(jsonRoundTrip, "RecordingBackend"), "CaptureDiagnosticsSnapshot JSON RecordingBackend");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "RecordingEncodingFailed"), "CaptureDiagnosticsSnapshot JSON RecordingEncodingFailed");
        AssertEqual(2_000_000L, GetLongProperty(jsonRoundTrip, "FlashbackTotalBytesWritten"), "CaptureDiagnosticsSnapshot JSON FlashbackTotalBytesWritten");
        AssertEqual(120_000L, GetLongProperty(jsonRoundTrip, "FlashbackStartupCacheBytes"), "CaptureDiagnosticsSnapshot JSON FlashbackStartupCacheBytes");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FatalCleanupInProgress"), "CaptureDiagnosticsSnapshot JSON FatalCleanupInProgress");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackCleanupInProgress"), "CaptureDiagnosticsSnapshot JSON FlashbackCleanupInProgress");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateActive"), "CaptureDiagnosticsSnapshot JSON FlashbackForceRotateActive");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateRequested"), "CaptureDiagnosticsSnapshot JSON FlashbackForceRotateRequested");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateDraining"), "CaptureDiagnosticsSnapshot JSON FlashbackForceRotateDraining");
        AssertEqual(180, GetIntProperty(jsonRoundTrip, "FlashbackVideoQueueCapacity"), "CaptureDiagnosticsSnapshot JSON FlashbackVideoQueueCapacity");
        AssertEqual(1, GetCountProperty(GetPropertyValue(jsonRoundTrip, "MjpegPerDecoder")!), "CaptureDiagnosticsSnapshot JSON MjpegPerDecoder count");
        AssertEqual(1, GetIntProperty(GetSingleEnumerableItem(GetPropertyValue(jsonRoundTrip, "MjpegPerDecoder")!), "WorkerIndex"), "CaptureDiagnosticsSnapshot JSON MjpegPerDecoder WorkerIndex");

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
        AssertEqual(0, GetCountProperty(GetPropertyValue(health, "SourceTelemetryDetails")!), "CaptureHealthSnapshot.SourceTelemetryDetails default count");
    }

    private static object CreateSourceTelemetryDetailEntry(Type detailType)
    {
        var detailEntry = Activator.CreateInstance(detailType, "Signal", "Colorimetry", "BT.2020", "bt2020")
            ?? throw new InvalidOperationException("Failed to create SourceTelemetryDetailEntry.");
        return detailEntry;
    }

    private static void AssertSourceTelemetryDetailEntryValues(object detailEntry)
    {
        AssertEqual("Signal", GetStringProperty(detailEntry, "Group"), "SourceTelemetryDetailEntry.Group");
        AssertEqual("Colorimetry", GetStringProperty(detailEntry, "Label"), "SourceTelemetryDetailEntry.Label");
        AssertEqual("BT.2020", GetStringProperty(detailEntry, "DisplayValue"), "SourceTelemetryDetailEntry.DisplayValue");
        AssertEqual("bt2020", GetStringProperty(detailEntry, "RawValue"), "SourceTelemetryDetailEntry.RawValue");
    }

    private static void AssertSourceTelemetryDetailEntryJsonRoundTrip(Type detailType, object detailEntry)
    {
        var detailJsonRoundTrip = ReflectionJsonRoundTrip(detailType, detailEntry);
        AssertEqual("BT.2020", GetStringProperty(detailJsonRoundTrip, "DisplayValue"), "SourceTelemetryDetailEntry JSON DisplayValue");
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
        AssertEqual("FFmpeg", GetStringProperty(health, "RecordingBackend"), "CaptureHealthSnapshot inherited RecordingBackend round-trip");
        AssertEqual(123456L, GetLongProperty(health, "FlashbackOutputBytes"), "CaptureHealthSnapshot.FlashbackOutputBytes round-trip");
        AssertEqual("flashback.ts", GetStringProperty(health, "FlashbackFilePath"), "CaptureHealthSnapshot.FlashbackFilePath round-trip");
        AssertEqual("Paused", GetStringProperty(health, "FlashbackPlaybackState"), "CaptureHealthSnapshot.FlashbackPlaybackState round-trip");
        AssertEqual("D3D11", GetStringProperty(health, "FlashbackDecoderHwAccel"), "CaptureHealthSnapshot.FlashbackDecoderHwAccel round-trip");
        AssertEqual(4L, GetLongProperty(health, "FlashbackPlaybackDroppedFrames"), "CaptureHealthSnapshot.FlashbackPlaybackDroppedFrames round-trip");
        AssertEqual(2L, GetLongProperty(health, "FlashbackPlaybackSegmentSwitches"), "CaptureHealthSnapshot.FlashbackPlaybackSegmentSwitches round-trip");
        AssertEqual(3L, GetLongProperty(health, "FlashbackPlaybackFmp4Reopens"), "CaptureHealthSnapshot.FlashbackPlaybackFmp4Reopens round-trip");
        AssertEqual(5L, GetLongProperty(health, "FlashbackPlaybackWriteHeadWaits"), "CaptureHealthSnapshot.FlashbackPlaybackWriteHeadWaits round-trip");
        AssertEqual(1L, GetLongProperty(health, "FlashbackPlaybackNearLiveSnaps"), "CaptureHealthSnapshot.FlashbackPlaybackNearLiveSnaps round-trip");
        AssertEqual(0L, GetLongProperty(health, "FlashbackPlaybackDecodeErrorSnaps"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeErrorSnaps round-trip");
        AssertEqual(6L, GetLongProperty(health, "FlashbackPlaybackSubmitFailures"), "CaptureHealthSnapshot.FlashbackPlaybackSubmitFailures round-trip");
        AssertEqual(666L, GetLongProperty(health, "FlashbackPlaybackLastDropUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastDropUtcUnixMs round-trip");
        AssertEqual("av_sync_skip", GetStringProperty(health, "FlashbackPlaybackLastDropReason"), "CaptureHealthSnapshot.FlashbackPlaybackLastDropReason round-trip");
        AssertEqual(777L, GetLongProperty(health, "FlashbackPlaybackLastSubmitFailureUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastSubmitFailureUtcUnixMs round-trip");
        AssertEqual("seek:null_texture", GetStringProperty(health, "FlashbackPlaybackLastSubmitFailure"), "CaptureHealthSnapshot.FlashbackPlaybackLastSubmitFailure round-trip");
        AssertEqual(123L, GetLongProperty(health, "FlashbackPlaybackLastSegmentSwitchUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastSegmentSwitchUtcUnixMs round-trip");
        AssertEqual(456L, GetLongProperty(health, "FlashbackPlaybackLastFmp4ReopenUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastFmp4ReopenUtcUnixMs round-trip");
        AssertEqual(789L, GetLongProperty(health, "FlashbackPlaybackLastWriteHeadWaitGapMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastWriteHeadWaitGapMs round-trip");
        AssertEqual(120d, GetDoubleProperty(health, "FlashbackPlaybackTargetFps"), "CaptureHealthSnapshot.FlashbackPlaybackTargetFps round-trip");
        AssertEqual(2L, GetLongProperty(health, "FlashbackPlaybackPtsCadenceMismatchCount"), "CaptureHealthSnapshot.FlashbackPlaybackPtsCadenceMismatchCount round-trip");
        AssertEqual(123456700L, GetLongProperty(health, "FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs round-trip");
        AssertEqual(16.67d, GetDoubleProperty(health, "FlashbackPlaybackLastPtsCadenceDeltaMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastPtsCadenceDeltaMs round-trip");
        AssertEqual(8.33d, GetDoubleProperty(health, "FlashbackPlaybackLastPtsCadenceExpectedMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastPtsCadenceExpectedMs round-trip");
        AssertEqual(3L, GetLongProperty(health, "FlashbackPlaybackSeekForwardDecodeCapHits"), "CaptureHealthSnapshot.FlashbackPlaybackSeekForwardDecodeCapHits round-trip");
        AssertEqual(true, GetBoolProperty(health, "FlashbackPlaybackLastSeekHitForwardDecodeCap"), "CaptureHealthSnapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap round-trip");
        AssertEqual(120, GetIntProperty(health, "FlashbackPlaybackDecodeSampleCount"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeSampleCount round-trip");
        AssertEqual(1.25d, GetDoubleProperty(health, "FlashbackPlaybackDecodeAvgMs"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeAvgMs round-trip");
        AssertEqual(2.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeP95Ms"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeP95Ms round-trip");
        AssertEqual(3.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeP99Ms"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeP99Ms round-trip");
        AssertEqual(4.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeMaxMs"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeMaxMs round-trip");
        AssertEqual("audio", GetStringProperty(health, "FlashbackPlaybackMaxDecodePhase"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodePhase round-trip");
        AssertEqual(0.5d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeReceiveMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeReceiveMs round-trip");
        AssertEqual(4.0d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeFeedMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeFeedMs round-trip");
        AssertEqual(0.75d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeReadMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeReadMs round-trip");
        AssertEqual(3.5d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeSendMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeSendMs round-trip");
        AssertEqual(3.25d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeAudioMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeAudioMs round-trip");
        AssertEqual(0.25d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeConvertMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeConvertMs round-trip");
        AssertEqual(123456789L, GetLongProperty(health, "FlashbackPlaybackMaxDecodeUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeUtcUnixMs round-trip");
        AssertEqual(2345L, GetLongProperty(health, "FlashbackPlaybackMaxDecodePositionMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodePositionMs round-trip");
        AssertEqual(9L, GetLongProperty(health, "FlashbackPlaybackCommandsEnqueued"), "CaptureHealthSnapshot.FlashbackPlaybackCommandsEnqueued round-trip");
        AssertEqual(7L, GetLongProperty(health, "FlashbackPlaybackScrubUpdatesCoalesced"), "CaptureHealthSnapshot.FlashbackPlaybackScrubUpdatesCoalesced round-trip");
        AssertEqual(8L, GetLongProperty(health, "FlashbackPlaybackSeekCommandsCoalesced"), "CaptureHealthSnapshot.FlashbackPlaybackSeekCommandsCoalesced round-trip");
        AssertEqual(256, GetIntProperty(health, "FlashbackPlaybackCommandQueueCapacity"), "CaptureHealthSnapshot.FlashbackPlaybackCommandQueueCapacity round-trip");
        AssertEqual(2, GetIntProperty(health, "FlashbackPlaybackPendingCommands"), "CaptureHealthSnapshot.FlashbackPlaybackPendingCommands round-trip");
        AssertEqual(5, GetIntProperty(health, "FlashbackPlaybackMaxPendingCommands"), "CaptureHealthSnapshot.FlashbackPlaybackMaxPendingCommands round-trip");
        AssertEqual(14L, GetLongProperty(health, "FlashbackPlaybackLastCommandQueueLatencyMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastCommandQueueLatencyMs round-trip");
        AssertEqual(88L, GetLongProperty(health, "FlashbackPlaybackMaxCommandQueueLatencyMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxCommandQueueLatencyMs round-trip");
        AssertEqual("Play", GetStringProperty(health, "FlashbackPlaybackMaxCommandQueueLatencyCommand"), "CaptureHealthSnapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand round-trip");
        AssertEqual("UpdateScrub", GetStringProperty(health, "FlashbackPlaybackLastCommandQueued"), "CaptureHealthSnapshot.FlashbackPlaybackLastCommandQueued round-trip");
        AssertEqual(999L, GetLongProperty(health, "FlashbackPlaybackLastCommandFailureUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs round-trip");
        AssertEqual(11L, GetLongProperty(health, "FlashbackVideoQueueRejectedFrames"), "CaptureHealthSnapshot.FlashbackVideoQueueRejectedFrames round-trip");
        AssertEqual("force_rotate_draining", GetStringProperty(health, "FlashbackVideoQueueLastRejectReason"), "CaptureHealthSnapshot.FlashbackVideoQueueLastRejectReason round-trip");
        AssertEqual(13L, GetLongProperty(health, "FlashbackGpuQueueRejectedFrames"), "CaptureHealthSnapshot.FlashbackGpuQueueRejectedFrames round-trip");
        AssertEqual("encoding_failed:InvalidOperationException", GetStringProperty(health, "FlashbackGpuQueueLastRejectReason"), "CaptureHealthSnapshot.FlashbackGpuQueueLastRejectReason round-trip");
        AssertEqual(true, GetBoolProperty(health, "FlashbackBackendSettingsStale"), "CaptureHealthSnapshot.FlashbackBackendSettingsStale round-trip");
        AssertEqual("preset:P1->P2", GetStringProperty(health, "FlashbackBackendSettingsStaleReason"), "CaptureHealthSnapshot.FlashbackBackendSettingsStaleReason round-trip");
        AssertEqual("HevcMp4", GetStringProperty(health, "FlashbackBackendActiveFormat"), "CaptureHealthSnapshot.FlashbackBackendActiveFormat round-trip");
        AssertEqual("P2", GetStringProperty(health, "FlashbackBackendRequestedPreset"), "CaptureHealthSnapshot.FlashbackBackendRequestedPreset round-trip");
        AssertEqual(true, GetBoolProperty(health, "FlashbackExportActive"), "CaptureHealthSnapshot.FlashbackExportActive round-trip");
        AssertEqual("Running", GetStringProperty(health, "FlashbackExportStatus"), "CaptureHealthSnapshot.FlashbackExportStatus round-trip");
        AssertEqual("NoMediaWritten", GetStringProperty(health, "FlashbackExportFailureKind"), "CaptureHealthSnapshot.FlashbackExportFailureKind round-trip");
        AssertEqual(2L, GetLongProperty(health, "FlashbackExportForceRotateFallbacks"), "CaptureHealthSnapshot.FlashbackExportForceRotateFallbacks round-trip");
        AssertEqual(12345L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackUtcUnixMs"), "CaptureHealthSnapshot.FlashbackExportLastForceRotateFallbackUtcUnixMs round-trip");
        AssertEqual(3, GetIntProperty(health, "FlashbackExportLastForceRotateFallbackSegments"), "CaptureHealthSnapshot.FlashbackExportLastForceRotateFallbackSegments round-trip");
        AssertEqual(1000L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackInPointMs"), "CaptureHealthSnapshot.FlashbackExportLastForceRotateFallbackInPointMs round-trip");
        AssertEqual(9000L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackOutPointMs"), "CaptureHealthSnapshot.FlashbackExportLastForceRotateFallbackOutPointMs round-trip");
        AssertEqual("HevcMp4", GetStringProperty(health, "FlashbackExportVerificationFormat"), "CaptureHealthSnapshot.FlashbackExportVerificationFormat round-trip");
        AssertEqual("AV1->HEVC", GetStringProperty(health, "FlashbackCodecDowngradeReason"), "CaptureHealthSnapshot.FlashbackCodecDowngradeReason round-trip");
        AssertEqual(37.5d, GetDoubleProperty(health, "FlashbackExportPercent"), "CaptureHealthSnapshot.FlashbackExportPercent round-trip");
        AssertEqual(2000L, GetLongProperty(health, "FlashbackExportElapsedMs"), "CaptureHealthSnapshot.FlashbackExportElapsedMs round-trip");
        AssertEqual(100L, GetLongProperty(health, "FlashbackExportLastProgressAgeMs"), "CaptureHealthSnapshot.FlashbackExportLastProgressAgeMs round-trip");
        AssertEqual(1048576L, GetLongProperty(health, "FlashbackExportOutputBytes"), "CaptureHealthSnapshot.FlashbackExportOutputBytes round-trip");
        AssertEqual(524288d, GetDoubleProperty(health, "FlashbackExportThroughputBytesPerSec"), "CaptureHealthSnapshot.FlashbackExportThroughputBytesPerSec round-trip");
        AssertEqual(3, GetIntProperty(health, "FlashbackExportSegmentsProcessed"), "CaptureHealthSnapshot.FlashbackExportSegmentsProcessed round-trip");
        AssertEqual(42L, GetLongProperty(health, "LastExportId"), "CaptureHealthSnapshot.LastExportId round-trip");
        AssertEqual("YCbCr422", GetStringProperty(health, "SourceVideoFormat"), "CaptureHealthSnapshot.SourceVideoFormat round-trip");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(health, "SourceHdrTransferCode")), "CaptureHealthSnapshot.SourceHdrTransferCode round-trip");
        AssertEqual(1, GetCountProperty(GetPropertyValue(health, "SourceTelemetryDetails")!), "CaptureHealthSnapshot.SourceTelemetryDetails round-trip count");
        AssertEqual("Signal", GetStringProperty(roundTripDetail, "Group"), "SourceTelemetryDetailEntry.Group round-trip");
        AssertEqual("Colorimetry", GetStringProperty(roundTripDetail, "Label"), "SourceTelemetryDetailEntry.Label round-trip");
        AssertEqual("BT.2020", GetStringProperty(roundTripDetail, "DisplayValue"), "SourceTelemetryDetailEntry.DisplayValue round-trip");
        AssertEqual("bt2020", GetStringProperty(roundTripDetail, "RawValue"), "SourceTelemetryDetailEntry.RawValue round-trip");
        AssertEqual(17L, GetLongProperty(health, "LastVideoEnqueueAgeMs"), "CaptureHealthSnapshot.LastVideoEnqueueAgeMs round-trip");
        AssertEqual(-1.5d, (double)GetPropertyValue(health, "AvSyncCaptureDriftMs")!, "CaptureHealthSnapshot.AvSyncCaptureDriftMs round-trip");
        AssertEqual(48L, Convert.ToInt64(GetPropertyValue(health, "AvSyncEncoderCorrectionSamples")), "CaptureHealthSnapshot.AvSyncEncoderCorrectionSamples round-trip");
    }

    private static void AssertCaptureHealthSnapshotJsonRoundTrip(Type healthType, object health)
    {
        var jsonRoundTrip = ReflectionJsonRoundTrip(healthType, health);
        AssertEqual("Paused", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackState"), "CaptureHealthSnapshot JSON FlashbackPlaybackState");
        AssertEqual(6L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackSubmitFailures"), "CaptureHealthSnapshot JSON FlashbackPlaybackSubmitFailures");
        AssertEqual(666L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastDropUtcUnixMs"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastDropUtcUnixMs");
        AssertEqual("av_sync_skip", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackLastDropReason"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastDropReason");
        AssertEqual(777L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastSubmitFailureUtcUnixMs"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        AssertEqual("seek:null_texture", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackLastSubmitFailure"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastSubmitFailure");
        AssertEqual(9L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackCommandsEnqueued"), "CaptureHealthSnapshot JSON FlashbackPlaybackCommandsEnqueued");
        AssertEqual(256, GetIntProperty(jsonRoundTrip, "FlashbackPlaybackCommandQueueCapacity"), "CaptureHealthSnapshot JSON FlashbackPlaybackCommandQueueCapacity");
        AssertEqual(999L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastCommandFailureUtcUnixMs"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertEqual(11L, GetLongProperty(jsonRoundTrip, "FlashbackVideoQueueRejectedFrames"), "CaptureHealthSnapshot JSON FlashbackVideoQueueRejectedFrames");
        AssertEqual("force_rotate_draining", GetStringProperty(jsonRoundTrip, "FlashbackVideoQueueLastRejectReason"), "CaptureHealthSnapshot JSON FlashbackVideoQueueLastRejectReason");
        AssertEqual(13L, GetLongProperty(jsonRoundTrip, "FlashbackGpuQueueRejectedFrames"), "CaptureHealthSnapshot JSON FlashbackGpuQueueRejectedFrames");
        AssertEqual("encoding_failed:InvalidOperationException", GetStringProperty(jsonRoundTrip, "FlashbackGpuQueueLastRejectReason"), "CaptureHealthSnapshot JSON FlashbackGpuQueueLastRejectReason");
        AssertEqual(2L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackPtsCadenceMismatchCount"), "CaptureHealthSnapshot JSON FlashbackPlaybackPtsCadenceMismatchCount");
        AssertEqual(16.67d, GetDoubleProperty(jsonRoundTrip, "FlashbackPlaybackLastPtsCadenceDeltaMs"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastPtsCadenceDeltaMs");
        AssertEqual(3L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackSeekForwardDecodeCapHits"), "CaptureHealthSnapshot JSON FlashbackPlaybackSeekForwardDecodeCapHits");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackPlaybackLastSeekHitForwardDecodeCap"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastSeekHitForwardDecodeCap");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackBackendSettingsStale"), "CaptureHealthSnapshot JSON FlashbackBackendSettingsStale");
        AssertEqual("preset:P1->P2", GetStringProperty(jsonRoundTrip, "FlashbackBackendSettingsStaleReason"), "CaptureHealthSnapshot JSON FlashbackBackendSettingsStaleReason");
        AssertEqual("Running", GetStringProperty(jsonRoundTrip, "FlashbackExportStatus"), "CaptureHealthSnapshot JSON FlashbackExportStatus");
        AssertEqual("NoMediaWritten", GetStringProperty(jsonRoundTrip, "FlashbackExportFailureKind"), "CaptureHealthSnapshot JSON FlashbackExportFailureKind");
        AssertEqual(2L, GetLongProperty(jsonRoundTrip, "FlashbackExportForceRotateFallbacks"), "CaptureHealthSnapshot JSON FlashbackExportForceRotateFallbacks");
        AssertEqual(3, GetIntProperty(jsonRoundTrip, "FlashbackExportLastForceRotateFallbackSegments"), "CaptureHealthSnapshot JSON FlashbackExportLastForceRotateFallbackSegments");
        AssertEqual("HevcMp4", GetStringProperty(jsonRoundTrip, "FlashbackExportVerificationFormat"), "CaptureHealthSnapshot JSON FlashbackExportVerificationFormat");
        AssertEqual("AV1->HEVC", GetStringProperty(jsonRoundTrip, "FlashbackCodecDowngradeReason"), "CaptureHealthSnapshot JSON FlashbackCodecDowngradeReason");
        AssertEqual(1048576L, GetLongProperty(jsonRoundTrip, "FlashbackExportOutputBytes"), "CaptureHealthSnapshot JSON FlashbackExportOutputBytes");
        AssertEqual(42L, GetLongProperty(jsonRoundTrip, "LastExportId"), "CaptureHealthSnapshot JSON LastExportId");
        AssertEqual("YCbCr422", GetStringProperty(jsonRoundTrip, "SourceVideoFormat"), "CaptureHealthSnapshot JSON SourceVideoFormat");
        AssertEqual(1, GetCountProperty(GetPropertyValue(jsonRoundTrip, "SourceTelemetryDetails")!), "CaptureHealthSnapshot JSON SourceTelemetryDetails count");
        AssertEqual("BT.2020", GetStringProperty(GetSingleEnumerableItem(GetPropertyValue(jsonRoundTrip, "SourceTelemetryDetails")!), "DisplayValue"), "CaptureHealthSnapshot JSON SourceTelemetryDetails DisplayValue");
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
        AssertContains(healthRootText, "public sealed class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot");
        AssertContains(healthRootText, "public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails");
        AssertContains(healthRootText, "public bool FlashbackBackendSettingsStale { get; init; }");
        AssertContains(healthRootText, "public int FlashbackAudioQueueCapacity { get; init; }");
        AssertContains(healthRootText, "public string FlashbackPlaybackState { get; init; } = \"N/A\";");
        AssertContains(healthRootText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(healthRootText, "public string FlashbackExportStatus { get; init; } = \"NotStarted\";");
        AssertContains(healthRootText, "public string? FlashbackExportVerificationFormat { get; init; }");
        AssertDoesNotContain(healthRootText, "partial class CaptureHealthSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureHealthSnapshot.cs")),
            "CaptureHealthSnapshot.cs folded into CaptureModels.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureSnapshotModels.cs")),
            "CaptureSnapshotModels.cs folded into CaptureModels.cs");

        var detailEntry = CreateSourceTelemetryDetailEntry(detailType);
        AssertSourceTelemetryDetailEntryValues(detailEntry);
        AssertSourceTelemetryDetailEntryJsonRoundTrip(detailType, detailEntry);

        var health = CreatePopulatedCaptureHealthSnapshot(healthType, detailType, detailEntry);
        AssertCaptureHealthSnapshotRoundTripValues(health);
        AssertCaptureHealthSnapshotJsonRoundTrip(healthType, health);
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

    [Fact]
    public void CaptureSettings_DefaultsAndOutputContracts()
    {
        var asm = SussudioAssembly.Load();
        var settingsType = RequireType(asm, "Sussudio.Models.CaptureSettings");
        var recordingFormatType = RequireType(asm, "Sussudio.Models.RecordingFormat");
        var videoQualityType = RequireType(asm, "Sussudio.Models.VideoQuality");
        var hdrOutputModeType = RequireType(asm, "Sussudio.Models.HdrOutputMode");
        var previewModeType = RequireType(asm, "Sussudio.Models.PreviewMode");
        var audioPathModeType = RequireType(asm, "Sussudio.Models.AudioPathMode");
        var pipelineOptionsType = RequireType(asm, "Sussudio.Models.RecordingPipelineOptions");
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
                Property("AudioPathMode", audioPathModeType, SetterExpectation.Set),
                Property("PipelineOptions", pipelineOptionsType, SetterExpectation.Set),
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
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.AudioPathMode", "PostMuxDefault"), Get(settings, "AudioPathMode"));
        Assert.NotNull(Get(settings, "PipelineOptions"));
        Assert.False(Get<bool>(settings, "ForceMjpegDecode"));
        Assert.True(Get<bool>(settings, "FlashbackGpuDecode"));
        Assert.Equal(5, Get<int>(settings, "FlashbackBufferMinutes"));
        Assert.Equal(6, Get<int>(settings, "MjpegDecoderCount"));
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        var otherSettings = CreateInstance(settingsType);
        Assert.NotSame(Get(settings, "PipelineOptions"), Get(otherSettings, "PipelineOptions"));

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
    public void RecordingPipelineOptions_DefaultsAndCapacityBounds()
    {
        var asm = SussudioAssembly.Load();
        var optionsType = RequireType(asm, "Sussudio.Models.RecordingPipelineOptions");
        var dropPolicyType = RequireType(asm, "Sussudio.Models.VideoFrameDropPolicy");
        AssertEnumValues(dropPolicyType, ("DropOldest", 0), ("DropNewest", 1));
        AssertDeclaredProperties(
            optionsType,
            new[]
            {
                Property("TargetVideoLatencyMs", typeof(int), SetterExpectation.Set),
                Property("MinBufferedVideoFrames", typeof(int), SetterExpectation.Set),
                Property("MaxBufferedVideoFrames", typeof(int), SetterExpectation.Set),
                Property("VideoDropPolicy", dropPolicyType, SetterExpectation.Set)
            });

        var options = CreateInstance(optionsType);
        var resolve = RequireMethod(optionsType, "ResolveVideoQueueCapacity", ReflectionFlags.Instance);
        Assert.Equal(250, Get<int>(options, "TargetVideoLatencyMs"));
        Assert.Equal(4, Get<int>(options, "MinBufferedVideoFrames"));
        Assert.Equal(30, Get<int>(options, "MaxBufferedVideoFrames"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoFrameDropPolicy", "DropOldest"), Get(options, "VideoDropPolicy"));
        Assert.Equal(15, (int)resolve.Invoke(options, new object[] { 60d })!);
        Assert.Equal(15, (int)resolve.Invoke(options, new object[] { -1d })!);

        Set(options, "TargetVideoLatencyMs", 1);
        Assert.Equal(4, (int)resolve.Invoke(options, new object[] { 60d })!);

        Set(options, "MinBufferedVideoFrames", 0);
        Set(options, "MaxBufferedVideoFrames", 2);
        Assert.Equal(1, (int)resolve.Invoke(options, new object[] { 10d })!);

        Set(options, "TargetVideoLatencyMs", 250);
        Set(options, "MinBufferedVideoFrames", 8);
        Set(options, "MaxBufferedVideoFrames", 4);
        Assert.Equal(8, (int)resolve.Invoke(options, new object[] { 120d })!);

        Set(options, "VideoDropPolicy", ParseEnum(asm, "Sussudio.Models.VideoFrameDropPolicy", "DropNewest"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoFrameDropPolicy", "DropNewest"), Get(options, "VideoDropPolicy"));
    }

    [Fact]
    public void RecordingPipelineOptions_ResolvesVideoQueueCapacity()
    {
        var options = CreateInstance(RequireType(SussudioAssembly.Load(), "Sussudio.Models.RecordingPipelineOptions"));
        var resolve = RequireMethod(options.GetType(), "ResolveVideoQueueCapacity", ReflectionFlags.Instance);

        Assert.Equal(15, (int)resolve.Invoke(options, new object[] { 60.0 })!);
        Assert.Equal(30, (int)resolve.Invoke(options, new object[] { 120.0 })!);
        Assert.Equal(8, (int)resolve.Invoke(options, new object[] { 30.0 })!);
        Assert.Equal(15, (int)resolve.Invoke(options, new object[] { 0.0 })!);
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
