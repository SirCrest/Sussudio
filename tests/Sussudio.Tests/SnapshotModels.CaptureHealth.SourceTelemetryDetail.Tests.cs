using System;

static partial class Program
{
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
}
