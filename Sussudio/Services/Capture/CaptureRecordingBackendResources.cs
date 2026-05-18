using System;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

internal sealed class CaptureRecordingBackendResources
{
    public LibAvRecordingSink? LibAvSink { get; set; }
    public IRecordingSink? Sink { get; set; }
    public RecordingContext? Context { get; set; }
    public CaptureSettings? SettingsSnapshot { get; set; }
    public Task? PendingLibAvDrainTask { get; set; }

    public bool HasActiveBackend => Sink != null || LibAvSink != null;

    public bool IsFlashbackBackend(FlashbackEncoderSink? flashbackSink)
        => ReferenceEquals(Sink, flashbackSink);

    public void InstallLibAv(
        LibAvRecordingSink libAvSink,
        IRecordingSink recordingSink,
        RecordingContext context,
        CaptureSettings settings)
    {
        LibAvSink = libAvSink;
        Sink = recordingSink;
        Context = context;
        SettingsSnapshot = settings;
        PendingLibAvDrainTask = null;
    }

    public void InstallFlashback(
        FlashbackEncoderSink flashbackSink,
        RecordingContext context,
        CaptureSettings settings)
    {
        Sink = flashbackSink;
        LibAvSink = null;
        Context = context;
        SettingsSnapshot = settings;
        PendingLibAvDrainTask = null;
    }

    public ActiveRecordingBackend DetachLibAvBackend()
    {
        var backend = new ActiveRecordingBackend(Sink, LibAvSink, Context);
        Sink = null;
        LibAvSink = null;
        PendingLibAvDrainTask = null;
        return backend;
    }

    public RecordingContext? DetachFlashbackBackend()
    {
        var context = Context;
        Sink = null;
        return context;
    }

    public void ClearActiveBackend()
    {
        Sink = null;
        LibAvSink = null;
        PendingLibAvDrainTask = null;
    }

    public void ClearContextAndSettings()
    {
        Context = null;
        SettingsSnapshot = null;
    }

    public void ClearAll()
    {
        ClearActiveBackend();
        ClearContextAndSettings();
    }

    public void ClearPendingLibAvDrainIfCompletedSuccessfully()
    {
        if (PendingLibAvDrainTask?.IsCompletedSuccessfully == true)
        {
            PendingLibAvDrainTask = null;
        }
    }

    public void ThrowIfPendingLibAvDrainBlocksReentry()
    {
        var pendingLibAvDrainTask = PendingLibAvDrainTask;
        if (pendingLibAvDrainTask == null)
        {
            return;
        }

        if (pendingLibAvDrainTask.IsCompletedSuccessfully)
        {
            PendingLibAvDrainTask = null;
            return;
        }

        if (pendingLibAvDrainTask.IsFaulted)
        {
            throw new InvalidOperationException(
                "Previous recording backend failed to finalize cleanly. Check the logs and retry.",
                pendingLibAvDrainTask.Exception?.GetBaseException());
        }

        if (pendingLibAvDrainTask.IsCanceled)
        {
            throw new InvalidOperationException("Previous recording backend cleanup was canceled. Check the logs and retry.");
        }

        throw new InvalidOperationException("Previous recording backend is still finalizing. Please wait a moment and try again.");
    }

    internal readonly record struct ActiveRecordingBackend(
        IRecordingSink? Sink,
        LibAvRecordingSink? LibAvSink,
        RecordingContext? Context);
}
