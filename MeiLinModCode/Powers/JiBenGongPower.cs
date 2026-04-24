using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class JiBenGongPower : MeiLinModPower
{
    private decimal _appliedBonus;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => (int)_appliedBonus;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncBonusesToCurrentAmount();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        ApplyBonusesToCard(card);
        return Task.CompletedTask;
    }

    private void ApplyBonusesToAllCards()
    {
        var player = Owner.Player;
        if (player?.PlayerCombatState == null)
            return;

        var bonusDelta = Amount - _appliedBonus;
        foreach (var card in player.PlayerCombatState.AllCards)
            ApplyBonusToCard(card, bonusDelta, Owner);

        _appliedBonus = Amount;
    }

    private void ApplyBonusesToCard(CardModel card)
    {
        if (card.Owner?.Creature != Owner)
            return;

        if (BasicStrikeDefendHelper.IsBasicStrike(card))
            card.DynamicVars.Damage.BaseValue += _appliedBonus;
        else if (BasicStrikeDefendHelper.IsBasicDefend(card))
            card.DynamicVars.Block.BaseValue += _appliedBonus;
    }

    private void SyncBonusesToCurrentAmount()
    {
        if (_appliedBonus == Amount)
            return;

        ApplyBonusesToAllCards();
    }

    private static void ApplyBonusToCard(CardModel card, decimal bonus, Creature owner)
    {
        if (bonus == 0m || card.Owner?.Creature != owner)
            return;

        if (BasicStrikeDefendHelper.IsBasicStrike(card))
            card.DynamicVars.Damage.BaseValue += bonus;
        else if (BasicStrikeDefendHelper.IsBasicDefend(card))
            card.DynamicVars.Block.BaseValue += bonus;
    }
}
