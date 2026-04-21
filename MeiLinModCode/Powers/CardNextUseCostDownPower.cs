using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class CardNextUseCostDownPower : MeiLinModPower
{
    private CardModel? _targetCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _targetCard = cardSource;
        return Task.CompletedTask;
    }

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
        if (_targetCard == null || cardPlay.Card != _targetCard)
            return;

        await PowerCmd.Remove(this);
    }

    private bool ShouldAffectCard(CardModel card, decimal originalCost)
    {
        if (_targetCard == null || card != _targetCard)
            return false;

        if (card.Owner?.Creature != Owner || originalCost <= 0m)
            return false;

        return card.Pile?.Type is PileType.Hand or PileType.Play;
    }
}
