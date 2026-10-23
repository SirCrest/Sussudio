using System;

namespace Sussudio.Services.Automation;

/// <summary>Bounds that keep the UI commands cheap on the UI thread and their output readable.</summary>
public static class UiTreeLimits
{
    public const int DefaultMaxDepth = 10;
    public const int MaxDepthLimit = 64;

    /// <summary>Most nodes one GetUiTree response will carry; beyond it the response is marked truncated.</summary>
    public const int MaxNodes = 1500;

    /// <summary>Most elements one walk will touch, emitted or not, before it gives up and says so.</summary>
    public const int MaxVisitedElements = 20000;

    /// <summary>Most item texts listed for a list or combo box (the full count is reported separately).</summary>
    public const int MaxListedItems = 50;

    /// <summary>Pause between performing an action and re-reading the element: acknowledgements can precede UI transitions.</summary>
    public const int SettleDelayMs = 100;
}


