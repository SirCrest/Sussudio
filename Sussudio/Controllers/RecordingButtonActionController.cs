using System;
using System.Threading.Tasks;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal readonly record struct RecordingPreviewActivitySnapshot(
    bool GpuActive,
    bool CpuActive,
    bool PlaceholderVisible)
{
    public bool RendererActive => GpuActive || CpuActive;
}

internal sealed class RecordingButtonActionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Func<RecordingPreviewActivitySnapshot> GetPreviewActivitySnapshot { get; init; }
}

internal sealed class RecordingButtonActionController
{
    private readonly RecordingButtonActionControllerContext _context;

    public RecordingButtonActionController(RecordingButtonActionControllerContext context)
    {
        _context = context;
    }

    public async Task ToggleRecordingAsync()
    {
        await _context.ViewModel.ToggleRecordingAsync();

        if (!_context.ViewModel.IsRecording)
        {
            return;
        }

        var snapshot = _context.GetPreviewActivitySnapshot();
        Logger.Log(
            $"PreviewStateDuringRecording: rendererActive={snapshot.RendererActive}, " +
            $"gpuActive={snapshot.GpuActive}, cpuActive={snapshot.CpuActive}, " +
            $"placeholderVisible={snapshot.PlaceholderVisible}");

        if (!snapshot.RendererActive || snapshot.PlaceholderVisible)
        {
            Logger.Log("WARNING: preview renderer appears inactive while recording.");
        }
    }
}
