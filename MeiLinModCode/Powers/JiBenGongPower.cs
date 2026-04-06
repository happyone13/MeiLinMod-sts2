using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class JiBenGongPower : MeiLinModPower
{
    private int _strikeBonus;
    private int _defendBonus;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => _strikeBonus;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var bonus = (int)Amount;
        _strikeBonus += bonus;
        _defendBonus += bonus;
        ApplyBonusesToAllCards();
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

        foreach (var card in player.PlayerCombatState.AllCards)
            ApplyBonusesToCard(card);
    }

    private void ApplyBonusesToCard(CardModel card)
    {
        if (card.Owner?.Creature != Owner)
            return;

        if (BasicStrikeDefendHelper.IsBasicStrike(card))
            card.DynamicVars.Damage.BaseValue += _strikeBonus;
        else if (BasicStrikeDefendHelper.IsBasicDefend(card))
            card.DynamicVars.Block.BaseValue += _defendBonus;
    }
}

