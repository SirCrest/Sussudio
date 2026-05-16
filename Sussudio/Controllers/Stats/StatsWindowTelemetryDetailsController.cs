using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

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
