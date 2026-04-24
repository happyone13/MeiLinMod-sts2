using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models.Exceptions;
using System.Linq;

namespace MeiLinMod.MeiLinModCode.Cards;

public static class BasicStrikeDefendHelper
{
    private const string StrikeId = "MEILINMOD-STRIKE_MEILIN";
    private const string DefendId = "MEILINMOD-DEFEND_MEILIN";

    public static bool IsStrikeCard(CardModel? card)
    {
        return card != null && card.Tags.Contains(CardTag.Strike);
    }

    public static bool IsDefendCard(CardModel? card)
    {
        return card != null && card.Tags.Contains(CardTag.Defend);
    }

    public static bool IsBasicStrikeOrDefend(CardModel? card)
    {
        if (card == null)
            return false;

        return IsBasicStrike(card) || IsBasicDefend(card);
    }

    public static bool IsBasicStrike(CardModel? card)
    {
        return card != null &&
               ((card.IsBasicStrikeOrDefend && card.Tags.Contains(CardTag.Strike)) ||
                IsStarterStrike(card));
    }

    public static bool IsBasicDefend(CardModel? card)
    {
        return card != null &&
               ((card.IsBasicStrikeOrDefend && card.Tags.Contains(CardTag.Defend)) ||
                IsStarterDefend(card));
    }

    public static bool IsStarterStrike(CardModel? card)
    {
        if (card == null)
            return false;

        return IsStarterStrike(card, TryGetOwner(card));
    }

    public static bool IsStarterStrike(CardModel? card, Player? player)
    {
        if (card == null)
            return false;

        return card is StrikeMeilin ||
               card.Id.Entry == StrikeId ||
               (card.IsBasicStrikeOrDefend && card.Tags.Contains(CardTag.Strike)) ||
               IsCharacterStarterCard(card, player, CardTag.Strike);
    }

    public static bool IsStarterDefend(CardModel? card)
    {
        if (card == null)
            return false;

        return IsStarterDefend(card, TryGetOwner(card));
    }

    public static bool IsStarterDefend(CardModel? card, Player? player)
    {
        if (card == null)
            return false;

        return card is DefendMeilin ||
               card.Id.Entry == DefendId ||
               (card.IsBasicStrikeOrDefend && card.Tags.Contains(CardTag.Defend)) ||
               IsCharacterStarterCard(card, player, CardTag.Defend);
    }

    public static CardModel? CreateBasicStrikeForPlayer(Player player, ICombatState? combatState)
    {
        if (combatState == null)
            return null;

        var canonical = GetCanonicalBasicStrike(player);
        return canonical == null ? null : combatState.CreateCard(canonical, player);
    }

    public static CardModel? CreateBasicDefendForPlayer(Player player, ICombatState? combatState)
    {
        if (combatState == null)
            return null;

        var canonical = GetCanonicalBasicDefend(player);
        return canonical == null ? null : combatState.CreateCard(canonical, player);
    }

    private static CardModel? GetCanonicalBasicStrike(Player player)
    {
        return GetCharacterStarterCard(player, CardTag.Strike) ??
               GetCharacterBasicCards(player).FirstOrDefault(c => c.IsBasicStrikeOrDefend && c.Tags.Contains(CardTag.Strike));
    }

    private static CardModel? GetCanonicalBasicDefend(Player player)
    {
        return GetCharacterStarterCard(player, CardTag.Defend) ??
               GetCharacterBasicCards(player).FirstOrDefault(c => c.IsBasicStrikeOrDefend && c.Tags.Contains(CardTag.Defend));
    }

    private static IEnumerable<CardModel> GetCharacterBasicCards(Player player)
    {
        return player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
    }

    private static bool IsCharacterStarterCard(CardModel card, Player? player, CardTag tag)
    {
        return player?.Character.StartingDeck.Any(startingCard =>
                   startingCard.Id == card.Id &&
                   startingCard.Tags.Contains(tag)) == true;
    }

    private static CardModel? GetCharacterStarterCard(Player player, CardTag tag)
    {
        return player.Character.StartingDeck.FirstOrDefault(card => card.Tags.Contains(tag));
    }

    private static Player? TryGetOwner(CardModel card)
    {
        try
        {
            return card.Owner;
        }
        catch (CanonicalModelException)
        {
            return null;
        }
    }
}
