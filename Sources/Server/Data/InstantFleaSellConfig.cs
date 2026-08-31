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
}
