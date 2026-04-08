using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class NextPowerCardCostDownPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (!ShouldAffectCard(card, originalCost))
        {
            modifiedCost = originalCost;
            return false;
        }

        modifiedCost = decimal.Max(0m, originalCost - Amount);
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        var originalCostWithoutGlobalHooks = cardPlay.Card.EnergyCost.GetWithModifiers(CostModifiers.Local);
        if (!ShouldAffectCard(cardPlay.Card, originalCostWithoutGlobalHooks))
            return;

        await PowerCmd.Remove(this);
    }

    private bool ShouldAffectCard(CardModel card, decimal originalCost)
    {
        if (card.Owner?.Creature != Owner || card.Type != CardType.Power)
            return false;

        if (originalCost <= 0m)
            return false;

        return card.Pile?.Type is PileType.Hand or PileType.Play;
    }
}
