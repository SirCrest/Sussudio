using System;
using System.Collections.Generic;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelRecordingCapabilityControllerContext
    {
        public required string DefaultRecordingFormat { get; init; }
        public required string HevcRecordingFormat { get; init; }
        public required string Av1RecordingFormat { get; init; }
        public required Func<IReadOnlyCollection<string>> GetAvailableRecordingFormats { get; init; }
        public required Action<IReadOnlyList<string>> ReplaceAvailableRecordingFormats { get; init; }
        public required Func<string> GetSelectedRecordingFormat { get; init; }
        public required Action<string> SetSelectedRecordingFormat { get; init; }
        public required Action NotifySelectedRecordingFormatChanged { get; init; }
        public required Func<bool> IsHdrEnabled { get; init; }
        public required Action<string> SetStatusText { get; init; }
        public required Func<bool> IsFfmpegMissing { get; init; }
        public required Action<bool> SetIsFfmpegMissing { get; init; }
        public required Func<bool> HasUiThreadAccess { get; init; }
        public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
        public required Func<IReadOnlyCollection<string>> GetAvailableSplitEncodeModes { get; init; }
        public required Action<IReadOnlyList<string>> ReplaceAvailableSplitEncodeModes { get; init; }
        public required Func<string> GetSelectedSplitEncodeMode { get; init; }
        public required Action<string> SetSelectedSplitEncodeMode { get; init; }
        public required Func<string, bool> AvailableSplitEncodeModesContains { get; init; }
    }
}
