namespace SwiftXP.SPT.ShowMeTheMoney.Server.Data;

/// <summary>
/// Settings for <see cref="Services.InstantFleaSellService"/>, read from
/// <c>instant-flea-sell.json</c> next to the server mod.
/// </summary>
public class InstantFleaSellConfig
{
    /// <summary>
    /// Whether player flea offers should complete straight away instead of over the usual random
    /// delay. Applies to every offer the player lists, not only quick sells - the server cannot tell
    /// the two apart, because a quick sell goes through the same ragfair route as listing by hand.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether the flea sales tax is still charged. Left on so an instant sale pays out the same
    /// amount as waiting for the offer would have.
    /// </summary>
    public bool ChargeFees { get; set; } = true;

    /// <summary>
    /// How often, in seconds, the server checks the flea while out of raid. This is what decides how
    /// long "instant" actually takes: the offer is set to sell straight away, but it is only settled
    /// on the next check, so SPT's default leaves a noticeable pause. Set to 0 to leave SPT's own
    /// value alone.
    /// </summary>
    /// <remarks>
    /// Only the out-of-raid interval is touched. The same pass also refreshes the flea market
    /// itself, which is not cheap, so running it more often during a raid would cost performance
    /// exactly when it matters most - and a quick sell happens in the menus anyway.
    /// </remarks>
    public int OutOfRaidCheckIntervalSeconds { get; set; } = 10;
}
