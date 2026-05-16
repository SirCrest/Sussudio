using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class CaptureOptionBindingControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ComboBox ResolutionComboBox { get; init; }
    public required ComboBox FrameRateComboBox { get; init; }
    public required ComboBox FormatComboBox { get; init; }
    public required ComboBox QualityComboBox { get; init; }
    public required ComboBox PresetComboBox { get; init; }
    public required ComboBox SplitEncodeComboBox { get; init; }
    public required ComboBox VideoFormatComboBox { get; init; }
    public required ComboBox DecoderCountComboBox { get; init; }
    public required NumberBox CustomBitrateNumberBox { get; init; }
    public required ToggleButton HdrToggle { get; init; }
    public required ToggleButton TrueHdrPreviewToggle { get; init; }
    public required ToggleButton ShowAllCaptureOptionsToggle { get; init; }
    public required Action ApplyInitialDecoderCountSelection { get; init; }
    public required Action ApplyBitrateVisibility { get; init; }
    public required Action ApplyHdrToggleEnabledState { get; init; }
    public required Action<bool> SetHdrPassthroughEnabled { get; init; }
    public required Action UpdateDecoderCountVisibility { get; init; }
    public required Action EnsureResolutionSelection { get; init; }
    public required Action EnsureFrameRateSelection { get; init; }
    public required Action EnsureFormatSelection { get; init; }
    public required Action EnsureQualitySelection { get; init; }
    public required Action EnsurePresetSelection { get; init; }
    public required Action EnsureSplitEncodeModeSelection { get; init; }
    public required Action AttachRecordingStringSelectionBindings { get; init; }
}
