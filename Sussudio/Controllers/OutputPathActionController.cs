using System;
using System.IO;
using System.Threading.Tasks;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class OutputPathActionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Func<Task> OpenRecordingsFolderAsync { get; init; }
}

internal sealed class OutputPathActionController
{
    private readonly OutputPathActionControllerContext _context;

    public OutputPathActionController(OutputPathActionControllerContext context)
    {
        _context = context;
    }

    public Task BrowseAsync()
        => _context.ViewModel.BrowseOutputPathAsync();

    public Task OpenRecordingsFolderIfAvailableAsync()
    {
        var path = _context.ViewModel.OutputPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return Task.CompletedTask;
        }

        return _context.OpenRecordingsFolderAsync();
    }
}
