using System;
using System.IO;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SwiftXP.SPT.ShowMeTheMoney.Server.Data;

namespace SwiftXP.SPT.ShowMeTheMoney.Server.Services;

/// <summary>
/// Makes the player's flea offers sell immediately, so a quick sell pays out without the usual wait.
/// </summary>
/// <remarks>
/// This deliberately works by adjusting SPT's own ragfair sell simulation rather than by moving the
/// items and money directly. A direct transfer would need a custom route, and the client only
/// applies inventory changes that come back from its own standard item-event route - so the server
/// would drop the items while the client still displayed them, and the two would have to be kept in
/// step by hand. Going through the vanilla path means the client, the profile and the sales tax all
/// stay correct on their own.
///
/// The trade-off is that the money arrives in the messenger, the way any completed flea sale does,
/// and that this applies to every offer the player lists - the server sees a quick sell and a
/// hand-listed offer as the same ragfair request.
/// </remarks>
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader - 1)]
public class InstantFleaSellService(ISptLogger<InstantFleaSellService> sptLogger, ConfigServer configServer)
{
    private const string ConfigFileName = "instant-flea-sell.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private bool _applied;

    public void Apply()
    {
        if (_applied)
            return;

        _applied = true;

        InstantFleaSellConfig config = LoadConfig();

        if (!config.Enabled)
        {
            sptLogger.Info($"{Constants.LoggerPrefix}Instant flea selling is disabled in {ConfigFileName}.");
            return;
        }

#pragma warning disable CS0618 // ConfigServer is deprecated in SPT 4.2; there is no replacement yet on 4.0.x.
        RagfairConfig ragfairConfig = configServer.GetConfig<RagfairConfig>();
#pragma warning restore CS0618

        // Always sell, and sell now. Min/Max are the delay window in minutes that SPT rolls a sale
        // time from, so zeroing both makes the offer complete on the next ragfair pass.
        ragfairConfig.Sell.Chance.Base = 100;
        ragfairConfig.Sell.Chance.MinSellChancePercent = 100;
        ragfairConfig.Sell.Chance.MaxSellChancePercent = 100;
        ragfairConfig.Sell.Time.Min = 0;
        ragfairConfig.Sell.Time.Max = 0;

        // Leaving fees on means an instant sale nets exactly what waiting for the offer would have.
        ragfairConfig.Sell.Fees = config.ChargeFees;

        // A sale time of zero still only takes effect on the server's next flea pass, so that
        // interval is what the delay is actually made of.
        if (config.OutOfRaidCheckIntervalSeconds > 0)
        {
            ragfairConfig.RunIntervalSeconds = config.OutOfRaidCheckIntervalSeconds;
            ragfairConfig.RunIntervalValues.OutOfRaid = config.OutOfRaidCheckIntervalSeconds;
        }

        sptLogger.Info(
            $"{Constants.LoggerPrefix}Player flea offers now sell immediately (fees {(config.ChargeFees ? "charged" : "waived")}, "
            + $"checked every {(config.OutOfRaidCheckIntervalSeconds > 0 ? config.OutOfRaidCheckIntervalSeconds + "s" : "SPT default")} out of raid). "
            + $"Money arrives in the messenger, as it does for any completed flea sale.");
    }

    private InstantFleaSellConfig LoadConfig()
    {
        string path = Path.Combine(AppContext.BaseDirectory, ConfigFileName);

        try
        {
            if (!File.Exists(path))
            {
                InstantFleaSellConfig defaults = new();

                File.WriteAllText(path, JsonSerializer.Serialize(defaults, s_jsonOptions));
                sptLogger.Info($"{Constants.LoggerPrefix}Wrote default {ConfigFileName}.");

                return defaults;
            }

            return JsonSerializer.Deserialize<InstantFleaSellConfig>(File.ReadAllText(path)) ?? new InstantFleaSellConfig();
        }
        catch (Exception ex)
        {
            sptLogger.Warning($"{Constants.LoggerPrefix}Could not read {ConfigFileName} ({ex.Message}); using defaults.");

            return new InstantFleaSellConfig();
        }
    }
}
