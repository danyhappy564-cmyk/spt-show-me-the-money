using EFT.InventoryLogic;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Data;

/// <summary>
/// Compatibility shim for plugins built against the released 2.7.0 assembly.
/// </summary>
/// <remarks>
/// The plugin context holder lives at
/// <see cref="Contexts.Holders.PluginContextHolder"/> in this source tree, but the released 2.7.0
/// assembly exposed it as <c>Client.Data.PluginContextDataHolder</c>. Quick Sell 2.3.0, which is
/// published as "recompiled for compatibility with Show Me The Money 2.7.0", is linked against that
/// old name and calls <c>PluginContextDataHolder.SetHoveredItem(null)</c> after a quick sell.
///
/// Building this repository and dropping the result in therefore breaks Quick Sell: resolving its
/// patch method fails on the missing type, the exception escapes before GridItemView.OnClick can
/// run, and every click in the inventory silently does nothing - no item inspect, no opening
/// containers. Keeping the old name available as a forwarder fixes that without needing to rebuild
/// Quick Sell.
///
/// Only <see cref="SetHoveredItem"/> is forwarded, because that is the only member Quick Sell uses.
/// </remarks>
public static class PluginContextDataHolder
{
    public static void SetHoveredItem(Item? hoveredItem) =>
        PluginContextHolder.SetHoveredItem(hoveredItem);
}
