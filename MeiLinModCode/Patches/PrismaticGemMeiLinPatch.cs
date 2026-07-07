using System.Collections.Generic;
using System.Linq;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class PrismaticGemMeiLinPatch : IPatchMethod
{
    public static string PatchId => "meilin_prismatic_gem_card_pool";

    public static bool IsCritical => false;

    public static string Description => "Allow Prismatic Gem to add MeiLin cards when MeiLin is available";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<PrismaticGem>(nameof(PrismaticGem.ModifyCardRewardCreationOptions))
    ];

    public static void Postfix(
        PrismaticGem __instance,
        Player player,
        CardCreationOptions options,
        ref CardCreationOptions __result)
    {
        if (__instance.Owner != player)
            return;

        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
            return;

        if (options.CardPools.All(p => p.IsColorless))
            return;

        if (!IsMeiLinAvailable(player))
            return;

        var meiLinPool = ModelDb.CardPool<MeiLinModCardPool>();
        if (__result.CardPools.Any(p => p.Id == meiLinPool.Id))
            return;

        var mergedPools = __result.CardPools.Union(new List<CardPoolModel> { meiLinPool });
        __result = __result.WithCardPools(mergedPools);
    }

    private static bool IsMeiLinAvailable(Player player)
    {
        var meiLinId = ModelDb.Character<MeiLinCharacter>().Id;
        if (player.Character.Id == meiLinId)
            return true;

        return player.UnlockState.Characters.Any(c => c.Id == meiLinId);
    }
}
