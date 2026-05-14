using System;
using System.Collections.Specialized;
using System.Threading;

namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    private const int SyncDevice = 0;
    private const int SyncAudio = 1;
    private const int SyncResolution = 2;
    private const int SyncFrameRate = 3;
    private const int SyncFormat = 4;
    private const int SyncQuality = 5;
    private const int SyncPreset = 6;
    private const int SyncSplitEncode = 7;
    private const int SyncMicrophone = 8;

    private readonly int[] _selectionSyncQueued = new int[9];

    private static void AttachCollectionSync(INotifyCollectionChanged collection, Action queueSync)
    {
        collection.CollectionChanged += (_, e) =>
        {
            if (e.Action is NotifyCollectionChangedAction.Add
                or NotifyCollectionChangedAction.Reset
                or NotifyCollectionChangedAction.Remove)
            {
                queueSync();
            }
        };
    }

    private void QueueSelectionSync(int syncIndex, Action ensureMethod)
    {
        if (Interlocked.Exchange(ref _selectionSyncQueued[syncIndex], 1) != 0)
        {
            return;
        }

        _context.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                ensureMethod();
            }
            finally
            {
                Interlocked.Exchange(ref _selectionSyncQueued[syncIndex], 0);
            }
        });
    }

    private void QueueDeviceSelectionSync() => QueueSelectionSync(SyncDevice, EnsureDeviceSelection);
    private void QueueAudioSelectionSync() => QueueSelectionSync(SyncAudio, EnsureAudioInputSelection);
    private void QueueMicrophoneSelectionSync() => QueueSelectionSync(SyncMicrophone, EnsureMicrophoneSelection);
    private void QueueResolutionSelectionSync() => QueueSelectionSync(SyncResolution, EnsureResolutionSelection);
    private void QueueFrameRateSelectionSync() => QueueSelectionSync(SyncFrameRate, EnsureFrameRateSelection);
    private void QueueFormatSelectionSync() => QueueSelectionSync(SyncFormat, EnsureFormatSelection);
    private void QueueQualitySelectionSync() => QueueSelectionSync(SyncQuality, EnsureQualitySelection);
    private void QueuePresetSelectionSync() => QueueSelectionSync(SyncPreset, EnsurePresetSelection);
    private void QueueSplitEncodeModeSelectionSync() => QueueSelectionSync(SyncSplitEncode, EnsureSplitEncodeModeSelection);
}
