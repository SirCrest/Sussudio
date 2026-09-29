using System;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Ownership checks for the runtime health subscription, message presentation,
/// and presentation-driven prewarm request. Backend lifetimes execute in
/// FlashbackHealthLifetimeTests.
/// </summary>
public sealed class FlashbackUiHealthTests
{
    private static string MainWindowXaml()
        => RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml");

    [Fact]
    public void MainWindowXaml_HasNewFlashbackHealthInfoBar_AndDoesNotRenameDiskWarningInfoBar()
    {
        var xaml = MainWindowXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"FlashbackHealthInfoBar\"", xaml);

        // Guard against accidental rename of the pre-existing AutomationId this
        // task's InfoBar sits next to (hard project rail: never rename an
        // existing AutomationId).
        Assert.Contains("AutomationProperties.AutomationId=\"DiskWarningInfoBar\"", xaml);
    }
}
