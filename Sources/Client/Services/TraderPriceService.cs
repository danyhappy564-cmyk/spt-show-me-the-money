using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using EFT.Trading;
using SwiftXP.SPT.Common.ConfigurationManager;
using SwiftXP.SPT.Common.Constants;
using SwiftXP.SPT.Common.Sessions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data;
using SwiftXP.SPT.ShowMeTheMoney.Client.Extensions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Utilities;
using UnityEngine;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Services;

public class TraderPriceService
{
    private static readonly Lazy<TraderPriceService> s_instance = new(() => new TraderPriceService());

    private TraderPriceService() { }

    /// <summary>
    /// Best trader price for an item, as raw values. Held separately from <see cref="TradePrice"/>
    /// because that binds to a specific <see cref="TradeItem"/> instance, and a fresh one of those
    /// is built on every hover - so only the numbers are worth keeping.
    /// </summary>
    private readonly struct TraderQuote
    {
        public TraderQuote(string? traderId, string traderName, int singleObjectPrice, int? totalPrice,
            double? currencyCourse, MongoID? currencyId)
        {
            TraderId = traderId;
            TraderName = traderName;
            SingleObjectPrice = singleObjectPrice;
            TotalPrice = totalPrice;
            CurrencyCourse = currencyCourse;
            CurrencyId = currencyId;
        }

        public string? TraderId { get; }

        public string TraderName { get; }

        public int SingleObjectPrice { get; }

        public int? TotalPrice { get; }

        public double? CurrencyCourse { get; }

        public MongoID? CurrencyId { get; }

        public TradePrice ToTradePrice(TradeItem tradeItem) =>
            new(tradeItem, TraderId, TraderName, SingleObjectPrice, TotalPrice, CurrencyCourse, CurrencyId);
    }

    private readonly Dictionary<string, KeyValuePair<float, TraderQuote?>> _quoteCache = [];

    private const float CacheSeconds = 10f;

    private const int CacheLimit = 512;

    /// <summary>Drops every cached quote. Called when trader data is rebuilt.</summary>
    public void ClearCache() => _quoteCache.Clear();

    public bool GetBestTraderPrice(TradeItem tradeItem)
    {
        // Every trader is asked about the same item, so the quote only has to be worked out once
        // per item and can then be reused. Hovering back and forth between items used to redo the
        // whole thing each time - with a stack of mods that is hundreds of item clones and price
        // lookups per hover, which is what made fast hovering stutter.
        // RoublesOnly decides which traders are eligible at all, so it belongs in the key -
        // otherwise toggling it would keep serving the previous winner until the entry expired.
        bool roublesOnly = PluginContextHolder.Current!.Configuration!.RoublesOnly.IsEnabled();
        string cacheKey = $"{tradeItem.Item.Id}:{tradeItem.Item.StackObjectsCount}:{roublesOnly}";
        float now = Time.realtimeSinceStartup;

        if (_quoteCache.TryGetValue(cacheKey, out KeyValuePair<float, TraderQuote?> cached)
            && now - cached.Key < CacheSeconds)
        {
            tradeItem.TraderPrice = cached.Value?.ToTradePrice(tradeItem);
            return tradeItem.TraderPrice is not null;
        }

        TraderQuote? best = FindBestQuote(tradeItem, roublesOnly);

        if (_quoteCache.Count >= CacheLimit)
            _quoteCache.Clear();

        _quoteCache[cacheKey] = new KeyValuePair<float, TraderQuote?>(now, best);

        tradeItem.TraderPrice = best?.ToTradePrice(tradeItem);

        return tradeItem.TraderPrice is not null;
    }

