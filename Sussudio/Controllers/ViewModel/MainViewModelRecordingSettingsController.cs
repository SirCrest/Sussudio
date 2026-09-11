using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed class MainViewModelRecordingSettingsControllerContext
{
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<IEnumerable<string>> GetAvailableRecordingFormats { get; init; }
    public required Func<IEnumerable<string>> GetAvailableQualities { get; init; }
    public required Func<IEnumerable<string>> GetAvailableSplitEncodeModes { get; init; }
    public required Func<IEnumerable<string>> GetAvailablePresets { get; init; }
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Func<string, bool> IsHdrCompatibleFormat { get; init; }
    public required Func<double, double> ClampCustomBitrateMbps { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsLoadingSettings { get; init; }
    public required Action<string> SetSelectedRecordingFormat { get; init; }
    public required Action<string> SetSelectedQuality { get; init; }
    public required Action<string> SetSelectedSplitEncodeMode { get; init; }
    public required Action<string> SetSelectedPreset { get; init; }
    public required Action<double> SetCustomBitrateMbps { get; init; }
    public required Action<string> SetOutputPath { get; init; }
    public required Func<RecordingSettingsSelection> CaptureSelection { get; init; }
    public required Func<RecordingSettingsSelection, RecordingSettingsChangeKind, CancellationToken, Task<RecordingSettingsApplyDisposition>> ApplyAsync { get; init; }
    public required Action<string> Log { get; init; }
}

// Owns recording-selection application for property reactions and awaitable
// automation. Selection writes and admission happen on the UI thread; the
// coordinator continues to own serialization and native transition cancellation.
internal sealed class MainViewModelRecordingSettingsController
{
    private readonly MainViewModelRecordingSettingsControllerContext _context;
    private int _propertyReactionSuppressionDepth;
    private Task? _pendingApplication;

    public MainViewModelRecordingSettingsController(MainViewModelRecordingSettingsControllerContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public Task? PendingApplication => Volatile.Read(ref _pendingApplication);

    public void ClearPendingIfSameAndCompleted(Task task)
    {
        if (task.IsCompleted)
        {
            Interlocked.CompareExchange(ref _pendingApplication, null, task);
        }
    }

    public IDisposable SuppressPropertyReactions()
    {
        _propertyReactionSuppressionDepth++;
        return new PropertyReactionSuppression(this);
    }

    public void OnSelectionChanged(RecordingSettingsChangeKind kind, string description)
    {
        if (_propertyReactionSuppressionDepth != 0 ||
            !_context.IsPreviewing() || _context.IsRecording() || _context.IsLoadingSettings())
        {
            return;
        }

        _ = BeginApplication(kind, description, CancellationToken.None);
    }

    public Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
        => ApplySelectionChangeAsync(RecordingSettingsChangeKind.RecordingFormat, "recording format", () =>
        {
            var matched = MatchAvailable(_context.GetAvailableRecordingFormats(), format, "Recording format");
            if (_context.IsHdrEnabled() && !_context.IsHdrCompatibleFormat(matched))
            {
                throw new InvalidOperationException("HDR recording requires HEVC or AV1 (10-bit).");
            }
            _context.SetSelectedRecordingFormat(matched);
        }, cancellationToken);

    public Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
        => ApplySelectionChangeAsync(RecordingSettingsChangeKind.EncoderParameters, "quality", () =>
            _context.SetSelectedQuality(MatchAvailable(_context.GetAvailableQualities(), quality, "Quality")), cancellationToken);

    public Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
        => ApplySelectionChangeAsync(RecordingSettingsChangeKind.EncoderParameters, "split encode", () =>
            _context.SetSelectedSplitEncodeMode(MatchAvailable(_context.GetAvailableSplitEncodeModes(), splitEncodeMode, "Split encode mode")), cancellationToken);

    public Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
        => ApplySelectionChangeAsync(RecordingSettingsChangeKind.EncoderParameters, "bitrate", () =>
            _context.SetCustomBitrateMbps(_context.ClampCustomBitrateMbps(bitrateMbps)), cancellationToken);

    public Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
        => ApplySelectionChangeAsync(RecordingSettingsChangeKind.EncoderParameters, "preset", () =>
            _context.SetSelectedPreset(MatchAvailable(_context.GetAvailablePresets(), preset, "Preset")), cancellationToken);

    public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
        => _context.InvokeOnUiThreadAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("Output path cannot be empty.");
            }
            Directory.CreateDirectory(outputPath);
            _context.SetOutputPath(outputPath);
            return Task.CompletedTask;
        }, cancellationToken);

    private Task ApplySelectionChangeAsync(
        RecordingSettingsChangeKind kind,
        string description,
        Action applySelection,
        CancellationToken cancellationToken)
        => _context.InvokeOnUiThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (SuppressPropertyReactions())
            {
                applySelection();
            }

            // Capture and enqueue before leaving this UI dispatch turn so later
            // selections cannot enqueue ahead of an older captured selection.
            // Automation intentionally also admits desired changes during recording.
            return BeginApplication(kind, description, cancellationToken);
        }, cancellationToken);

    private Task<RecordingSettingsApplyDisposition> BeginApplication(
        RecordingSettingsChangeKind kind,
        string description,
        CancellationToken cancellationToken)
    {
        Task<RecordingSettingsApplyDisposition> application;
        try
        {
            application = _context.ApplyAsync(_context.CaptureSelection(), kind, cancellationToken);
        }
        catch (Exception ex)
        {
            application = Task.FromException<RecordingSettingsApplyDisposition>(ex);
        }

        Volatile.Write(ref _pendingApplication, application);
        _ = application.ContinueWith(
            completed =>
            {
                if (completed.IsCompletedSuccessfully)
                {
                    ClearPendingIfSameAndCompleted(completed);
                }
                // A later preview-reinitialize waiter must still be able to
                // observe a failed/canceled cycle before explicitly clearing it.
                if (completed.IsFaulted)
                {
                    _context.Log($"CycleFlashbackEncoder({description}) failed: {completed.Exception!.InnerException?.Message}");
                }
                else if (completed.IsCanceled)
                {
                    _context.Log($"CycleFlashbackEncoder({description}) canceled");
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return application;
    }

    private static string MatchAvailable(IEnumerable<string> available, string requested, string label)
        => available.FirstOrDefault(value => string.Equals(value, requested, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"{label} '{requested}' is not available.");

    private sealed class PropertyReactionSuppression : IDisposable
    {
        private MainViewModelRecordingSettingsController? _owner;

        public PropertyReactionSuppression(MainViewModelRecordingSettingsController owner) => _owner = owner;

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            if (owner != null)
            {
                owner._propertyReactionSuppressionDepth--;
            }
        }
    }
}
