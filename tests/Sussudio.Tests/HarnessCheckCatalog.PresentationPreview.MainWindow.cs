using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewMainWindowChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MainWindow property changed routing delegates to focused controllers",
            MainWindowPropertyChangedRouting_DelegatesToFocusedControllers);
    }
}
