using Sussudio.Services.Automation;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the "Via" text a select reports (docs/architecture/performance-telemetry.md section 2.10). The live
// FlashbackBufferDurationCombo select reported "has no realized container to select through its peer" while its dropdown
// was open and every ComboBoxItem was realized, so the response lied about why the fallback was used. Written before the
// code:
//   1. The fallback text names the real reason and never claims "no realized container" for a container that exists.
//   2. A missing container, a container without a peer and a container whose peer lacks SelectionItem are three distinct
//      reasons with three distinct texts.
//   3. A failed item-data peer lookup carries its error message; a lookup that simply found nothing says so; neither is
//      confused with the other.
//   4. Every text names the item index and its text, so a response can be read without the request.
//   5. The two peer routes say which peer selected, because the item-data peer and the container peer are different
//      automation objects and the agent comparing runs needs to know which one ran.
public sealed class UiSelectionViaTests
{
    [Fact]
    public void FallbackWithARealizedContainer_NeverClaimsThereIsNoRealizedContainer()
    {
        foreach (var container in new[] { UiItemContainerState.NoPeer, UiItemContainerState.PeerLacksSelectionItem })
        {
            var via = UiSelectionVia.SelectedIndex(5, "30 minutes", UiItemPeerLookup.NotFound, null, container);

            Assert.DoesNotContain("no realized container", via);
            Assert.Contains("realized container", via);
        }
    }

    [Fact]
    public void FallbackWithoutAContainer_SaysTheContainerIsNotRealized()
    {
        var via = UiSelectionVia.SelectedIndex(5, "30 minutes", UiItemPeerLookup.NotFound, null, UiItemContainerState.NotRealized);

        Assert.Contains("it has no realized container", via);
    }

    [Fact]
    public void ContainerReasons_AreDistinct()
    {
        var texts = new[]
        {
            UiSelectionVia.SelectedIndex(1, "x", UiItemPeerLookup.NotFound, null, UiItemContainerState.NotRealized),
            UiSelectionVia.SelectedIndex(1, "x", UiItemPeerLookup.NotFound, null, UiItemContainerState.NoPeer),
            UiSelectionVia.SelectedIndex(1, "x", UiItemPeerLookup.NotFound, null, UiItemContainerState.PeerLacksSelectionItem),
        };

        Assert.Equal(3, new System.Collections.Generic.HashSet<string>(texts).Count);
        Assert.Contains("has no automation peer", texts[1]);
        Assert.Contains("peer has no SelectionItem pattern", texts[2]);
    }

    [Fact]
    public void FailedItemPeerLookup_CarriesItsError()
    {
        var via = UiSelectionVia.SelectedIndex(2, "A", UiItemPeerLookup.Failed, "ElementNotAvailableException: gone", UiItemContainerState.NotRealized);

        Assert.Contains("ElementNotAvailableException: gone", via);
        Assert.Contains("failed", via);
    }

    [Fact]
    public void ItemPeerLookupThatFoundNothing_SaysSoWithoutAnError()
    {
        var via = UiSelectionVia.SelectedIndex(2, "A", UiItemPeerLookup.NotFound, null, UiItemContainerState.NotRealized);

        Assert.Contains("exposes no item-data peer", via);
        Assert.DoesNotContain("failed", via);
    }

    [Fact]
    public void FailedLookupWithoutAMessage_StillSaysItFailed()
    {
        var via = UiSelectionVia.SelectedIndex(2, "A", UiItemPeerLookup.Failed, null, UiItemContainerState.NotRealized);

        Assert.Contains("failed", via);
    }

    [Fact]
    public void EveryText_NamesTheItemIndexAndText()
    {
        var texts = new[]
        {
            UiSelectionVia.ItemDataPeer(3, "10 minutes"),
            UiSelectionVia.ContainerPeer(3, "10 minutes"),
            UiSelectionVia.SelectedIndex(3, "10 minutes", UiItemPeerLookup.NotFound, null, UiItemContainerState.NotRealized),
        };

        foreach (var text in texts)
        {
            Assert.Contains("#3", text);
            Assert.Contains("\"10 minutes\"", text);
        }
    }

    [Fact]
    public void PeerRoutes_SayWhichPeerSelected()
    {
        Assert.StartsWith("peer:SelectionItem", UiSelectionVia.ItemDataPeer(0, "a"));
        Assert.Contains("item-data peer", UiSelectionVia.ItemDataPeer(0, "a"));
        Assert.StartsWith("peer:SelectionItem", UiSelectionVia.ContainerPeer(0, "a"));
        Assert.Contains("container peer", UiSelectionVia.ContainerPeer(0, "a"));
        Assert.StartsWith("Selector.SelectedIndex", UiSelectionVia.SelectedIndex(0, "a", UiItemPeerLookup.NotFound, null, UiItemContainerState.NotRealized));
    }
}
