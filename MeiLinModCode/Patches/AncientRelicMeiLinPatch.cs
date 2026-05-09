using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch]
public static class AncientRelicMeiLinPatch
{
    [HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained))]
    [HarmonyPrefix]
    public static bool ArchaicToothAfterObtainedPrefix(ArchaicTooth __instance, ref Task __result)
    {
        var owner = __instance.Owner;
        if (owner == null)
        {
            return true;
        }

        var starterInDeck = owner.Deck.Cards.FirstOrDefault(c =>
            c is AttackDefenseUnity || c.Id.Entry == "MEILINMOD-ATTACK_DEFENSE_UNITY");
        if (starterInDeck == null)
        {
            return true;
        }

        MainFile.Logger.Info("[AncientRelicMeiLinPatch] ArchaicTooth.AfterObtained: applying MeiLin custom transform.");
        __result = HandleArchaicToothTransform(owner, starterInDeck);
        return false;
    }

    [HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
    [HarmonyPrefix]
    public static bool DustyTomeSetupForPlayerPrefix(DustyTome __instance, Player player)
    {
        var characterEntry = player.Character?.Id.Entry ?? "null";
        var hasStarterInDeck = player.Deck.Cards.Any(c =>
            c is AttackDefenseUnity || c.Id.Entry == "MEILINMOD-ATTACK_DEFENSE_UNITY");
        var isMeiLin = IsMeiLinPlayer(player);
        var poolType = player.Character?.CardPool?.GetType().FullName ?? "null";
        MainFile.Logger.Info(
            $"[AncientRelicMeiLinPatch] DustyTome.SetupForPlayer enter: character={characterEntry}, hasStarter={hasStarterInDeck}, isMeiLin={isMeiLin}, poolType={poolType}");

        if (!isMeiLin)
        {
            return true;
        }

        var cardPool = player.Character?.CardPool;
        if (cardPool == null)
        {
            MainFile.Logger.Info("[AncientRelicMeiLinPatch] DustyTome.SetupForPlayer: MeiLin player has no card pool.");
            return true;
        }

        var poolCards = GetCardPoolCards(player).ToList();
        var unlockedCards = cardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .ToList();

        var ancientFromPool = poolCards
            .Where(c => c.Rarity == CardRarity.Ancient)
            .Select(c => c.Id.Entry)
            .ToList();
        var ancientFromUnlocked = unlockedCards
            .Where(c => c.Rarity == CardRarity.Ancient)
            .Select(c => c.Id.Entry)
            .ToList();

        MainFile.Logger.Info(
            $"[AncientRelicMeiLinPatch] DustyTome.SetupForPlayer pool stats: allCards={poolCards.Count}, unlocked={unlockedCards.Count}, ancientInPool={ancientFromPool.Count}, ancientInUnlocked={ancientFromUnlocked.Count}");
        MainFile.Logger.Info(
            $"[AncientRelicMeiLinPatch] DustyTome.SetupForPlayer ancientInPool=[{string.Join(",", ancientFromPool)}], ancientInUnlocked=[{string.Join(",", ancientFromUnlocked)}]");

        var candidates = poolCards
            .Where(IsDustyTomeCandidate)
            .ToList();

        if (candidates.Count == 0)
        {
            MainFile.Logger.Info("[AncientRelicMeiLinPatch] DustyTome.SetupForPlayer: no ancient card candidates found in MeiLin card pool.");
            return true;
        }

        var selected = player.PlayerRng.Rewards.NextItem(candidates);
        if (selected == null)
        {
            MainFile.Logger.Info("[AncientRelicMeiLinPatch] DustyTome.SetupForPlayer: reward RNG returned null candidate.");
            return true;
        }

        __instance.AncientCard = selected.Id;
        MainFile.Logger.Info(
            $"[AncientRelicMeiLinPatch] DustyTome.SetupForPlayer: selected={selected.Id.Entry}, candidates={string.Join(",", candidates.Select(c => c.Id.Entry))}");
        return false;
    }

    private static async Task HandleArchaicToothTransform(Player owner, CardModel starterInDeck)
    {
        var transformedCard = owner.RunState.CreateCard<ShenGongFangYiTi>(owner);
        if (starterInDeck.IsUpgraded)
            CardCmd.Upgrade(transformedCard);

        await CardCmd.Transform(starterInDeck, transformedCard);
        MainFile.Logger.Info("[AncientRelicMeiLinPatch] ArchaicTooth.AfterObtained: transform complete.");
    }

    private static bool IsMeiLinPlayer(Player? player)
    {
        if (player == null)
        {
            return false;
        }

        if (player.Character?.Id.Entry == "MEILINMOD-MEI_LIN_MOD")
        {
            return true;
        }

        return player.Deck.Cards.Any(c =>
            c is AttackDefenseUnity || c.Id.Entry == "MEILINMOD-ATTACK_DEFENSE_UNITY");
    }

    private static IEnumerable<CardModel> GetCardPoolCards(Player player)
    {
        var allCardsProp = AccessTools.Property(player.Character.CardPool.GetType(), "AllCards");
        if (allCardsProp?.GetValue(player.Character.CardPool) is IEnumerable<CardModel> allCards)
        {
            return allCards;
        }

        return player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
    }

    private static bool IsDustyTomeCandidate(CardModel card)
    {
        if (card.Rarity != CardRarity.Ancient)
        {
            return false;
        }

        if (ArchaicTooth.TranscendenceCards.Contains(card))
        {
            return false;
        }

        // Custom ArchaicTooth transform target for MeiLin should not be offered by Dusty Tome.
        if (card is ShenGongFangYiTi || card.Id.Entry == "MEILINMOD-SHEN_GONG_FANG_YI_TI")
        {
            return false;
        }

        return true;
    }
}
