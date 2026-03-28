using System.Linq;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer))]
public static class ArchaicToothMeiLinPatch
{
    [HarmonyPrefix]
    public static bool SetupForPlayerPrefix(ArchaicTooth __instance, Player player, ref bool __result)
    {
        var starter = player.Deck.Cards.FirstOrDefault(c =>
            c is AttackDefenseUnity || c.Id.Entry == "MEILINMOD-ATTACK_DEFENSE_UNITY");

        if (starter == null)
            return true;

        var ancient = player.RunState.CreateCard<ShenGongFangYiTi>(player);
        if (starter.IsUpgraded)
            CardCmd.Upgrade(ancient);

        __instance.SetupForTests(starter.ToSerializable(), ancient.ToSerializable());
        __result = true;
        return false;
    }
}
