using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MeiLinMod.MeiLinModCode.Cards;

public static class RandomStrikeHelper
{
    public static CardModel? CreateRandomNonBasicStrike(Player player, CombatState? combatState, bool upgraded, CardModel? original = null)
    {
        return CreateRandomNonBasicStrike(player, combatState, upgraded, false, original);
    }

    public static CardModel? CreateRandomNonBasicStrike(Player player, CombatState? combatState, bool upgraded, bool forceOneCost, CardModel? original = null)
    {
        if (combatState == null)
            return null;

        var canonical = PickRandomCanonicalNonBasicStrike(player, player.RunState.Rng.CombatCardGeneration, original);
        if (canonical == null)
            return null;

        var created = combatState.CreateCard(canonical, player);
        if (created == null)
            return null;

        if (upgraded)
            CardCmd.Upgrade(created, CardPreviewStyle.None);
        if (forceOneCost)
            created.EnergyCost.SetThisCombat(1);
        return created;
    }

    public static async Task TransformAllStrikes(Player player, bool upgraded)
    {
        await TransformAllStrikes(player, upgraded, false);
    }

    public static async Task TransformAllStrikes(Player player, bool upgraded, bool forceOneCost)
    {
        if (player.PlayerCombatState == null)
            return;

        var strikeCards = player.PlayerCombatState.AllCards
            .Where(card => card.Owner == player && BasicStrikeDefendHelper.IsStrikeCard(card))
            .ToList();
        if (strikeCards.Count == 0)
            return;

        var transformations = new List<CardTransformation>();
        foreach (var strikeCard in strikeCards)
        {
            var replacement = CreateRandomNonBasicStrike(
                player,
                strikeCard.CombatState ?? player.Creature?.CombatState,
                upgraded,
                forceOneCost,
                strikeCard);
            if (replacement == null)
                continue;

            transformations.Add(new CardTransformation(strikeCard, replacement));
        }

        if (transformations.Count == 0)
            return;

        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);
    }

    private static CardModel? PickRandomCanonicalNonBasicStrike(Player player, dynamic rng, CardModel? original)
    {
        var candidates = GetCanonicalNonBasicStrikes(player).ToList();
        if (candidates.Count == 0)
            return null;

        if (original != null)
        {
            var filtered = candidates.Where(c => c.Id != original.Id).ToList();
            if (filtered.Count > 0)
                candidates = filtered;
        }

        return rng.NextItem(candidates);
    }

    private static IEnumerable<CardModel> GetCanonicalNonBasicStrikes(Player player)
    {
        return player.UnlockState.CardPools
            .Distinct()
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(card => IsNonBasicStrike(player, card))
            .DistinctBy(card => card.Id);
    }

    private static bool IsNonBasicStrike(Player player, CardModel? card)
    {
        return card != null &&
               BasicStrikeDefendHelper.IsStrikeCard(card) &&
               !BasicStrikeDefendHelper.IsStarterStrike(card, player) &&
               card.Type is not CardType.Status and not CardType.Curse and not CardType.Quest;
    }
}
