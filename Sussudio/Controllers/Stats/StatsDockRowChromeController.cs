using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal enum StatsDockSimpleRowPool
{
    Decode,
    Gpu
}

internal sealed class StatsDockRowChromeControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
}

internal sealed class StatsDockRowChromeController
{
    private readonly StatsDockRowChromePresenter _rowChrome;
    private readonly List<StatsDockRowChromeSlot> _decodeRowPool = new();
    private readonly List<StatsDockRowChromeSlot> _gpuRowPool = new();

    public StatsDockRowChromeController(StatsDockRowChromeControllerContext context)
    {
        _rowChrome = new StatsDockRowChromePresenter(context.ResourceOwner);
    }

    public void CollapseSimpleRows(StatsDockSimpleRowPool poolKind)
    {
        StatsDockRowChromePresenter.CollapseRows(GetSimpleRowPool(poolKind));
    }

    public void UpdateSimpleRows(
        StatsDockSimpleRowPool poolKind,
        StackPanel container,
        IReadOnlyList<StatsHardwareRowPresentation> rows,
        int minimumCapacity)
    {
        var pool = GetSimpleRowPool(poolKind);
        EnsureRowPool(container, pool, Math.Max(minimumCapacity, rows.Count));
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            _rowChrome.UpdateRowSlot(pool[i], row.Label, row.Value, alt: (i % 2) != 0);
        }

        StatsDockRowChromePresenter.CollapseRows(pool, startIndex: rows.Count);
    }

    private List<StatsDockRowChromeSlot> GetSimpleRowPool(StatsDockSimpleRowPool poolKind)
        => poolKind switch
        {
            StatsDockSimpleRowPool.Decode => _decodeRowPool,
            StatsDockSimpleRowPool.Gpu => _gpuRowPool,
            _ => throw new ArgumentOutOfRangeException(nameof(poolKind), poolKind, null)
        };

    private void EnsureRowPool(StackPanel container, List<StatsDockRowChromeSlot> pool, int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            var slot = _rowChrome.CreateRowSlot();
            pool.Add(slot);
            container.Children.Add(slot.Row);
        }
    }
}
