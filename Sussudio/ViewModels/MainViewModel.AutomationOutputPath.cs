using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutator for recording output folder selection.
/// </summary>
public partial class MainViewModel
{
    public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("Output path cannot be empty.");
            }

            Directory.CreateDirectory(outputPath);
            OutputPath = outputPath;
            return Task.CompletedTask;
        }, cancellationToken);
    }
}