    private TraderQuote? FindBestQuote(TradeItem tradeItem, bool roublesOnly)
    {
        TraderQuote? best = null;
        double bestComparePrice = 0d;

        // The single-unit item does not depend on the trader, so clone it once instead of once per
        // trader (upstream clones inside the per-trader call). For an unstacked item no clone is
        // needed at all - it already is a single unit - and its total price is by definition the
        // same as its single price, so that second lookup can go too.
        Item item = tradeItem.Item;
        bool isStacked = item.StackObjectsCount > 1;
        Item singleItem = item;

        if (isStacked)
        {
            try
            {
                // 4.1's Item.CloneItem needs an IDatabaseIdGenerator; CloneForPricing supplies a
                // throwaway one, which is what this clone wants - it never reaches the profile.
                singleItem = item.CloneForPricing();
                singleItem.StackObjectsCount = 1;
            }
            catch (Exception)
            {
                PluginContextHolder.Current.SptLogger?
                    .LogDebug("Could not clone the hovered item for a single-unit price. Skipping trader prices.");

                return null;
            }
        }

        foreach (Trader trader in SptSession.Session.Traders)
        {
            if (!IsTraderAvailable(trader))
                continue;

            if (!TryGetTraderUserItemPrice(trader, item, singleItem, isStacked,
                    out Trader.ItemPrice? singleObjectPrice, out Trader.ItemPrice? totalPrice))
            {
                continue;
            }

            if (roublesOnly
                && singleObjectPrice!.Value.CurrencyId.ToString() != SptConstants.CurrencyIds.Roubles)
            {
                continue;
            }

            MongoID? currencyId = singleObjectPrice!.Value.CurrencyId;

            TraderQuote quote = new(
                trader.Id,
                trader.LocalizedName,
                singleObjectPrice.Value.Amount,
                totalPrice?.Amount,
                GetCurrencyCourse(trader, currencyId),
                currencyId
            );

            double comparePrice = quote.ToTradePrice(tradeItem).GetComparePriceInRouble();
            if (best is null || comparePrice > bestComparePrice)
            {
                best = quote;
                bestComparePrice = comparePrice;
            }
        }

        return best;
    }

    private bool IsTraderAvailable(Trader trader)
    {
        bool isAvailable = trader.Info.Available && !trader.Info.Disabled && trader.Info.Unlocked;
        bool isIgnored = TradersToIgnore.Any(
            x => x.Equals(trader.Id, StringComparison.OrdinalIgnoreCase)
            || x.Equals(trader.LocalizedName, StringComparison.OrdinalIgnoreCase));

        return isAvailable && !isIgnored;
    }

    private static bool TryGetTraderUserItemPrice(Trader trader, Item item, Item singleItem, bool isStacked,
        out Trader.ItemPrice? singleObjectPrice, out Trader.ItemPrice? totalPrice)
    {
        singleObjectPrice = null;
        totalPrice = null;

        try
        {
            singleObjectPrice = trader.GetUserItemPrice(singleItem);

            // An unstacked item's total is its single price, so skip the duplicate lookup.
            totalPrice = isStacked ? trader.GetUserItemPrice(item) : singleObjectPrice;
        }
        catch (Exception)
        {
            PluginContextHolder.Current.SptLogger?
                .LogDebug($"Could not get price from trader \"{trader.LocalizedName}\". Skipping.");
        }

        return singleObjectPrice is not null;
    }

    private static double? GetCurrencyCourse(Trader trader, MongoID? currencyId)
    {
        if (!currencyId.HasValue)
            return null;

        // 4.1 exposes the courses directly on the trader; GetSupplyData stays as the fallback.
        if (trader.CurrencyCourses != null && trader.CurrencyCourses.TryGetValue(currencyId.Value.ToString(), out double course))
            return course;

        double? result = trader.GetSupplyData()?.CurrencyCourses[currencyId.Value];

        return result ?? 1;
    }

    public static TraderPriceService Instance => s_instance.Value;

    /// <summary>
    /// Traders excluded from price comparison. Assigning a new list clears the cache, since which
    /// traders are eligible decides which quote wins.
    /// </summary>
    public List<string> TradersToIgnore
    {
        get => _tradersToIgnore;
        set
        {
            _tradersToIgnore = value;
            ClearCache();
        }
    }

    private List<string> _tradersToIgnore = [];
}
