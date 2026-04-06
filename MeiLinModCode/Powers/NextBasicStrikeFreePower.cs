using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class NextBasicStrikeFreePower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrike(card))
        {
            modifiedCost = originalCost;
            return false;
        }

        modifiedCost = 0m;
        return true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrike(cardPlay.Card))
            return;

        if (Amount <= 1m)
        {
            await PowerCmd.Remove(this);
            return;
        }

        await PowerCmd.Apply<NextBasicStrikeFreePower>(Owner, -1m, Owner, cardPlay.Card, silent: true);
    }
}
