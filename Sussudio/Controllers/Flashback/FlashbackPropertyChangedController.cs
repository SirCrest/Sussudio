using System;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FlashbackPropertyChangedControllerContext
{
    public required Func<bool> IsTimelineVisible { get; init; }
    public required Func<double> GetExportProgress { get; init; }
    public required Func<bool> IsExporting { get; init; }
    public required Action<bool> ApplyTimelineVisibility { get; init; }
    public required Action ApplyTimelineLockout { get; init; }
    public required Action UpdateState { get; init; }
    public required Action UpdateBuffer { get; init; }
    public required Action UpdateBitrate { get; init; }
    public required Action UpdatePlaybackPosition { get; init; }
    public required Action UpdateRangeMarkers { get; init; }
    public required Action<double> UpdateExportProgress { get; init; }
    public required Action<bool> UpdateExportingPresentation { get; init; }
    public required Action SyncGpuDecodeSetting { get; init; }
    public required Action SyncBufferDurationSetting { get; init; }
}

internal sealed class FlashbackPropertyChangedController
{
    private readonly FlashbackPropertyChangedControllerContext _context;

    public FlashbackPropertyChangedController(FlashbackPropertyChangedControllerContext context)
    {
        _context = context;
    }

    public bool TryHandlePropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsFlashbackTimelineVisible):
                _context.ApplyTimelineVisibility(_context.IsTimelineVisible());
                return true;

            case nameof(MainViewModel.IsFlashbackEnabled):
                _context.ApplyTimelineLockout();
                return true;

            case nameof(MainViewModel.FlashbackState):
                _context.UpdateState();
                return true;

            case nameof(MainViewModel.FlashbackBufferFillPercent):
            case nameof(MainViewModel.FlashbackBufferDiskBytes):
                _context.UpdateBuffer();
                return true;

            case nameof(MainViewModel.FlashbackBitrateInfo):
                _context.UpdateBitrate();
                return true;

            case nameof(MainViewModel.FlashbackPlaybackPosition):
                _context.UpdatePlaybackPosition();
                return true;

            case nameof(MainViewModel.FlashbackInPoint):
            case nameof(MainViewModel.FlashbackOutPoint):
                _context.UpdateRangeMarkers();
                return true;

            case nameof(MainViewModel.FlashbackExportProgress):
                _context.UpdateExportProgress(_context.GetExportProgress());
                return true;

            case nameof(MainViewModel.IsFlashbackExporting):
                _context.UpdateExportingPresentation(_context.IsExporting());
                return true;

            case nameof(MainViewModel.FlashbackGpuDecode):
                _context.SyncGpuDecodeSetting();
                return true;

            case nameof(MainViewModel.FlashbackBufferMinutes):
                _context.SyncBufferDurationSetting();
                return true;

            default:
                return false;
        }
    }
}
