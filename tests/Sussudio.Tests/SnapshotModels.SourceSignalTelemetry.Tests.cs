using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static Task SourceSignalTelemetrySnapshot_DefaultsHaveExpectedValues()
    {
        var type = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var instance = RuntimeHelpers.GetUninitializedObject(type);

        // Uninitialized record: nullable properties should be default (null for nullable, 0 for value types)
        // Use the factory method to test proper defaults
        var createMethod = type.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!;
        var snapshot = createMethod.Invoke(null, new object?[] { "test-reason", null })!;

        AssertEqual("Unavailable",
            GetStringProperty(snapshot, "Availability"),
            "CreateUnavailable Availability");
        AssertEqual("Unknown",
            GetStringProperty(snapshot, "Origin"),
            "CreateUnavailable Origin");
        AssertEqual("Unavailable",
            GetStringProperty(snapshot, "OriginDetail"),
            "CreateUnavailable OriginDetail");
        AssertContains(GetStringProperty(snapshot, "DiagnosticSummary"), "test-reason");

        return Task.CompletedTask;
    }

    private static Task SourceSignalTelemetrySnapshot_PropertiesRoundTrip()
    {
        var type = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);

        SetPropertyBackingField(snapshot, "Width", (int?)1920);
        SetPropertyBackingField(snapshot, "Height", (int?)1080);
        SetPropertyBackingField(snapshot, "FrameRateExact", (double?)59.94);
        SetPropertyBackingField(snapshot, "IsHdr", (bool?)true);
        SetPropertyBackingField(snapshot, "VideoFormat", "P010");
        SetPropertyBackingField(snapshot, "Firmware", "1.2.3");

        AssertEqual(1920, GetIntProperty(snapshot, "Width"), "Width round-trip");
        AssertEqual(1080, GetIntProperty(snapshot, "Height"), "Height round-trip");
        AssertEqual("P010", GetStringProperty(snapshot, "VideoFormat"), "VideoFormat round-trip");
        AssertEqual("1.2.3", GetStringProperty(snapshot, "Firmware"), "Firmware round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "IsHdr"), "IsHdr round-trip");

        return Task.CompletedTask;
    }

    private static Task SourceSignalTelemetrySnapshot_PreservesFullTelemetryContract()
    {
        var snapshotType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");
        var originType = RequireType("Sussudio.Models.SourceTelemetryOrigin");
        var confidenceType = RequireType("Sussudio.Models.SourceTelemetryConfidence");
        var audioAvailabilityType = RequireType("Sussudio.Models.SourceAudioInputAvailability");
        var audioModeType = RequireType("Sussudio.Models.SourceAudioInputMode");
        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");

        AssertDeclaredProperties(
            snapshotType,
            new SnapshotPropertySpec[]
            {
                new("TimestampUtc", typeof(DateTimeOffset)),
                new("Availability", availabilityType),
                new("Origin", originType),
                NonNullString("OriginDetail"),
                new("Confidence", confidenceType),
                new("Width", typeof(int?)),
                new("Height", typeof(int?)),
                new("FrameRateExact", typeof(double?)),
                NullableString("FrameRateArg"),
                new("IsHdr", typeof(bool?)),
                NullableString("VideoFormat"),
                NullableString("Colorimetry"),
                NullableString("Quantization"),
                NullableString("HdrTransferFunction"),
                new("HdrTransferCode", typeof(int?)),
                NullableString("Firmware"),
                NullableString("AudioFormat"),
                NullableString("AudioSampleRate"),
                NullableString("InputSource"),
                new("AdcOnOff", typeof(bool?)),
                new("AdcVolumeGain", typeof(int?)),
                new("AnalogGainByte", typeof(int?)),
                new("UacVolumeGain", typeof(int?)),
                new("UacOut1Mute", typeof(bool?)),
                new("UacOut2Mute", typeof(bool?)),
                new("UacOut2MixerSource", typeof(int?)),
                NullableString("UsbHostProtocol"),
                new("TxEdidValid", typeof(bool?)),
                NullableString("HdcpMode"),
                NullableString("HdcpVersion"),
                NullableString("RxTxHdcpVersion"),
                NullableString("CustomerVersion"),
                new("RescueVersion", typeof(int?)),
                NullableString("RawTimingHex"),
                NonNullRef("DetailEntries", typeof(IReadOnlyList<>).MakeGenericType(detailType), SnapshotNullability.NotNull),
                NullableString("DiagnosticSummary"),
                NullableString("EgavInitializeResultName"),
                NullableString("EgavOpenResultName"),
                NullableString("EgavSignalStatusResultName"),
                NullableString("EgavIsVideoHdrResultName"),
                new("AudioInputAvailability", audioAvailabilityType),
                new("AudioInputMode", typeof(Nullable<>).MakeGenericType(audioModeType)),
                NullableString("AudioInputOrigin"),
                GetterOnly("HasDimensions", typeof(bool)),
                GetterOnly("HasFrameRate", typeof(bool)),
                GetterOnly("HasSignalData", typeof(bool))
            });
        AssertDeclaredProperties(
            detailType,
            new SnapshotPropertySpec[]
            {
                NonNullString("Group"),
                NonNullString("Label"),
                NonNullString("DisplayValue"),
                NullableString("RawValue")
            });

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var snapshot = CreateInstance("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        var timestamp = (DateTimeOffset)GetPropertyValue(snapshot, "TimestampUtc")!;
        if (timestamp < before || timestamp > after)
        {
            throw new InvalidOperationException("SourceSignalTelemetrySnapshot.TimestampUtc should default to current UTC time.");
        }

        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Unknown"), GetPropertyValue(snapshot, "Availability"), "SourceSignalTelemetrySnapshot.Availability default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "Unknown"), GetPropertyValue(snapshot, "Origin"), "SourceSignalTelemetrySnapshot.Origin default");
        AssertNonNullStringValue(snapshot, "OriginDetail", "Unknown", "SourceSignalTelemetrySnapshot.OriginDetail default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Unknown"), GetPropertyValue(snapshot, "Confidence"), "SourceSignalTelemetrySnapshot.Confidence default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceAudioInputAvailability", "Unavailable"), GetPropertyValue(snapshot, "AudioInputAvailability"), "SourceSignalTelemetrySnapshot.AudioInputAvailability default");
        AssertEqual("not-implemented", GetStringProperty(snapshot, "AudioInputOrigin"), "SourceSignalTelemetrySnapshot.AudioInputOrigin default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "DetailEntries")!), "SourceSignalTelemetrySnapshot.DetailEntries default count");
        AssertEqual(false, GetBoolProperty(snapshot, "HasDimensions"), "SourceSignalTelemetrySnapshot.HasDimensions default");
        AssertEqual(false, GetBoolProperty(snapshot, "HasFrameRate"), "SourceSignalTelemetrySnapshot.HasFrameRate default");
        AssertEqual(false, GetBoolProperty(snapshot, "HasSignalData"), "SourceSignalTelemetrySnapshot.HasSignalData default");
        AssertEqual(string.Empty, InvokeInstanceMethod(snapshot, "GetModeKey") as string, "SourceSignalTelemetrySnapshot.GetModeKey default");

        var detailEntry = Activator.CreateInstance(detailType, "Audio / Input", "Analog Gain", "12 dB", "0C")
            ?? throw new InvalidOperationException("Failed to create SourceTelemetryDetailEntry.");
        var details = CreateGenericList(detailType, detailEntry);
        SetPropertyOrBackingField(snapshot, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(snapshot, "Origin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(snapshot, "OriginDetail", "NativeXuAtCommandProvider");
        SetPropertyOrBackingField(snapshot, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(snapshot, "Width", 3840);
        SetPropertyOrBackingField(snapshot, "Height", 2160);
        SetPropertyOrBackingField(snapshot, "FrameRateExact", 120000d / 1001d);
        SetPropertyOrBackingField(snapshot, "FrameRateArg", "120000/1001");
        SetPropertyOrBackingField(snapshot, "IsHdr", true);
        SetPropertyOrBackingField(snapshot, "VideoFormat", "YCbCr422");
        SetPropertyOrBackingField(snapshot, "Colorimetry", "BT.2020");
        SetPropertyOrBackingField(snapshot, "Quantization", "Limited");
        SetPropertyOrBackingField(snapshot, "HdrTransferFunction", "HDR10 / PQ");
        SetPropertyOrBackingField(snapshot, "HdrTransferCode", 2);
        SetPropertyOrBackingField(snapshot, "Firmware", "1.2.3");
        SetPropertyOrBackingField(snapshot, "AudioFormat", "PCM");
        SetPropertyOrBackingField(snapshot, "AudioSampleRate", "48 kHz");
        SetPropertyOrBackingField(snapshot, "InputSource", "HDMI");
        SetPropertyOrBackingField(snapshot, "AdcOnOff", true);
        SetPropertyOrBackingField(snapshot, "AdcVolumeGain", 12);
        SetPropertyOrBackingField(snapshot, "AnalogGainByte", 0x0C);
        SetPropertyOrBackingField(snapshot, "UacVolumeGain", 24);
        SetPropertyOrBackingField(snapshot, "UacOut1Mute", false);
        SetPropertyOrBackingField(snapshot, "UacOut2Mute", true);
        SetPropertyOrBackingField(snapshot, "UacOut2MixerSource", 1);
        SetPropertyOrBackingField(snapshot, "UsbHostProtocol", "Isochronous");
        SetPropertyOrBackingField(snapshot, "TxEdidValid", true);
        SetPropertyOrBackingField(snapshot, "HdcpMode", "Off");
        SetPropertyOrBackingField(snapshot, "HdcpVersion", "0200");
        SetPropertyOrBackingField(snapshot, "RxTxHdcpVersion", "0200/0200");
        SetPropertyOrBackingField(snapshot, "CustomerVersion", "custom-a");
        SetPropertyOrBackingField(snapshot, "RescueVersion", 7);
        SetPropertyOrBackingField(snapshot, "RawTimingHex", "3000CA0830117008");
        SetPropertyOrBackingField(snapshot, "DetailEntries", details);
        SetPropertyOrBackingField(snapshot, "DiagnosticSummary", "ok");
        SetPropertyOrBackingField(snapshot, "EgavInitializeResultName", "Ok");
        SetPropertyOrBackingField(snapshot, "EgavOpenResultName", "Ok");
        SetPropertyOrBackingField(snapshot, "EgavSignalStatusResultName", "Ok");
        SetPropertyOrBackingField(snapshot, "EgavIsVideoHdrResultName", "Ok");
        SetPropertyOrBackingField(snapshot, "AudioInputAvailability", ParseEnum("Sussudio.Models.SourceAudioInputAvailability", "Available"));
        SetPropertyOrBackingField(snapshot, "AudioInputMode", ParseEnum("Sussudio.Models.SourceAudioInputMode", "Analog"));
        SetPropertyOrBackingField(snapshot, "AudioInputOrigin", "native-xu");

        var roundTripDetail = GetSingleEnumerableItem(GetPropertyValue(snapshot, "DetailEntries")!);
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"), GetPropertyValue(snapshot, "Availability"), "SourceSignalTelemetrySnapshot.Availability round-trip");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"), GetPropertyValue(snapshot, "Origin"), "SourceSignalTelemetrySnapshot.Origin round-trip");
        AssertEqual("NativeXuAtCommandProvider", GetStringProperty(snapshot, "OriginDetail"), "SourceSignalTelemetrySnapshot.OriginDetail round-trip");
        AssertEqual(3840, Convert.ToInt32(GetPropertyValue(snapshot, "Width")), "SourceSignalTelemetrySnapshot.Width round-trip");
        AssertEqual("YCbCr422", GetStringProperty(snapshot, "VideoFormat"), "SourceSignalTelemetrySnapshot.VideoFormat round-trip");
        AssertEqual("HDR10 / PQ", GetStringProperty(snapshot, "HdrTransferFunction"), "SourceSignalTelemetrySnapshot.HdrTransferFunction round-trip");
        AssertEqual("PCM", GetStringProperty(snapshot, "AudioFormat"), "SourceSignalTelemetrySnapshot.AudioFormat round-trip");
        AssertEqual(true, (bool)GetPropertyValue(snapshot, "AdcOnOff")!, "SourceSignalTelemetrySnapshot.AdcOnOff round-trip");
        AssertEqual("0200/0200", GetStringProperty(snapshot, "RxTxHdcpVersion"), "SourceSignalTelemetrySnapshot.RxTxHdcpVersion round-trip");
        AssertEqual(1, GetCountProperty(GetPropertyValue(snapshot, "DetailEntries")!), "SourceSignalTelemetrySnapshot.DetailEntries round-trip count");
        AssertEqual("Audio / Input", GetStringProperty(roundTripDetail, "Group"), "SourceSignalTelemetryDetailEntry.Group round-trip");
        AssertEqual("Analog Gain", GetStringProperty(roundTripDetail, "Label"), "SourceSignalTelemetryDetailEntry.Label round-trip");
        AssertEqual("12 dB", GetStringProperty(roundTripDetail, "DisplayValue"), "SourceSignalTelemetryDetailEntry.DisplayValue round-trip");
        AssertEqual("0C", GetStringProperty(roundTripDetail, "RawValue"), "SourceSignalTelemetryDetailEntry.RawValue round-trip");
        AssertEqual(ParseEnum("Sussudio.Models.SourceAudioInputAvailability", "Available"), GetPropertyValue(snapshot, "AudioInputAvailability"), "SourceSignalTelemetrySnapshot.AudioInputAvailability round-trip");
        AssertEqual(ParseEnum("Sussudio.Models.SourceAudioInputMode", "Analog"), GetPropertyValue(snapshot, "AudioInputMode"), "SourceSignalTelemetrySnapshot.AudioInputMode round-trip");
        AssertEqual("native-xu", GetStringProperty(snapshot, "AudioInputOrigin"), "SourceSignalTelemetrySnapshot.AudioInputOrigin round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "HasDimensions"), "SourceSignalTelemetrySnapshot.HasDimensions round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "HasFrameRate"), "SourceSignalTelemetrySnapshot.HasFrameRate round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "HasSignalData"), "SourceSignalTelemetrySnapshot.HasSignalData round-trip");
        AssertEqual("3840x2160@120000/1001:hdr", InvokeInstanceMethod(snapshot, "GetModeKey") as string, "SourceSignalTelemetrySnapshot.GetModeKey round-trip");
        var detailJsonRoundTrip = ReflectionJsonRoundTrip(detailType, detailEntry);
        AssertEqual("Analog Gain", GetStringProperty(detailJsonRoundTrip, "Label"), "SourceTelemetryDetailEntry JSON Label");
        var jsonRoundTrip = ReflectionJsonRoundTrip(snapshotType, snapshot);
        AssertEqual("NativeXuAtCommandProvider", GetStringProperty(jsonRoundTrip, "OriginDetail"), "SourceSignalTelemetrySnapshot JSON OriginDetail");
        AssertEqual("YCbCr422", GetStringProperty(jsonRoundTrip, "VideoFormat"), "SourceSignalTelemetrySnapshot JSON VideoFormat");
        AssertEqual("PCM", GetStringProperty(jsonRoundTrip, "AudioFormat"), "SourceSignalTelemetrySnapshot JSON AudioFormat");
        AssertEqual(1, GetCountProperty(GetPropertyValue(jsonRoundTrip, "DetailEntries")!), "SourceSignalTelemetrySnapshot JSON DetailEntries count");
        AssertEqual("Analog Gain", GetStringProperty(GetSingleEnumerableItem(GetPropertyValue(jsonRoundTrip, "DetailEntries")!), "Label"), "SourceSignalTelemetrySnapshot JSON DetailEntries Label");

        return Task.CompletedTask;
    }

}
