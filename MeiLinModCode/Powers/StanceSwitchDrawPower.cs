using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class StanceSwitchDrawPower : MeiLinModPower
{
    private int _seenSwitchCount;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _seenSwitchCount = Owner.GetPower<XiangzuLegacyPower>()?.StanceSwitchCount ?? 0;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || Owner.Player == null)
            return;

        var legacy = Owner.GetPower<XiangzuLegacyPower>();
        if (legacy == null)
            return;

        var delta = legacy.StanceSwitchCount - _seenSwitchCount;
        if (delta <= 0)
            return;

        _seenSwitchCount = legacy.StanceSwitchCount;
        await CardPileCmd.Draw(context, delta * Amount, Owner.Player);
    }
}
