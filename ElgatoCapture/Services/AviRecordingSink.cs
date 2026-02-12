using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using WinRT;

namespace ElgatoCapture.Services;

public sealed class AviRecordingSink : IRecordingSink
{
    private AviWriter? _writer;
    private bool _disposed;
    private RecordingContext? _context;

    public async Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);

        var outputFile = await StorageFile.GetFileFromPathAsync(context.VideoOutputPath);
        var stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
        _writer = new AviWriter(
            stream,
            context.EffectiveWidth,
            context.EffectiveHeight,
            (uint)Math.Max(1, Math.Round(context.EffectiveFrameRate)));
        await _writer.WriteHeaderAsync();
        _context = context;
    }

    public async Task WriteVideoAsync(SoftwareBitmap frame, CancellationToken cancellationToken = default)
    {
        if (_disposed || _writer == null)
        {
            return;
        }

        SoftwareBitmap? converted = null;
        try
        {
            var frameToWrite = frame;
            if (frame.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                converted = SoftwareBitmap.Convert(frame, BitmapPixelFormat.Bgra8);
                frameToWrite = converted;
            }

            await _writer.WriteFrameAsync(frameToWrite);
        }
        finally
        {
            converted?.Dispose();
        }
    }

    public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        // AVI path in this application is video-only.
        return Task.CompletedTask;
    }

    public async Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        if (_writer != null)
        {
            await _writer.FinalizeAsync();
            _writer.Dispose();
            _writer = null;
        }

        return FinalizeResult.Success(_context?.FinalOutputPath ?? string.Empty, "Stopped");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writer?.Dispose();
        _writer = null;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
