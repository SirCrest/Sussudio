using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelRecordingSettingsAutomationControllerContext
    {
        public required Func<Func<RecordingFormat>, CancellationToken, Task<RecordingFormat>> InvokeRecordingFormatOnUiThreadAsync { get; init; }
        public required Func<Func<MainViewModelRecordingEncoderSettings>, CancellationToken, Task<MainViewModelRecordingEncoderSettings>> InvokeEncoderSettingsOnUiThreadAsync { get; init; }
        public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
        public required Func<IEnumerable<string>> GetAvailableRecordingFormats { get; init; }
        public required Func<IEnumerable<string>> GetAvailableQualities { get; init; }
        public required Func<IEnumerable<string>> GetAvailableSplitEncodeModes { get; init; }
        public required Func<IEnumerable<string>> GetAvailablePresets { get; init; }
        public required Func<bool> IsHdrEnabled { get; init; }
        public required Action<bool> SetSuppressFlashbackFormatCycle { get; init; }
        public required Action<bool> SetSuppressFlashbackEncoderSettingsCycle { get; init; }
        public required Action<string> SetSelectedRecordingFormat { get; init; }
        public required Func<string> GetSelectedQuality { get; init; }
        public required Action<string> SetSelectedQuality { get; init; }
        public required Func<string> GetSelectedSplitEncodeMode { get; init; }
        public required Action<string> SetSelectedSplitEncodeMode { get; init; }
        public required Func<string> GetSelectedPreset { get; init; }
        public required Action<string> SetSelectedPreset { get; init; }
        public required Func<double> GetCustomBitrateMbps { get; init; }
        public required Action<double> SetCustomBitrateMbps { get; init; }
        public required Action<string> SetOutputPath { get; init; }
        public required Func<RecordingFormat, CancellationToken, Task> UpdateRecordingFormatAsync { get; init; }
        public required Func<VideoQuality, double, string, string?, CancellationToken, Task> CycleFlashbackEncoderSettingsAsync { get; init; }
    }
}
