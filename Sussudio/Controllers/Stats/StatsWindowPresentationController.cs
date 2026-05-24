using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsWindowPresentationControllerContext
{
    public required TextBlock SessionStateValue { get; init; }
    public required TextBlock DiagnosticStatusValue { get; init; }
    public required TextBlock DiagnosticStageValue { get; init; }
    public required TextBlock DiagnosticEvidenceValue { get; init; }
    public required TextBlock SourceResolutionValue { get; init; }
    public required TextBlock SourceFrameRateValue { get; init; }
    public required TextBlock SourceHdrValue { get; init; }
    public required TextBlock SourceFormatValue { get; init; }
    public required TextBlock TelemetryOriginValue { get; init; }
    public required TextBlock SourceFpsValue { get; init; }
    public required TextBlock SourceExpectedFpsValue { get; init; }
    public required TextBlock SourceAvgValue { get; init; }
    public required TextBlock SourceP95Value { get; init; }
    public required TextBlock SourceJitterValue { get; init; }
    public required TextBlock SourceGapsValue { get; init; }
    public required TextBlock SourceDropsValue { get; init; }
    public required TextBlock PreviewFpsValue { get; init; }
    public required TextBlock PreviewAvgValue { get; init; }
    public required TextBlock PreviewP95Value { get; init; }
    public required TextBlock PreviewSlowValue { get; init; }
    public required TextBlock PipelineLatencyValue { get; init; }
    public required TextBlock SourceDeliveredValue { get; init; }
    public required TextBlock SourceDroppedValue { get; init; }
    public required TextBlock RendererRenderedValue { get; init; }
    public required TextBlock RendererDroppedValue { get; init; }
    public required TextBlock PerfScoreValue { get; init; }
}

internal sealed class StatsWindowPresentationController
{
    private readonly StatsWindowPresentationControllerContext _context;
    private readonly StatsWindowTelemetryDetailsController _telemetryDetailsController;

    public StatsWindowPresentationController(
        StatsWindowPresentationControllerContext context,
        StatsWindowTelemetryDetailsController telemetryDetailsController)
    {
        _context = context;
        _telemetryDetailsController = telemetryDetailsController;
    }

    public void Apply(StatsWindowPresentation presentation)
    {
        SetTextIfChanged(_context.SessionStateValue, presentation.SessionState);
        SetTextIfChanged(_context.DiagnosticStatusValue, presentation.DiagnosticStatus);
        SetTextIfChanged(_context.DiagnosticStageValue, presentation.DiagnosticStage);
        SetTextIfChanged(_context.DiagnosticEvidenceValue, presentation.DiagnosticEvidence);
        SetTextIfChanged(_context.SourceResolutionValue, presentation.SourceResolution);
        SetTextIfChanged(_context.SourceFrameRateValue, presentation.SourceFrameRate);
        SetTextIfChanged(_context.SourceHdrValue, presentation.SourceHdr);
        SetTextIfChanged(_context.SourceFormatValue, presentation.SourceFormat);
        SetTextIfChanged(_context.TelemetryOriginValue, presentation.TelemetryOrigin);
        SetTextIfChanged(_context.SourceFpsValue, presentation.SourceFps);
        SetTextIfChanged(_context.SourceExpectedFpsValue, presentation.SourceExpectedFps);
        SetTextIfChanged(_context.SourceAvgValue, presentation.SourceAvg);
        SetTextIfChanged(_context.SourceP95Value, presentation.SourceP95);
        SetTextIfChanged(_context.SourceJitterValue, presentation.SourceJitter);
        SetTextIfChanged(_context.SourceGapsValue, presentation.SourceGaps);
        SetTextIfChanged(_context.SourceDropsValue, presentation.SourceDrops);
        SetTextIfChanged(_context.PreviewFpsValue, presentation.PreviewFps);
        SetTextIfChanged(_context.PreviewAvgValue, presentation.PreviewAvg);
        SetTextIfChanged(_context.PreviewP95Value, presentation.PreviewP95);
        SetTextIfChanged(_context.PreviewSlowValue, presentation.PreviewSlow);
        SetTextIfChanged(_context.PipelineLatencyValue, presentation.PipelineLatency);
        SetTextIfChanged(_context.SourceDeliveredValue, presentation.SourceDelivered);
        SetTextIfChanged(_context.SourceDroppedValue, presentation.SourceDropped);
        SetTextIfChanged(_context.RendererRenderedValue, presentation.RendererRendered);
        SetTextIfChanged(_context.RendererDroppedValue, presentation.RendererDropped);
        SetTextIfChanged(_context.PerfScoreValue, presentation.PerformanceScore);
        _telemetryDetailsController.Apply(presentation.TelemetryDetails);
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }
}

internal sealed class StatsWindowTelemetryDetailsControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
    public required StackPanel TelemetryDetailsContent { get; init; }
}

internal sealed class StatsWindowTelemetryDetailsController
{
    private readonly StatsWindowTelemetryDetailsControllerContext _context;

    public StatsWindowTelemetryDetailsController(StatsWindowTelemetryDetailsControllerContext context)
    {
        _context = context;
    }

    public void Apply(StatsWindowTelemetryDetailsPresentation presentation)
    {
        _context.TelemetryDetailsContent.Children.Clear();

        if (presentation.IsEmpty)
        {
            _context.TelemetryDetailsContent.Children.Add(new TextBlock
            {
                Text = presentation.EmptyText,
                Style = GetStyle("StatsLabelStyle"),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        foreach (var row in presentation.Rows)
        {
            if (row.GroupHeader != null)
            {
                _context.TelemetryDetailsContent.Children.Add(new TextBlock
                {
                    Text = row.GroupHeader,
                    Margin = new Thickness(0, 8, 0, 2),
                    Style = GetStyle("StatsSectionHeaderStyle")
                });
            }

            _context.TelemetryDetailsContent.Children.Add(CreateTelemetryDetailRow(row.Label, row.Value));
        }
    }

    private Grid CreateTelemetryDetailRow(string label, string value)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = GetStyle("StatsLabelStyle")
        };
        var valueBlock = new TextBlock
        {
            Text = value,
            Style = GetStyle("StatsValueStyle"),
            HorizontalAlignment = HorizontalAlignment.Right,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);
        return grid;
    }

    private Style GetStyle(string key) => (Style)_context.ResourceOwner.Resources[key];
}
