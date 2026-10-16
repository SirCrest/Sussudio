namespace Sussudio.Services.Automation;

/// <summary>Why no item-data peer could be used to select an item.</summary>
public enum UiItemPeerLookup
{
    /// <summary>The selector's automation peer exposes no item-data peer for the item.</summary>
    NotFound,

    /// <summary>Asking for the item-data peer threw (the control was probably being torn down).</summary>
    Failed,
}

/// <summary>What was found for an item's container when no peer could select the item.</summary>
public enum UiItemContainerState
{
    NotRealized,
    NoPeer,
    PeerLacksSelectionItem,
}

/// <summary>
/// The <c>Via</c> text a list or combo select reports. The three routes are tried in order: the item-data peer (the
/// selector's own peer creates one per item and it carries the SelectionItem pattern), the container's peer, and finally
/// <c>Selector.SelectedIndex</c>. The fallback text states the real reason it was reached; the first live version said
/// "no realized container" while the dropdown was open and every container existed.
/// Contract: docs/architecture/performance-telemetry.md section 2.10.
/// </summary>
public static class UiSelectionVia
{
    public static string ItemDataPeer(int index, string itemText)
        => $"peer:SelectionItem (item-data peer of item #{index} \"{itemText}\")";

    public static string ContainerPeer(int index, string itemText)
        => $"peer:SelectionItem (container peer of item #{index} \"{itemText}\")";

    public static string SelectedIndex(int index, string itemText, UiItemPeerLookup itemPeer, string? itemPeerError, UiItemContainerState container)
    {
        var peerReason = itemPeer == UiItemPeerLookup.Failed
            ? $"looking up its item-data peer failed: {itemPeerError ?? "unknown error"}"
            : "the selector's automation peer exposes no item-data peer for it";
        var containerReason = container switch
        {
            UiItemContainerState.NotRealized => "it has no realized container",
            UiItemContainerState.NoPeer => "its realized container has no automation peer",
            _ => "its realized container's peer has no SelectionItem pattern",
        };
        return $"Selector.SelectedIndex (item #{index} \"{itemText}\": {peerReason}; {containerReason})";
    }
}
