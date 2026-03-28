using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class LiuShuiXingYunPower : MeiLinModPower
{
    private int _triggerCount = 4;
    private int _progress;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override int DisplayAmount => _triggerCount - _progress;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _triggerCount = Math.Max(1, (int)Amount);
        _progress = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(cardPlay.Card))
            return;

        _progress++;
        if (_progress < _triggerCount)
        {
            InvokeDisplayAmountChanged();
            return;
        }

        _progress -= _triggerCount;
        InvokeDisplayAmountChanged();
        if (Owner.Player != null)
        {
            await CardPileCmd.Draw(context, 1, Owner.Player);
        }
    }
}
