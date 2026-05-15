using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsDockPresentationControllerContext
{
    public required TextBlock SessionStateValue { get; init; }
    public required TextBlock SummaryCaptureValue { get; init; }
    public required TextBlock SummaryPreviewValue { get; init; }
    public required TextBlock SummaryRendererFpsValue { get; init; }
    public required TextBlock SummaryVisualFpsValue { get; init; }
    public required TextBlock SummaryLatencyValue { get; init; }
    public required TextBlock SourceResolutionValue { get; init; }
    public required TextBlock SourceFrameRateValue { get; init; }
    public required TextBlock SourceHdrValue { get; init; }
    public required TextBlock SourceFormatValue { get; init; }
    public required TextBlock TelemetryOriginValue { get; init; }
    public required TextBlock AdcOnOffValue { get; init; }
    public required TextBlock AdcGainValue { get; init; }
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
    public required TextBlock VisualFpsValue { get; init; }
    public required TextBlock VisualMotionValue { get; init; }
    public required TextBlock PipelineLatencyValue { get; init; }
    public required TextBlock SourceDeliveredValue { get; init; }
    public required TextBlock SourceDroppedValue { get; init; }
    public required TextBlock RendererRenderedValue { get; init; }
    public required TextBlock RendererDroppedValue { get; init; }
    public required TextBlock PerformanceScoreValue { get; init; }
    public required TextBlock AvSyncDriftValue { get; init; }
    public required TextBlock AvSyncDriftRateValue { get; init; }
    public required UIElement AvSyncEncoderRow { get; init; }
    public required TextBlock AvSyncEncoderValue { get; init; }
    public required UIElement EncoderSection { get; init; }
    public required TextBlock EncoderCodecValue { get; init; }
    public required TextBlock EncoderResolutionValue { get; init; }
    public required TextBlock EncoderFrameRateValue { get; init; }
    public required TextBlock EncoderBitrateValue { get; init; }
}

internal sealed class StatsDockPresentationController
{
    private static readonly SolidColorBrush MetricNeutralBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xF1, 0xF1, 0xF1));
    private static readonly SolidColorBrush MetricGoodBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x70, 0xF0, 0x8B));
    private static readonly SolidColorBrush MetricInfoBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x55, 0xD6, 0xFF));
    private static readonly SolidColorBrush MetricWarningBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xC8, 0x57));
    private static readonly SolidColorBrush MetricBadBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B));

    private readonly StatsDockPresentationControllerContext _context;

    public StatsDockPresentationController(StatsDockPresentationControllerContext context)
    {
        _context = context;
    }

    public void Apply(StatsDockPresentation presentation)
    {
        SetTextIfChanged(_context.SessionStateValue, presentation.SessionState);
        SetTextIfChanged(_context.SummaryCaptureValue, presentation.SummaryCapture);
        SetTextIfChanged(_context.SummaryPreviewValue, presentation.SummaryPreview);
        SetTextIfChanged(_context.SummaryRendererFpsValue, presentation.SummaryRendererFps);
        SetTextIfChanged(_context.SummaryVisualFpsValue, presentation.SummaryVisualFps);
        SetTextIfChanged(_context.SummaryLatencyValue, presentation.SummaryLatency);
        SetMetricBrush(_context.SummaryCaptureValue, presentation.SummaryCaptureStatus);
        SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);
        SetMetricBrush(_context.SummaryVisualFpsValue, presentation.SummaryVisualFpsStatus);
        SetMetricBrush(_context.SummaryLatencyValue, presentation.SummaryLatencyStatus);
        SetTextIfChanged(_context.SourceResolutionValue, presentation.SourceResolution);
        SetTextIfChanged(_context.SourceFrameRateValue, presentation.SourceFrameRate);
        SetTextIfChanged(_context.SourceHdrValue, presentation.SourceHdr);
        SetTextIfChanged(_context.SourceFormatValue, presentation.SourceFormat);
        SetTextIfChanged(_context.TelemetryOriginValue, presentation.TelemetryOrigin);
        SetTextIfChanged(_context.AdcOnOffValue, presentation.AdcOnOff);
        SetTextIfChanged(_context.AdcGainValue, presentation.AdcGain);
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
        SetTextIfChanged(_context.VisualFpsValue, presentation.VisualFps);
        SetTextIfChanged(_context.VisualMotionValue, presentation.VisualMotion);
        SetMetricBrush(_context.VisualFpsValue, presentation.VisualFpsStatus);
        SetTextIfChanged(_context.PipelineLatencyValue, presentation.PipelineLatency);
        SetTextIfChanged(_context.SourceDeliveredValue, presentation.SourceDelivered);
        SetTextIfChanged(_context.SourceDroppedValue, presentation.SourceDropped);
        SetTextIfChanged(_context.RendererRenderedValue, presentation.RendererRendered);
        SetTextIfChanged(_context.RendererDroppedValue, presentation.RendererDropped);
        SetTextIfChanged(_context.PerformanceScoreValue, presentation.PerformanceScore);
        SetTextIfChanged(_context.AvSyncDriftValue, presentation.AvSyncDrift);
        SetTextIfChanged(_context.AvSyncDriftRateValue, presentation.AvSyncDriftRate);

        SetVisibilityIfChanged(_context.AvSyncEncoderRow, presentation.EncoderDriftVisible ? Visibility.Visible : Visibility.Collapsed);
        if (presentation.EncoderDriftVisible)
        {
            SetTextIfChanged(_context.AvSyncEncoderValue, presentation.EncoderDrift);
        }

        SetVisibilityIfChanged(_context.EncoderSection, presentation.EncoderActive ? Visibility.Visible : Visibility.Collapsed);
        if (presentation.EncoderActive)
        {
            SetTextIfChanged(_context.EncoderCodecValue, presentation.EncoderCodec);
            SetTextIfChanged(_context.EncoderResolutionValue, presentation.EncoderResolution);
            SetTextIfChanged(_context.EncoderFrameRateValue, presentation.EncoderFrameRate);
            SetTextIfChanged(_context.EncoderBitrateValue, presentation.EncoderBitrate);
        }
    }

    private static void SetMetricBrush(TextBlock target, StatsMetricStatus status)
    {
        target.Foreground = status switch
        {
            StatsMetricStatus.Good => MetricGoodBrush,
            StatsMetricStatus.Info => MetricInfoBrush,
            StatsMetricStatus.Warning => MetricWarningBrush,
            StatsMetricStatus.Bad => MetricBadBrush,
            _ => MetricNeutralBrush
        };
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, System.StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
}
