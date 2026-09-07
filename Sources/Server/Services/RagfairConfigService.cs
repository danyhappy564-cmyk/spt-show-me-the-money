using System.Collections.Generic;
using System.Linq;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SwiftXP.SPT.ShowMeTheMoney.Server.Data;

namespace SwiftXP.SPT.ShowMeTheMoney.Server.Services;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.Preload - 1)]
public class RagfairConfigService(RagfairConfig ragfairConfig)
{
    private PartialRagfairConfig? _cachedConfig;

    public PartialRagfairConfig Get()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        Dictionary<string, double> itemPriceMultipliers = ragfairConfig.Dynamic.ItemPriceMultiplier?
            .ToDictionary(x => x.Key.ToString(), x => x.Value) ?? [];

        _cachedConfig = new()
        {
            ItemPriceMultiplier = itemPriceMultipliers,
            Base = ragfairConfig.Sell.Chance.Base,
            MaxSellChancePercent = ragfairConfig.Sell.Chance.MaxSellChancePercent,
            SellMultiplier = ragfairConfig.Sell.Chance.SellMultiplier
        };

        return _cachedConfig;
    }
}
