using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

public static class BasicStrikeDefendHelper
{
    private const string StrikeId = "MEILINMOD-STRIKE_MEILIN";
    private const string DefendId = "MEILINMOD-DEFEND_MEILIN";

    public static bool IsBasicStrikeOrDefend(CardModel? card)
    {
        if (card == null)
            return false;

        if (card is StrikeMeilin || card is DefendMeilin)
            return true;

        var id = card.Id.Entry;
        if (id == StrikeId || id == DefendId)
            return true;

        return card.IsBasicStrikeOrDefend;
    }

    public static bool IsBasicStrike(CardModel? card)
    {
        if (card == null)
            return false;

        return card is StrikeMeilin ||
               card.Id.Entry == StrikeId ||
               (IsBasicStrikeOrDefend(card) && card.Tags.Contains(CardTag.Strike));
    }

    public static bool IsBasicDefend(CardModel? card)
    {
        if (card == null)
            return false;

        return card is DefendMeilin ||
               card.Id.Entry == DefendId ||
               (IsBasicStrikeOrDefend(card) && card.Tags.Contains(CardTag.Defend));
    }
}
