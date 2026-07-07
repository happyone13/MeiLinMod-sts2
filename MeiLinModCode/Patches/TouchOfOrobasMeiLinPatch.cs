using System.Linq;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinMod.MeiLinModCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class TouchOfOrobasMeiLinPatch : IPatchMethod
{
    public static string PatchId => "meilin_touch_of_orobas_starter_relic";

    public static bool IsCritical => false;

    public static string Description => "Allow Touch of Orobas to upgrade MeiLin starter relic";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<TouchOfOrobas>(nameof(TouchOfOrobas.SetupForPlayer))
    ];

    public static bool Prefix(TouchOfOrobas __instance, Player player, ref bool __result)
    {
        var starterRelic = player.Relics.FirstOrDefault(r =>
            r is XiangzuLegacyRelic || MeiLinTarget.EntryEquals(r.Id.Entry, "MEILINMOD_XIANGZU_LEGACY_RELIC"));

        if (starterRelic == null)
            return true;

        var upgradedRelicId = ModelDb.Relic<XiangzuSpiritRelic>().Id;
        __instance.SetupForTests(starterRelic.Id, upgradedRelicId);
        __result = true;
        return false;
    }
}
