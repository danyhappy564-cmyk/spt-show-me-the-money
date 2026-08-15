using System.Reflection;
using EFT.Trading;
using HarmonyLib;
using SPT.Reflection.Patching;
using SwiftXP.SPT.ShowMeTheMoney.Client.Extensions;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Patches;

public class TraderClassPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstConstructor(typeof(Trader), x => true);

    [PatchPostfix]
#pragma warning disable CA1707 // Identifiers should not contain underscores

    public static void PatchPostfix(Trader __instance)
#pragma warning restore CA1707 // Identifiers should not contain underscores

    {
        __instance.UpdateSupplyData();
    }
}
