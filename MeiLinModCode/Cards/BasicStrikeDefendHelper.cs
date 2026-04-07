using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using System.Collections.Generic;
using System.Linq;

namespace MeiLinMod.MeiLinModCode.Cards;

public static class BasicStrikeDefendHelper
{
    private const string StrikeId = "MEILINMOD-STRIKE_MEILIN";
    private const string DefendId = "MEILINMOD-DEFEND_MEILIN";
    private static readonly HashSet<string> StrikeIds =
    [
        "ULTIMATE_STRIKE",
        "ASHEN_STRIKE",
        "PERFECTED_STRIKE",
        "POMMEL_STRIKE",
        "SETUP_STRIKE",
        "TWIN_STRIKE"
    ];
    private static readonly HashSet<string> DefendIds =
    [
        "ULTIMATE_DEFEND"
    ];

    public static bool IsBasicStrikeOrDefend(CardModel? card)
    {
        if (card == null)
            return false;

        if (card is StrikeMeilin || card is DefendMeilin)
            return true;

        var id = card.Id.Entry;
        if (id == StrikeId || id == DefendId)
            return true;

        if (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend))
            return true;

        if (StrikeIds.Contains(id) || DefendIds.Contains(id))
            return true;

        return card.IsBasicStrikeOrDefend;
    }

    public static bool IsBasicStrike(CardModel? card)
    {
        if (card == null)
            return false;

        return card is StrikeMeilin ||
               card.Id.Entry == StrikeId ||
               StrikeIds.Contains(card.Id.Entry) ||
               (IsBasicStrikeOrDefend(card) && card.Tags.Contains(CardTag.Strike));
    }

    public static bool IsBasicDefend(CardModel? card)
    {
        if (card == null)
            return false;

        return card is DefendMeilin ||
               card.Id.Entry == DefendId ||
               DefendIds.Contains(card.Id.Entry) ||
               (IsBasicStrikeOrDefend(card) && card.Tags.Contains(CardTag.Defend));
    }

    public static CardModel? CreateBasicStrikeForPlayer(Player player, CombatState? combatState)
    {
        if (combatState == null)
            return null;

        var canonical = GetCanonicalBasicStrike(player);
        return canonical == null ? null : combatState.CreateCard(canonical, player);
    }

    public static CardModel? CreateBasicDefendForPlayer(Player player, CombatState? combatState)
    {
        if (combatState == null)
            return null;

        var canonical = GetCanonicalBasicDefend(player);
        return canonical == null ? null : combatState.CreateCard(canonical, player);
    }

    private static CardModel? GetCanonicalBasicStrike(Player player)
    {
        return GetCharacterBasicCards(player).FirstOrDefault(c => c.IsBasicStrikeOrDefend && c.Tags.Contains(CardTag.Strike));
    }

    private static CardModel? GetCanonicalBasicDefend(Player player)
    {
        return GetCharacterBasicCards(player).FirstOrDefault(c => c.IsBasicStrikeOrDefend && c.Tags.Contains(CardTag.Defend));
    }

    private static IEnumerable<CardModel> GetCharacterBasicCards(Player player)
    {
        return player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
    }
}
