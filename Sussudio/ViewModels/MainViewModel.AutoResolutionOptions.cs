using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automatic resolution dropdown option construction.
/// </summary>
public partial class MainViewModel
{
    private bool ShouldSelectAutoResolutionOption(string? previousSelection)
        => IsAutoResolutionValue(previousSelection) ||
           string.IsNullOrWhiteSpace(previousSelection) ||
           !_hasUserOverriddenResolutionForCurrentMode;

    private ResolutionOption CreateAutoResolutionOption()
        => new()
        {
            Value = AutoResolutionValue,
            Width = 0,
            Height = 0,
            IsEnabled = true,
            DisplayTextOverride = BuildAutoResolutionDisplayText()
        };

    private string BuildAutoResolutionDisplayText()
        => AutoResolutionValue;

}
