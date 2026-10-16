using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Sussudio.Services.Automation;

namespace Sussudio.Controllers;

internal sealed class UiAutomationBridgeContext
{
    public required Func<UIElement?> GetContent { get; init; }
    public required Func<Action, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
}

// The agent's "fingers": reads the live WinUI visual tree (window content plus open popups and flyouts) and operates
// controls through their automation peers. Every touch of the tree runs on the UI thread through the window's UI dispatch
// controller; nothing here caches elements between calls, so a response always describes the tree as it was when read.
// Pure decisions (target resolution, action planning, visibility, list selection) live in Sussudio.Services.Automation
// and are unit-tested; this file owns the walk, the peer calls and the re-read after an action.
// Contract: docs/architecture/performance-telemetry.md section 2.10.
internal sealed class UiAutomationBridge
{
    private const int MaxChildDepthGuard = 256;

    private readonly UiAutomationBridgeContext _context;

    public UiAutomationBridge(UiAutomationBridgeContext context)
    {
        _context = context;
    }

    public async Task<UiTreeResult> GetTreeAsync(UiTreeRequest request, CancellationToken cancellationToken)
    {
        UiTreeResult? result = null;
        await _context.InvokeOnUiThreadAsync(() => result = BuildTree(request), cancellationToken).ConfigureAwait(false);
        return result!;
    }

    public async Task<UiInvokeResult> InvokeAsync(UiInvokeRequest request, CancellationToken cancellationToken)
    {
        Attempt? attempt = null;
        await _context.InvokeOnUiThreadAsync(() => attempt = PerformAction(request), cancellationToken).ConfigureAwait(false);
        if (attempt!.Final is { } early)
        {
            return early;
        }

        // The peer call returned, but the control may still be mid-transition (bindings, animations, a flyout opening):
        // let the UI settle, then describe the element as it is now.
        await Task.Delay(UiTreeLimits.SettleDelayMs, cancellationToken).ConfigureAwait(false);

        UiNode? element = null;
        var detached = false;
        await _context.InvokeOnUiThreadAsync(
            () => (element, detached) = ReadBack(attempt.Element!, attempt.Path!),
            cancellationToken).ConfigureAwait(false);

        var message = attempt.Message!;
        if (detached)
        {
            message += " The element is no longer in the visual tree after the action (for example a flyout item whose flyout closed).";
        }

        return new UiInvokeResult(
            UiActionStatus.Invoked,
            message,
            request.Target,
            attempt.MatchedBy,
            attempt.Action,
            attempt.Via,
            attempt.Overridden is { Count: > 0 } ? attempt.Overridden : null,
            element,
            null,
            null);
    }

    // ---- tree ----

    private UiTreeResult BuildTree(UiTreeRequest request)
    {
        var maxDepth = Math.Clamp(request.MaxDepth, 1, UiTreeLimits.MaxDepthLimit);
        if (!TryGetXamlRoot(out var content, out var xamlRoot))
        {
            return TreeFailure(UiTreeStatus.WindowUnavailable, "The window content is not available (the window is closing or has not loaded).", request, maxDepth);
        }

        var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot);
        var roots = new List<WalkRoot> { new("window", content) };
        for (var index = 0; index < popups.Count; index++)
        {
            if (popups[index].Child is { } child)
            {
                roots.Add(new WalkRoot($"popup[{index}]", child));
            }
        }

        string? rootPath = null;
        var walkRoots = roots.Select(root => new WalkStart(root.Element, $"{root.Label}/{UiElementReader.SegmentOf(root.Element, 0)}", null, null)).ToList();
        if (!string.IsNullOrWhiteSpace(request.Root))
        {
            var live = CollectLiveElements(roots);
            var resolution = UiTargetResolver.Resolve(request.Root, live.Select(item => item.Probe.ToCandidate()).ToList(), force: true);
            if (resolution.Kind != UiResolutionKind.Matched)
            {
                var status = resolution.Kind == UiResolutionKind.Ambiguous ? UiTreeStatus.RootAmbiguous : UiTreeStatus.RootNotFound;
                return TreeFailure(status, resolution.Message, request, maxDepth) with { OpenPopups = popups.Count };
            }

            var element = live.First(item => item.Probe.Path == resolution.Element!.Path).Probe.Element;
            var (chain, disabledBy) = UiElementReader.ChainOf(element);
            rootPath = resolution.Element!.Path;
            // The chain includes the element itself; the walk pushes its own layer.
            chain.RemoveAt(chain.Count - 1);
            walkRoots = new List<WalkStart> { new(element, rootPath, chain, disabledBy) };
        }

