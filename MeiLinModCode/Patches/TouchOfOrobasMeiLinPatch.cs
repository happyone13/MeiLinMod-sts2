using System.Linq;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.SetupForPlayer))]
public static class TouchOfOrobasMeiLinPatch
{
    [HarmonyPrefix]
    public static bool SetupForPlayerPrefix(TouchOfOrobas __instance, Player player, ref bool __result)
    {
        var starterRelic = player.Relics.FirstOrDefault(r =>
            r is XiangzuLegacyRelic || r.Id.Entry == "MEILINMOD-XIANGZU_LEGACY_RELIC");

        if (starterRelic == null)
            return true;

        var upgradedRelicId = ModelDb.Relic<XiangzuSpiritRelic>().Id;
        __instance.SetupForTests(starterRelic.Id, upgradedRelicId);
        __result = true;
        return false;
    }
}
