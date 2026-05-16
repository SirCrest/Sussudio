namespace Sussudio.ViewModels;

/// <summary>
/// View-model bridge for output drive free-space presentation.
/// </summary>
public partial class MainViewModel
{
    private void UpdateDiskSpace()
    {
        DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);
    }
}
