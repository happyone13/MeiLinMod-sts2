using HarmonyLib;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
public static class HuoYongYuXiaCiAfterCombatPatch
{
    [HarmonyPostfix]
    public static void AfterCombatEndPostfix(Player __instance)
    {
        var pending = HuoYongYuXiaCiUpgradePower.ConsumePendingUpgrades(__instance);
        if (pending <= 0)
            return;

        var success = 0;
        for (var i = 0; i < pending; i++)
        {
            if (HuoYongYuXiaCiUpgradePower.TryUpgradeRandomCard(__instance))
                success++;
        }

        MainFile.Logger.Info($"[HuoYongYuXiaCi] post-combat resolution: pending={pending}, upgraded={success}.");
    }
}
