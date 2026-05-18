using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;
using Windows.Storage.Pickers;

namespace Sussudio.Controllers;

internal sealed class OutputPathControllerContext
{
    public required TextBox OutputPathTextBox { get; init; }
    public required Func<IntPtr> GetWindowHandle { get; init; }
    public required Func<string?> GetOutputPath { get; init; }
    public required Action<string> SetOutputPath { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Func<Task> OpenRecordingsFolderAsync { get; init; }
}

internal sealed class OutputPathController
{
    private readonly OutputPathControllerContext _context;

    public OutputPathController(OutputPathControllerContext context)
    {
        _context = context;
    }

    public void AttachDisplay()
        => _context.OutputPathTextBox.SizeChanged += (_, _) => UpdateDisplay();

    public void UpdateDisplay()
    {
        var path = _context.GetOutputPath();
        if (string.IsNullOrEmpty(path))
        {
            _context.OutputPathTextBox.Text = string.Empty;
            return;
        }

        ToolTipService.SetToolTip(_context.OutputPathTextBox, path);

        var availableWidth = _context.OutputPathTextBox.ActualWidth;
        _context.OutputPathTextBox.Text = OutputPathDisplayTextFormatter.Format(path, availableWidth);
    }

    public bool TryHandlePropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.OutputPath):
                UpdateDisplay();
                return true;

            default:
                return false;
        }
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
