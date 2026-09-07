using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Trading;
using SwiftXP.SPT.Common.Sessions;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Extensions;

public static class TraderClassExtensions
{
    private static readonly FieldInfo s_supplyDataField =
        typeof(Trader).GetField("_supplyData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    public static SupplyData? GetSupplyData(this Trader trader) =>
        s_supplyDataField?.GetValue(trader) as SupplyData;

    public static async void UpdateSupplyData(this Trader trader)
    {
        try
        {
            if (s_supplyDataField?.GetValue(trader) is null)
            {
                Result<SupplyData> result = await SptSession.Session.GetSupplyData(trader.Id);
                if (result.Failed)
                {
                    PluginContextHolder.Current.SptLogger?
                        .LogError("Unable to update supply data for trader(s)! Plug-in will not work properly without that data");

                    return;
                }

                s_supplyDataField?.SetValue(trader, result.Value);
            }
        }
        catch (Exception exception)
        {
            PluginContextHolder.Current.SptLogger?
                .LogException(exception);
        }
    }
}
