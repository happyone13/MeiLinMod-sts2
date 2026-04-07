using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(PrismaticGem), nameof(PrismaticGem.ModifyCardRewardCreationOptions))]
public static class PrismaticGemMeiLinPatch
{
    [HarmonyPostfix]
    public static void ModifyCardRewardCreationOptionsPostfix(
        PrismaticGem __instance,
        Player player,
        CardCreationOptions options,
        ref CardCreationOptions __result)
    {
        if (__instance.Owner != player)
            return;

        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
            return;

        if (options.CustomCardPool != null)
            return;

        if (options.CardPools.All(p => p.IsColorless))
            return;

        if (!IsMeiLinAvailable(player))
            return;

        var meiLinPool = ModelDb.CardPool<MeiLinModCardPool>();
        if (__result.CardPools.Any(p => p.Id == meiLinPool.Id))
            return;

        var mergedPools = __result.CardPools.Union(new List<CardPoolModel> { meiLinPool });
        __result = __result.WithCardPools(mergedPools, __result.CardPoolFilter);
    }

    private static bool IsMeiLinAvailable(Player player)
    {
        var meiLinId = ModelDb.Character<MeiLinCharacter>().Id;
        if (player.Character.Id == meiLinId)
            return true;

        return player.UnlockState.Characters.Any(c => c.Id == meiLinId);
    }
}
