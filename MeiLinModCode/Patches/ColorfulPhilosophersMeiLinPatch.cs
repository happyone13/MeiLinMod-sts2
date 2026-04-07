using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(ColorfulPhilosophers), "OfferRewards")]
public static class ColorfulPhilosophersMeiLinPatch
{
    private static readonly System.Reflection.MethodInfo? SetEventFinishedMethod =
        AccessTools.Method(typeof(EventModel), "SetEventFinished");

    [HarmonyPrefix]
    public static bool OfferRewardsPrefix(ColorfulPhilosophers __instance, CardPoolModel pool, ref Task __result)
    {
        var owner = __instance.Owner;
        if (owner == null)
            return true;

        if (!IsMeiLinAvailable(owner))
            return true;

        __result = OfferRewardsWithMeiLin(__instance, owner, pool);
        return false;
    }

    private static async Task OfferRewardsWithMeiLin(ColorfulPhilosophers eventModel, Player owner, CardPoolModel selectedPool)
    {
        var pools = new List<CardPoolModel> { selectedPool };
        var meiLinPool = ModelDb.CardPool<MeiLinModCardPool>();
        if (pools.All(p => p.Id != meiLinPool.Id))
            pools.Add(meiLinPool);

        CardCreationOptions commonOptions = new CardCreationOptions(
                pools,
                CardCreationSource.Other,
                CardRarityOddsType.Uniform,
                (CardModel c) => c.Rarity == CardRarity.Common)
            .WithFlags(CardCreationFlags.NoRarityModification);
        CardCreationOptions uncommonOptions = new CardCreationOptions(
                pools,
                CardCreationSource.Other,
                CardRarityOddsType.Uniform,
                (CardModel c) => c.Rarity == CardRarity.Uncommon)
            .WithFlags(CardCreationFlags.NoRarityModification);
        CardCreationOptions rareOptions = new CardCreationOptions(
                pools,
                CardCreationSource.Other,
                CardRarityOddsType.Uniform,
                (CardModel c) => c.Rarity == CardRarity.Rare)
            .WithFlags(CardCreationFlags.NoRarityModification);

        await RewardsCmd.OfferCustom(owner, new List<Reward>(3)
        {
            new CardReward(commonOptions, eventModel.DynamicVars.Cards.IntValue, owner),
            new CardReward(uncommonOptions, eventModel.DynamicVars.Cards.IntValue, owner),
            new CardReward(rareOptions, eventModel.DynamicVars.Cards.IntValue, owner)
        });

        SetEventFinishedMethod?.Invoke(eventModel, [new LocString("events", "COLORFUL_PHILOSOPHERS.pages.DONE.description")]);
    }

    private static bool IsMeiLinAvailable(Player player)
    {
        var meiLinId = ModelDb.Character<MeiLinCharacter>().Id;
        if (player.Character.Id == meiLinId)
            return true;

        return player.UnlockState.Characters.Any(c => c.Id == meiLinId);
    }
}