        var state = new WalkState(request.InteractiveOnly, maxDepth, content);
        var nodes = new List<UiNode>();
        foreach (var start in walkRoots)
        {
            var chain = start.Chain ?? new List<UiVisibilityLayer>();
            Emit(start.Element, start.Path, chain, start.DisabledBy, 0, counting: false, state, nodes);
            if (state.Stop)
            {
                break;
            }
        }

        var reasons = new List<string>();
        if (state.NodeCapHit)
        {
            reasons.Add($"the {UiTreeLimits.MaxNodes}-node cap was reached; narrow with root or maxDepth, or keep interactiveOnly");
        }

        if (state.VisitCapHit)
        {
            reasons.Add($"the walk stopped after {UiTreeLimits.MaxVisitedElements} elements; narrow with root");
        }

        if (state.OmittedByDepth > 0)
        {
            reasons.Add($"{state.OmittedByDepth} node(s) below maxDepth {maxDepth} were omitted; raise maxDepth");
        }

        var truncated = reasons.Count > 0;
        var message = $"{state.Emitted} node(s) of {state.Visited} visited element(s)"
            + (rootPath is null ? string.Empty : $" under {rootPath}")
            + $", {popups.Count} open popup(s)."
            + (truncated ? " Truncated: " + string.Join("; ", reasons) + "." : string.Empty);
        return new UiTreeResult(
            UiTreeStatus.Ok,
            message,
            rootPath,
            maxDepth,
            request.InteractiveOnly,
            state.Visited,
            state.Emitted,
            UiTreeLimits.MaxNodes,
            truncated,
            truncated ? string.Join("; ", reasons) : null,
            popups.Count,
            Math.Round(xamlRoot.Size.Width, 1),
            Math.Round(xamlRoot.Size.Height, 1),
            nodes);
    }

    // Depth-first walk that emits nodes into <paramref name="sink"/>. In interactive-only mode a non-interactive element is
    // not emitted and its emitted descendants are hoisted into the nearest emitted ancestor's list. A node at maxDepth is
    // emitted without children; its would-be descendants are counted (not emitted) so the truncation is reported.
    private void Emit(
        UIElement element,
        string path,
        List<UiVisibilityLayer> chain,
        string? disabledBy,
        int depth,
        bool counting,
        WalkState state,
        List<UiNode> sink)
    {
        if (state.Stop || chain.Count > MaxChildDepthGuard)
        {
            return;
        }

        if (++state.Visited > UiTreeLimits.MaxVisitedElements)
        {
            state.VisitCapHit = true;
            return;
        }

        chain.Add(UiElementReader.LayerOf(element));
        try
        {
            var probe = UiElementReader.Probe(element, path, chain, disabledBy);
            var emit = !state.InteractiveOnly || probe.Interactive;
            if (emit && !counting && state.Emitted >= UiTreeLimits.MaxNodes)
            {
                state.NodeCapHit = true;
                return;
            }

            if (emit)
            {
                if (counting)
                {
                    state.OmittedByDepth++;
                }
                else
                {
                    state.Emitted++;
                }
            }

            var childCounting = counting || (emit && depth >= state.MaxDepth);
            var childSink = emit && !counting && !childCounting ? new List<UiNode>() : sink;
            var childDisabledBy = UiElementReader.DisabledByIncluding(element, disabledBy);
            var childDepth = emit ? depth + 1 : depth;
            var count = VisualTreeHelper.GetChildrenCount(element);
            for (var index = 0; index < count; index++)
            {
                if (VisualTreeHelper.GetChild(element, index) is not UIElement child)
                {
                    continue;
                }

                Emit(child, $"{path}/{UiElementReader.SegmentOf(child, index)}", chain, childDisabledBy, childDepth, childCounting, state, childSink);
                if (state.Stop)
                {
                    break;
                }
            }

            if (emit && !counting)
            {
                sink.Add(UiElementReader.ToNode(probe, state.BoundsRoot, ReferenceEquals(childSink, sink) ? null : childSink));
            }
        }
        finally
        {
            chain.RemoveAt(chain.Count - 1);
        }
    }

    // ---- target resolution and actions ----

    private sealed class Attempt
    {
        public UiInvokeResult? Final { get; init; }
        public UIElement? Element { get; init; }
        public string? Path { get; init; }
        public string? MatchedBy { get; init; }
        public string? Action { get; init; }
        public string? Via { get; init; }
        public string? Message { get; init; }
        public IReadOnlyList<string>? Overridden { get; init; }
    }

    private Attempt PerformAction(UiInvokeRequest request)
    {
        if (!TryGetXamlRoot(out var content, out var xamlRoot))
        {
            return Done(Result(UiActionStatus.WindowUnavailable, "The window content is not available (the window is closing or has not loaded).", request));
        }

        var roots = new List<WalkRoot> { new("window", content) };
        var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot);
        for (var index = 0; index < popups.Count; index++)
        {
            if (popups[index].Child is { } child)
            {
                roots.Add(new WalkRoot($"popup[{index}]", child));
            }
        }

        var live = CollectLiveElements(roots);
        var byPath = live.ToDictionary(item => item.Probe.Path, item => item.Probe.Element, StringComparer.Ordinal);
        var resolution = UiTargetResolver.Resolve(request.Target, live.Select(item => item.Probe.ToCandidate()).ToList(), request.Force);
        switch (resolution.Kind)
        {
            case UiResolutionKind.NotFound:
                return Done(Result(UiActionStatus.NotFound, resolution.Message, request, candidates: resolution.Candidates, candidateTotal: resolution.CandidateTotal));
            case UiResolutionKind.Ambiguous:
                return Done(Result(UiActionStatus.Ambiguous, resolution.Message, request, matchedBy: resolution.MatchedBy, candidates: resolution.Candidates, candidateTotal: resolution.CandidateTotal));
            case UiResolutionKind.Hidden:
            case UiResolutionKind.Disabled:
                return Done(Result(
                    resolution.Kind == UiResolutionKind.Hidden ? UiActionStatus.Hidden : UiActionStatus.Disabled,
                    resolution.Message,
                    request,
                    matchedBy: resolution.MatchedBy,
                    element: Describe(byPath[resolution.Element!.Path], resolution.Element.Path, xamlRoot)));
        }

        var matched = resolution.Element!;
        var target = byPath[matched.Path];
        var facts = UiElementReader.ReadFacts(target);
        var plan = UiActionPlanner.Plan(request.Action, request.Value, facts);
        if (!plan.Ok)
        {
            return Done(Result(
                plan.ErrorStatus!,
                plan.Error!,
                request,
                matchedBy: resolution.MatchedBy,
                element: Describe(target, matched.Path, xamlRoot)));
        }

        // A list selection that matches no item (or several) is a request the agent can fix by rewording, like an
        // out-of-range slider value, so it is rejected with the item list before anything runs.
        ListSelection? listSelection = null;
        if (plan.Action == UiActions.Select && plan.SelectionValue is not null)
        {
            if (target is not Selector selector)
            {
                return Done(Result(
                    UiActionStatus.ActionFailed,
                    $"Action 'select' on {matched.Path} failed: the element supports Selection but is not a Selector, so its items cannot be enumerated.",
                    request,
                    matchedBy: resolution.MatchedBy,
                    action: plan.Action,
                    element: Describe(target, matched.Path, xamlRoot)));
            }

            var items = UiElementReader.ItemTexts(selector, selector.Items.Count);
            var match = UiSelectionMatcher.Match(items, plan.SelectionValue);
            if (!match.Ok)
            {
                return Done(Result(
                    UiActionStatus.InvalidValue,
                    match.Error!,
                    request,
                    matchedBy: resolution.MatchedBy,
                    action: plan.Action,
                    element: Describe(target, matched.Path, xamlRoot)));
            }

            listSelection = new ListSelection(selector, match.Index, items[match.Index]);
        }

        var outcome = Perform(target, plan, listSelection);
        var overridden = resolution.Overridden.Count > 0 ? resolution.Overridden : null;
        Logger.LogEvent(
            "AUTO-UI-INVOKE",
            $"target='{request.Target}' path={matched.Path} action={plan.Action} via={outcome.Via ?? "<none>"} ok={outcome.Error is null}"
            + (overridden is null ? string.Empty : $" forced={string.Join("|", overridden)}"));
        if (outcome.Error is not null)
        {
            return Done(Result(
                UiActionStatus.ActionFailed,
                $"Action '{plan.Action}' on {matched.Path} failed: {outcome.Error}",
                request,
                matchedBy: resolution.MatchedBy,
                action: plan.Action,
                via: outcome.Via,
                overridden: overridden,
                element: Describe(target, matched.Path, xamlRoot)));
        }

        return new Attempt
        {
            Element = target,
            Path = matched.Path,
            MatchedBy = resolution.MatchedBy,
            Action = plan.Action,
            Via = outcome.Via,
            Overridden = overridden,
            Message = $"Performed '{plan.Action}' on {matched.Path} (matched by {resolution.MatchedBy}, via {outcome.Via}).",
        };
    }

    private readonly record struct ActionOutcome(string? Via, string? Error);

    private readonly record struct ListSelection(Selector Selector, int Index, string ItemText);

    private static ActionOutcome Perform(UIElement element, UiActionPlan plan, ListSelection? listSelection)
    {
        try
        {
            switch (plan.Action)
            {
                case UiActions.Focus:
                    return element.Focus(FocusState.Programmatic)
                        ? new ActionOutcome("UIElement.Focus(Programmatic)", null)
                        : new ActionOutcome("UIElement.Focus(Programmatic)", "the element did not take focus (it may not be focusable, may be disabled, or its window is not active)");
                case UiActions.ScrollIntoView:
                    return ScrollIntoView(element);
            }

            var peer = UiElementReader.TryCreatePeer(element);
            if (peer is null)
            {
                return new ActionOutcome(null, "the element has no automation peer");
            }

            switch (plan.Action)
            {
                case UiActions.Invoke:
                    Pattern<IInvokeProvider>(peer, PatternInterface.Invoke).Invoke();
                    return new ActionOutcome("peer:Invoke", null);
                case UiActions.Toggle:
                    Pattern<IToggleProvider>(peer, PatternInterface.Toggle).Toggle();
                    return new ActionOutcome("peer:Toggle", null);
                case UiActions.Expand:
                    Pattern<IExpandCollapseProvider>(peer, PatternInterface.ExpandCollapse).Expand();
                    return new ActionOutcome("peer:ExpandCollapse", null);
                case UiActions.Collapse:
                    Pattern<IExpandCollapseProvider>(peer, PatternInterface.ExpandCollapse).Collapse();
                    return new ActionOutcome("peer:ExpandCollapse", null);
                case UiActions.SetValue when plan.NumericValue is { } number:
                    Pattern<IRangeValueProvider>(peer, PatternInterface.RangeValue).SetValue(number);
                    return new ActionOutcome("peer:RangeValue", null);
                case UiActions.SetValue:
                    Pattern<IValueProvider>(peer, PatternInterface.Value).SetValue(plan.TextValue ?? string.Empty);
                    return new ActionOutcome("peer:Value", null);
                case UiActions.Select when plan.SelectionValue is null && UiElementReader.OwnerOf(element) is { } owner:
                    // A ComboBoxItem/ListViewItem container: its own peer has no SelectionItem, so select it through its owner.
                    return SelectIndex(owner.Owner, owner.Index, UiElementReader.ItemText(owner.Owner, owner.Index), element);
                case UiActions.Select when plan.SelectionValue is null:
                    Pattern<ISelectionItemProvider>(peer, PatternInterface.SelectionItem).Select();
                    return new ActionOutcome("peer:SelectionItem", null);
                case UiActions.Select:
                    return SelectIndex(listSelection!.Value.Selector, listSelection.Value.Index, listSelection.Value.ItemText, null);
                default:
                    return new ActionOutcome(null, $"unhandled action '{plan.Action}'");
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Pattern providers throw ElementNotEnabledException, COMException and more for states the pre-check cannot
            // see; whatever it is belongs in the response, not in the pipe's generic failure path.
            return new ActionOutcome(null, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static ActionOutcome ScrollIntoView(UIElement element)
    {
        var peer = UiElementReader.TryCreatePeer(element);
        if (peer is not null && UiElementReader.TryGetPattern(peer, PatternInterface.ScrollItem) is IScrollItemProvider scrollItem)
        {
            scrollItem.ScrollIntoView();
            return new ActionOutcome("peer:ScrollItem", null);
        }

        element.StartBringIntoView();
        return new ActionOutcome("UIElement.StartBringIntoView (the element has no ScrollItem pattern)", null);
    }

    // Selecting one item of a list or combo box (by item text or #index, or because the agent picked its container). Three
    // routes in order, and the response says which one ran and, for the last, why the earlier ones were not usable:
    //   1. the item-data peer: the selector's own peer creates one per item, it carries the SelectionItem pattern, and it
    //      works whether or not a container exists (a closed combo box realizes none);
    //   2. the realized container's own peer, when it happens to expose SelectionItem;
    //   3. Selector.SelectedIndex, the same selection a click produces.
    private static ActionOutcome SelectIndex(Selector selector, int index, string itemText, UIElement? knownContainer)
    {
        var dataPeer = UiElementReader.FindItemDataPeer(selector, index);
        if (dataPeer.Provider is { } dataProvider)
        {
            dataProvider.Select();
            return new ActionOutcome(UiSelectionVia.ItemDataPeer(index, itemText), null);
        }

        var container = knownContainer ?? selector.ContainerFromIndex(index) as UIElement;
        var containerState = UiItemContainerState.NotRealized;
        if (container is not null)
        {
            if (UiElementReader.TryCreatePeer(container) is not { } containerPeer)
            {
                containerState = UiItemContainerState.NoPeer;
            }
            else if (UiElementReader.TryGetPattern(containerPeer, PatternInterface.SelectionItem) is ISelectionItemProvider containerProvider)
            {
                containerProvider.Select();
                return new ActionOutcome(UiSelectionVia.ContainerPeer(index, itemText), null);
            }
            else
            {
                containerState = UiItemContainerState.PeerLacksSelectionItem;
            }
        }

        selector.SelectedIndex = index;
        return new ActionOutcome(UiSelectionVia.SelectedIndex(index, itemText, dataPeer.Lookup, dataPeer.Error, containerState), null);
    }

    private static T Pattern<T>(AutomationPeer peer, PatternInterface pattern) where T : class
        => UiElementReader.TryGetPattern(peer, pattern) as T
            ?? throw new InvalidOperationException($"the automation peer no longer provides the {pattern} pattern");

    private (UiNode? Node, bool Detached) ReadBack(UIElement element, string path)
    {
        if (element is FrameworkElement { IsLoaded: false })
        {
            return (null, true);
        }

        try
        {
            return (UiElementReader.ToNode(UiElementReader.ProbeStandalone(element, path), element.XamlRoot?.Content, null), false);
        }
        catch (Exception ex) when (UiElementReader.IsPeerReadFailure(ex))
        {
            return (null, true);
        }
    }

    private static UiNode? Describe(UIElement element, string path, XamlRoot xamlRoot)
        => UiElementReader.ToNode(UiElementReader.ProbeStandalone(element, path), xamlRoot.Content, null);

    // ---- shared ----

    private sealed record WalkRoot(string Label, UIElement Element);

    private sealed record WalkStart(UIElement Element, string Path, List<UiVisibilityLayer>? Chain, string? DisabledBy);

    private sealed record LiveElement(UiElementProbe Probe);

    private sealed class WalkState
    {
        public WalkState(bool interactiveOnly, int maxDepth, UIElement boundsRoot)
        {
            InteractiveOnly = interactiveOnly;
            MaxDepth = maxDepth;
            BoundsRoot = boundsRoot;
        }

        public bool InteractiveOnly { get; }
        public int MaxDepth { get; }
        public UIElement BoundsRoot { get; }
        public int Visited { get; set; }
        public int Emitted { get; set; }
        public int OmittedByDepth { get; set; }
        public bool NodeCapHit { get; set; }
        public bool VisitCapHit { get; set; }
        public bool Stop => NodeCapHit || VisitCapHit;
    }

    // Every element of every root as a resolution candidate (identity fields, visibility, enabled state, interactivity).
    private static List<LiveElement> CollectLiveElements(IReadOnlyList<WalkRoot> roots)
    {
        var live = new List<LiveElement>();
        var visited = 0;
        foreach (var root in roots)
        {
            Collect(root.Element, $"{root.Label}/{UiElementReader.SegmentOf(root.Element, 0)}", new List<UiVisibilityLayer>(), null, live, ref visited);
        }

        return live;
    }

    private static void Collect(
        UIElement element,
        string path,
        List<UiVisibilityLayer> chain,
        string? disabledBy,
        List<LiveElement> live,
        ref int visited)
    {
        if (chain.Count > MaxChildDepthGuard || ++visited > UiTreeLimits.MaxVisitedElements)
        {
            return;
        }

        chain.Add(UiElementReader.LayerOf(element));
        try
        {
            live.Add(new LiveElement(UiElementReader.Probe(element, path, chain, disabledBy)));
            var childDisabledBy = UiElementReader.DisabledByIncluding(element, disabledBy);
            var count = VisualTreeHelper.GetChildrenCount(element);
            for (var index = 0; index < count; index++)
            {
                if (VisualTreeHelper.GetChild(element, index) is UIElement child)
                {
                    Collect(child, $"{path}/{UiElementReader.SegmentOf(child, index)}", chain, childDisabledBy, live, ref visited);
                }
            }
        }
        finally
        {
            chain.RemoveAt(chain.Count - 1);
        }
    }

    private bool TryGetXamlRoot(out UIElement content, out XamlRoot xamlRoot)
    {
        if (_context.GetContent() is FrameworkElement { XamlRoot: { } root } frameworkElement)
        {
            content = frameworkElement;
            xamlRoot = root;
            return true;
        }

        content = null!;
        xamlRoot = null!;
        return false;
    }

    private static UiTreeResult TreeFailure(string status, string message, UiTreeRequest request, int maxDepth)
        => new(status, message, request.Root, maxDepth, request.InteractiveOnly, 0, 0, UiTreeLimits.MaxNodes, false, null, 0, 0, 0, Array.Empty<UiNode>());

    private static Attempt Done(UiInvokeResult result) => new() { Final = result };

    private static UiInvokeResult Result(
        string status,
        string message,
        UiInvokeRequest request,
        string? matchedBy = null,
        string? action = null,
        string? via = null,
        IReadOnlyList<string>? overridden = null,
        UiNode? element = null,
        IReadOnlyList<UiTargetCandidate>? candidates = null,
        int? candidateTotal = null)
        => new(
            status,
            message,
            request.Target,
            matchedBy,
            action,
            via,
            overridden,
            element,
            candidates is { Count: > 0 }
                ? candidates.Select(candidate => new UiCandidateInfo(
                    candidate.Path,
                    candidate.Type,
                    candidate.Name,
                    candidate.AutomationId,
                    candidate.AutomationName,
                    candidate.Text,
                    candidate.IsVisible,
                    candidate.IsEnabled)).ToList()
                : null,
            candidateTotal is > 0 ? candidateTotal : null);
}
