using System;

namespace Sussudio.ViewModels;

/// <summary>
/// Observable recording-format option rebuilding and selected recording-format refresh.
/// </summary>
public partial class MainViewModel
{
    private void RebuildRecordingFormatOptions()
    {
        var selection = RecordingFormatSelectionPolicy.Select(
            _detectedRecordingFormats,
            AvailableRecordingFormats,
            SelectedRecordingFormat,
            IsHdrEnabled,
            DefaultRecordingFormat,
            HevcRecordingFormat,
            Av1RecordingFormat);

        AvailableRecordingFormats.Clear();
        foreach (var format in selection.AvailableFormats)
        {
            AvailableRecordingFormats.Add(format);
        }

        var previousSelection = SelectedRecordingFormat;
        SelectedRecordingFormat = selection.SelectedFormat;
        if (string.Equals(previousSelection, selection.SelectedFormat, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(SelectedRecordingFormat));
        }

        if (IsHdrEnabled && !RecordingFormatSelectionPolicy.IsHdrCompatible(SelectedRecordingFormat))
        {
            StatusText = "HDR recording requires HEVC or AV1 (10-bit).";
        }

        Logger.Log($"Selected recording format: {SelectedRecordingFormat}");
    }
}
