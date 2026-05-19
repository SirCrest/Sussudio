using System;
using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
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
        AssertNotNull(optionsType.GetProperty("ShowAllCaptureOptions"), "AutomationOptionsSnapshot.ShowAllCaptureOptions");
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
        AssertNotNull(snapshotType.GetProperty("ShowAllCaptureOptions"), "AutomationSnapshot.ShowAllCaptureOptions");
        AssertNotNull(snapshotType.GetProperty("PreviewVolumePercent"), "AutomationSnapshot.PreviewVolumePercent");
        AssertNotNull(snapshotType.GetProperty("IsStatsVisible"), "AutomationSnapshot.IsStatsVisible");
    }
}
