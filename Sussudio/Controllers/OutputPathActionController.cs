using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Sussudio.Controllers;

internal sealed class OutputPathActionControllerContext
{
    public required Func<IntPtr> GetWindowHandle { get; init; }
    public required Func<string?> GetOutputPath { get; init; }
    public required Action<string> SetOutputPath { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Func<Task> OpenRecordingsFolderAsync { get; init; }
}

internal sealed class OutputPathActionController
{
    private readonly OutputPathActionControllerContext _context;

    public OutputPathActionController(OutputPathActionControllerContext context)
    {
        _context = context;
    }

    public async Task BrowseAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");

            // Initialize the picker with the window handle for WinUI 3.
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _context.GetWindowHandle());

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                _context.SetOutputPath(folder.Path);
            }
        }
        catch (Exception ex)
        {
            _context.SetStatusText($"Error selecting folder: {ex.Message}");
        }
    }

    public Task OpenRecordingsFolderIfAvailableAsync()
    {
        var path = _context.GetOutputPath();
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return Task.CompletedTask;
        }

        return _context.OpenRecordingsFolderAsync();
    }
}
