using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Sussudio.ViewModels;

/// <summary>
/// UI-facing recording output folder selection.
/// </summary>
public partial class MainViewModel
{
    public async Task BrowseOutputPathAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");

            // Initialize the picker with the window handle for WinUI 3.
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandle);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                OutputPath = folder.Path;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error selecting folder: {ex.Message}";
        }
    }
}
