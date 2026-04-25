using System;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class LiuShuiXingYunUpgradedPower : MeiLinModPower
{
    private const int TriggerCount = 3;
    private int _progress;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;
    public override int DisplayAmount => TriggerCount - _progress;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _progress = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsStrikeOrDefendCard(cardPlay.Card))
            return;

        _progress++;
        if (_progress < TriggerCount)
        {
            InvokeDisplayAmountChanged();
            return;
        }

        _progress -= TriggerCount;
        InvokeDisplayAmountChanged();
        if (Owner.Player != null)
        {
            var drawCount = Math.Max(1, (int)Amount);
            await CardPileCmd.Draw(context, drawCount, Owner.Player);
        }
    }
}
