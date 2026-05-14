using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Sussudio.Models;

namespace Sussudio;

// Stats dock section expand/collapse chrome and automation-visible section state.
public sealed partial class MainWindow
{
    private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not Grid header || header.Tag is not string contentName)
        {
            return;
        }

        var content = StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        var collapsing = content.Visibility == Visibility.Visible;
        content.Visibility = collapsing ? Visibility.Collapsed : Visibility.Visible;

        var chevronName = contentName.Replace("_Content", "_Chevron", StringComparison.Ordinal);
        if (StatsDockPanel.FindName(chevronName) is FontIcon chevron &&
            chevron.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = collapsing ? -90 : 0;
        }

        if (!collapsing && ReferenceEquals(content, Diagnostics_Content))
        {
            var snapshot = GetStatsSnapshot();
            UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        }
    }

    private void SetStatsSectionVisible(string section, bool visible)
    {
        var contentName = section + "_Content";
        var content = StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        content.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        var chevronName = section + "_Chevron";
        if (StatsDockPanel.FindName(chevronName) is FontIcon chevron &&
            chevron.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = visible ? 0 : -90;
        }

        if (visible && contentName == "Diagnostics_Content")
        {
            var snapshot = GetStatsSnapshot();
            UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        }
    }
}
