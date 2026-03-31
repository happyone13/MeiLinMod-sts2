using System;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class XinLiuPower : MeiLinModPower
{
    private int _progress;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => GetTriggerCount() - _progress;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _progress = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(cardPlay.Card))
            return;

        var triggerCount = GetTriggerCount();
        _progress++;
        if (_progress < triggerCount)
        {
            InvokeDisplayAmountChanged();
            return;
        }

        _progress -= triggerCount;
        InvokeDisplayAmountChanged();
        if (Owner.Player != null)
        {
            await PlayerCmd.GainEnergy(1, Owner.Player);
        }
    }

    private int GetTriggerCount()
    {
        var count = (int)Amount;
        return Math.Max(1, count);
    }
}
